using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Web.Controllers;
public class QiraController(IQiraService service,ICoordinationService coordination):OmsController
{
    public async Task<IActionResult> Event(long id)=>View(await service.QiraForEvent(id));
    public async Task<IActionResult> Details(long id,string? node=null){ViewBag.Node=node;return View(await service.Qira(id));}
    [HttpGet,Authorize(Policy=Permissions.RiskCreate)]public async Task<IActionResult> Delete(long id)
    {
        var model=await service.Qira(id);
        if(!model.Editable||!model.Assessment.CanDelete)throw new AccessDeniedException();
        return View(model);
    }
    [HttpPost,ActionName("Delete"),Authorize(Policy=Permissions.RiskCreate)]public async Task<IActionResult> ConfirmDelete(long id,VersionInput input)
    {
        CheckInput();var eventId=await service.DeleteQiraDraft(id,input);TempData["Success"]="تم حذف المسودة مع حفظ سجل التدقيق.";
        return RedirectToAction(nameof(Event),new{id=eventId});
    }
    [HttpPost,Authorize(Policy=Permissions.RiskCreate)]public async Task<IActionResult> Start(long eventId,long? previousId)
    {CheckInput();return RedirectToAction(nameof(Details),new{id=await service.StartQira(eventId,previousId)});}
    [HttpPost]public Task<IActionResult> Scope(long id,QiraScopeInput input)=>Command(()=>service.SaveQiraScope(id,input),"Qira","Details",id);
    [HttpPost]public Task<IActionResult> Answer(long id,QiraAnswerInput input)=>Command(()=>service.SaveQiraAnswer(id,input),"Qira","Details",id);
    [HttpPost]public Task<IActionResult> Undo(long id,VersionInput input)=>Command(()=>service.UndoQiraAnswer(id,input),"Qira","Details",id);
    [HttpPost]public Task<IActionResult> Complete(long id,QiraCompleteInput input)=>Command(()=>service.CompleteQira(id,input),"Qira","Details",id);
    [HttpPost]public Task<IActionResult> Review(long id,QiraReviewInput input)=>Command(()=>service.ReviewQira(id,input),"Qira","Details",id);
    [HttpPost]public Task<IActionResult> Approve(long id,QiraReviewInput input)=>Command(()=>service.ApproveQira(id,input),"Qira","Details",id);
    [HttpPost]public Task<IActionResult> Share(long id,VersionInput input)=>Command(()=>service.ShareQiraActions(id,input),"Qira","Details",id);
    [HttpPost]public async Task<IActionResult> Action(long id,long assessmentId,QiraActionUpdate input)
    {CheckInput();await service.UpdateQiraAction(id,input);return RedirectToAction(nameof(Details),new{id=assessmentId});}
    [Authorize(Policy=Permissions.AdminSettings)]public async Task<IActionResult> Configuration()=>View(await service.QiraPolicies());
    [HttpPost,Authorize(Policy=Permissions.AdminSettings)]public Task<IActionResult> Policy(QiraPolicyInput input)=>Command(()=>service.AddQiraPolicy(input),"Qira","Configuration");
    [HttpPost,RequestSizeLimit(6291456)]public async Task<IActionResult> Upload(long id,IFormFile file)
    {
        var assessment=await service.Qira(id);
        if(!assessment.Editable)throw new AccessDeniedException();
        if(file==null||file.Length is <=0 or >5242880)throw new PHEOC.OMS.Domain.Rules.BusinessRuleException("اختر مرفقاً حتى 5 MB.");
        await using var stream=new MemoryStream();await file.CopyToAsync(stream);await coordination.UploadAttachment(assessment.Event.Id,file.FileName,stream.ToArray());
        return RedirectToAction(nameof(Details),new{id});
    }
}
