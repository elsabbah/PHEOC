using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class CommunicationsController(ICoordinationService service) : OmsController
{
    public async Task<IActionResult> Index()=>View(await service.Communications());
    [HttpPost]public Task<IActionResult> Message(long id,MessageInput input)=>Command(()=>service.AddMessage(id,input),"Events","Details",id);
    [HttpPost]public Task<IActionResult> Read(long id)=>Command(()=>service.ReadNotification(id),"Communications");
    [HttpPost]public Task<IActionResult> Sla(SlaInput input)=>Command(()=>service.AddSla(input),"Communications");
    [HttpPost,RequestSizeLimit(6291456)]public async Task<IActionResult> Upload(long id,IFormFile file)
    {
        if(file==null||file.Length is <=0 or >5242880)throw new PHEOC.OMS.Domain.Rules.BusinessRuleException("اختر مرفقاً حتى 5 MB.");
        await using var stream=new MemoryStream();await file.CopyToAsync(stream);await service.UploadAttachment(id,file.FileName,stream.ToArray());return RedirectToAction("Details","Events",new{id});
    }
    public async Task<IActionResult> Download(long id){var download=await service.DownloadAttachment(id);return File(download.Content,download.ContentType,download.Metadata.Name);}
}
