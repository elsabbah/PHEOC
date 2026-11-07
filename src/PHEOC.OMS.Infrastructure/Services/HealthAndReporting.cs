using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;

public partial class OmsService
{
    public async Task<HealthBoard> Health()
    {
        Demand(Permissions.SensitiveView);
        var contacts=await db.Contacts.Include(x=>x.Event).Include(x=>x.FollowUps).Where(x=>user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        var samples=await db.Samples.Include(x=>x.Event).Include(x=>x.Transfers).Include(x=>x.Results).Where(x=>user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        var events=await ScopedEvents().Include(x=>x.Governorate).Where(x=>x.Status!=EventStatus.Closed).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        return new(contacts.Select(x=>new HealthRow(x.Id,x.EventId,x.Event.Title,x.Code,x.RiskLevel,$"متابعة حتى {x.FollowUpUntil:yyyy-MM-dd} — "+string.Join("؛ ",x.FollowUps.OrderBy(f=>f.Id).Select(f=>f.Outcome)),x.LastExposureAt)).ToList(),samples.Select(x=>new HealthRow(x.Id,x.EventId,x.Event.Title,x.Code,x.Type,string.Join("؛ ",x.Transfers.OrderBy(t=>t.Id).Select(t=>t.From+" ← "+t.To))+" | النتائج: "+string.Join("؛ ",x.Results.OrderBy(r=>r.Id).Select(r=>r.Result)),x.CollectedAt)).ToList(),events.Select(Row).ToList());
    }
    public async Task AddContact(ContactInput input)
    {
        Demand(Permissions.HealthManage); Validate(input); await Open(input.EventId);
        Require(input.LastExposureAt<=DateTime.UtcNow&&input.FollowUpUntil>=input.LastExposureAt,"تواريخ تعرض أو متابعة غير صحيحة.");
        db.Contacts.Add(new(){EventId=input.EventId,Code=input.Code,LastExposureAt=Utc(input.LastExposureAt)!.Value,FollowUpUntil=Utc(input.FollowUpUntil)!.Value,RiskLevel=input.RiskLevel}); Timeline(await Open(input.EventId),"ContactRegistered","تسجيل مخالط برمز تشغيلي"); await db.SaveChangesAsync();
    }
    public async Task FollowContact(long id,NoteInput input)
    {
        Demand(Permissions.HealthManage); Validate(input); var c=await db.Contacts.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); await Open(c.EventId);
        db.ContactFollowUps.Add(new(){ContactId=id,Outcome=input.Text,RecordedBy=user.Id});Timeline(await Open(c.EventId),"ContactFollowUp","تحديث متابعة مخالط"); await db.SaveChangesAsync();
    }
    public async Task AddSample(SampleInput input)
    {
        Demand(Permissions.HealthManage); Validate(input); await Open(input.EventId); Require(input.CollectedAt<=DateTime.UtcNow,"تاريخ جمع العينة في المستقبل.");
        db.Samples.Add(new(){EventId=input.EventId,Code=input.Code,Type=input.Type,CollectedAt=Utc(input.CollectedAt)!.Value});Timeline(await Open(input.EventId),"SampleRegistered","تسجيل عينة"); await db.SaveChangesAsync();
    }
    public async Task TransferSample(long id,TransferInput input)
    {
        Demand(Permissions.HealthManage); Validate(input); var s=await db.Samples.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); await Open(s.EventId);
        db.SampleTransfers.Add(new(){SampleId=id,From=input.From,To=input.To,RecordedBy=user.Id});Timeline(await Open(s.EventId),"SampleTransferred","نقل عينة"); await db.SaveChangesAsync();
    }
    public async Task SampleResult(long id,NoteInput input)
    {
        Demand(Permissions.HealthManage); Validate(input); var s=await db.Samples.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); await Open(s.EventId);
        db.LaboratoryResults.Add(new(){SampleId=id,Result=input.Text,RecordedBy=user.Id});Timeline(await Open(s.EventId),"SampleResult","تسجيل نتيجة عينة"); await db.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<ReportRow>> Reports()
    {
        Demand(Permissions.ReportsView);
        return await db.SitRepReports.Where(x=>user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId).OrderByDescending(x=>x.Id).Take(200).Select(x=>new ReportRow(x.Id,x.EventId,x.Event.Title,x.Version,x.Summary,x.Challenges,x.NextActions,x.Snapshot,x.PreparedBy,x.ApprovedBy,x.CreatedAt)).ToListAsync();
    }
    public async Task<long> AddReport(ReportInput input)
    {
        Demand(Permissions.ReportsCreate); Validate(input); await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var e=await Load(input.EventId); var version=(await db.SitRepReports.Where(x=>x.EventId==e.Id).MaxAsync(x=>(int?)x.Version)??0)+1;
        var snapshot=JsonSerializer.Serialize(new{e.PublicId,e.Title,e.Status,e.CasesCount,e.DeathsCount,e.CreatedAt,OpenTasks=await db.Tasks.CountAsync(x=>x.EventId==e.Id&&x.State!=TaskState.Completed&&x.State!=TaskState.Cancelled),ActiveDeployments=await db.Deployments.CountAsync(x=>x.EventId==e.Id&&x.ReturnedAt==null)});
        var report=new SitRep{EventId=e.Id,Version=version,Summary=input.Summary,Challenges=input.Challenges,NextActions=input.NextActions,Snapshot=snapshot,PreparedBy=user.Id};
        db.SitRepReports.Add(report); Timeline(e,"SitRepCreated","تقرير موقفي إصدار "+version); await db.SaveChangesAsync(); if(tx!=null)await tx.CommitAsync(); return report.Id;
    }
    public async Task ApproveReport(long id)
    {
        Demand(Permissions.ReportsApprove); var r=await db.SitRepReports.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException(); var e=await Load(r.EventId);
        Require(r.ApprovedBy==null,"التقرير معتمد بالفعل."); Require(r.PreparedBy!=user.Id,"اعتماد التقرير يحتاج مستخدماً آخر غير المُعد.");r.ApprovedBy=user.Id;Timeline(e,"SitRepApproved","اعتماد تقرير #"+id); await db.SaveChangesAsync();
    }
    public async Task RequestClosure(long id,ClosureInput input)
    {
        Demand(Permissions.EventsClose);Validate(input);var e=await Open(id);
        var policy=await db.ClosurePolicies.OrderByDescending(x=>x.Version).FirstOrDefaultAsync();
        if(input.Exceptional){Demand(Permissions.CloseExceptional);Require(policy?.AllowExceptional==true,"السياسة الحالية لا تسمح بإغلاق استثنائي.");}
        else{
            Require(!(policy?.RequireResourcesSettled??true)||input.ResourcesSettled,"يلزم الإقرار بتسوية الموارد.");
            Require(!(policy?.RequireEpidemiologyReview??true)||input.EpidemiologyReviewed,"يلزم الإقرار بالمراجعة الوبائية.");
            Require(!(policy?.RequireFieldReview??true)||input.FieldReviewed,"يلزم الإقرار بمراجعة العمليات الميدانية.");
        }
        db.ClosureRequests.Add(new(){EventId=id,PolicyId=policy?.Id,Exceptional=input.Exceptional,Rationale=input.Rationale,RequestedBy=user.Id,ResourcesSettled=input.ResourcesSettled,EpidemiologyReviewed=input.EpidemiologyReviewed,FieldReviewed=input.FieldReviewed});Timeline(e,"ClosureRequested",input.Rationale+(input.Exceptional?" [استثنائي]":""));await db.SaveChangesAsync();
    }
    public async Task ApproveClosure(long id,VersionInput input)
    {
        Demand(Permissions.EventsClose);Validate(input);await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var r=await db.ClosureRequests.Include(x=>x.Policy).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();var e=await Open(r.EventId);CheckVersion(r,input.Version);
        Require(r.ApprovedBy==null&&r.RequestedBy!=user.Id,"الاعتماد يحتاج مستخدماً آخر وطلباً غير معتمد.");
        if(r.Exceptional)Demand(Permissions.CloseExceptional);
        if(!r.Exceptional&&(r.Policy?.RequireCompletedTasks??true))Require(!await db.Tasks.AnyAsync(x=>x.EventId==e.Id&&x.State!=TaskState.Completed&&x.State!=TaskState.Cancelled),"توجد مهام مفتوحة.");
        Require(!await db.Deployments.AnyAsync(x=>x.EventId==e.Id&&x.ReturnedAt==null),"توجد فرق لم تعد من المهمة.");
        Require(!await db.VehicleAssignments.AnyAsync(x=>x.Deployment.EventId==e.Id&&x.ReturnedAt==null),"توجد مركبات لم تعد من المهمة.");
        if(!r.Exceptional&&(r.Policy?.RequireResourcesSettled??true))Require(!await db.SupplyRequests.AnyAsync(x=>x.EventId==e.Id&&x.ApprovedBy==null),"توجد طلبات إمداد لم تُسوَّ بعد.");
        if(!r.Exceptional&&(r.Policy?.RequireApprovedReport??true))Require(await db.SitRepReports.AnyAsync(x=>x.EventId==e.Id&&x.ApprovedBy!=null),"يلزم تقرير موقفي معتمد قبل الإغلاق.");
        r.ApprovedBy=user.Id;e.Status=EventStatus.Closed;e.ClosedAt=DateTime.UtcNow;Timeline(e,"Closed",r.Rationale);await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
    public async Task AddAar(long id,AarInput input)
    {
        Demand(Permissions.EventsClose);Validate(input);var e=await Load(id);Require(e.Status==EventStatus.Closed,"المراجعة بعد إغلاق الحدث.");
        db.AARReviews.Add(new(){EventId=id,Expected=input.Expected,Actual=input.Actual,WentWell=input.WentWell,Improvements=input.Improvements,RecordedBy=user.Id});Timeline(e,"AAR","تسجيل المراجعة بعد الحدث");await db.SaveChangesAsync();
    }
    public async Task AddImprovement(long reviewId,ImprovementInput input)
    {
        Demand(Permissions.EventsClose);Validate(input);var r=await db.AARReviews.SingleOrDefaultAsync(x=>x.Id==reviewId)??throw new KeyNotFoundException();await Load(r.EventId);Require(input.DueAt>DateTime.UtcNow,"أدخل موعداً مستقبلياً.");
        db.ImprovementActions.Add(new(){ReviewId=reviewId,Action=input.Action,Owner=input.Owner,DueAt=Utc(input.DueAt)!.Value});await db.SaveChangesAsync();
    }
    public async Task UpdateImprovement(long id,ImprovementUpdate input)
    {
        Demand(Permissions.EventsClose);Validate(input);var a=await db.ImprovementActions.Include(x=>x.Review).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();await Load(a.Review.EventId);CheckVersion(a,input.Version);a.Progress=input.Progress;a.Evidence=input.Evidence;await db.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<ActivityRow>> Improvements()
    {
        Demand(Permissions.EventsView);var rows=await db.ImprovementActions.Where(x=>user.GovernorateId==null||x.Review.Event.GovernorateId==user.GovernorateId).OrderBy(x=>x.DueAt).Take(200).ToListAsync();
        return rows.Select(x=>new ActivityRow(x.Id,"توصية #"+x.ReviewId,x.Action+" | "+x.Evidence,x.Owner,x.DueAt,x.Progress+"%",Version(x))).ToList();
    }
    public async Task<IReadOnlyList<MetricsRow>> Metrics()
    {
        Demand(Permissions.ReportsView);var events=await ScopedEvents().Where(x=>!x.IsTraining).OrderByDescending(x=>x.Id).Take(500).ToListAsync();
        return events.Select(x=>new MetricsRow(x.Id,x.Title,ElapsedDays(x.OccurredAt,x.DetectedAt),ElapsedDays(x.DetectedAt,x.NotifiedAt),ElapsedDays(x.NotifiedAt,x.FirstResponseAt))).ToList();
    }
    public async Task<IReadOnlyList<ActivityRow>> Audit()
    {
        Demand(Permissions.AuditView);if(user.GovernorateId.HasValue)throw new AccessDeniedException();
        return await db.AuditLogs.OrderByDescending(x=>x.Id).Take(300).Select(x=>new ActivityRow(x.Id,x.Action,x.Entity+" #"+x.EntityId,x.Actor,x.RecordedAt,"","")).ToListAsync();
    }
    public async Task<IntegrationBoard> Integration()
    {
        Demand(Permissions.AdminSettings);if(user.GovernorateId.HasValue)throw new AccessDeniedException();
        var inbox=await db.InboxMessages.OrderByDescending(x=>x.Id).Take(100).Select(x=>new IntegrationRow(x.Id,x.ExternalEventId,x.EventId,x.ReceivedAt)).ToListAsync();
        var outbox=await db.OutboxMessages.OrderByDescending(x=>x.Id).Take(100).Select(x=>new ActivityRow(x.Id,"Outbox",x.Payload,"system",x.CreatedAt,x.Status+" / "+x.Attempts,"")).ToListAsync();return new(inbox,outbox);
    }
}
