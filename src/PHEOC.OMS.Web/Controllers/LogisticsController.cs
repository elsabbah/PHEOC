using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class LogisticsController(ILogisticsService service) : OmsController
{
    public async Task<IActionResult> Index()=>View(await service.Logistics());
    [HttpPost]public Task<IActionResult> CreateRequest(SupplyInput input)=>Command(()=>service.RequestSupply(input),"Logistics");
    [HttpPost]public Task<IActionResult> Approve(long id,SupplyApproveInput input)=>Command(()=>service.ApproveSupply(id,input),"Logistics");
    [HttpPost]public Task<IActionResult> Vehicle(VehicleInput input)=>Command(()=>service.AddVehicle(input),"Logistics");
    [HttpPost]public Task<IActionResult> Assign(VehicleAssignInput input)=>Command(()=>service.AssignVehicle(input),"Logistics");
    [HttpPost]public Task<IActionResult> Return(long id,VersionInput input)=>Command(()=>service.ReturnVehicle(id,input),"Logistics");
    [HttpPost]public Task<IActionResult> Transfer(long id,StockTransferInput input)=>Command(()=>service.TransferStock(id,input),"Inventory");
}
