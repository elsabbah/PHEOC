using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Web.Controllers;
public class DashboardController(IOmsService oms) : OmsController
{
    public async Task<IActionResult> Index(int? governorate,string? query,EventStatus? status,int page=1)=>View(await oms.Dashboard(governorate,query,status,page));
    public async Task<IActionResult> Wall()=>View(await oms.Dashboard());
    public async Task<IActionResult> Metrics()=>View(await oms.Metrics());
}
