using System.Data;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Domain.Rules;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;

public partial class EgyptEbsReader
{
    public async Task<EgyptEbsBoard> Browse(EgyptEbsFilter filter,int? before=null,bool includeLookups=true)
    {
        Demand();System.ComponentModel.DataAnnotations.Validator.ValidateObject(filter,new(filter),true);
        Require(filter.PageSize is 20 or 50 or 100,"اختر 20 أو 50 أو 100 إشارة في الصفحة.");
        Require(filter.Receipt is "all" or "pending" or "received","حالة استقبال غير صالحة.");
        Require(filter.Display is "table" or "cards","طريقة عرض غير صالحة.");
        Require(!filter.From.HasValue||!filter.To.HasValue||filter.From<=filter.To,"تاريخ التحقق من يجب ألا يتجاوز تاريخ إلى.");
        Require(filter.To!=DateOnly.MaxValue,"تاريخ نهاية غير صالح.");
        Require(before==null||before>0,"معرف إشارة غير صالح.");
        filter.Search=filter.Search?.Trim();filter.Governorate=filter.Governorate?.Trim();
        if(string.IsNullOrWhiteSpace(config.GetConnectionString("EgyptEbsReader")))return new(false,[],Filter:filter,Before:before);
        var sync=await db.EbsSyncStates.AsNoTracking().OrderBy(x=>x.Id).FirstOrDefaultAsync();
        var importedKeys=filter.Receipt=="all"?[]:await db.InboxMessages.AsNoTracking().Where(x=>x.Source=="EgyptEBS").Select(x=>x.ExternalEventId).ToListAsync();
        var importedIds=importedKeys.Select(key=>int.TryParse(key,NumberStyles.None,CultureInfo.InvariantCulture,out var id)?id:0).Where(id=>id>0).Distinct();
        var xml=new XElement("ids",importedIds.Select(id=>new XElement("id",id))).ToString(SaveOptions.DisableFormatting);
        var original=(connections==null?config.GetConnectionString("EgyptEbsReader"):await connections.ConnectionString())??throw new BusinessRuleException("لم يتم إعداد اتصال EgyptEBS.");
        var cs=new SqlConnectionStringBuilder(original);Require(cs.InitialCatalog.Equals("EgyptEBS",StringComparison.OrdinalIgnoreCase),"قارئ EBS يجب أن يشير إلى EgyptEBS فقط.");
        await using var connection=new SqlConnection(cs.ConnectionString);await connection.OpenAsync();
        await using var command=connection.CreateCommand();command.CommandTimeout=30;
        string? search=string.IsNullOrWhiteSpace(filter.Search)?null:filter.Search.TrimStart('#').Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[");
        int? signalId=int.TryParse(filter.Search?.TrimStart('#'),NumberStyles.None,CultureInfo.InvariantCulture,out var parsed)?parsed:null;
        if(signalId.HasValue)search=null;
        var withLocation=search!=null||!string.IsNullOrWhiteSpace(filter.Governorate);
        const string sql="""
        WITH Geo AS (
          SELECT a.Id,a.NameAR AS GovernorateName FROM dbo.AdministrativeLevels a
          JOIN dbo.AdministrativeLevelCodeTables ac ON ac.Id=a.LevelId WHERE ac.Code='GV' AND a.DeletedAt IS NULL
          UNION ALL
          SELECT child.Id,parent.GovernorateName FROM dbo.AdministrativeLevels child JOIN Geo parent ON child.ParentId=parent.Id WHERE child.DeletedAt IS NULL
        ), Received AS (
          SELECT item.value('(text())[1]','int') AS SignalId FROM @received.nodes('/ids/id') AS ids(item)
        ), VerificationRanks AS (
          SELECT v.Id,v.SignalId,v.[Date],yn.Code,ROW_NUMBER() OVER(PARTITION BY v.SignalId ORDER BY v.[Date] DESC,v.Id DESC) AS Position
          FROM dbo.Verifications v JOIN dbo.YesNoCodeTables yn ON yn.Id=v.IsValidId
          WHERE v.DeletedAt IS NULL AND (@signal IS NULL OR v.SignalId=@signal)
        ), TriageRanks AS (
          SELECT t.SignalId,t.SignalTypeId,rc.Code,ROW_NUMBER() OVER(PARTITION BY t.SignalId ORDER BY t.TriageDate DESC,t.Id DESC) AS Position
          FROM dbo.Triages t JOIN dbo.RelevanceCodeTables rc ON rc.Id=t.RelevanceId
          WHERE t.DeletedAt IS NULL AND (@signal IS NULL OR t.SignalId=@signal)
        ), LocationRanks AS (
          SELECT sl.SignalId,geo.GovernorateName,ROW_NUMBER() OVER(PARTITION BY sl.SignalId ORDER BY sl.Id) AS Position
          FROM dbo.SignalLocations sl JOIN Geo geo ON geo.Id=sl.LocationId WHERE sl.DeletedAt IS NULL
        ), Eligible AS (
          SELECT s.Id AS SignalId,v.Id AS VerificationId,v.[Date] AS VerificationDate,triage.SignalTypeId
          FROM dbo.Signals s
          JOIN VerificationRanks v ON v.SignalId=s.Id AND v.Position=1
          JOIN TriageRanks triage ON triage.SignalId=s.Id AND triage.Position=1
          /*FILTER_JOINS*/
          WHERE s.DeletedAt IS NULL AND v.Code='YES' AND triage.Code='IN'
            AND (@signal IS NULL OR s.Id=@signal) AND (@before IS NULL OR s.Id<@before)
            AND (@type IS NULL OR triage.SignalTypeId=@type)
            AND (@from IS NULL OR v.[Date]>=@from) AND (@to IS NULL OR v.[Date]<@to)
            AND (@receipt='all' OR (@receipt='received' AND EXISTS(SELECT 1 FROM Received r WHERE r.SignalId=s.Id)) OR (@receipt='pending' AND NOT EXISTS(SELECT 1 FROM Received r WHERE r.SignalId=s.Id)))
            /*FILTER_PREDICATES*/
        ), Numbered AS (
          SELECT *,COUNT(*) OVER() AS TotalCount,ROW_NUMBER() OVER(ORDER BY SignalId DESC) AS RowNumber FROM Eligible
        ), Paged AS (
          SELECT *,CASE WHEN @page>(TotalCount-1)/@take+1 THEN (TotalCount-1)/@take+1 ELSE @page END AS ActualPage FROM Numbered
        )
        SELECT p.TotalCount,p.ActualPage,p.SignalId,p.VerificationId,COALESCE(st.NameAR,N''),COALESCE(sc.NameAR,N''),COALESCE(loc.GovernorateName,N''),
          COALESCE(s.DetailedAddress,N''),LEFT(COALESCE(s.SignalInformation,N''),4000),s.EventStartDate,s.DetectionDate,p.VerificationDate
        FROM Paged p JOIN dbo.Signals s ON s.Id=p.SignalId
        LEFT JOIN dbo.SignalTypes st ON st.Id=p.SignalTypeId
        LEFT JOIN dbo.SignalCategoryCodeTables sc ON sc.Id=st.SignalCategoryId
        OUTER APPLY (SELECT TOP (1) geo.GovernorateName FROM dbo.SignalLocations sl JOIN Geo geo ON geo.Id=sl.LocationId WHERE sl.SignalId=p.SignalId AND sl.DeletedAt IS NULL ORDER BY sl.Id) loc
        WHERE p.RowNumber BETWEEN (p.ActualPage-1)*@take+1 AND p.ActualPage*@take
        ORDER BY p.SignalId DESC OPTION(RECOMPILE,MAXRECURSION 12);
        """;
        var joins=(withLocation?"LEFT JOIN LocationRanks loc ON loc.SignalId=s.Id AND loc.Position=1 ":"")+
            (search!=null?"LEFT JOIN dbo.SignalTypes st ON st.Id=triage.SignalTypeId LEFT JOIN dbo.SignalCategoryCodeTables sc ON sc.Id=st.SignalCategoryId ":"");
        var predicates=(!string.IsNullOrWhiteSpace(filter.Governorate)?"AND loc.GovernorateName=@governorate ":"")+
            (search!=null?"AND (st.NameAR LIKE @search ESCAPE N'~' OR sc.NameAR LIKE @search ESCAPE N'~' OR s.SignalInformation LIKE @search ESCAPE N'~' OR s.DetailedAddress LIKE @search ESCAPE N'~' OR loc.GovernorateName LIKE @search ESCAPE N'~') ":"");
        command.CommandText=sql.Replace("/*FILTER_JOINS*/",joins,StringComparison.Ordinal).Replace("/*FILTER_PREDICATES*/",predicates,StringComparison.Ordinal);
        if(includeLookups)command.CommandText+="SELECT a.NameAR FROM dbo.AdministrativeLevels a JOIN dbo.AdministrativeLevelCodeTables ac ON ac.Id=a.LevelId WHERE ac.Code='GV' AND a.DeletedAt IS NULL AND a.NameAR IS NOT NULL GROUP BY a.NameAR ORDER BY a.NameAR; SELECT Id,COALESCE(NameAR,N'') FROM dbo.SignalTypes ORDER BY NameAR,Id;";
        object Db(object? value)=>value??DBNull.Value;

        command.Parameters.Add(new("@signal",SqlDbType.Int){Value=Db(signalId)});command.Parameters.Add(new("@received",SqlDbType.Xml){Value=xml});command.Parameters.Add(new("@receipt",SqlDbType.VarChar,10){Value=filter.Receipt});
        command.Parameters.Add(new("@search",SqlDbType.NVarChar,600){Value=Db(search==null?null:"%"+search+"%")});
        command.Parameters.Add(new("@governorate",SqlDbType.NVarChar,120){Value=Db(string.IsNullOrWhiteSpace(filter.Governorate)?null:filter.Governorate)});
        command.Parameters.Add(new("@type",SqlDbType.Int){Value=Db(filter.TypeId)});command.Parameters.Add(new("@before",SqlDbType.Int){Value=Db(before)});
        command.Parameters.Add(new("@from",SqlDbType.DateTime2){Value=Db(filter.From?.ToDateTime(TimeOnly.MinValue))});
        command.Parameters.Add(new("@to",SqlDbType.DateTime2){Value=Db(filter.To?.AddDays(1).ToDateTime(TimeOnly.MinValue))});
        command.Parameters.Add(new("@page",SqlDbType.Int){Value=filter.Page});command.Parameters.Add(new("@take",SqlDbType.Int){Value=filter.PageSize});
        var signals=new List<EgyptEbsSignal>();var governorates=new List<string>();var types=new List<Lookup>();int total=0,actualPage=1;
        await using(var reader=await command.ExecuteReaderAsync())
        {
            while(await reader.ReadAsync()) { total=reader.GetInt32(0);actualPage=reader.GetInt32(1);signals.Add(new(reader.GetInt32(2),reader.GetInt32(3),reader.GetString(4),reader.GetString(5),reader.GetString(6),reader.GetString(7),reader.GetString(8),reader.GetDateTime(9),reader.GetDateTime(10),reader.GetDateTime(11),false,default)); }
            if(includeLookups)
            {
                await reader.NextResultAsync();while(await reader.ReadAsync())governorates.Add(reader.GetString(0));
                await reader.NextResultAsync();while(await reader.ReadAsync())types.Add(new(reader.GetInt32(0),reader.GetString(1)));
            }
        }
        filter.Page=actualPage;
        var keys=signals.Select(s=>s.SignalId.ToString(CultureInfo.InvariantCulture)).ToArray();
        var received=await db.InboxMessages.AsNoTracking().Where(x=>x.Source=="EgyptEBS"&&keys.Contains(x.ExternalEventId)).Select(x=>new{x.ExternalEventId,x.ReceivedAt}).ToDictionaryAsync(x=>x.ExternalEventId,x=>DateTime.SpecifyKind(x.ReceivedAt,DateTimeKind.Utc));
        signals=signals.Select(s=>received.TryGetValue(s.SignalId.ToString(CultureInfo.InvariantCulture),out var at)?s with{Imported=true,ReceivedAtUtc=at}:s).ToList();
        return new(true,signals,sync?.Enabled??false,sync?.LastSuccessAt,sync?.LastError??"",filter,total,governorates,types,before);
    }
}
