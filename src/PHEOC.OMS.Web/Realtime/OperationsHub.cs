using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Web.Realtime;
[Authorize(Policy=Permissions.EventsView)]
public class OperationsHub(ICurrentUser user) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId,user.GovernorateId.HasValue?"governorate:"+user.GovernorateId:"central");await base.OnConnectedAsync();
    }
}
