using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Web.ViewSupport;
namespace PHEOC.OMS.Web.Controllers;
public class EgyptEbsController(IEgyptEbsReader reader) : OmsController
{
    public async Task<IActionResult> Index(EgyptEbsFilter filter,int? before=null){CheckInput();DefaultDates(filter);return View(await reader.Browse(filter,before));}
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Status()=>Json(await reader.Status());
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Signals(EgyptEbsFilter filter,int? before=null){CheckInput();DefaultDates(filter);return PartialView("_Signals",await reader.Browse(filter,before,false));}
    private void DefaultDates(EgyptEbsFilter filter)=>EgyptEbsDisplay.DefaultDates(filter,Request.Query.ContainsKey("From")||Request.Query.ContainsKey("To"),DateTime.UtcNow);
    [HttpPost]public async Task<IActionResult> Import(int id){CheckInput();var imported=await reader.Import(id);return RedirectToAction("Details","Events",new{id=imported.EventId});}
    [HttpPost]public Task<IActionResult> Automatic(bool enabled)=>Command(()=>reader.SetAutomatic(enabled),"EgyptEbs");
}
