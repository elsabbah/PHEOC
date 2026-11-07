using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class ReportsController(IOmsService oms,IReportExporter exporter) : OmsController
{
    public async Task<IActionResult> Index(){ViewBag.Events=(await oms.Dashboard()).Events;return View(await oms.Reports());}
    [HttpPost]public async Task<IActionResult> Create(ReportInput input){CheckInput();await oms.AddReport(input);return RedirectToAction(nameof(Index));}
    [HttpPost]public Task<IActionResult> Approve(long id)=>Command(()=>oms.ApproveReport(id),"Reports");
    public async Task<IActionResult> Excel()=>File(exporter.Excel(await oms.Reports()),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","SitRep.xlsx");
    public async Task<IActionResult> Word(long id){var report=(await oms.Reports()).SingleOrDefault(x=>x.Id==id)??throw new KeyNotFoundException();return File(exporter.Word(report),"application/vnd.openxmlformats-officedocument.wordprocessingml.document","SitRep-"+id+".docx");}
    public async Task<IActionResult> Print(long id)=>View((await oms.Reports()).SingleOrDefault(x=>x.Id==id)??throw new KeyNotFoundException());
}
