using System.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;

public class KpiSourceDatesTests
{
    private sealed class Actor(int? governorate=null,string? denied=null):ICurrentUser
    {public string Id=>"qa.kpi.source";public int? GovernorateId=>governorate;public bool Can(string p)=>p!=denied;}
    public sealed class Source:IEgyptEbsKpiDateSource
    {
        public int Calls{get;private set;}
        public Task<EgyptEbsKpiDates> Read(int id){Calls++;return Task.FromResult(new EgyptEbsKpiDates(id,new(2026,1,2),new(2026,1,3),new(2026,1,4),"EgyptEBS / Signals.EventStartDate=2026-01-02; Signals.DetectionDate=2026-01-03; Responses.CreatedAt=2026-01-04T13:45:00; QA source-map-v2",DateTime.UtcNow,"QA dates need review; response CreatedAt is notification",new(7,new(2026,1,4,13,45,0),false)));}
    }
    private static OmsDbContext Db(ICurrentUser actor)=>new(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),o=>o.UseCompatibilityLevel(130)).Options,actor);
    public static async Task<long> CreateImported(OmsDbContext db)
    {
        var id=Random.Shared.Next(1_500_000_000,2_000_000_000).ToString();
        return (await new EventIntake(db,new EphemeralDataProtectionProvider()).Receive("EgyptEBS","qa.kpi."+Guid.NewGuid().ToString("N"),id,new(){Title="[QA rollback] تواريخ مؤشر المصدر",EventType="تدريب",ThreatType="QA",GovernorateId=1,IsTraining=true,OccurredAt=new DateTime(2026,1,2,10,0,0,DateTimeKind.Utc),DetectedAt=new DateTime(2026,1,3,10,0,0,DateTimeKind.Utc),ConfirmedAt=new DateTime(2026,1,4,10,0,0,DateTimeKind.Utc)})).EventId;
    }
    [SqlFact]public async Task SourcePreviewUsesFirstResponseForNotificationWithoutWritingOrOverwritingSavedRecords()
    {
        var actor=new Actor();await using var db=Db(actor);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var source=new Source();var service=new OmsService(db,actor,kpiSource:source);var eventId=await CreateImported(db);
        var before=await db.KpiDateRecords.CountAsync();var preview=await service.KpiSourceDates(eventId);Assert.Equal(new DateOnly(2026,1,4),preview.Notification);Assert.NotNull(preview.FirstResponse);Assert.False(preview.FirstResponse.IsConfirmed);Assert.Equal(before,await db.KpiDateRecords.CountAsync());Assert.Empty((await service.KpiDetails(eventId)).Dates);
        await service.RecordKpiDates(eventId,new(){Emergence=preview.Emergence,Detection=preview.Detection,Notification=new(2026,1,5),Excluded=true,Evidence="Manual reviewed evidence; "+preview.Evidence});
        await service.KpiSourceDates(eventId);db.ChangeTracker.Clear();var current=(await service.KpiDetails(eventId)).CurrentDates!;Assert.True(current.Excluded);Assert.Equal(new DateOnly(2026,1,5),current.Notification);Assert.StartsWith("Manual reviewed evidence",current.Evidence);Assert.Single((await service.KpiDetails(eventId)).Dates);
        var calls=source.Calls;
        await Assert.ThrowsAsync<KeyNotFoundException>(()=>new OmsService(db,new Actor(2),kpiSource:source).KpiSourceDates(eventId));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new OmsService(db,new Actor(denied:Permissions.ReportsView),kpiSource:source).KpiSourceDates(eventId));Assert.Equal(calls,source.Calls);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>service.RecordKpiDates(eventId,new(){Emergence=new(2026,1,3),Detection=new(2026,1,2),Evidence="invalid order"}));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>new OmsService(db,new Actor(denied:Permissions.ReportsCreate),kpiSource:source).RecordKpiDates(eventId,new(){Evidence="denied"}));
        await tx.RollbackAsync();
    }
}
