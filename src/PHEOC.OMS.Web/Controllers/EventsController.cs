using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Web.Controllers;
public class EventsController(IOmsService oms,ICoordinationService coordination) : OmsController
{
    public async Task<IActionResult> Index(int? governorate,string? query,PHEOC.OMS.Domain.Events.EventStatus? status,int page=1)=>View(await oms.Dashboard(governorate,query,status,page));
    [Authorize(Policy=Permissions.EventsCreate)]public async Task<IActionResult> Create(){ViewBag.Governorates=await oms.Governorates();return View(new EventInput());}
    [HttpPost,Authorize(Policy=Permissions.EventsCreate)]public async Task<IActionResult> Create(EventInput input)
    {
        if(!ModelState.IsValid){ViewBag.Governorates=await oms.Governorates();return View(input);}var id=await oms.CreateEvent(input);return RedirectToAction(nameof(Details),new{id});
    }
    public async Task<IActionResult> Details(long id){ViewBag.Messages=await coordination.Messages(id);ViewBag.Attachments=await coordination.Attachments(id);return View(await oms.Event(id));}
    [HttpPost]public Task<IActionResult> Status(long id,StatusInput input)=>Command(()=>oms.ChangeStatus(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Activate(long id,ActivationInput input)=>Command(()=>oms.Activate(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Assign(long id,AssignmentInput input)=>Command(()=>oms.Assign(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Task(long id,TaskInput input)=>Command(()=>oms.AddTask(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> UpdateTask(long id,long eventId,TaskUpdate input)=>Command(()=>oms.UpdateTask(id,input),"Events","Details",eventId);
    [HttpPost]public Task<IActionResult> Decide(long id,DecisionInput input)=>Command(()=>oms.Decide(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Deploy(long id,DeploymentInput input)=>Command(()=>oms.Deploy(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Closure(long id,ClosureInput input)=>Command(()=>oms.RequestClosure(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> ApproveClosure(long id,long eventId,VersionInput input)=>Command(()=>oms.ApproveClosure(id,input),"Events","Details",eventId);
    [HttpPost]public Task<IActionResult> Aar(long id,AarInput input)=>Command(()=>oms.AddAar(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Improve(long id,long eventId,ImprovementInput input)=>Command(()=>oms.AddImprovement(id,input),"Events","Details",eventId);
    public async Task<IActionResult> Improvements()=>View(await oms.Improvements());
    [HttpPost]public Task<IActionResult> UpdateImprovement(long id,ImprovementUpdate input)=>Command(()=>oms.UpdateImprovement(id,input),"Events","Improvements");
}
