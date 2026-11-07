using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.Domain.Operations;
public class KpiDateRecord : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public DateOnly? Emergence { get; set; }
    public DateOnly? Detection { get; set; }
    public DateOnly? Notification { get; set; }
    public bool Excluded { get; set; }
    [MaxLength(2000)] public string Evidence { get; set; } = "";
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
    [MaxLength(100)] public string MethodologyVersion { get; set; } = SevenOneSeven.Version;
}
public class EarlyResponseMilestone : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public EarlyResponseAction Action { get; set; }
    public DateOnly? Date { get; set; }
    public bool NotApplicable { get; set; }
    [MaxLength(2000)] public string Evidence { get; set; } = "";
    [MaxLength(450)] public string RecordedBy { get; set; } = "";
}
