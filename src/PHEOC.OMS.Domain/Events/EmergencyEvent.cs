using System.ComponentModel.DataAnnotations;

namespace PHEOC.OMS.Domain.Events;

public enum EventStatus { Received, UnderAssessment, Monitoring, Activated, ResponseOngoing, Closed }
public enum TaskState { New, Assigned, InProgress, Pending, Completed, Cancelled }
public enum TeamState { Ready, Assigned, Mobilizing, EnRoute, OnSite, Returning, Resting, Unavailable }

public abstract class Record
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public class Governorate { public int Id { get; set; } [MaxLength(100)] public string Name { get; set; } = ""; }
public class EmergencyEvent : Record
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public bool IsTraining { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(80)] public string EventType { get; set; } = "";
    [MaxLength(80)] public string ThreatType { get; set; } = "";
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = null!;
    [MaxLength(500)] public string Location { get; set; } = "";
    [MaxLength(4000)] public string Description { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? CasesCount { get; set; }
    public int? DeathsCount { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Received;
    public DateTime? OccurredAt { get; set; }
    public DateTime? DetectedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<TimelineEntry> Timeline { get; set; } = [];
}
public class TimelineEntry
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    [MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(2000)] public string Summary { get; set; } = "";
    [MaxLength(450)] public string Actor { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
public class AuditEntry
{
    public long Id { get; set; }
    [MaxLength(450)] public string Actor { get; set; } = "";
    [MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(100)] public string Entity { get; set; } = "";
    [MaxLength(100)] public string EntityId { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
public class InboxMessage
{
    public long Id { get; set; }
    [MaxLength(64)] public string Source { get; set; } = "EBS";
    [MaxLength(128)] public string ExternalEventId { get; set; } = "";
    [MaxLength(128)] public string IdempotencyKey { get; set; } = "";
    [MaxLength(64)] public string PayloadHash { get; set; } = "";
    public string ProtectedPayload { get; set; } = "";
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
public class OutboxMessage : Record
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public long EventId { get; set; }
    public EmergencyEvent Event { get; set; } = null!;
    public string Payload { get; set; } = "";
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
