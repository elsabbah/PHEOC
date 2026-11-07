using System.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;

namespace PHEOC.OMS.IntegrationTests;

public class EventAlertSqlTests
{
    private sealed class Actor(string id, int? governorate = null, bool allowed = true) : ICurrentUser
    {
        public string Id => id; public int? GovernorateId => governorate; public bool Can(string permission) => allowed;
    }
    private static OmsDbContext Db(ICurrentUser user) => new(new DbContextOptionsBuilder<OmsDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"), sql => sql.UseCompatibilityLevel(130)).Options, user);
    private static EventInput Input(bool training = false, int governorate = 1) => new() {
        Title = "[QA rollback] تنبيه وصول", EventType = "QA", ThreatType = "QA", GovernorateId = governorate,
        ConfirmedAt = DateTime.UtcNow.AddMinutes(-1), IsTraining = training
    };

    [SqlFact] public async Task ArrivalAndAlertCommitTogetherAndDuplicateIntakeDoesNotCreateAnotherAlert()
    {
        var actor = new Actor("qa-alerts-" + Guid.NewGuid());
        await using var db = Db(actor); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var intake = new EventIntake(db, new EphemeralDataProtectionProvider()); var key = Guid.NewGuid().ToString();
        var input = Input(); var first = await intake.Receive("EBS", key, key, input);
        var again = await intake.Receive("EBS", key, key, input);
        Assert.True(again.Duplicate); Assert.Equal(first.EventId, again.EventId);
        Assert.Equal(1, await db.Notifications.CountAsync(n => n.EventId == first.EventId && n.Severity == EventArrival.NotificationKind));
        var training = await intake.Receive("EBS", key+"-training", key+"-training", Input(true));
        Assert.False(await db.Notifications.AnyAsync(n => n.EventId == training.EventId && n.Severity == EventArrival.NotificationKind));
        await tx.RollbackAsync();
    }

    [SqlFact] public async Task ScopedFeedAndPerUserReadingArePersistedAndIdempotent()
    {
        var suffix = Guid.NewGuid().ToString(); var cairo = new Actor("qa-alert-cairo-"+suffix, 1);
        await using var db = Db(cairo); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var admin = new Actor("qa-alert-admin-"+suffix); var oms = new OmsService(db,admin);
        var eventId = await oms.CreateEvent(Input()); var other = await oms.CreateEvent(Input(governorate:2));
        var alerts = new EventAlertService(db,cairo); var feed = await alerts.Feed();
        var own = Assert.Single(feed.Items,n=>n.EventId==eventId);
        Assert.DoesNotContain(feed.Items,n=>n.EventId==other);
        var forbidden = await db.Notifications.Where(n=>n.EventId==other&&n.Severity==EventArrival.NotificationKind).Select(n=>n.Id).SingleAsync();
        await Assert.ThrowsAsync<KeyNotFoundException>(()=>alerts.Read([forbidden]));
        await alerts.Read([own.Id]); await alerts.Read([own.Id]);
        Assert.DoesNotContain((await alerts.Feed()).Items,n=>n.Id==own.Id);
        Assert.Equal(1,await db.NotificationReads.CountAsync(n=>n.UserId==cairo.Id&&n.NotificationId==own.Id));
        Assert.Contains((await new EventAlertService(db,new Actor("qa-second-"+suffix,1)).Feed()).Items,n=>n.Id==own.Id);
        var training = await oms.CreateEvent(Input(true)); Assert.DoesNotContain((await alerts.Feed()).Items,n=>n.EventId==training);
        await tx.RollbackAsync();
    }

    [SqlFact] public async Task StateChangesAreNotNewArrivalsAndCursorCountsOnlyNewUnreadEvents()
    {
        var actor = new Actor("qa-alert-cursor-"+Guid.NewGuid());
        await using var db=Db(actor); await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var oms=new OmsService(db,actor); var alerts=new EventAlertService(db,actor);
        var id=await oms.CreateEvent(Input()); var before=await alerts.Feed(); var details=await oms.Event(id);
        await oms.ChangeStatus(id,new(){Version=details.Version,Status=EventStatus.UnderAssessment,Reason="QA"});
        Assert.Equal(1,await db.Notifications.CountAsync(n=>n.EventId==id&&n.Severity==EventArrival.NotificationKind));
        Assert.Equal(0,(await alerts.Feed(before.LatestId)).NewCount);
        var second=await oms.CreateEvent(Input()); var after=await alerts.Feed(before.LatestId);
        Assert.Equal(1,after.NewCount); Assert.Contains(after.Items,n=>n.EventId==second);
        await tx.RollbackAsync();
    }

    [Fact] public async Task AlertPermissionIsCheckedBeforeQueryingSql()
    {
        var actor=new Actor("qa-denied",allowed:false);
        await using var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer("Server=invalid;Database=invalid;Integrated Security=true").Options,actor);
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new EventAlertService(db,actor).Feed());
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new EventAlertService(db,actor).Read([1]));
    }
}
