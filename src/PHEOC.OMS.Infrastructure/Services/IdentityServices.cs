using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Infrastructure.Services;

public class UserAdministration(OmsDbContext db, UserManager<AppUser> manager, ICurrentUser actor) : IUserAdministration
{
    private void Demand() { if(!actor.Can(Permissions.AdminUsers)||actor.GovernorateId.HasValue)throw new AccessDeniedException(); }
    public async Task<IReadOnlyList<UserRow>> Users()
    {
        Demand();var result=new List<UserRow>();foreach(var u in await db.Users.OrderBy(x=>x.UserName).Take(200).ToListAsync())
            result.Add(new(u.Id,u.UserName??"",u.GovernorateId,string.Join(", ",await manager.GetRolesAsync(u)),string.Join(", ",await EffectivePermissions(u)),u.LockoutEnd>DateTimeOffset.UtcNow));
        return result;
    }
    private async Task<string[]> EffectivePermissions(AppUser user)
    {
        var claims=await manager.GetClaimsAsync(user);var result=claims.Where(c=>c.Type=="permission").Select(c=>c.Value).ToList();
        if(!claims.Any(c=>c.Type==RolePermissionDefaults.ExplicitSelectionClaim&&c.Value==RolePermissionDefaults.ExplicitSelectionValue))
        {
            var roles=db.UserRoles.Where(r=>r.UserId==user.Id).Select(r=>r.RoleId);
            result.AddRange(await db.RoleClaims.Where(c=>roles.Contains(c.RoleId)&&c.ClaimType=="permission"&&c.ClaimValue!=null).Select(c=>c.ClaimValue!).ToListAsync());
        }
        return result.Where(Permissions.All.Contains).Distinct().Order().ToArray();
    }
    public async Task<UserPermissionEdit> EditPermissions(string id)
    {
        Demand();var user=await manager.FindByIdAsync(id)??throw new KeyNotFoundException();
        return new(user.Id,user.UserName??"",user.GovernorateId,(await manager.GetRolesAsync(user)).ToArray(),await EffectivePermissions(user),user.ConcurrencyStamp??"");
    }
    public async Task UpdatePermissions(string id,UserPermissionsInput input)
    {
        Demand();System.ComponentModel.DataAnnotations.Validator.ValidateObject(input,new(input),true);
        OperationalRules.Require(input.Permissions.All(Permissions.All.Contains),"توجد صلاحية غير معروفة.");
        var selected=input.Permissions.Distinct().Order().ToArray();
        OperationalRules.Require(selected.Contains(Permissions.EventsView),"احتفظ بصلاحية Events.View حتى يتمكن المستخدم من فتح التطبيق.");
        OperationalRules.Require(id!=actor.Id||selected.Contains(Permissions.AdminUsers),"لا يمكنك إزالة صلاحية إدارة المستخدمين من حسابك الحالي.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable):null;
        var user=await manager.FindByIdAsync(id)??throw new KeyNotFoundException();
        OperationalRules.Require(input.Version==user.ConcurrencyStamp,"تغيرت بيانات المستخدم؛ أعد تحميل صفحة الصلاحيات قبل الحفظ.");
        OperationalRules.Require(!user.GovernorateId.HasValue||!selected.Any(RolePermissionDefaults.RequiresCentralScope),"الصلاحيات الإدارية والتدقيق تتطلب نطاقاً مركزياً.");
        var before=await EffectivePermissions(user);var claims=await manager.GetClaimsAsync(user);
        var replaced=claims.Where(c=>c.Type=="permission"||c.Type==RolePermissionDefaults.ExplicitSelectionClaim).ToArray();
        if(replaced.Length>0)Check(await manager.RemoveClaimsAsync(user,replaced));
        Check(await manager.AddClaimsAsync(user,selected.Select(p=>new Claim("permission",p)).Append(new Claim(RolePermissionDefaults.ExplicitSelectionClaim,RolePermissionDefaults.ExplicitSelectionValue))));
        Check(await manager.UpdateSecurityStampAsync(user));
        foreach(var granted in selected.Except(before))db.AuditLogs.Add(new(){Actor=actor.Id,Action="PermissionGranted: "+granted,Entity="UserPermissions",EntityId=id});
        foreach(var removed in before.Except(selected))db.AuditLogs.Add(new(){Actor=actor.Id,Action="PermissionRevoked: "+removed,Entity="UserPermissions",EntityId=id});
        db.AuditLogs.Add(new(){Actor=actor.Id,Action="PermissionsUpdated",Entity="UserPermissions",EntityId=id});
        await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
    public async Task Create(UserInput input)
    {
        Demand();System.ComponentModel.DataAnnotations.Validator.ValidateObject(input,new(input),true);
        OperationalRules.Require(input.Permissions.All(Permissions.All.Contains)&&Permissions.Roles.Contains(input.Role),"صلاحيات أو دور غير معروف.");
        if(input.GovernorateId.HasValue) OperationalRules.Require(await db.Governorates.AnyAsync(x=>x.Id==input.GovernorateId),"محافظة غير صالحة.");
        OperationalRules.Require(input.GovernorateId==null||(!input.Permissions.Any(x=>x.StartsWith("Admin.")||x==Permissions.AuditView)&&input.Role!="System Administrator"),"الصلاحيات الإدارية تتطلب نطاقاً مركزياً.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        var u=new AppUser { UserName=input.UserName,GovernorateId=input.GovernorateId };
        Check(await manager.CreateAsync(u,input.Password));Check(await manager.AddToRoleAsync(u,input.Role));
        Check(await manager.AddClaimsAsync(u,input.Permissions.Distinct().Select(p=>new Claim("permission",p))
            .Append(new Claim(RolePermissionDefaults.ExplicitSelectionClaim,RolePermissionDefaults.ExplicitSelectionValue))));if(tx!=null)await tx.CommitAsync();
    }
    private static void Check(IdentityResult result) { if(!result.Succeeded)throw new BusinessRuleException(string.Join("؛ ",result.Errors.Select(x=>x.Description))); }
    public async Task Disable(string id,bool disabled)
    {
        Demand();OperationalRules.Require(id!=actor.Id,"لا يمكن تعطيل حسابك الحالي.");await using var tx=await db.Database.BeginTransactionAsync();var u=await manager.FindByIdAsync(id)??throw new KeyNotFoundException();Check(await manager.SetLockoutEnabledAsync(u,true));Check(await manager.SetLockoutEndDateAsync(u,disabled?DateTimeOffset.MaxValue:null));Check(await manager.UpdateSecurityStampAsync(u));await tx.CommitAsync();
    }
    public async Task ResetPassword(string id,PasswordResetInput input)
    {
        Demand();System.ComponentModel.DataAnnotations.Validator.ValidateObject(input,new(input),true);await using var tx=await db.Database.BeginTransactionAsync();var u=await manager.FindByIdAsync(id)??throw new KeyNotFoundException();var token=await manager.GeneratePasswordResetTokenAsync(u);Check(await manager.ResetPasswordAsync(u,token,input.Password));Check(await manager.UpdateSecurityStampAsync(u));await tx.CommitAsync();
    }
}
public static class InitialData
{
    public static async Task Seed(OmsDbContext db,RoleManager<IdentityRole> roles,UserManager<AppUser> users,string adminName,string adminPassword)
    {
        if(!await db.Governorates.AnyAsync())
        {
            string[] names=["القاهرة","الإسكندرية","بورسعيد","السويس","دمياط","الدقهلية","الشرقية","القليوبية","كفر الشيخ","الغربية","المنوفية","البحيرة","الإسماعيلية","الجيزة","بني سويف","الفيوم","المنيا","أسيوط","سوهاج","قنا","أسوان","الأقصر","البحر الأحمر","الوادي الجديد","مطروح","شمال سيناء","جنوب سيناء"];
            db.Governorates.AddRange(names.Select(n=>new Governorate{Name=n})); await db.SaveChangesAsync();
        }
        foreach(var roleName in Permissions.Roles)
        {
            if(!await roles.RoleExistsAsync(roleName)) Check(await roles.CreateAsync(new(roleName)));
        }
        var adminRole=(await roles.FindByNameAsync("System Administrator"))!;
        var claims=await roles.GetClaimsAsync(adminRole);
        foreach(var permission in Permissions.All) if(!claims.Any(c=>c.Type=="permission"&&c.Value==permission))Check(await roles.AddClaimAsync(adminRole,new("permission",permission)));
        if(await users.FindByNameAsync(adminName)==null)
        {
            var admin=new AppUser{UserName=adminName};Check(await users.CreateAsync(admin,adminPassword));Check(await users.AddToRoleAsync(admin,"System Administrator"));
        }
    }
    private static void Check(IdentityResult result) { if(!result.Succeeded)throw new BusinessRuleException(string.Join("; ",result.Errors.Select(x=>x.Description))); }
}
