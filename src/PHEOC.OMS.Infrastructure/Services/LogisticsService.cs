using System.Data;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Operations;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;
public partial class OmsService
{
    public async Task<LogisticsBoard> Logistics()
    {
        Demand(Permissions.InventoryView);
        var requests=await db.SupplyRequests.Where(x=>user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId).Include(x=>x.Event).Include(x=>x.Item).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        var vehicles=await db.Vehicles.Where(x=>user.GovernorateId==null||x.GovernorateId==user.GovernorateId).Select(x=>new VehicleRow(x.Id,x.PlateNumber,x.Type,x.Governorate.Name,x.Available)).ToListAsync();
        var assignments=await db.VehicleAssignments.Where(x=>user.GovernorateId==null||x.Deployment.Event.GovernorateId==user.GovernorateId).Include(x=>x.Vehicle).Include(x=>x.Deployment).ThenInclude(x=>x.Team).Include(x=>x.Deployment).ThenInclude(x=>x.Event).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        return new(requests.Select(x=>new SupplyRow(x.Id,x.EventId,x.Event.Title,x.Item.Name,x.Quantity,x.Reason,x.RequestedBy,x.ApprovedBy,Version(x))).ToList(),vehicles,assignments.Select(x=>new VehicleAssignmentRow(x.Id,x.Vehicle.PlateNumber,x.Deployment.Team.Name,x.Deployment.Event.Title,x.CreatedAt,x.ReturnedAt,Version(x))).ToList(),await DeploymentRows(db.Deployments.Where(x=>x.ReturnedAt==null&&(user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId))),(await Dashboard()).Events,(await Inventory()).Items,await Governorates(),(await Inventory()).Stock);
    }
    public async Task RequestSupply(SupplyInput input)
    {
        Demand(Permissions.InventoryManage);Validate(input);var e=await Open(input.EventId);Require(await db.InventoryItems.AnyAsync(x=>x.Id==input.ItemId),"الصنف غير موجود.");db.SupplyRequests.Add(new(){EventId=e.Id,ItemId=input.ItemId,Quantity=input.Quantity,Reason=input.Reason,RequestedBy=user.Id});Timeline(e,"SupplyRequested","طلب إمداد");await db.SaveChangesAsync();
    }
    public async Task ApproveSupply(long id,SupplyApproveInput input)
    {
        Demand(Permissions.InventoryManage);Validate(input);await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var r=await db.SupplyRequests.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();var e=await Open(r.EventId);CheckVersion(r,input.Version);Require(r.ApprovedBy==null&&r.RequestedBy!=user.Id,"الطلب يحتاج اعتماد مستخدم آخر ولم يُصرف سابقاً.");
        var batch=await db.InventoryBatches.Include(x=>x.Warehouse).SingleOrDefaultAsync(x=>x.Id==input.BatchId)??throw new KeyNotFoundException();Scope(batch.Warehouse.GovernorateId);Require(batch.ItemId==r.ItemId,"الدفعة لا تخص الصنف المطلوب.");
        await MoveStock(batch.Id,new(){Version=Version(batch),Quantity=-r.Quantity,EventId=r.EventId,Reason="صرف طلب #"+r.Id+": "+r.Reason});r.BatchId=batch.Id;r.ApprovedBy=user.Id;Timeline(e,"SupplyIssued","اعتماد وصرف طلب #"+r.Id);await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
    public async Task AddVehicle(VehicleInput input)
    {
        Demand(Permissions.InventoryManage);Validate(input);Scope(input.GovernorateId);Require(await db.Governorates.AnyAsync(x=>x.Id==input.GovernorateId),"محافظة غير صالحة.");db.Vehicles.Add(new(){PlateNumber=input.PlateNumber,Type=input.Type,GovernorateId=input.GovernorateId});await db.SaveChangesAsync();
    }
    public async Task AssignVehicle(VehicleAssignInput input)
    {
        Demand(Permissions.InventoryManage);await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var vehicle=await db.Vehicles.SingleOrDefaultAsync(x=>x.Id==input.VehicleId)??throw new KeyNotFoundException();Scope(vehicle.GovernorateId);Require(vehicle.Available,"المركبة مكلفة بالفعل.");var d=await db.Deployments.SingleOrDefaultAsync(x=>x.Id==input.DeploymentId)??throw new KeyNotFoundException();var e=await Open(d.EventId);Require(d.ReturnedAt==null,"أمر النشر منتهٍ.");vehicle.Available=false;db.VehicleAssignments.Add(new(){VehicleId=vehicle.Id,DeploymentId=d.Id,AssignedBy=user.Id});Timeline(e,"VehicleAssigned","تكليف مركبة");await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
    public async Task ReturnVehicle(long id,VersionInput input)
    {
        Demand(Permissions.InventoryManage);Validate(input);var a=await db.VehicleAssignments.Include(x=>x.Vehicle).Include(x=>x.Deployment).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();await Load(a.Deployment.EventId);Scope(a.Vehicle.GovernorateId);CheckVersion(a,input.Version);Require(a.ReturnedAt==null,"تم تسجيل العودة بالفعل.");a.ReturnedAt=DateTime.UtcNow;a.Vehicle.Available=true;await db.SaveChangesAsync();
    }
    public async Task TransferStock(long id,StockTransferInput input)
    {
        Demand(Permissions.InventoryManage);Validate(input);await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var from=await db.InventoryBatches.Include(x=>x.Warehouse).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();Scope(from.Warehouse.GovernorateId);CheckVersion(from,input.Version);Require(from.WarehouseId!=input.ToWarehouseId,"اختر مستودعاً آخر.");
        var warehouse=await db.Warehouses.SingleOrDefaultAsync(x=>x.Id==input.ToWarehouseId)??throw new KeyNotFoundException();Scope(warehouse.GovernorateId);Require(from.ExpiresAt==null||from.ExpiresAt>=DateTime.UtcNow.Date,"الدفعة منتهية الصلاحية.");
        var to=await db.InventoryBatches.SingleOrDefaultAsync(x=>x.WarehouseId==warehouse.Id&&x.ItemId==from.ItemId&&x.BatchNumber==from.BatchNumber);
        if(to==null){to=new(){WarehouseId=warehouse.Id,ItemId=from.ItemId,BatchNumber=from.BatchNumber,ExpiresAt=from.ExpiresAt};db.InventoryBatches.Add(to);}
        from.Balance=StockBalance(from.Balance,-input.Quantity);to.Balance=StockBalance(to.Balance,input.Quantity);var transfer=Guid.NewGuid().ToString("N");
        db.InventoryTransactions.AddRange(new InventoryTransaction{Batch=from,Quantity=-input.Quantity,Reason="تحويل "+transfer+" — "+input.Reason,ApprovedBy=user.Id},new InventoryTransaction{Batch=to,Quantity=input.Quantity,Reason="تحويل "+transfer+" — "+input.Reason,ApprovedBy=user.Id});await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
}
