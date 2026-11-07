using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;
public partial class EgyptEbsReader(IConfiguration config,OmsDbContext db,IEventIntake intake,ICurrentUser actor,IEgyptEbsConnectionSource? connections=null) : IEgyptEbsReader, IEgyptEbsSyncSource
{
    private void Demand(){if(!actor.Can(Permissions.AdminSettings)||actor.GovernorateId!=null)throw new AccessDeniedException();}
    public async Task<EgyptEbsStatus> Status()
    {
        Demand();
        var state=await db.EbsSyncStates.AsNoTracking().OrderBy(x=>x.Id).FirstOrDefaultAsync();
        return new(state?.Enabled??false,state?.LastSuccessAt is DateTime last?DateTime.SpecifyKind(last,DateTimeKind.Utc):null,state?.LastError??"",DateTime.UtcNow);
    }
    public async Task<EgyptEbsBoard> Candidates(int? before=null)
    {
        return await Browse(new EgyptEbsFilter(),before);
    }
    internal async Task<List<EgyptEbsSignal>> Read(int? id,int? before,DateTime? afterChange=null,int afterVerification=0)
    {
        var connectionString=(connections==null?config.GetConnectionString("EgyptEbsReader"):await connections.ConnectionString())??throw new BusinessRuleException("لم يتم إعداد حساب قراءة EgyptEBS.");
        var cs=new SqlConnectionStringBuilder(connectionString);Require(cs.InitialCatalog.Equals("EgyptEBS",StringComparison.OrdinalIgnoreCase),"قارئ EBS يجب أن يشير إلى EgyptEBS فقط.");
        await using var connection=new SqlConnection(cs.ConnectionString);await connection.OpenAsync();await using var cmd=connection.CreateCommand();cmd.CommandTimeout=30;
        cmd.CommandText="""
        WITH Geo AS (
          SELECT a.Id,a.NameAR AS GovernorateName FROM dbo.AdministrativeLevels a
          JOIN dbo.AdministrativeLevelCodeTables ac ON ac.Id=a.LevelId
          WHERE ac.Code='GV' AND a.DeletedAt IS NULL
          UNION ALL
          SELECT child.Id,parent.GovernorateName FROM dbo.AdministrativeLevels child JOIN Geo parent ON child.ParentId=parent.Id WHERE child.DeletedAt IS NULL
        ), Candidates AS (
        SELECT TOP (50) s.Id AS SignalId,v.Id AS VerificationId,v.[Date] AS VerificationDate,triage.SignalTypeId,changed.ChangedAt
        FROM dbo.Signals s
        CROSS APPLY (SELECT TOP (1) verification.Id,verification.[Date],yn.Code,COALESCE(verification.ModifiedAt,verification.CreatedAt) AS ChangedAt FROM dbo.Verifications verification JOIN dbo.YesNoCodeTables yn ON yn.Id=verification.IsValidId WHERE verification.SignalId=s.Id AND verification.DeletedAt IS NULL ORDER BY verification.[Date] DESC,verification.Id DESC) v
        CROSS APPLY (SELECT TOP (1) t.SignalTypeId,rc.Code,COALESCE(t.ModifiedAt,t.CreatedAt) AS ChangedAt FROM dbo.Triages t JOIN dbo.RelevanceCodeTables rc ON rc.Id=t.RelevanceId WHERE t.SignalId=s.Id AND t.DeletedAt IS NULL ORDER BY t.TriageDate DESC,t.Id DESC) triage
        OUTER APPLY (SELECT MAX(COALESCE(sl.ModifiedAt,sl.CreatedAt)) AS ChangedAt FROM dbo.SignalLocations sl WHERE sl.SignalId=s.Id) locationChange
        CROSS APPLY (SELECT MAX(stamps.ChangedAt) AS ChangedAt FROM (VALUES (v.ChangedAt),(triage.ChangedAt),(COALESCE(s.ModifiedAt,s.CreatedAt)),(locationChange.ChangedAt)) stamps(ChangedAt)) changed
        WHERE s.DeletedAt IS NULL AND v.Code='YES' AND triage.Code='IN'
          AND (@id IS NULL OR s.Id=@id) AND (@before IS NULL OR s.Id<@before)
          AND (@afterChange IS NULL OR changed.ChangedAt>@afterChange OR (changed.ChangedAt=@afterChange AND v.Id>@afterVerification))
        /*CANDIDATE_ORDER*/
        )
        SELECT candidate.SignalId,candidate.VerificationId,COALESCE(st.NameAR,N''),COALESCE(sc.NameAR,N''),COALESCE(loc.GovernorateName,N''),COALESCE(s.DetailedAddress,N''),LEFT(COALESCE(s.SignalInformation,N''),4000),s.EventStartDate,s.DetectionDate,candidate.VerificationDate,candidate.ChangedAt
        FROM Candidates candidate
        JOIN dbo.Signals s ON s.Id=candidate.SignalId
        LEFT JOIN dbo.SignalTypes st ON st.Id=candidate.SignalTypeId
        LEFT JOIN dbo.SignalCategoryCodeTables sc ON sc.Id=st.SignalCategoryId
        OUTER APPLY (SELECT TOP (1) geo.GovernorateName FROM dbo.SignalLocations sl JOIN Geo geo ON geo.Id=sl.LocationId WHERE sl.SignalId=candidate.SignalId AND sl.DeletedAt IS NULL ORDER BY sl.Id) loc
        /*RESULT_ORDER*/ OPTION(MAXRECURSION 12)
        """;
        cmd.CommandText=cmd.CommandText
            .Replace("/*CANDIDATE_ORDER*/",afterChange.HasValue?"ORDER BY changed.ChangedAt,v.Id":"ORDER BY s.Id DESC",StringComparison.Ordinal)
            .Replace("/*RESULT_ORDER*/",afterChange.HasValue?"ORDER BY candidate.ChangedAt,candidate.VerificationId":"ORDER BY candidate.SignalId DESC",StringComparison.Ordinal);
        cmd.Parameters.Add(new("@id",System.Data.SqlDbType.Int){Value=(object?)id??DBNull.Value});cmd.Parameters.Add(new("@before",System.Data.SqlDbType.Int){Value=(object?)before??DBNull.Value});
        cmd.Parameters.Add(new("@afterChange",System.Data.SqlDbType.DateTime2){Scale=7,Value=(object?)afterChange??DBNull.Value});cmd.Parameters.Add(new("@afterVerification",System.Data.SqlDbType.Int){Value=afterVerification});
        var result=new List<EgyptEbsSignal>();await using(var reader=await cmd.ExecuteReaderAsync())
        {while(await reader.ReadAsync())result.Add(new(reader.GetInt32(0),reader.GetInt32(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetString(6),reader.GetDateTime(7),reader.GetDateTime(8),reader.GetDateTime(9),false,reader.GetDateTime(10)));}
        var keys=result.Select(x=>x.SignalId.ToString(CultureInfo.InvariantCulture)).ToArray();var imported=await db.InboxMessages.Where(x=>x.Source=="EgyptEBS"&&keys.Contains(x.ExternalEventId)).Select(x=>new{x.ExternalEventId,x.ReceivedAt}).ToDictionaryAsync(x=>x.ExternalEventId,x=>DateTime.SpecifyKind(x.ReceivedAt,DateTimeKind.Utc));return result.Select(x=>x with{Imported=imported.ContainsKey(x.SignalId.ToString(CultureInfo.InvariantCulture)),ReceivedAtUtc=imported.TryGetValue(x.SignalId.ToString(CultureInfo.InvariantCulture),out var received)?received:null}).ToList();
    }
    public async Task<IntakeResult> Import(int signalId)
    {
        Demand();return await ImportCore(signalId);
    }
    async Task<IReadOnlyList<EgyptEbsSignal>> IEgyptEbsSyncSource.ReadChanges(DateTime afterChange,int afterVerification) => await Read(null,null,afterChange,afterVerification);
    Task<IntakeResult> IEgyptEbsSyncSource.ImportSignal(int signalId) => ImportCore(signalId);
    internal async Task<IntakeResult> ImportCore(int signalId)
    {
        var original=await db.InboxMessages.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.Source=="EgyptEBS"&&x.ExternalEventId==signalId.ToString());
        if(original!=null)return new(original.EventId,original.Event.PublicId,true);
        var signal=(await Read(signalId,null)).SingleOrDefault()??throw new BusinessRuleException("الإشارة غير موجودة أو لم تعد مؤكدة ومدرجة وفق أحدث تحقق وفرز.");
        var name=Normalize(signal.Governorate);var governors=await db.Governorates.ToListAsync();var governor=governors.SingleOrDefault(x=>Normalize(x.Name)==name)??throw new BusinessRuleException("محافظة الإشارة غير مربوطة. لا يتم استبدالها بمحافظة الجهة المبلغة.");
        DateTime Utc(DateTime value){var local=DateTime.SpecifyKind(value,DateTimeKind.Unspecified);var zone=TimeZoneInfo.FindSystemTimeZoneById(config["Integration:EgyptEbs:SourceTimeZone"]??"Egypt Standard Time");Require(!zone.IsInvalidTime(local)&&!zone.IsAmbiguousTime(local),"توقيت المصدر غامض بسبب التوقيت الصيفي ويحتاج مراجعة.");return TimeZoneInfo.ConvertTimeToUtc(local,zone);}
        return await intake.Receive("EgyptEBS",$"signal:{signal.SignalId}:verification:{signal.VerificationId}",signal.SignalId.ToString(),new(){Title=$"{signal.Type} — إشارة EBS #{signal.SignalId}",EventType=signal.Type,ThreatType=string.IsNullOrWhiteSpace(signal.Category)?"غير مصنف في المصدر":signal.Category,GovernorateId=governor.Id,Location=signal.Location,Description=signal.Description,OccurredAt=Utc(signal.EventStartLocal),DetectedAt=Utc(signal.DetectionLocal),VerifiedAt=Utc(signal.VerificationLocal),ConfirmedAt=Utc(signal.VerificationLocal)});
    }
    public async Task SetAutomatic(bool enabled)
    {
        Demand();var state=await db.EbsSyncStates.OrderBy(x=>x.Id).FirstOrDefaultAsync();if(state==null){state=new();db.EbsSyncStates.Add(state);}
        if(enabled&&!state.Enabled){state.CursorLocal=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById(config["Integration:EgyptEbs:SourceTimeZone"]??"Egypt Standard Time"));state.CursorVerificationId=0;}
        state.Enabled=enabled;state.EnabledBy=actor.Id;state.LastError="";await db.SaveChangesAsync();
    }
    private static string Normalize(string value)=>value.Trim().Replace("أ","ا").Replace("إ","ا").Replace("آ","ا").Replace("ى","ي").Replace(" ","");
}
