using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;

namespace PHEOC.OMS.Web.Controllers;

public class EventAlertsController(IEventAlertService alerts) : OmsController
{
    [HttpGet] public async Task<IActionResult> Feed(long afterId = 0) { CheckInput(); return Json(await alerts.Feed(afterId)); }
    [HttpPost] public async Task<IActionResult> Read(long[] ids) { CheckInput(); await alerts.Read(ids); return Json(new { success = true }); }
}
