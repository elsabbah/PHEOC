using System.ComponentModel.DataAnnotations;
namespace PHEOC.OMS.Application.Contracts;
public class SupplyInput { public long EventId { get; set; } public long ItemId { get; set; } [Range(0.001,1000000000)]public decimal Quantity { get; set; } [Required,StringLength(2000)]public string Reason { get; set; }=""; }
public class SupplyApproveInput : VersionInput { public long BatchId { get; set; } }
public class VehicleInput { [Required,StringLength(80)]public string PlateNumber { get; set; }=""; [Required,StringLength(120)]public string Type { get; set; }="";public int GovernorateId { get; set; } }
public class VehicleAssignInput { public long VehicleId { get; set; } public long DeploymentId { get; set; } }
public class StockTransferInput : VersionInput { public long ToWarehouseId { get; set; } [Range(0.001,1000000000)]public decimal Quantity { get; set; } [Required,StringLength(2000)]public string Reason { get; set; }=""; }
public record SupplyRow(long Id,long EventId,string EventTitle,string Item,decimal Quantity,string Reason,string RequestedBy,string? ApprovedBy,string Version);
public record VehicleRow(long Id,string Plate,string Type,string Governorate,bool Available);
public record VehicleAssignmentRow(long Id,string Plate,string Team,string EventTitle,DateTime At,DateTime? ReturnedAt,string Version);
public record LogisticsBoard(IReadOnlyList<SupplyRow> Requests,IReadOnlyList<VehicleRow> Vehicles,IReadOnlyList<VehicleAssignmentRow> Assignments,IReadOnlyList<DeploymentRow> Deployments,IReadOnlyList<EventRow> Events,IReadOnlyList<Lookup> Items,IReadOnlyList<Lookup> Governorates,IReadOnlyList<StockRow> Stock);
public interface ILogisticsService
{
    Task<LogisticsBoard> Logistics();
    Task RequestSupply(SupplyInput input);
    Task ApproveSupply(long id,SupplyApproveInput input);
    Task AddVehicle(VehicleInput input);
    Task AssignVehicle(VehicleAssignInput input);
    Task ReturnVehicle(long id,VersionInput input);
    Task TransferStock(long id,StockTransferInput input);
}
