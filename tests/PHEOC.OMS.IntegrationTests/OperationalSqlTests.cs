using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;
public class OperationalSqlTests
{
    private sealed class Actor(string id="qa.creator") : ICurrentUser {public string Id=>id;public int? GovernorateId=>null;public bool Can(string permission)=>true;}
    private static OmsDbContext Db(ICurrentUser actor)=>new(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),o=>o.UseCompatibilityLevel(130)).Options,actor);
    private static EventInput Event()=>new(){Title="[QA rollback] operational",EventType="training",ThreatType="QA",GovernorateId=1,IsTraining=true};
    [SqlFact]public async Task InventoryIssuingUsesVersionAndRollsBackInsufficientBalance()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var service=new OmsService(db,actor);var id=await service.CreateEvent(Event());
        await service.AddWarehouse(new(){Name="[QA rollback] warehouse",GovernorateId=1});await service.AddItem(new(){Name="[QA rollback] item",Unit="unit",Minimum=2});
        var warehouse=await db.Warehouses.OrderByDescending(x=>x.Id).FirstAsync();var item=await db.InventoryItems.OrderByDescending(x=>x.Id).FirstAsync();
        await service.AddBatch(new(){WarehouseId=warehouse.Id,ItemId=item.Id,BatchNumber=Guid.NewGuid().ToString(),Quantity=10});var batch=await db.InventoryBatches.OrderByDescending(x=>x.Id).FirstAsync();var original=Convert.ToBase64String(batch.RowVersion);
        await service.MoveStock(batch.Id,new(){Version=original,Quantity=-4,EventId=id,Reason="QA"});Assert.Equal(6,batch.Balance);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>service.MoveStock(batch.Id,new(){Version=original,Quantity=-1,EventId=id,Reason="stale"}));
        await Assert.ThrowsAsync<BusinessRuleException>(()=>service.MoveStock(batch.Id,new(){Version=Convert.ToBase64String(batch.RowVersion),Quantity=-7,EventId=id,Reason="excess"}));Assert.Equal(6,batch.Balance);await tx.RollbackAsync();
    }
    [SqlFact]public async Task SharedTeamMemberCannotBeDeployedTwice()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var service=new OmsService(db,actor);var id=await service.CreateEvent(Event());
        var e=await service.Event(id);await service.ChangeStatus(id,new(){Version=e.Version,Status=EventStatus.UnderAssessment,Reason="QA"});
        await QiraSqlTests.Approved(service,new OmsService(db,new Actor("qa.independent")),id);
        e=await service.Event(id);await service.Activate(id,new(){Version=e.Version,Level=1,Rationale="QA"});
        await service.AddPerson(new(){Name="QA rollback",EmployeeNumber=Guid.NewGuid().ToString(),Organization="QA",Specialty="QA",GovernorateId=1});var person=await db.Personnel.OrderByDescending(x=>x.Id).FirstAsync();
        await service.AddTeam(new(){Name="QA rollback A",GovernorateId=1,LeaderId=person.Id,PersonIds=[person.Id]});var a=await db.Teams.OrderByDescending(x=>x.Id).FirstAsync();
        await service.AddTeam(new(){Name="QA rollback B",GovernorateId=1,LeaderId=person.Id,PersonIds=[person.Id]});var b=await db.Teams.OrderByDescending(x=>x.Id).FirstAsync();
        await service.Deploy(id,new(){TeamId=a.Id,Mission="QA"});await Assert.ThrowsAsync<BusinessRuleException>(()=>service.Deploy(id,new(){TeamId=b.Id,Mission="QA duplicate"}));
        var deployment=await db.Deployments.OrderByDescending(x=>x.Id).FirstAsync();var field=new FieldInput{ClientRequestId=Guid.NewGuid(),DeploymentId=deployment.Id,Activities="QA",Needs="QA"};await service.AddFieldReport(field);await service.AddFieldReport(field);Assert.Equal(1,await db.FieldReports.CountAsync(x=>x.ClientRequestId==field.ClientRequestId));
        await service.Sos(deployment.Id,new(){Text="QA SOS"});Assert.True(await db.Notifications.AnyAsync(x=>x.EventId==id&&x.Severity=="Critical"));
        await service.AddVehicle(new(){PlateNumber="QA-"+Guid.NewGuid(),Type="QA",GovernorateId=1});var vehicle=await db.Vehicles.OrderByDescending(x=>x.Id).FirstAsync();await service.AssignVehicle(new(){VehicleId=vehicle.Id,DeploymentId=deployment.Id});await Assert.ThrowsAsync<BusinessRuleException>(()=>service.AssignVehicle(new(){VehicleId=vehicle.Id,DeploymentId=deployment.Id}));
        var assignment=await db.VehicleAssignments.OrderByDescending(x=>x.Id).FirstAsync();await service.ReturnVehicle(assignment.Id,new(){Version=Convert.ToBase64String(assignment.RowVersion)});Assert.True(vehicle.Available);
        await service.DeploymentStatus(deployment.Id,new(){Version=Convert.ToBase64String(deployment.RowVersion)},false);await service.DeploymentStatus(deployment.Id,new(){Version=Convert.ToBase64String(deployment.RowVersion)},true);await service.AddFieldReport(field);Assert.Equal(1,await db.FieldReports.CountAsync(x=>x.ClientRequestId==field.ClientRequestId));await tx.RollbackAsync();
    }
    [SqlFact]public async Task ClosingRequiresIndependentApprovalAndCompletedWork()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var service=new OmsService(db,actor);var id=await service.CreateEvent(Event());
        await service.AddTask(id,new(){Title="QA",AssignedTo="QA",DueAt=DateTime.UtcNow.AddDays(1)});
        await service.RequestClosure(id,new(){Rationale="QA",ResourcesSettled=true,EpidemiologyReviewed=true,FieldReviewed=true});var closure=await db.ClosureRequests.OrderByDescending(x=>x.Id).FirstAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(()=>service.ApproveClosure(closure.Id,new(){Version=Convert.ToBase64String(closure.RowVersion)}));
        var reviewer=new OmsService(db,new Actor("qa.approver"));await Assert.ThrowsAsync<BusinessRuleException>(()=>reviewer.ApproveClosure(closure.Id,new(){Version=Convert.ToBase64String(closure.RowVersion)}));
        var task=await db.Tasks.OrderByDescending(x=>x.Id).FirstAsync();await service.UpdateTask(task.Id,new(){Version=Convert.ToBase64String(task.RowVersion),State=TaskState.Completed,Progress=100});
        var report=await service.AddReport(new(){EventId=id,Summary="QA",NextActions="QA"});await reviewer.ApproveReport(report);await reviewer.ApproveClosure(closure.Id,new(){Version=Convert.ToBase64String(closure.RowVersion)});
        Assert.Equal(EventStatus.Closed,(await service.Event(id)).Event.Status);await Assert.ThrowsAsync<BusinessRuleException>(()=>service.AddTask(id,new(){Title="QA",AssignedTo="QA",DueAt=DateTime.UtcNow.AddDays(1)}));await tx.RollbackAsync();
    }
    [SqlFact]public async Task EbsDuplicateAndConflictingPayloadUseRealUniqueConstraints()
    {
        await using var db=Db(new Actor());await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var intake=new EventIntake(db,new EphemeralDataProtectionProvider());var input=Event();input.ConfirmedAt=DateTime.UtcNow.AddMinutes(-1);var key=Guid.NewGuid().ToString();
        var first=await intake.Receive("EBS",key,key,input);var duplicate=await intake.Receive("EBS",key,key,input);Assert.Equal(first.EventId,duplicate.EventId);Assert.True(duplicate.Duplicate);
        input.Title="different";await Assert.ThrowsAsync<IdempotencyConflictException>(()=>intake.Receive("EBS",key,key,input));Assert.Equal(1,await db.InboxMessages.CountAsync(x=>x.IdempotencyKey==key));await tx.RollbackAsync();
    }
    [SqlFact]public async Task EgyptEbsReaderUsesActualSchemaAndCanImportWithoutSourceWrites()
    {
        var connection=Environment.GetEnvironmentVariable("PHEOC_EBS_SQL");Assert.False(string.IsNullOrWhiteSpace(connection));
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:EgyptEbsReader",connection}}).Build();
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var intake=new EventIntake(db,new EphemeralDataProtectionProvider());var reader=new EgyptEbsReader(config,db,intake,actor);var board=await reader.Candidates();Assert.True(board.Configured);Assert.NotEmpty(board.Signals);
        var candidate=board.Signals.First(x=>!x.Imported);var result=await reader.Import(candidate.SignalId);var repeated=await reader.Import(candidate.SignalId);Assert.True(repeated.Duplicate);Assert.Equal(result.EventId,repeated.EventId);
        var imported=await db.Events.SingleAsync(x=>x.Id==result.EventId);Assert.Null(imported.NotifiedAt);Assert.Null(imported.CasesCount);Assert.NotEqual(default,imported.DetectedAt);
        var received=(await reader.Candidates()).Signals.Single(s=>s.SignalId==candidate.SignalId);
        Assert.True(received.Imported);Assert.NotNull(received.ReceivedAtUtc);Assert.Equal(DateTimeKind.Utc,received.ReceivedAtUtc.Value.Kind);
        Assert.Equal(DateTime.SpecifyKind((await db.InboxMessages.SingleAsync(m=>m.Source=="EgyptEBS"&&m.ExternalEventId==candidate.SignalId.ToString())).ReceivedAt,DateTimeKind.Utc),received.ReceivedAtUtc.Value);
        await tx.RollbackAsync();
    }
    [SqlFact]public async Task ContactsSamplesAndMilestonesPersistWithSourceEvidence()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var service=new OmsService(db,actor);var id=await service.CreateEvent(Event());
        await service.AddContact(new(){EventId=id,Code="qa-"+Guid.NewGuid(),LastExposureAt=DateTime.UtcNow.AddDays(-2),FollowUpUntil=DateTime.UtcNow.AddDays(3),RiskLevel="QA"});var contact=await db.Contacts.OrderByDescending(x=>x.Id).FirstAsync();await service.FollowContact(contact.Id,new(){Text="QA follow up"});
        await service.AddSample(new(){EventId=id,Code="qa-"+Guid.NewGuid(),Type="QA",CollectedAt=DateTime.UtcNow.AddDays(-1)});var sample=await db.Samples.OrderByDescending(x=>x.Id).FirstAsync();await service.TransferSample(sample.Id,new(){From="QA origin",To="QA lab"});await service.SampleResult(sample.Id,new(){Text="QA result"});
        var board=await service.Health();Assert.Contains(board.Contacts,x=>x.Id==contact.Id&&x.Details.Contains("QA follow up"));Assert.Contains(board.Samples,x=>x.Id==sample.Id&&x.Details.Contains("QA result"));
        var date=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));await service.RecordKpiDates(id,new(){Emergence=date.AddDays(-3),Detection=date.AddDays(-1),Notification=date,Evidence="QA source"});
        foreach(var action in Enum.GetValues<EarlyResponseAction>())await service.RecordMilestone(id,new(){Action=action,Date=date,Evidence="QA source"});var kpi=await service.KpiDetails(id);Assert.Equal(7,kpi.Milestones.Count);Assert.Single(kpi.Dates);await tx.RollbackAsync();
    }
    [SqlFact]public async Task AtomicTransferConservesStockAndImmutableLedgersRejectUpdates()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var service=new OmsService(db,actor);
        await service.AddWarehouse(new(){Name="QA source",GovernorateId=1});var first=await db.Warehouses.OrderByDescending(x=>x.Id).FirstAsync();await service.AddWarehouse(new(){Name="QA target",GovernorateId=1});var second=await db.Warehouses.OrderByDescending(x=>x.Id).FirstAsync();await service.AddItem(new(){Name="QA transfer",Unit="unit",Minimum=0});var item=await db.InventoryItems.OrderByDescending(x=>x.Id).FirstAsync();
        await service.AddBatch(new(){WarehouseId=first.Id,ItemId=item.Id,BatchNumber=Guid.NewGuid().ToString(),Quantity=10});var batch=await db.InventoryBatches.OrderByDescending(x=>x.Id).FirstAsync();await service.TransferStock(batch.Id,new(){Version=Convert.ToBase64String(batch.RowVersion),ToWarehouseId=second.Id,Quantity=4,Reason="QA transfer"});
        Assert.Equal(10,await db.InventoryBatches.Where(x=>x.ItemId==item.Id).SumAsync(x=>x.Balance));Assert.Equal(6,batch.Balance);
        var eventId=await service.CreateEvent(Event());await service.RequestSupply(new(){EventId=eventId,ItemId=item.Id,Quantity=2,Reason="QA request"});var request=await db.SupplyRequests.OrderByDescending(x=>x.Id).FirstAsync();await Assert.ThrowsAsync<BusinessRuleException>(()=>service.ApproveSupply(request.Id,new(){BatchId=batch.Id,Version=Convert.ToBase64String(request.RowVersion)}));await new OmsService(db,new Actor("qa.logistics.approver")).ApproveSupply(request.Id,new(){BatchId=batch.Id,Version=Convert.ToBase64String(request.RowVersion)});Assert.Equal(4,batch.Balance);
        var ledger=await db.InventoryTransactions.OrderByDescending(x=>x.Id).FirstAsync();ledger.Reason="silent rewrite";await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());await tx.RollbackAsync();
    }
    [SqlFact]public async Task SlaBindsCurrentVersionWithoutChangingHistoricalTarget()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var service=new OmsService(db,actor);
        await service.AddSla(new(){Name="QA SLA v1",Stage="Acknowledge",TargetMinutes=30,EscalateEveryMinutes=10});var original=await db.SlaDefinitions.OrderByDescending(x=>x.Id).FirstAsync();var input=Event();input.IsTraining=false;var id=await service.CreateEvent(input);
        await service.AddSla(new(){Name="QA SLA v2",Stage="Acknowledge",TargetMinutes=60,EscalateEveryMinutes=20});var binding=await db.EventSlas.Include(x=>x.Definition).SingleAsync(x=>x.EventId==id&&x.Definition.Stage=="Acknowledge");Assert.Equal(original.Id,binding.DefinitionId);Assert.Equal(30,binding.Definition.TargetMinutes);await tx.RollbackAsync();
    }
}
