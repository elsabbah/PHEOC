using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
using PHEOC.OMS.Web.Security;
namespace PHEOC.OMS.IntegrationTests;

public class RolePermissionTests
{
    [SqlFact]public async Task AdministrativeDeletionPreservesHistoryRejectsStaleAndUnauthorizedRequestsAndCannotBeReenabled()
    {
        await using var provider=Provider();await using var scope=provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();await using var tx=await db.Database.BeginTransactionAsync();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var service=new UserAdministration(db,users,new Actor());var suffix=Guid.NewGuid().ToString("N");
        await service.Create(new(){UserName="qa.delete."+suffix,Password="Qa!8aaA"+suffix,Role="Risk Assessment Officer",Permissions=[Permissions.EventsView]});
        var user=(await users.FindByNameAsync("qa.delete."+suffix))!;var model=await service.EditPermissions(user.Id);var stamp=user.SecurityStamp;
        var input=new UserAccountDeleteInput{Version=model.Version};
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new UserAdministration(db,users,new LimitedActor("qa.denied",null,false)).Delete(user.Id,input));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new UserAdministration(db,users,new LimitedActor("qa.local",1)).Delete(user.Id,input));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>new UserAdministration(db,users,new LimitedActor(user.Id)).Delete(user.Id,input));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.Delete(user.Id,new(){Version="stale"}));
        await service.Delete(user.Id,input);db.ChangeTracker.Clear();user=(await users.FindByIdAsync(user.Id))!;
        Assert.True(await users.IsLockedOutAsync(user));Assert.NotEqual(stamp,user.SecurityStamp);
        Assert.Contains(await users.GetClaimsAsync(user),c=>c.Type==UserAccountStatus.ClaimType&&c.Value==UserAccountStatus.Deleted);
        Assert.Contains("Risk Assessment Officer",await users.GetRolesAsync(user));Assert.DoesNotContain(await service.Users(),u=>u.Id==user.Id);
        Assert.True(await db.AuditLogs.AnyAsync(a=>a.EntityId==user.Id&&a.Action=="UserAdministrativelyDeleted"));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.Disable(user.Id,false));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.UpdatePermissions(user.Id,new(){Version=user.ConcurrencyStamp!,Permissions=[Permissions.EventsView]}));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.ResetPassword(user.Id,new(){Password="Qa!9Aa"+suffix}));
        await tx.RollbackAsync();
    }
    [Fact]public void EveryRoleHasValidDistinctViewableDefaultsAndSensitiveRolesStayLimited()
    {
        foreach(var role in Permissions.Roles)
        {
            var defaults=RolePermissionDefaults.For(role);
            Assert.NotEmpty(defaults);Assert.Contains(Permissions.EventsView,defaults);
            Assert.All(defaults,p=>Assert.Contains(p,Permissions.All));
            Assert.Equal(defaults.Count,defaults.Distinct().Count());
        }
        Assert.Equal(Permissions.All.Order(),RolePermissionDefaults.For("System Administrator").Order());
        Assert.DoesNotContain(Permissions.RiskApprove,RolePermissionDefaults.For("Risk Assessment Officer"));
        Assert.DoesNotContain(Permissions.AdminUsers,RolePermissionDefaults.For("Duty Officer"));
        Assert.Equal(new[]{Permissions.EventsView},RolePermissionDefaults.For("External Agency User"));
        Assert.Empty(RolePermissionDefaults.For("unknown"));
    }
    private sealed class Actor:ICurrentUser {public string Id=>"qa-role-defaults";public int? GovernorateId=>null;public bool Can(string permission)=>true;}
    private sealed class LimitedActor(string id,int? governorate=null,bool manage=true):ICurrentUser{public string Id=>id;public int? GovernorateId=>governorate;public bool Can(string p)=>manage;}
    private static ServiceProvider Provider()
    {
        var services=new ServiceCollection();services.AddLogging();services.AddSingleton<ICurrentUser>(new Actor());
        services.AddDbContext<OmsDbContext>(o=>o.UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),s=>s.UseCompatibilityLevel(130)));
        services.AddIdentityCore<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<OmsDbContext>().AddClaimsPrincipalFactory<UserClaimsFactory>();return services.BuildServiceProvider();
    }
    [SqlFact]public async Task EditingLegacyPermissionsPreservesRolesUnrelatedClaimsAndInvalidatesSecurityStamp()
    {
        await using var provider=Provider();await using var scope=provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();await using var tx=await db.Database.BeginTransactionAsync();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var factory=scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<AppUser>>();var service=new UserAdministration(db,users,new Actor());
        var suffix=Guid.NewGuid().ToString("N");var user=new AppUser{UserName="qa.editlegacy."+suffix};Assert.True((await users.CreateAsync(user,"Qa!8aaA"+suffix)).Succeeded);Assert.True((await users.AddToRoleAsync(user,"System Administrator")).Succeeded);Assert.True((await users.AddClaimAsync(user,new("qa-unrelated","preserve"))).Succeeded);
        var before=await service.EditPermissions(user.Id);Assert.Contains(Permissions.AdminSettings,before.Permissions);var stamp=user.SecurityStamp;
        var selected=new[]{Permissions.EventsView,Permissions.QiraReview,Permissions.RiskApprove};await service.UpdatePermissions(user.Id,new(){Version=before.Version,Permissions=selected});
        db.ChangeTracker.Clear();var reloaded=await users.FindByIdAsync(user.Id);Assert.NotNull(reloaded);var after=await service.EditPermissions(user.Id);
        Assert.Equal(selected.Order(),after.Permissions.Order());Assert.Equal(before.Roles,after.Roles);Assert.Equal(before.GovernorateId,after.GovernorateId);Assert.NotEqual(stamp,reloaded.SecurityStamp);
        var principal=await factory.CreateAsync(reloaded);Assert.True(principal.IsInRole("System Administrator"));Assert.True(principal.HasClaim("permission",Permissions.QiraReview));Assert.False(principal.HasClaim("permission",Permissions.AdminSettings));Assert.Contains(await users.GetClaimsAsync(reloaded),c=>c.Type=="qa-unrelated"&&c.Value=="preserve");
        Assert.True(await db.AuditLogs.AnyAsync(a=>a.Entity=="UserPermissions"&&a.EntityId==user.Id&&a.Action=="PermissionsUpdated"));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.UpdatePermissions(user.Id,new(){Version=before.Version,Permissions=[Permissions.EventsView]}));
        Assert.Equal(selected.Order(),(await service.EditPermissions(user.Id)).Permissions.Order());await tx.RollbackAsync();
    }
    [SqlFact]public async Task EditingPermissionsEnforcesCentralScopeKnownValuesAndSelfManagement()
    {
        await using var provider=Provider();await using var scope=provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();await using var tx=await db.Database.BeginTransactionAsync();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var service=new UserAdministration(db,users,new Actor());var suffix=Guid.NewGuid().ToString("N");
        var input=new UserInput{UserName="qa.editscope."+suffix,Password="Qa!8aaA"+suffix,Role="Risk Assessment Officer",GovernorateId=1,Permissions=[Permissions.EventsView]};await service.Create(input);var user=(await users.FindByNameAsync(input.UserName))!;var model=await service.EditPermissions(user.Id);
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new UserAdministration(db,users,new LimitedActor("qa.denied",null,false)).UpdatePermissions(user.Id,new(){Version=model.Version,Permissions=[Permissions.EventsView]}));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new UserAdministration(db,users,new LimitedActor("qa.local",1)).EditPermissions(user.Id));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.UpdatePermissions(user.Id,new(){Version=model.Version,Permissions=[Permissions.EventsView,Permissions.AdminUsers]}));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.UpdatePermissions(user.Id,new(){Version=model.Version,Permissions=[Permissions.EventsView,"unknown"]}));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.UpdatePermissions(user.Id,new(){Version=model.Version,Permissions=[Permissions.QiraReview]}));
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>new UserAdministration(db,users,new LimitedActor(user.Id)).UpdatePermissions(user.Id,new(){Version=model.Version,Permissions=[Permissions.EventsView]}));
        Assert.Equal(new[]{Permissions.EventsView},(await service.EditPermissions(user.Id)).Permissions);await tx.RollbackAsync();
    }
    [SqlFact] public async Task FinalSelectionPersistsAndControlsClaimsEvenForAdministratorWhileLegacyStaysInherited()
    {
        var actor=new Actor();var services=new ServiceCollection();services.AddLogging();services.AddSingleton<ICurrentUser>(actor);
        services.AddDbContext<OmsDbContext>(o=>o.UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),s=>s.UseCompatibilityLevel(130)));
        services.AddIdentityCore<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<OmsDbContext>().AddClaimsPrincipalFactory<UserClaimsFactory>();
        await using var provider=services.BuildServiceProvider();await using var scope=provider.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();await using var tx=await db.Database.BeginTransactionAsync();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var factory=scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<AppUser>>();
        var admin=new UserAdministration(db,users,actor);var suffix=Guid.NewGuid().ToString("N");
        var input=new UserInput{UserName="qa.preset."+suffix,Password="Qa!8aaA"+suffix,Role="System Administrator",Permissions=[Permissions.EventsView,Permissions.ReportsView]};
        await admin.Create(input);
        var created=await users.FindByNameAsync(input.UserName);Assert.NotNull(created);
        var reloaded=await db.Users.AsNoTracking().SingleAsync(u=>u.Id==created.Id);
        var claims=await users.GetClaimsAsync(reloaded);
        Assert.Equal(input.Permissions.Order(),claims.Where(c=>c.Type=="permission").Select(c=>c.Value).Order());
        var principal=await factory.CreateAsync(reloaded);
        Assert.True(principal.IsInRole("System Administrator"));Assert.True(principal.HasClaim("permission",Permissions.EventsView));
        Assert.True(principal.HasClaim("permission",Permissions.ReportsView));Assert.False(principal.HasClaim("permission",Permissions.AdminUsers));
        Assert.False(principal.HasClaim("permission",Permissions.AdminSettings));
        var legacy=new AppUser{UserName="qa.legacy."+suffix};Assert.True((await users.CreateAsync(legacy,"Qa!8aaA"+suffix)).Succeeded);
        Assert.True((await users.AddToRoleAsync(legacy,"System Administrator")).Succeeded);
        Assert.True((await factory.CreateAsync(legacy)).HasClaim("permission",Permissions.AdminUsers));
        var custom=new UserInput{UserName="qa.custom."+suffix,Password="Qa!8aaA"+suffix,Role="Reporting Officer",Permissions=[Permissions.EventsView,Permissions.FieldManage]};
        await admin.Create(custom);var customUser=await users.FindByNameAsync(custom.UserName);Assert.NotNull(customUser);
        var customPrincipal=await factory.CreateAsync(customUser);
        Assert.True(customPrincipal.HasClaim("permission",Permissions.FieldManage));Assert.False(customPrincipal.HasClaim("permission",Permissions.ReportsCreate));
        await tx.RollbackAsync();
    }
}
