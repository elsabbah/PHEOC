using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class AnalyticsController(IAnalyticsService analytics) : OmsController
{
    public async Task<IActionResult> Index()=>View(await analytics.SevenOneSevenBoard());
    public async Task<IActionResult> Event(long id)=>View(await analytics.KpiDetails(id));
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]public async Task<IActionResult> SourceDates(long id)=>Json(await analytics.KpiSourceDates(id));
    [HttpPost]public Task<IActionResult> Dates(long id,KpiDatesInput input)=>Command(()=>analytics.RecordKpiDates(id,input),"Analytics","Event",id);
    [HttpPost]public Task<IActionResult> Milestone(long id,MilestoneInput input)=>Command(()=>analytics.RecordMilestone(id,input),"Analytics","Event",id);
}
