using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Operations;
public class EventMessage : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(2000)] public string Text { get; set; } = "";
    [MaxLength(450)] public string Sender { get; set; } = "";
}
public class Notification : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(1000)] public string Text { get; set; } = "";
    [MaxLength(40)] public string Severity { get; set; } = "Information";
}
public class NotificationRead
{
    public long NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    [MaxLength(450)] public string UserId { get; set; } = "";
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
public class SlaDefinition : Record
{
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Stage { get; set; } = "";
    public int Version { get; set; }
    public int TargetMinutes { get; set; }
    public int EscalateEveryMinutes { get; set; }
    [MaxLength(450)] public string ApprovedBy { get; set; } = "";
}
public class EventSla : Record
{
    public DateTime? LastEvaluatedAt { get; set; }
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public long DefinitionId { get; set; }
    public SlaDefinition Definition { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool? Compliant { get; set; }
}
public class SlaEscalation : Record
{
    public long EventSlaId { get; set; }
    public EventSla EventSla { get; set; } = null!;
    public int Level { get; set; }
}
public class Attachment : Record
{
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(240)] public string OriginalName { get; set; } = "";
    [MaxLength(100)] public string StorageKey { get; set; } = "";
    [MaxLength(80)] public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    [MaxLength(64)] public string Sha256 { get; set; } = "";
    [MaxLength(450)] public string UploadedBy { get; set; } = "";
}
