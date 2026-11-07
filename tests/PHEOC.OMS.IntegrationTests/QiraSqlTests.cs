using System.Data;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Qira;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;
public class QiraSqlTests
{
    private sealed class Actor(string id="qa.qira",int? governorate=null,string? denied=null):ICurrentUser
    {public string Id=>id;public int? GovernorateId=>governorate;public bool Can(string permission)=>permission!=denied;}
    private static OmsDbContext Db(ICurrentUser actor)=>new(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),o=>o.UseCompatibilityLevel(130)).Options,actor);
    private static Task<long> Event(OmsService service)=>service.CreateEvent(new(){Title="[QA rollback] QIRA",EventType="training",ThreatType="QA",GovernorateId=1,IsTraining=true});
    private static async Task Scope(OmsService s,long id,bool novel=false)=>await s.SaveQiraScope(id,new(){Version=(await s.Qira(id)).Assessment.Version,Scope="QA scope",Population="QA population",TimeWindow="QA 7 days",Hazard="QA",NovelHazard=novel});
    private static async Task Answer(OmsService s,long id,string node,QiraChoice choice)=>await s.SaveQiraAnswer(id,new(){Version=(await s.Qira(id)).Assessment.Version,Node=node,Choice=choice,Notes="QA evidence reason",Source="QA source",Evidence="QA evidence"});
    public static async Task<long> Completed(OmsService s,long eventId)
    {
        var id=await s.StartQira(eventId);await Scope(s,id);await Answer(s,id,"Q1",QiraChoice.Unknown);await Answer(s,id,"Q2",QiraChoice.No);await Answer(s,id,"Q41",QiraChoice.No);
        await s.CompleteQira(id,new(){Version=(await s.Qira(id)).Assessment.Version,Confidence=QiraConfidence.Low,ConfidenceReason="QA limited evidence",Uncertainties="QA missing national list"});return id;
    }
    public static async Task<long> Approved(OmsService s,OmsService reviewer,long eventId)
    {
        var id=await Completed(s,eventId);await reviewer.ReviewQira(id,new(){Version=(await reviewer.Qira(id)).Assessment.Version,Note="QA independent review"});
        await reviewer.ApproveQira(id,new(){Version=(await reviewer.Qira(id)).Assessment.Version,Note="QA independent approval"});return id;
    }
    [SqlFact]public async Task DraftBranchesVersionsImmutabilityAndIndependentApprovalPersist()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var s=new OmsService(db,actor);var eid=await Event(s);var id=await s.StartQira(eid);
        Assert.Equal(id,await s.StartQira(eid));await Scope(s,id);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.CompleteQira(id,new(){Version=db.QiraAssessments.Local.Single(x=>x.Id==id).RowVersion is {} v?Convert.ToBase64String(v):"",Confidence=QiraConfidence.High,ConfidenceReason="QA"}));
        var stale=(await s.Qira(id)).Assessment.Version;await Answer(s,id,"Q1",QiraChoice.Unknown);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.SaveQiraAnswer(id,new(){Version=stale,Node="Q2",Choice=QiraChoice.Yes}));
        await Answer(s,id,"Q2",QiraChoice.Yes);await Answer(s,id,"Q3",QiraChoice.No);await Answer(s,id,"Q42-L",QiraChoice.Yes);
        await Answer(s,id,"Q2",QiraChoice.No);var changed=await s.Qira(id);Assert.Equal("Q41",changed.Evaluation.Next);Assert.Contains(changed.Answers,x=>x.Node=="Q3"&&!x.Active);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>Answer(s,id,"C52-LM",QiraChoice.Yes));
        await Answer(s,id,"Q41",QiraChoice.No);await s.CompleteQira(id,new(){Version=(await s.Qira(id)).Assessment.Version,Confidence=QiraConfidence.Low,ConfidenceReason="QA",Uncertainties="QA missing national list"});
        Assert.Equal(QiraRisk.VeryLow,(await s.Qira(id)).Assessment.Risk);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>Answer(s,id,"Q41",QiraChoice.Yes));
        await Assert.ThrowsAsync<BusinessRuleException>(async()=>await s.ReviewQira(id,new(){Version=(await s.Qira(id)).Assessment.Version,Note="self"}));
        var reviewer=new OmsService(db,new Actor("qa.reviewer"));await reviewer.ReviewQira(id,new(){Version=(await s.Qira(id)).Assessment.Version,Note="QA review"});await reviewer.ApproveQira(id,new(){Version=(await s.Qira(id)).Assessment.Version,Note="QA approve"});
        var next=await s.StartQira(eid,id);Assert.NotEqual(id,next);Assert.Equal(id,(await s.Qira(next)).Assessment.PreviousId);Assert.Equal(2,(await s.Qira(next)).Assessment.Revision);Assert.Empty((await s.Qira(next)).Answers);
        db.ChangeTracker.Clear();var persisted=await s.Qira(id);Assert.Equal(QiraState.Approved,persisted.Assessment.State);Assert.Equal(QiraRisk.VeryLow,persisted.Assessment.Risk);Assert.Contains(persisted.History,h=>h.Type=="اعتماد التقييم");
        var completed=await db.QiraAssessments.FindAsync(id);completed!.Risk=QiraRisk.High;await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());await tx.RollbackAsync();
    }
    [SqlFact]public async Task NationalPolicyIsNotInventedAndActionsSnapshotDispatchOnlyOnce()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var s=new OmsService(db,actor);
        await s.AddQiraPolicy(new(){Name="[QA rollback] empty",Reference="QA no national assumptions"});var eventId=await Event(s);var first=await s.StartQira(eventId);await Scope(s,first);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>Answer(s,first,"Q1",QiraChoice.Yes));
        await s.AddQiraPolicy(new(){Name="[QA rollback] configured",Reference="QA only",Hazards="QA",Actions=[new(){Level=QiraRisk.VeryLow,Title="QA configured action",Organization="QA agency",Priority=1,DueHours=3}]});
        Assert.Empty((await s.Qira(first)).HighThreatHazards); // Existing draft retains empty national version.
        var otherEvent=await Event(s);var id=await Approved(s,new OmsService(db,new Actor("qa.reviewer")),otherEvent);var result=await s.Qira(id);Assert.Single(result.Actions);Assert.Equal("QA agency",result.Actions[0].Organization);
        await s.AddQiraPolicy(new(){Name="[QA rollback] next",Reference="QA",Hazards="different"});Assert.Equal("QA",Assert.Single((await s.Qira(id)).HighThreatHazards));
        await s.ShareQiraActions(id,new(){Version=result.Assessment.Version});await s.ShareQiraActions(id,new(){Version=(await s.Qira(id)).Assessment.Version});Assert.Single(await db.Tasks.Where(t=>t.EventId==otherEvent).ToListAsync());
        var action=Assert.Single((await s.Qira(id)).Actions);await s.UpdateQiraAction(action.Id,new(){Version=action.Version,TaskVersion=action.TaskVersion,State=TaskState.Completed,Progress=100,Notes="QA completion evidence"});
        db.ChangeTracker.Clear();action=Assert.Single((await s.Qira(id)).Actions);Assert.Equal("Completed",action.State);Assert.Equal("QA completion evidence",action.Notes);
        await tx.RollbackAsync();
    }
    [SqlFact]public async Task UndoReopensLastQuestionKeepsAuditAndRejectsStaleOrCompletedChanges()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var s=new OmsService(db,actor);var eventId=await Event(s);var id=await s.StartQira(eventId);await Scope(s,id);
        await Assert.ThrowsAsync<BusinessRuleException>(async()=>await s.UndoQiraAnswer(id,new(){Version=(await s.Qira(id)).Assessment.Version}));
        await Answer(s,id,"Q1",QiraChoice.Unknown);await Answer(s,id,"Q2",QiraChoice.No);await Answer(s,id,"Q41",QiraChoice.No);
        var before=await s.Qira(id);Assert.Equal(QiraRisk.VeryLow,before.Evaluation.Risk);
        await s.UndoQiraAnswer(id,new(){Version=before.Assessment.Version});db.ChangeTracker.Clear();var after=await s.Qira(id);
        Assert.Equal("Q41",after.Evaluation.Next);Assert.Null(after.Evaluation.Risk);Assert.Equal(3,after.Answers.Count);
        Assert.Contains(after.Answers,a=>a.Node=="Q41"&&!a.Active&&a.Notes=="QA evidence reason");Assert.Contains(after.History,h=>h.Type=="إلغاء آخر اختيار");
        await Assert.ThrowsAsync<BusinessRuleException>(()=>s.UndoQiraAnswer(id,new(){Version=before.Assessment.Version}));
        await Assert.ThrowsAsync<BusinessRuleException>(()=>new OmsService(db,new Actor("qa.other")).UndoQiraAnswer(id,new(){Version=after.Assessment.Version}));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new OmsService(db,new Actor("qa.denied",null,Permissions.RiskCreate)).UndoQiraAnswer(id,new(){Version=after.Assessment.Version}));
        await s.UndoQiraAnswer(id,new(){Version=after.Assessment.Version});Assert.Equal("Q2",(await s.Qira(id)).Evaluation.Next);
        await s.UndoQiraAnswer(id,new(){Version=(await s.Qira(id)).Assessment.Version});Assert.Equal("Q1",(await s.Qira(id)).Evaluation.Next);
        Assert.All((await s.Qira(id)).Answers,a=>Assert.False(a.Active));
        await Completed(s,eventId);await Assert.ThrowsAsync<BusinessRuleException>(async()=>await s.UndoQiraAnswer(id,new(){Version=(await s.Qira(id)).Assessment.Version}));
        await tx.RollbackAsync();
    }
    [SqlFact]public async Task ScopeAndPermissionsAreEnforcedOnServer()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var s=new OmsService(db,actor);var eid=await Event(s);var id=await s.StartQira(eid);
        await Assert.ThrowsAsync<KeyNotFoundException>(()=>new OmsService(db,new Actor("qa.outside",2)).Qira(id));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new OmsService(db,new Actor("qa.no-create",null,Permissions.RiskCreate)).StartQira(eid));
        await Scope(s,id,true);await Assert.ThrowsAsync<BusinessRuleException>(()=>s.SaveQiraAnswer(id,new(){Version=db.QiraAssessments.Local.Single(x=>x.Id==id).RowVersion is {} v?Convert.ToBase64String(v):"",Node="Q1",Choice=QiraChoice.Yes}));
        await Answer(s,id,"Q1",QiraChoice.Yes);Assert.Equal("C52-HV",(await s.Qira(id)).Evaluation.Next);await tx.RollbackAsync();
    }
}
