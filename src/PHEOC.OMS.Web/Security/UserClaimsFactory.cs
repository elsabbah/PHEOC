using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Web.Security;
public class UserClaimsFactory(UserManager<AppUser> users,RoleManager<IdentityRole> roles,IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<AppUser,IdentityRole>(users,roles,options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity=await base.GenerateClaimsAsync(user);
        var direct=await UserManager.GetClaimsAsync(user);
        if(direct.Any(c=>c.Type==RolePermissionDefaults.ExplicitSelectionClaim&&c.Value==RolePermissionDefaults.ExplicitSelectionValue))
        {
            foreach(var inherited in identity.FindAll("permission").ToArray())identity.RemoveClaim(inherited);
            identity.AddClaims(direct.Where(c=>c.Type=="permission"&&Permissions.All.Contains(c.Value)));
        }
        if(user.GovernorateId.HasValue)identity.AddClaim(new("governorate",user.GovernorateId.Value.ToString()));return identity;
    }
}
