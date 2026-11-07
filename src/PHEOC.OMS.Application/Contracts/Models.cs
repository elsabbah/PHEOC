using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Application.Contracts;

public record Lookup(int Id, string Name);
public record EventRow(long Id, Guid PublicId, string Title, string Governorate, int GovernorateId, string EventType, string ThreatType, EventStatus Status, int? Cases, int? Deaths, DateTime ReceivedAt, decimal? Latitude, decimal? Longitude, bool IsTraining);
public record ActivityRow(long Id, string Type, string Text, string Actor, DateTime At, string State = "", string Version = "");
public record TaskRow(long Id, string Title, string AssignedTo, DateTime DueAt, TaskState State, int Progress, string Version);
public record DeploymentRow(long Id, long EventId, string EventTitle, string TeamName, string Mission, DateTime CreatedAt, DateTime? ArrivedAt, DateTime? ReturnedAt, string Version);
public record TeamRow(long Id, string Name, string Governorate, TeamState State, string Members, string Version);
public record PersonRow(long Id, string Name, string Specialty, string Organization, string EmployeeNumber, bool Available);
public record StockRow(long Id, string Warehouse, string Item, string Unit, string Batch, DateTime? ExpiresAt, decimal Balance, decimal Minimum, string Version);
public record StockMovement(long Id, string Item, decimal Quantity, string Reason, string Actor, DateTime At);
public record ReportRow(long Id, long EventId, string EventTitle, int Version, string Summary, string Challenges, string NextActions, string Snapshot, string PreparedBy, string? ApprovedBy, DateTime At);
public record HealthRow(long Id, long EventId, string EventTitle, string Code, string Type, string Details, DateTime At);
public record ClosureRow(long Id, string Rationale, string RequestedBy, string? ApprovedBy, string Version);
public record DashboardModel(int Active, int UnderAssessment, int Activated, int Closed, int ReadyTeams, int DeployedTeams, int OpenTasks, int OverdueTasks, int LowStock, IReadOnlyList<EventRow> Events, IReadOnlyList<Lookup> Governorates, IReadOnlyList<ActivityRow> Recent);
public record EventDetails(EventRow Event, string Description, string Location, string Version, IReadOnlyList<ActivityRow> Timeline, IReadOnlyList<ActivityRow> Assessments, IReadOnlyList<ActivityRow> Assignments, IReadOnlyList<ActivityRow> Decisions, IReadOnlyList<TaskRow> Tasks, IReadOnlyList<DeploymentRow> Deployments, IReadOnlyList<ClosureRow> Closures, IReadOnlyList<ActivityRow> Reviews, IReadOnlyList<Lookup> Policies, IReadOnlyList<Lookup> Teams, QiraEventSummary? Qira=null);
public record TeamBoard(IReadOnlyList<TeamRow> Teams, IReadOnlyList<PersonRow> People, IReadOnlyList<DeploymentRow> Deployments, IReadOnlyList<Lookup> Governorates);
public record InventoryBoard(IReadOnlyList<StockRow> Stock, IReadOnlyList<StockMovement> Movements, IReadOnlyList<Lookup> Warehouses, IReadOnlyList<Lookup> Items, IReadOnlyList<Lookup> Governorates);
public record HealthBoard(IReadOnlyList<HealthRow> Contacts, IReadOnlyList<HealthRow> Samples, IReadOnlyList<EventRow> Events);
public record FieldBoard(IReadOnlyList<DeploymentRow> Deployments, IReadOnlyList<ActivityRow> Reports);
public record MetricsRow(long Id, string Title, double? DetectionDays, double? NotificationDays, double? FirstActionDays);
public record UserRow(string Id, string Name, int? GovernorateId, string Roles, string Permissions,bool Disabled=false);
public record IntegrationRow(long Id, string ExternalId, long EventId, DateTime ReceivedAt);
public record IntegrationBoard(IReadOnlyList<IntegrationRow> Inbox, IReadOnlyList<ActivityRow> Outbox);

