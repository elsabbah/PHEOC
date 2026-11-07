using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;
public class EgyptEbsBrowseTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private sealed class Actor:ICurrentUser{public string Id=>"qa-browse-rollback";public int? GovernorateId=>null;public bool Can(string permission)=>true;}
    [SqlFact]public async Task SourceFiltersPaginationCountsAndReceiptStateWorkBeforePaging()
    {
        var actor=new Actor();await using var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"),s=>s.UseCompatibilityLevel(130)).Options,actor);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:EgyptEbsReader",Environment.GetEnvironmentVariable("PHEOC_EBS_SQL")}}).Build();
        var reader=new EgyptEbsReader(config,db,new EventIntake(db,new EphemeralDataProtectionProvider()),actor);
        var timer=Stopwatch.StartNew();var first=await reader.Browse(new(){PageSize=20});output.WriteLine($"First page with count: {timer.ElapsedMilliseconds} ms, {first.TotalCount} matching rows");
        Assert.True(first.TotalCount>20);Assert.Equal(20,first.Signals.Count);Assert.NotEmpty(first.SignalTypes!);Assert.NotEmpty(first.Governorates!);
        var second=await reader.Browse(new(){Page=2,PageSize=20},includeLookups:false);
        Assert.Empty(first.Signals.Select(s=>s.SignalId).Intersect(second.Signals.Select(s=>s.SignalId)));
        Assert.True(first.Signals.Min(s=>s.SignalId)>second.Signals.Max(s=>s.SignalId));
        var target=second.Signals.First(s=>!string.IsNullOrWhiteSpace(s.Governorate));
        var found=await reader.Browse(new(){Search="#"+target.SignalId,PageSize=20},includeLookups:false);Assert.Contains(found.Signals,s=>s.SignalId==target.SignalId);
        var gov=await reader.Browse(new(){Governorate=target.Governorate,PageSize=20},includeLookups:false);Assert.All(gov.Signals,s=>Assert.Equal(target.Governorate,s.Governorate));
        var type=first.SignalTypes!.First(t=>t.Name==target.Type);
        var typed=await reader.Browse(new(){TypeId=type.Id,PageSize=20},includeLookups:false);Assert.All(typed.Signals,s=>Assert.Equal(type.Name,s.Type));
        var day=DateOnly.FromDateTime(target.VerificationLocal);
        var dated=await reader.Browse(new(){From=day,To=day,PageSize=20},includeLookups:false);Assert.NotEmpty(dated.Signals);Assert.All(dated.Signals,s=>Assert.Equal(day,DateOnly.FromDateTime(s.VerificationLocal)));
        var empty=await reader.Browse(new(){Search="%'_"+Guid.NewGuid().ToString("N"),Page=100,PageSize=20},includeLookups:false);Assert.Empty(empty.Signals);Assert.Equal(0,empty.TotalCount);Assert.Equal(1,empty.Filter!.Page);
        var last=await reader.Browse(new(){Page=1000000,PageSize=20},includeLookups:false);Assert.Equal(last.PageCount,last.Filter!.Page);Assert.NotEmpty(last.Signals);
        var candidate=first.Signals.First(s=>!s.Imported);await reader.Import(candidate.SignalId);
        var received=await reader.Browse(new(){Search=candidate.SignalId.ToString(),Receipt="received",PageSize=20},includeLookups:false);Assert.Contains(received.Signals,s=>s.SignalId==candidate.SignalId&&s.Imported&&s.ReceivedAtUtc!=null);
        var pending=await reader.Browse(new(){Search=candidate.SignalId.ToString(),Receipt="pending",PageSize=20},includeLookups:false);Assert.DoesNotContain(pending.Signals,s=>s.SignalId==candidate.SignalId);
        await Assert.ThrowsAsync<BusinessRuleException>(()=>reader.Browse(new(){From=day.AddDays(1),To=day}));
        await tx.RollbackAsync();
    }
}
