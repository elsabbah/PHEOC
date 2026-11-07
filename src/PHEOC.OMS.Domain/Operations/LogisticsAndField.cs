using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Operations;

public class Warehouse : Record
{
    [MaxLength(160)] public string Name { get; set; } = "";
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = null!;
}
public class InventoryItem : Record
{
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Unit { get; set; } = "";
    public decimal Minimum { get; set; }
}
public class InventoryBatch : Record
{
    public long WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public long ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;
    [MaxLength(80)] public string BatchNumber { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public decimal Balance { get; set; }
}
public class InventoryTransaction : Record
{
    public long BatchId { get; set; }
    public InventoryBatch Batch { get; set; } = null!;
    public long? EventId { get; set; }
    public EmergencyEvent? Event { get; set; }
    public decimal Quantity { get; set; }
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(450)] public string ApprovedBy { get; set; } = "";
}
public class FieldReport : Record
{
    public Guid? ClientRequestId { get; set; }
    public long DeploymentId { get; set; }
    public Deployment Deployment { get; set; } = null!;
    [MaxLength(4000)] public string Activities { get; set; } = "";
    [MaxLength(2000)] public string Needs { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    [MaxLength(450)] public string ReportedBy { get; set; } = "";
}
public class ContactRecord : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(80)] public string Code { get; set; } = "";
    public DateTime LastExposureAt { get; set; }
    public DateTime FollowUpUntil { get; set; }
    [MaxLength(40)] public string RiskLevel { get; set; } = "";
    public List<ContactFollowUp> FollowUps { get; set; } = [];
}
public class ContactFollowUp : Record
{
    public long ContactId { get; set; }
    public ContactRecord Contact { get; set; } = null!;
    [MaxLength(1000)] public string Outcome { get; set; } = "";
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
}
public class Sample : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(80)] public string Code { get; set; } = "";
    [MaxLength(100)] public string Type { get; set; } = "";
    public DateTime CollectedAt { get; set; }
    public List<SampleTransfer> Transfers { get; set; } = [];
    public List<LaboratoryResult> Results { get; set; } = [];
}
public class SampleTransfer : Record
{
    public long SampleId { get; set; }
    public Sample Sample { get; set; } = null!;
    [MaxLength(160)] public string From { get; set; } = "";
    [MaxLength(160)] public string To { get; set; } = "";
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
}
public class LaboratoryResult : Record
{
    public long SampleId { get; set; }
    public Sample Sample { get; set; } = null!;
    [MaxLength(2000)] public string Result { get; set; } = "";
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
}
public class SitRep : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public int Version { get; set; }
    [MaxLength(4000)] public string Summary { get; set; } = "";
    [MaxLength(4000)] public string Challenges { get; set; } = "";
    [MaxLength(4000)] public string NextActions { get; set; } = "";
    public string Snapshot { get; set; } = "";
    [MaxLength(450)] public string PreparedBy { get; set; } = "";
    [MaxLength(450)] public string? ApprovedBy { get; set; }
}
public class ClosureRequest : Record
{
    public long? PolicyId { get; set; }
    public ClosurePolicy? Policy { get; set; }
    public bool Exceptional { get; set; }
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(2000)] public string Rationale { get; set; } = "";
    [MaxLength(450)] public string RequestedBy { get; set; } = "";
    [MaxLength(450)] public string? ApprovedBy { get; set; }
    public bool ResourcesSettled { get; set; }
    public bool EpidemiologyReviewed { get; set; }
    public bool FieldReviewed { get; set; }
}
public class AfterActionReview : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(4000)] public string Expected { get; set; } = "";
    [MaxLength(4000)] public string Actual { get; set; } = "";
    [MaxLength(4000)] public string WentWell { get; set; } = "";
    [MaxLength(4000)] public string Improvements { get; set; } = "";
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
}
public class ImprovementAction : Record
{
    public long ReviewId { get; set; }
    public AfterActionReview Review { get; set; } = null!;
    [MaxLength(2000)] public string Action { get; set; } = "";
    [MaxLength(450)] public string Owner { get; set; } = "";
    public DateTime DueAt { get; set; }
    public int Progress { get; set; }
    [MaxLength(2000)] public string Evidence { get; set; } = "";
}
