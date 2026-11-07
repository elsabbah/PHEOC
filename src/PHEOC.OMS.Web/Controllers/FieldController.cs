using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class FieldController(IOmsService oms,ICoordinationService coordination) : OmsController
{
    public async Task<IActionResult> Index()=>View(await oms.Field());
    [HttpPost]public Task<IActionResult> Report(FieldInput input)=>Command(()=>oms.AddFieldReport(input),"Field");
    [HttpPost]public Task<IActionResult> Sos(long deploymentId,NoteInput input)=>Command(()=>coordination.Sos(deploymentId,input),"Field");
}
