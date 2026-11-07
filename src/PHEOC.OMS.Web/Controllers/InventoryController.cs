using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class InventoryController(IOmsService oms) : OmsController
{
    public async Task<IActionResult> Index(){ViewBag.Events=(await oms.Dashboard()).Events;return View(await oms.Inventory());}
    [HttpPost]public Task<IActionResult> Warehouse(WarehouseInput input)=>Command(()=>oms.AddWarehouse(input),"Inventory");
    [HttpPost]public Task<IActionResult> Item(ItemInput input)=>Command(()=>oms.AddItem(input),"Inventory");
    [HttpPost]public Task<IActionResult> Batch(BatchInput input)=>Command(()=>oms.AddBatch(input),"Inventory");
    [HttpPost]public Task<IActionResult> Move(long id,MovementInput input)=>Command(()=>oms.MoveStock(id,input),"Inventory");
}
