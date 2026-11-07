using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class TeamsController(IOmsService oms) : OmsController
{
    public async Task<IActionResult> Index()=>View(await oms.Teams());
    [HttpPost]public Task<IActionResult> Person(PersonInput input)=>Command(()=>oms.AddPerson(input),"Teams");
    [HttpPost]public Task<IActionResult> Team(TeamInput input)=>Command(()=>oms.AddTeam(input),"Teams");
    [HttpPost]public Task<IActionResult> Arrive(long id,VersionInput input)=>Command(()=>oms.DeploymentStatus(id,input,false),"Teams");
    [HttpPost]public Task<IActionResult> Return(long id,VersionInput input)=>Command(()=>oms.DeploymentStatus(id,input,true),"Teams");
}
