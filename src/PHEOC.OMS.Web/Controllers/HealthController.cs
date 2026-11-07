using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class HealthController(IOmsService oms) : OmsController
{
    public async Task<IActionResult> Index()=>View(await oms.Health());
    [HttpPost]public Task<IActionResult> Contact(ContactInput input)=>Command(()=>oms.AddContact(input),"Health");
    [HttpPost]public Task<IActionResult> Follow(long id,NoteInput input)=>Command(()=>oms.FollowContact(id,input),"Health");
    [HttpPost]public Task<IActionResult> Sample(SampleInput input)=>Command(()=>oms.AddSample(input),"Health");
    [HttpPost]public Task<IActionResult> Transfer(long id,TransferInput input)=>Command(()=>oms.TransferSample(id,input),"Health");
    [HttpPost]public Task<IActionResult> Result(long id,NoteInput input)=>Command(()=>oms.SampleResult(id,input),"Health");
}
