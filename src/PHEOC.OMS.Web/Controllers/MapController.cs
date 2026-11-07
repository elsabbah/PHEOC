using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class MapController(ICoordinationService service) : OmsController
{
    public async Task<IActionResult> Index()=>View(await service.MapEvents());
    public async Task<IActionResult> Data()=>Json(await service.MapEvents());
}