public class EventInput
{
    public bool IsTraining { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(80)] public string EventType { get; set; } = "";
    [Required, StringLength(80)] public string ThreatType { get; set; } = "";
    [Range(1, int.MaxValue)] public int GovernorateId { get; set; }
    [StringLength(500)] public string Location { get; set; } = "";
    [StringLength(4000)] public string Description { get; set; } = "";
    [Range(0, int.MaxValue)] public int? CasesCount { get; set; }
    [Range(0, int.MaxValue)] public int? DeathsCount { get; set; }
    [Range(-90,90)] public decimal? Latitude { get; set; }
    [Range(-180,180)] public decimal? Longitude { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime? DetectedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
public class VersionInput { [Required] public string Version { get; set; } = ""; }
public class StatusInput : VersionInput { public EventStatus Status { get; set; } [Required,StringLength(2000)] public string Reason { get; set; } = ""; }
public class ActivationInput : VersionInput { [Range(1,3)] public int Level { get; set; } [Required,StringLength(2000)] public string Rationale { get; set; } = ""; }
public class AssignmentInput { [Required,StringLength(120)] public string Position { get; set; } = ""; [Required,StringLength(160)] public string PersonName { get; set; } = ""; [Required,StringLength(160)] public string Organization { get; set; } = ""; }
public class TaskInput { [Required,StringLength(200)] public string Title { get; set; } = ""; [Required,StringLength(450)] public string AssignedTo { get; set; } = ""; public DateTime DueAt { get; set; } }
public class TaskUpdate : VersionInput { public TaskState State { get; set; } [Range(0,100)] public int Progress { get; set; } }
public class DecisionInput { [Required,StringLength(4000)] public string Text { get; set; } = ""; public long? CorrectsDecisionId { get; set; } }
public class PersonInput { [Required,StringLength(160)] public string Name { get; set; } = ""; [Required,StringLength(80)] public string EmployeeNumber { get; set; } = ""; [Required,StringLength(160)] public string Organization { get; set; } = ""; [Required,StringLength(120)] public string Specialty { get; set; } = ""; [StringLength(120)] public string Skills { get; set; } = ""; [StringLength(80)] public string Phone { get; set; } = ""; [Range(1,int.MaxValue)] public int GovernorateId { get; set; } }
public class TeamInput { [Required,StringLength(160)] public string Name { get; set; } = ""; [Range(1,int.MaxValue)] public int GovernorateId { get; set; } [MinLength(1)] public long[] PersonIds { get; set; } = []; public long LeaderId { get; set; } }
public class DeploymentInput { public long TeamId { get; set; } [Required,StringLength(2000)] public string Mission { get; set; } = ""; }
public class WarehouseInput { [Required,StringLength(160)] public string Name { get; set; } = ""; [Range(1,int.MaxValue)] public int GovernorateId { get; set; } }
public class ItemInput { [Required,StringLength(160)] public string Name { get; set; } = ""; [Required,StringLength(40)] public string Unit { get; set; } = ""; [Range(0,1000000000)] public decimal Minimum { get; set; } }
public class BatchInput { public long WarehouseId { get; set; } public long ItemId { get; set; } [Required,StringLength(80)] public string BatchNumber { get; set; } = ""; public DateTime? ExpiresAt { get; set; } [Range(0.001,1000000000)] public decimal Quantity { get; set; } }
public class MovementInput : VersionInput { [Range(-1000000000,1000000000)] public decimal Quantity { get; set; } public long? EventId { get; set; } [Required,StringLength(2000)] public string Reason { get; set; } = ""; }
public class FieldInput { public Guid ClientRequestId { get; set; } public long DeploymentId { get; set; } [Required,StringLength(4000)] public string Activities { get; set; } = ""; [StringLength(2000)] public string Needs { get; set; } = ""; [Range(-90,90)] public decimal? Latitude { get; set; } [Range(-180,180)] public decimal? Longitude { get; set; } }
public class ContactInput { public long EventId { get; set; } [Required,StringLength(80)] public string Code { get; set; } = ""; public DateTime LastExposureAt { get; set; } public DateTime FollowUpUntil { get; set; } [Required,StringLength(40)] public string RiskLevel { get; set; } = ""; }
public class NoteInput { [Required,StringLength(1000)] public string Text { get; set; } = ""; }
public class SampleInput { public long EventId { get; set; } [Required,StringLength(80)] public string Code { get; set; } = ""; [Required,StringLength(100)] public string Type { get; set; } = ""; public DateTime CollectedAt { get; set; } }
public class TransferInput { [Required,StringLength(160)] public string From { get; set; } = ""; [Required,StringLength(160)] public string To { get; set; } = ""; }
public class ReportInput { public long EventId { get; set; } [Required,StringLength(4000)] public string Summary { get; set; } = ""; [StringLength(4000)] public string Challenges { get; set; } = ""; [Required,StringLength(4000)] public string NextActions { get; set; } = ""; }
public class ClosureInput { public bool Exceptional { get; set; } [Required,StringLength(2000)] public string Rationale { get; set; } = ""; public bool ResourcesSettled { get; set; } public bool EpidemiologyReviewed { get; set; } public bool FieldReviewed { get; set; } }
public class AarInput { [Required,StringLength(4000)] public string Expected { get; set; } = ""; [Required,StringLength(4000)] public string Actual { get; set; } = ""; [Required,StringLength(4000)] public string WentWell { get; set; } = ""; [Required,StringLength(4000)] public string Improvements { get; set; } = ""; }
public class ImprovementInput { [Required,StringLength(2000)] public string Action { get; set; } = ""; [Required,StringLength(450)] public string Owner { get; set; } = ""; public DateTime DueAt { get; set; } }
public class ImprovementUpdate : VersionInput { [Range(0,100)] public int Progress { get; set; } [Required,StringLength(2000)] public string Evidence { get; set; } = ""; }
public class UserInput { [Required,StringLength(100)] public string UserName { get; set; } = ""; [Required,MinLength(12),StringLength(128)] public string Password { get; set; } = ""; public int? GovernorateId { get; set; } [MinLength(1)] public string[] Permissions { get; set; } = []; [Required] public string Role { get; set; } = ""; }

public interface IOmsService
{
    Task<IReadOnlyList<Lookup>> Governorates();
    Task<DashboardModel> Dashboard(int? governorate = null, string? query = null, EventStatus? status = null, int page = 1);
    Task<long> CreateEvent(EventInput input);
    Task<EventDetails> Event(long id);
    Task ChangeStatus(long id, StatusInput input);

    Task Activate(long id, ActivationInput input);
    Task Assign(long id, AssignmentInput input);
    Task AddTask(long id, TaskInput input);
    Task UpdateTask(long id, TaskUpdate input);
    Task Decide(long id, DecisionInput input);
    Task<TeamBoard> Teams();
    Task AddPerson(PersonInput input);
    Task AddTeam(TeamInput input);
    Task Deploy(long eventId, DeploymentInput input);
    Task DeploymentStatus(long id, VersionInput input, bool returned);
    Task<InventoryBoard> Inventory();
    Task AddWarehouse(WarehouseInput input);
    Task AddItem(ItemInput input);
    Task AddBatch(BatchInput input);
    Task MoveStock(long id, MovementInput input);
    Task<FieldBoard> Field();
    Task AddFieldReport(FieldInput input);
    Task<HealthBoard> Health();
    Task AddContact(ContactInput input);
    Task FollowContact(long id, NoteInput input);
    Task AddSample(SampleInput input);
    Task TransferSample(long id, TransferInput input);
    Task SampleResult(long id, NoteInput input);
    Task<IReadOnlyList<ReportRow>> Reports();
    Task<long> AddReport(ReportInput input);
    Task ApproveReport(long id);
    Task RequestClosure(long id, ClosureInput input);
    Task ApproveClosure(long id, VersionInput input);
    Task AddAar(long id, AarInput input);
    Task AddImprovement(long reviewId, ImprovementInput input);
    Task UpdateImprovement(long id, ImprovementUpdate input);
    Task<IReadOnlyList<ActivityRow>> Improvements();
    Task<IReadOnlyList<MetricsRow>> Metrics();
    Task<IReadOnlyList<ActivityRow>> Audit();

    Task<IntegrationBoard> Integration();
}
public class PasswordResetInput { [Required,MinLength(12),StringLength(128)]public string Password { get; set; }=""; }
public interface IUserAdministration { Task<IReadOnlyList<UserRow>> Users(); Task Create(UserInput input); Task Disable(string id,bool disabled); Task ResetPassword(string id,PasswordResetInput input); Task<UserPermissionEdit> EditPermissions(string id); Task UpdatePermissions(string id,UserPermissionsInput input); Task Delete(string id,UserAccountDeleteInput input); }
public record IntakeResult(long EventId, Guid PublicId, bool Duplicate);
public interface IEventIntake { Task<IntakeResult> Receive(string source, string key, string externalId, EventInput input); }
public interface IReportExporter { byte[] Excel(IReadOnlyList<ReportRow> reports); byte[] Word(ReportRow report); }
