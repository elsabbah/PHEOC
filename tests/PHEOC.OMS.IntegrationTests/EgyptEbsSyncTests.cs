using System.Data;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace PHEOC.OMS.IntegrationTests;

public class EgyptEbsSyncTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private sealed class Actor : ICurrentUser
    {
        public string Id => "qa-sync-rollback";
        public int? GovernorateId => null;
        public bool Can(string permission) => true;
    }
    private sealed class Source(IReadOnlyList<EgyptEbsSignal> records) : IEgyptEbsSyncSource
    {
        public int Reads { get; private set; }
        public int? FailId { get; set; }
        public Action? OnImport { get; set; }
        public List<int> Imported { get; } = [];
        public Task<IReadOnlyList<EgyptEbsSignal>> ReadChanges(DateTime afterChange, int afterVerification)
        {
            Reads++;
            return Task.FromResult<IReadOnlyList<EgyptEbsSignal>>(records
                .Where(x => x.ChangedLocal > afterChange || (x.ChangedLocal == afterChange && x.VerificationId > afterVerification))
                .OrderBy(x => x.ChangedLocal).ThenBy(x => x.VerificationId).Take(50).ToList());
        }
        public Task<IntakeResult> ImportSignal(int signalId)
        {
            if(FailId == signalId) throw new BusinessRuleException("تعذر ربط محافظة الإشارة في الاختبار.");
            Imported.Add(signalId); OnImport?.Invoke(); return Task.FromResult(new IntakeResult(signalId, Guid.Empty, false));
        }
    }
    private static OmsDbContext Db() => new(new DbContextOptionsBuilder<OmsDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL"), s => s.UseCompatibilityLevel(130)).Options, new Actor());
    private static readonly DateTime Start = new(2026, 1, 1);
    private static List<EgyptEbsSignal> Records(int count) => Enumerable.Range(1, count)
        .Select(id => new EgyptEbsSignal(id,id,"QA","QA","QA","","",Start,Start,Start,false,Start.AddSeconds(1))).ToList();
    private static async Task<long> State(OmsDbContext db, bool enabled = true)
    {
        var state = await db.EbsSyncStates.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if(state == null) { state = new EbsSyncState(); db.EbsSyncStates.Add(state); }
        state.Enabled = enabled; state.CursorLocal = Start; state.CursorVerificationId = 0;
        state.LastSuccessAt = null; state.LastError = ""; state.EnabledBy = "qa-sync-rollback";
        await db.SaveChangesAsync(); return state.Id;
    }
    [SqlFact] public async Task DisabledReceptionDoesNotReadOrImport()
    {
        await using var db = Db(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await State(db, false); var source = new Source(Records(3)); await new EgyptEbsSync(db,source).Tick(default);
        Assert.Equal(0,source.Reads); Assert.Empty(source.Imported); await tx.RollbackAsync();
    }
    [SqlFact] public async Task MoreThanFiftyWithSameTimestampResumeAcrossBatchesWithoutRepeating()
    {
        await using var db = Db(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var id = await State(db); var source = new Source(Records(51)); var sync = new EgyptEbsSync(db,source);
        await sync.Tick(default); db.ChangeTracker.Clear(); Assert.Equal(50,(await db.EbsSyncStates.SingleAsync(x=>x.Id==id)).CursorVerificationId);
        await sync.Tick(default); await sync.Tick(default); db.ChangeTracker.Clear();
        Assert.Equal(51,(await db.EbsSyncStates.SingleAsync(x=>x.Id==id)).CursorVerificationId);
        Assert.Equal(Enumerable.Range(1,51),source.Imported); await tx.RollbackAsync();
    }
    [SqlFact] public async Task FailureDoesNotSkipRecordAndRetryResumesAtFailure()
    {
        await using var db = Db(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var id=await State(db); var source=new Source(Records(3)){FailId=2}; var sync=new EgyptEbsSync(db,source);
        await sync.Tick(default); db.ChangeTracker.Clear(); var state=await db.EbsSyncStates.SingleAsync(x=>x.Id==id);
        Assert.Equal(1,state.CursorVerificationId); Assert.Contains("#2",state.LastError); Assert.Equal(new[]{1},source.Imported);
        source.FailId=null; await sync.Tick(default); db.ChangeTracker.Clear(); state=await db.EbsSyncStates.SingleAsync(x=>x.Id==id);
        Assert.Equal(3,state.CursorVerificationId); Assert.Empty(state.LastError); Assert.Equal(new[]{1,2,3},source.Imported); await tx.RollbackAsync();
    }
    [SqlFact] public async Task IntakeClearingChangeTrackerDoesNotLosePersistedCheckpoint()
    {
        await using var db = Db(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var id=await State(db); var source=new Source(Records(2)){OnImport=()=>db.ChangeTracker.Clear()};
        await new EgyptEbsSync(db,source).Tick(default); db.ChangeTracker.Clear();
        Assert.Equal(2,(await db.EbsSyncStates.SingleAsync(x=>x.Id==id)).CursorVerificationId); await tx.RollbackAsync();
    }
    [SqlFact] public async Task ActualSourceChangesQueryAcceptsSavedCursorAndReturnsOrderedBoundedBatch()
    {
        await using var db=Db();var state=await db.EbsSyncStates.AsNoTracking().OrderBy(x=>x.Id).FirstOrDefaultAsync();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["ConnectionStrings:EgyptEbsReader"]=Environment.GetEnvironmentVariable("PHEOC_EBS_SQL")
        }).Build();
        IEgyptEbsSyncSource source=new EgyptEbsReader(config,db,new EventIntake(db,new EphemeralDataProtectionProvider()),new Actor());
        var cursor=state?.CursorLocal??DateTime.Now;var tie=state?.CursorVerificationId??0;
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var records=await source.ReadChanges(cursor,tie);
        output.WriteLine($"Saved cursor query: {records.Count} candidate(s), {watch.ElapsedMilliseconds} ms; read only.");
        Assert.InRange(records.Count,0,50);
        Assert.All(records,x=>Assert.True(x.ChangedLocal>cursor||(x.ChangedLocal==cursor&&x.VerificationId>tie)));
        Assert.Equal(records.OrderBy(x=>x.ChangedLocal).ThenBy(x=>x.VerificationId),records);
        watch.Restart();var historical=await source.ReadChanges(new DateTime(1900,1,1),0);
        Assert.InRange(historical.Count,1,50);Assert.Equal(historical.OrderBy(x=>x.ChangedLocal).ThenBy(x=>x.VerificationId),historical);
        output.WriteLine($"Historical bounded query: {historical.Count} candidate(s), {watch.ElapsedMilliseconds} ms; no imports.");
    }
}
