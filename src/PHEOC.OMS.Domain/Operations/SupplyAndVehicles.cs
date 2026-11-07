using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Operations;
public class SupplyRequest : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public long ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;
    public decimal Quantity { get; set; }
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(450)] public string RequestedBy { get; set; } = "";
    [MaxLength(450)] public string? ApprovedBy { get; set; }
    public long? BatchId { get; set; }
    public InventoryBatch? Batch { get; set; }
}
public class Vehicle : Record
{
    [MaxLength(80)] public string PlateNumber { get; set; } = "";
    [MaxLength(120)] public string Type { get; set; } = "";
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = null!;
    public bool Available { get; set; } = true;
}
public class VehicleAssignment : Record
{
    public long VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public long DeploymentId { get; set; }
    public Deployment Deployment { get; set; } = null!;
    [MaxLength(450)] public string AssignedBy { get; set; } = "";
    public DateTime? ReturnedAt { get; set; }
}
