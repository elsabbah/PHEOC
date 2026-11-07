using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Qira;
namespace PHEOC.OMS.Infrastructure.Data;

public class AppUser : IdentityUser { public int? GovernorateId { get; set; } }
public class OmsDbContext(DbContextOptions<OmsDbContext> options, ICurrentUser? actor = null) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<EmergencyEvent> Events => Set<EmergencyEvent>();
    public DbSet<TimelineEntry> EventTimeline => Set<TimelineEntry>();
    public DbSet<AuditEntry> AuditLogs => Set<AuditEntry>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<RiskPolicy> RiskPolicies => Set<RiskPolicy>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<ActivationDecision> ActivationDecisions => Set<ActivationDecision>();
    public DbSet<IncidentAssignment> IncidentAssignments => Set<IncidentAssignment>();
    public DbSet<OperationalTask> Tasks => Set<OperationalTask>();
    public DbSet<IncidentDecision> IncidentDecisions => Set<IncidentDecision>();
    public DbSet<Person> Personnel => Set<Person>();
    public DbSet<ResponseTeam> Teams => Set<ResponseTeam>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAssignment> VehicleAssignments => Set<VehicleAssignment>();
    public DbSet<FieldReport> FieldReports => Set<FieldReport>();
    public DbSet<ContactRecord> Contacts => Set<ContactRecord>();
    public DbSet<ContactFollowUp> ContactFollowUps => Set<ContactFollowUp>();
    public DbSet<Sample> Samples => Set<Sample>();
    public DbSet<SampleTransfer> SampleTransfers => Set<SampleTransfer>();
    public DbSet<LaboratoryResult> LaboratoryResults => Set<LaboratoryResult>();
    public DbSet<SitRep> SitRepReports => Set<SitRep>();
    public DbSet<ClosureRequest> ClosureRequests => Set<ClosureRequest>();
    public DbSet<ClosurePolicy> ClosurePolicies => Set<ClosurePolicy>();
    public DbSet<AfterActionReview> AARReviews => Set<AfterActionReview>();
    public DbSet<ImprovementAction> ImprovementActions => Set<ImprovementAction>();
    public DbSet<EventMessage> EventMessages => Set<EventMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRead> NotificationReads => Set<NotificationRead>();
    public DbSet<SlaDefinition> SlaDefinitions => Set<SlaDefinition>();
    public DbSet<EventSla> EventSlas => Set<EventSla>();
    public DbSet<SlaEscalation> SlaEscalations => Set<SlaEscalation>();
    public DbSet<Attachment> EventAttachments => Set<Attachment>();
    public DbSet<KpiDateRecord> KpiDateRecords => Set<KpiDateRecord>();
    public DbSet<EarlyResponseMilestone> EarlyResponseMilestones => Set<EarlyResponseMilestone>();
    public DbSet<EbsSyncState> EbsSyncStates => Set<EbsSyncState>();
    public DbSet<QiraDefinition> QiraDefinitions=>Set<QiraDefinition>();
    public DbSet<QiraNationalPolicy> QiraNationalPolicies=>Set<QiraNationalPolicy>();
    public DbSet<QiraAssessment> QiraAssessments=>Set<QiraAssessment>();
    public DbSet<QiraAnswer> QiraAnswers=>Set<QiraAnswer>();
    public DbSet<QiraHistory> QiraHistories=>Set<QiraHistory>();
    public DbSet<QiraAction> QiraActions=>Set<QiraAction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<QiraDefinition>().HasIndex(x=>x.Code).IsUnique();
        b.Entity<QiraNationalPolicy>().HasIndex(x=>x.Version).IsUnique();
        b.Entity<QiraAssessment>().HasIndex(x=>new{x.EventId,x.Revision}).IsUnique();
        b.Entity<QiraAssessment>().HasOne<QiraAssessment>().WithMany().HasForeignKey(x=>x.PreviousId);
        b.Entity<QiraAnswer>().HasIndex(x=>new{x.AssessmentId,x.Node}).IsUnique();
        b.Entity<QiraAnswer>().HasOne<Attachment>().WithMany().HasForeignKey(x=>x.AttachmentId);
        b.Entity<QiraAction>().HasOne<OperationalTask>().WithMany().HasForeignKey(x=>x.TaskId);
        b.Entity<EmergencyEvent>().HasIndex(x => x.PublicId).IsUnique();
        b.Entity<EmergencyEvent>().HasIndex(x => new { x.GovernorateId, x.Status, x.CreatedAt });
        b.Entity<TimelineEntry>().HasIndex(x => new { x.EventId, x.RecordedAt });
        b.Entity<InboxMessage>().HasIndex(x => new { x.Source, x.ExternalEventId }).IsUnique();
        b.Entity<InboxMessage>().HasIndex(x => new { x.Source, x.IdempotencyKey }).IsUnique();
        b.Entity<OutboxMessage>().HasIndex(x => x.MessageId).IsUnique();
        b.Entity<OutboxMessage>().HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.Entity<Person>().HasIndex(x => x.EmployeeNumber).IsUnique();
        b.Entity<Vehicle>().HasIndex(x=>x.PlateNumber).IsUnique();
        b.Entity<EbsSyncState>().HasIndex(x=>x.ConnectorCode).IsUnique();
        b.Entity<FieldReport>().HasIndex(x=>x.ClientRequestId).IsUnique().HasFilter("[ClientRequestId] IS NOT NULL");
        b.Entity<TeamMember>().HasKey(x => new { x.TeamId, x.PersonId });
        b.Entity<NotificationRead>().HasKey(x=>new{x.NotificationId,x.UserId});
        b.Entity<SlaDefinition>().HasIndex(x=>new{x.Stage,x.Version}).IsUnique();
        b.Entity<EventSla>().HasIndex(x=>new{x.EventId,x.DefinitionId}).IsUnique();
        b.Entity<SlaEscalation>().HasIndex(x=>new{x.EventSlaId,x.Level}).IsUnique();
        b.Entity<Attachment>().HasIndex(x=>x.StorageKey).IsUnique();
        b.Entity<KpiDateRecord>().HasIndex(x=>new{x.EventId,x.Id});
        b.Entity<EarlyResponseMilestone>().HasIndex(x=>new{x.EventId,x.Action,x.Id});
        b.Entity<ContactRecord>().HasIndex(x => new { x.EventId, x.Code }).IsUnique();
        b.Entity<Sample>().HasIndex(x => x.Code).IsUnique();
        b.Entity<SitRep>().HasIndex(x => new { x.EventId, x.Version }).IsUnique();
        b.Entity<RiskPolicy>().HasIndex(x => x.Version).IsUnique();
        b.Entity<InventoryBatch>().HasIndex(x => new { x.WarehouseId, x.ItemId, x.BatchNumber }).IsUnique();
        foreach (var type in b.Model.GetEntityTypes())
        {
            foreach (var fk in type.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
            foreach (var property in type.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                { property.SetPrecision(property.Name is "Latitude" or "Longitude" ? 9 : 18); property.SetScale(property.Name is "Latitude" or "Longitude" ? 6 : 3); }
            }
        }
        b.Entity<EmergencyEvent>().ToTable("Events", t => { t.HasCheckConstraint("CK_Events_Counts", "([CasesCount] IS NULL OR [CasesCount]>=0) AND ([DeathsCount] IS NULL OR [DeathsCount]>=0)"); t.HasCheckConstraint("CK_Events_Coordinates", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)"); });
        b.Entity<InventoryBatch>().ToTable("InventoryBatches",t => t.HasCheckConstraint("CK_Inventory_Balance", "[Balance]>=0"));
        b.Entity<OperationalTask>().ToTable("Tasks",t=>t.HasCheckConstraint("CK_Tasks_Progress","[Progress] BETWEEN 0 AND 100"));
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        foreach(var entry in ChangeTracker.Entries<QiraAssessment>().Where(e=>e.State==EntityState.Modified))
        {
            var before=entry.OriginalValues.GetValue<QiraState>(nameof(QiraAssessment.State));
            if(before>=QiraState.Completed)
            {
                var allowed=new[]{nameof(QiraAssessment.State),nameof(QiraAssessment.ReviewedAt),nameof(QiraAssessment.ReviewedBy),nameof(QiraAssessment.ReviewNote),nameof(QiraAssessment.ApprovedAt),nameof(QiraAssessment.ApprovedBy),nameof(QiraAssessment.ApprovalNote),nameof(Record.ModifiedAt)};
                if(entry.Properties.Any(p=>p.IsModified&&!allowed.Contains(p.Metadata.Name)))throw new InvalidOperationException("نتيجة QIRA مكتملة ومحفوظة؛ أنشئ إعادة تقييم بإصدار جديد.");
            }
        }
        foreach(var entry in ChangeTracker.Entries<QiraAnswer>().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var parent=entry.Entity.Assessment??await QiraAssessments.FindAsync([entry.Entity.AssessmentId],cancellationToken);
            if(parent==null||parent.State>=QiraState.Completed)throw new InvalidOperationException("إجابات QIRA المكتملة لا تعدل.");
        }
        var immutable = new[] { typeof(QiraDefinition), typeof(QiraNationalPolicy), typeof(QiraHistory), typeof(AuditEntry), typeof(TimelineEntry), typeof(IncidentDecision), typeof(InventoryTransaction), typeof(RiskAssessment), typeof(RiskPolicy), typeof(ActivationDecision), typeof(AfterActionReview), typeof(ClosureRequest), typeof(LaboratoryResult), typeof(SampleTransfer), typeof(EventMessage), typeof(KpiDateRecord), typeof(EarlyResponseMilestone), typeof(ClosurePolicy), typeof(SlaDefinition), typeof(SlaEscalation), typeof(InboxMessage) };
        var changes = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        foreach (var e in changes)
        {
            if (immutable.Contains(e.Entity.GetType()) && e.State is EntityState.Modified or EntityState.Deleted)
            {
                // Closure approvals are the only permitted update on an existing closure request.
                if (e.Entity is ClosureRequest && e.State == EntityState.Modified && e.Properties.Where(p=>p.IsModified).All(p=>p.Metadata.Name == nameof(ClosureRequest.ApprovedBy))) continue;
                throw new InvalidOperationException("السجل محفوظ للتدقيق؛ أنشئ قيد تصحيح جديداً.");
            }
        }
        await using var tx = Database.CurrentTransaction == null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var states = changes.Where(e=>e.Entity is not AuditEntry && !e.Metadata.ClrType.Name.StartsWith("Identity", StringComparison.Ordinal)).Select(e=>(Entry:e, Action:e.State.ToString())).ToList();
        var result = await base.SaveChangesAsync(cancellationToken);
        foreach(var (entry, action) in states)
        {
            var key = string.Join(",", entry.Properties.Where(p=>p.Metadata.IsPrimaryKey()).Select(p=>p.CurrentValue?.ToString()));
            AuditLogs.Add(new AuditEntry { Actor = actor?.Id ?? "system", Action = action, Entity = entry.Metadata.ClrType.Name, EntityId = key });
        }
        if(states.Count > 0) await base.SaveChangesAsync(cancellationToken);
        if(tx != null) await tx.CommitAsync(cancellationToken);
        return result;
    }
}
