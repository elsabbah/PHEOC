using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;

public partial class OmsService(OmsDbContext db, ICurrentUser user, Microsoft.Extensions.Configuration.IConfiguration? config = null, IEgyptEbsKpiDateSource? kpiSource = null) : IOmsService, ICoordinationService, IAnalyticsService, IPolicyService, ILogisticsService
{
    private void Demand(string permission) { if (!user.Can(permission)) throw new AccessDeniedException(); }
    private void Scope(int governorate) { if(user.GovernorateId.HasValue && user.GovernorateId != governorate) throw new AccessDeniedException(); }
    private static void Validate(object input) => Validator.ValidateObject(input, new ValidationContext(input), true);
    private IQueryable<EmergencyEvent> ScopedEvents() => db.Events.Where(x=>user.GovernorateId == null || x.GovernorateId == user.GovernorateId);
    private async Task<EmergencyEvent> Load(long id) => await ScopedEvents().SingleOrDefaultAsync(x=>x.Id == id) ?? throw new KeyNotFoundException("الحدث غير موجود أو خارج نطاق صلاحيتك.");
    private async Task<EmergencyEvent> Open(long id) { var e=await Load(id); Require(e.Status != EventStatus.Closed,"الحدث مغلق."); return e; }
    private static string Version(Record entity) => Convert.ToBase64String(entity.RowVersion);
    private void CheckVersion(Record entity, string version)
    {
        byte[] supplied;
        try { supplied = Convert.FromBase64String(version); } catch(FormatException) { throw new BusinessRuleException("رمز نسخة غير صالح."); }
        Require(supplied.Length > 0 && supplied.SequenceEqual(entity.RowVersion),"تم تعديل السجل بواسطة مستخدم آخر. أعد تحميل الصفحة.");
        db.Entry(entity).Property(x=>x.RowVersion).OriginalValue = supplied;
    }
    private void Timeline(EmergencyEvent e,string action,string text)
    {
        e.ModifiedAt=DateTime.UtcNow;
        db.EventTimeline.Add(new TimelineEntry { Event = e, Action=action, Summary=text, Actor=user.Id });
        if(action=="Received"&&!e.IsTraining)
            db.Notifications.Add(new Notification { Event=e, Severity=EventArrival.NotificationKind, Text="تم استقبال حدث جديد" });
        db.OutboxMessages.Add(new OutboxMessage { Event=e, Payload=JsonSerializer.Serialize(new { e.PublicId, Status=e.Status.ToString(), Action=action }) });
    }
    private static EventRow Row(EmergencyEvent e) => new(e.Id,e.PublicId,e.Title,e.Governorate?.Name??"",e.GovernorateId,e.EventType,e.ThreatType,e.Status,e.CasesCount,e.DeathsCount,e.CreatedAt,e.Latitude,e.Longitude,e.IsTraining);
    public async Task<IReadOnlyList<Lookup>> Governorates()
    {
        Demand(Permissions.EventsView);
        return await db.Governorates.Where(x=>user.GovernorateId==null || x.Id==user.GovernorateId).OrderBy(x=>x.Id).Select(x=>new Lookup(x.Id,x.Name)).ToListAsync();
    }
    public async Task<DashboardModel> Dashboard(int? governorate=null,string? query=null,EventStatus? status=null,int page=1)
    {
        Demand(Permissions.EventsView);
        var q=ScopedEvents(); if(governorate.HasValue) q=q.Where(x=>x.GovernorateId==governorate);
        if(!string.IsNullOrWhiteSpace(query)) q=q.Where(x=>x.Title.Contains(query)||x.EventType.Contains(query));
        if(status.HasValue) q=q.Where(x=>x.Status==status);
        var operational=q.Where(x=>!x.IsTraining);
        var active=await operational.CountAsync(x=>x.Status!=EventStatus.Closed); var assessment=await operational.CountAsync(x=>x.Status==EventStatus.UnderAssessment);
        var activated=await operational.CountAsync(x=>x.Status==EventStatus.Activated||x.Status==EventStatus.ResponseOngoing); var closed=await operational.CountAsync(x=>x.Status==EventStatus.Closed);
        var ids=operational.Select(x=>x.Id);
        var tasks=db.Tasks.Where(t=>ids.Contains(t.EventId));
        var openTasks=await tasks.CountAsync(t=>t.State!=TaskState.Completed&&t.State!=TaskState.Cancelled);
        var overdue=await tasks.CountAsync(t=>t.State!=TaskState.Completed&&t.State!=TaskState.Cancelled&&t.DueAt<DateTime.UtcNow);
        var teams=db.Teams.Where(x=>user.GovernorateId==null||x.GovernorateId==user.GovernorateId);
        var ready=user.Can(Permissions.TeamsView)?await teams.CountAsync(t=>t.State==TeamState.Ready):0;
        var deployed=user.Can(Permissions.TeamsView)?await teams.CountAsync(t=>t.State==TeamState.Assigned||t.State==TeamState.OnSite):0;
        var low=user.Can(Permissions.InventoryView)?await db.InventoryBatches.CountAsync(x=>(user.GovernorateId==null||x.Warehouse.GovernorateId==user.GovernorateId)&&x.Balance<x.Item.Minimum):0;
        var events=await q.Include(x=>x.Governorate).OrderByDescending(x=>x.CreatedAt).Skip((Math.Clamp(page,1,100000)-1)*30).Take(30).ToListAsync();
        var recent=await db.EventTimeline.Where(x=>ids.Contains(x.EventId)).OrderByDescending(x=>x.Id).Take(10).Select(x=>new ActivityRow(x.Id,x.Action,x.Summary,x.Actor,x.RecordedAt,"","")).ToListAsync();
        return new(active,assessment,activated,closed,ready,deployed,openTasks,overdue,low,events.Select(Row).ToList(),await Governorates(),recent);
    }
    public async Task<long> CreateEvent(EventInput input)
    {
        Demand(Permissions.EventsCreate); Validate(input); Scope(input.GovernorateId);
        await ValidateEvent(input);
        var e=MapEvent(input); db.Events.Add(e); await SlaBinding.Bind(db,e); Timeline(e,"Received","تم تسجيل الحدث يدوياً"); await db.SaveChangesAsync(); return e.Id;
    }
    public static EmergencyEvent MapEvent(EventInput i) => new() { IsTraining=i.IsTraining,Title=i.Title.Trim(),EventType=i.EventType.Trim(),ThreatType=i.ThreatType.Trim(),GovernorateId=i.GovernorateId,Location=i.Location,Description=i.Description,CasesCount=i.CasesCount,DeathsCount=i.DeathsCount,Latitude=i.Latitude,Longitude=i.Longitude,OccurredAt=Utc(i.OccurredAt),DetectedAt=Utc(i.DetectedAt),NotifiedAt=Utc(i.NotifiedAt),VerifiedAt=Utc(i.VerifiedAt),ConfirmedAt=Utc(i.ConfirmedAt) };
    internal static DateTime? Utc(DateTime? value) => value.HasValue ? value.Value.Kind==DateTimeKind.Local?value.Value.ToUniversalTime():DateTime.SpecifyKind(value.Value,DateTimeKind.Utc) : null;
    private async Task ValidateEvent(EventInput i)
    {
        Require(await db.Governorates.AnyAsync(x=>x.Id==i.GovernorateId),"المحافظة غير موجودة.");
        Require(i.Latitude.HasValue == i.Longitude.HasValue,"أدخل خط العرض وخط الطول معاً.");
        Require(!i.DeathsCount.HasValue||!i.CasesCount.HasValue||i.DeathsCount<=i.CasesCount,"الوفيات تتجاوز عدد الحالات.");
        var dates=new[]{i.OccurredAt,i.DetectedAt,i.NotifiedAt,i.VerifiedAt,i.ConfirmedAt}.Select(Utc);
        Require(dates.All(x=>x==null||x<=DateTime.UtcNow.AddMinutes(5)),"لا يمكن إدخال تاريخ مستقبلي للحدث.");
        if(i.OccurredAt.HasValue&&i.DetectedAt.HasValue) Require(Utc(i.DetectedAt)>=Utc(i.OccurredAt),"الاكتشاف يسبق بداية الحدث.");
        if(i.DetectedAt.HasValue&&i.NotifiedAt.HasValue) Require(Utc(i.NotifiedAt)>=Utc(i.DetectedAt),"الإخطار يسبق الاكتشاف.");
    }
    public async Task<EventDetails> Event(long id)
    {
        Demand(Permissions.EventsView); var e=await Load(id); await db.Entry(e).Reference(x=>x.Governorate).LoadAsync();
        var timeline=await db.EventTimeline.Where(x=>x.EventId==id).OrderByDescending(x=>x.Id).Select(x=>new ActivityRow(x.Id,x.Action,x.Summary,x.Actor,x.RecordedAt,"","")).ToListAsync();
        var assignments=await db.IncidentAssignments.Where(x=>x.EventId==id&&x.EndedAt==null).OrderBy(x=>x.Id).Select(x=>new ActivityRow(x.Id,x.Position,x.PersonName,x.Organization,x.CreatedAt,"","")).ToListAsync();
        var decisions=await db.IncidentDecisions.Where(x=>x.EventId==id).OrderByDescending(x=>x.Id).Select(x=>new ActivityRow(x.Id,"قرار",x.Text,x.DecidedBy,x.CreatedAt,x.CorrectsDecisionId.HasValue?"تصحيح #"+x.CorrectsDecisionId:"أصلي","")).ToListAsync();
        var tasks=await db.Tasks.Where(x=>x.EventId==id).OrderByDescending(x=>x.Id).ToListAsync();
        var deployments=user.Can(Permissions.TeamsView)?await DeploymentRows(db.Deployments.Where(x=>x.EventId==id)):[];
        var closures=await db.ClosureRequests.Where(x=>x.EventId==id).OrderByDescending(x=>x.Id).ToListAsync();
        var reviews=await db.AARReviews.Where(x=>x.EventId==id).OrderByDescending(x=>x.Id).Select(x=>new ActivityRow(x.Id,"AAR",x.Improvements,x.RecordedBy,x.CreatedAt,"","")).ToListAsync();
        var teams=user.Can(Permissions.TeamsView)?await db.Teams.Where(x=>x.State==TeamState.Ready&&(user.GovernorateId==null||x.GovernorateId==user.GovernorateId)).Select(x=>new Lookup((int)x.Id,x.Name)).ToListAsync():[];
        return new(Row(e),e.Description,e.Location,Version(e),timeline,[],assignments,decisions,tasks.Select(x=>new TaskRow(x.Id,x.Title,x.AssignedTo,x.DueAt,x.State,x.Progress,Version(x))).ToList(),deployments,closures.Select(x=>new ClosureRow(x.Id,x.Rationale,x.RequestedBy,x.ApprovedBy,Version(x))).ToList(),reviews,[],teams,await QiraSummary(id));
    }
    public async Task ChangeStatus(long id,StatusInput input)
    {
        Demand(Permissions.EventsUpdate); Validate(input); var e=await Open(id); CheckVersion(e,input.Version);
        Require(input.Status is EventStatus.UnderAssessment or EventStatus.Monitoring or EventStatus.ResponseOngoing,"استخدم إجراء التفعيل أو الإغلاق المخصص.");
        Transition(e.Status,input.Status); if(input.Status==EventStatus.Monitoring) Require(await db.QiraAssessments.AnyAsync(x=>x.EventId==id&&x.State==PHEOC.OMS.Domain.Qira.QiraState.Approved),"يلزم تقييم QIRA معتمد قبل التحويل للمراقبة.");
        e.Status=input.Status; e.AcknowledgedAt??=DateTime.UtcNow; if(input.Status==EventStatus.ResponseOngoing) e.FirstResponseAt??=DateTime.UtcNow;
        Timeline(e,input.Status.ToString(),input.Reason); await db.SaveChangesAsync();
    }
    public async Task Activate(long id,ActivationInput input)
    {
        Demand(Permissions.EventsActivate); Validate(input); var e=await Open(id); CheckVersion(e,input.Version); Transition(e.Status,EventStatus.Activated);
        Require(await db.QiraAssessments.AnyAsync(x=>x.EventId==id&&x.State==PHEOC.OMS.Domain.Qira.QiraState.Approved),"يلزم تقييم QIRA معتمد قبل التفعيل.");
        db.ActivationDecisions.Add(new(){EventId=id,Level=input.Level,Rationale=input.Rationale,ApprovedBy=user.Id});
        e.Status=EventStatus.Activated; Timeline(e,"Activated",$"تفعيل المستوى {input.Level}: {input.Rationale}"); await db.SaveChangesAsync();
    }
    public async Task Assign(long id,AssignmentInput input)
    {
        Demand(Permissions.EventsUpdate); Validate(input); var e=await Open(id);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var previous=await db.IncidentAssignments.Where(x=>x.EventId==id&&x.Position==input.Position&&x.EndedAt==null).ToListAsync();
        foreach(var item in previous) item.EndedAt=DateTime.UtcNow;
        db.IncidentAssignments.Add(new(){EventId=id,Position=input.Position,PersonName=input.PersonName,Organization=input.Organization});
        Timeline(e,"Assignment",$"تعيين {input.Position}: {input.PersonName}"); await db.SaveChangesAsync(); if(tx!=null)await tx.CommitAsync();
    }
    public async Task AddTask(long id,TaskInput input)
    {
        Demand(Permissions.EventsUpdate); Validate(input); var e=await Open(id); Require(input.DueAt>DateTime.UtcNow,"موعد الاستحقاق يجب أن يكون في المستقبل.");
        db.Tasks.Add(new(){EventId=id,Title=input.Title,AssignedTo=input.AssignedTo,DueAt=Utc(input.DueAt)!.Value,State=TaskState.Assigned});
        Timeline(e,"TaskCreated",input.Title); await db.SaveChangesAsync();
    }
    public async Task UpdateTask(long id,TaskUpdate input)
    {
        Demand(Permissions.EventsUpdate); Validate(input); Require(Enum.IsDefined(input.State),"حالة مهمة غير صالحة.");
        var task=await db.Tasks.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); var e=await Open(task.EventId); CheckVersion(task,input.Version);
        Require(input.State!=TaskState.Completed||input.Progress==100,"إتمام المهمة يتطلب نسبة 100%.");
        task.State=input.State; task.Progress=input.Progress; Timeline(e,"TaskUpdated",task.Title+": "+task.State); await db.SaveChangesAsync();
    }
    public async Task Decide(long id,DecisionInput input)
    {
        Demand(Permissions.EventsUpdate); Validate(input); var e=await Open(id);
        if(input.CorrectsDecisionId.HasValue) Require(await db.IncidentDecisions.AnyAsync(x=>x.Id==input.CorrectsDecisionId&&x.EventId==id),"القرار الأصلي لا يخص هذا الحدث.");
        db.IncidentDecisions.Add(new(){EventId=id,Text=input.Text,DecidedBy=user.Id,CorrectsDecisionId=input.CorrectsDecisionId});
        Timeline(e,"Decision",input.CorrectsDecisionId.HasValue?"قيد تصحيح قرار":"تسجيل قرار"); await db.SaveChangesAsync();
    }
}
