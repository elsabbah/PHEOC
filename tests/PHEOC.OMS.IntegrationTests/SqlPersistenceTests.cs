using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;
public class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL")))Skip="Set PHEOC_TEST_SQL to an initialized SQL Server 2016 connection. Test writes are rolled back.";}
}
public class SqlPersistenceTests
{
    private sealed class Actor(int? governorate=null,bool allowed=true) : ICurrentUser { public string Id=>"qa-transaction";public int? GovernorateId=>governorate;public bool Can(string permission)=>allowed; }
    private static OmsDbContext Context(ICurrentUser actor)=>new(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),o=>o.UseCompatibilityLevel(130)).Options,actor);
    [SqlFact]public async Task SaveReopenAuditAndConcurrentEditAreRealSqlOperations()
    {
        var actor=new Actor();await using var db=Context(actor);await using var tx=await db.Database.BeginTransactionAsync();var service=new OmsService(db,actor);
        var id=await service.CreateEvent(new(){Title="[QA rollback] إعادة فتح عربية",EventType="QA",ThreatType="QA",GovernorateId=1});
        var first=await service.Event(id);Assert.Equal("[QA rollback] إعادة فتح عربية",first.Event.Title);Assert.NotEmpty(first.Version);Assert.NotEmpty(first.Timeline);
        await service.ChangeStatus(id,new(){Version=first.Version,Status=EventStatus.UnderAssessment,Reason="QA"});
        await Assert.ThrowsAsync<PHEOC.OMS.Domain.Rules.BusinessRuleException>(()=>service.ChangeStatus(id,new(){Version=first.Version,Status=EventStatus.Monitoring,Reason="stale"}));
        Assert.True(await db.AuditLogs.AnyAsync(x=>x.Entity==nameof(EmergencyEvent)&&x.EntityId==id.ToString()));await tx.RollbackAsync();
    }
    [SqlFact]public async Task ScopedUsersCannotReadOtherGovernorateEvents()
    {
        var actor=new Actor();await using var db=Context(actor);await using var tx=await db.Database.BeginTransactionAsync();var service=new OmsService(db,actor);
        var id=await service.CreateEvent(new(){Title="[QA rollback] scope",EventType="QA",ThreatType="QA",GovernorateId=1});
        var restricted=new OmsService(db,new Actor(2));await Assert.ThrowsAsync<KeyNotFoundException>(()=>restricted.Event(id));await tx.RollbackAsync();
    }
    [Fact]public async Task PermissionCheckPrecedesDataAccess(){var actor=new Actor(allowed:false);await using var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer("Server=invalid;Database=invalid;Integrated Security=true").Options,actor);await Assert.ThrowsAsync<AccessDeniedException>(()=>new OmsService(db,actor).CreateEvent(new()));}
}
