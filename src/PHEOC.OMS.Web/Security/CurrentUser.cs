using System.Security.Claims;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Web.Security;
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public string Id => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    public int? GovernorateId => int.TryParse(Principal?.FindFirstValue("governorate"),out var id)?id:null;
    public bool Can(string permission) => Principal?.Identity?.IsAuthenticated==true&&Principal.HasClaim("permission",permission);
}
