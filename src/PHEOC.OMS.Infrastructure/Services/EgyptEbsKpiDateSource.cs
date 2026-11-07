using System.Globalization;
using Microsoft.Data.SqlClient;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.Infrastructure.Services;

public sealed class EgyptEbsKpiDateSource(IEgyptEbsConnectionSource connections):IEgyptEbsKpiDateSource
{
    public async Task<EgyptEbsKpiDates> Read(int signalId)
    {
        OperationalRules.Require(signalId>0,"معرف إشارة غير صالح.");
        var original=await connections.ConnectionString();OperationalRules.Require(!string.IsNullOrWhiteSpace(original),"لم يتم إعداد حساب قراءة EgyptEBS.");
        var settings=new SqlConnectionStringBuilder(original){ConnectTimeout=20};
        OperationalRules.Require(settings.InitialCatalog.Equals("EgyptEBS",StringComparison.OrdinalIgnoreCase),"قارئ التواريخ يجب أن يشير إلى EgyptEBS فقط.");
        try
        {
            await using var connection=new SqlConnection(settings.ConnectionString);using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));await connection.OpenAsync(timeout.Token);
            await using var command=connection.CreateCommand();command.CommandTimeout=10;
            command.CommandText="SELECT EventStartDate,DetectionDate FROM dbo.Signals WHERE Id=@id AND DeletedAt IS NULL";
            command.Parameters.Add(new("@id",System.Data.SqlDbType.Int){Value=signalId});
            DateOnly? emergence;DateOnly? detection;
            await using(var reader=await command.ExecuteReaderAsync(timeout.Token))
            {
                OperationalRules.Require(await reader.ReadAsync(timeout.Token),"إشارة المصدر غير موجودة أو محذوفة؛ لم تتغير التواريخ الموثقة.");
                // Source-local calendar dates: do not round-trip through UTC or the browser timezone.
                emergence=reader.IsDBNull(0)?null:DateOnly.FromDateTime(reader.GetDateTime(0));
                detection=reader.IsDBNull(1)?null:DateOnly.FromDateTime(reader.GetDateTime(1));
            }
            EgyptEbsFirstResponse? response=null;var responseNotice="";
            command.CommandText="SELECT TOP(1) Id,CreatedAt,IsConfirmed FROM dbo.Responses WHERE SignalId=@id AND DeletedAt IS NULL ORDER BY CreatedAt,Id";
            try
            {
                await using var reader=await command.ExecuteReaderAsync(timeout.Token);
                if(await reader.ReadAsync(timeout.Token))response=new(reader.GetInt32(0),DateTime.SpecifyKind(reader.GetDateTime(1),DateTimeKind.Unspecified),reader.GetBoolean(2));
                else responseNotice="لا توجد استجابة مسجلة غير محذوفة لهذه الإشارة.";
            }
            catch(SqlException ex) when(ex.Number==229){responseNotice="حساب المصدر لا يملك قراءة Responses؛ يلزم إذن قراءة مستقل دون تعديل المصدر.";}
            var now=DateTime.UtcNow;
            var notification=response==null?(DateOnly?)null:DateOnly.FromDateTime(response.CreatedAtLocal);
            string Text(DateOnly? value)=>value?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"غير متاح";
            var responseEvidence=response==null?"Responses.CreatedAt=غير متاح":$"Responses.Id={response.Id}; Responses.CreatedAt={response.CreatedAtLocal.ToString("yyyy-MM-ddTHH:mm:ss.fffffff",CultureInfo.InvariantCulture)} (وقت المصدر); IsConfirmed={response.IsConfirmed}";
            var evidence=$"EgyptEBS / source-map-v2 / Signals.Id={signalId}; Signals.EventStartDate={Text(emergence)}; Signals.DetectionDate={Text(detection)}; {responseEvidence}; Notification={Text(notification)}; قراءة UTC={now:O}. حسب تعريف المستخدم: أقدم Responses.CreatedAt غير محذوف هو تاريخ إخطار أول جهة مسؤولة عن الاستجابة. بداية الحدث مرشح للنشوء يحتاج مراجعة المُوثّق.";
            return new(signalId,emergence,detection,notification,evidence,now,"الإخطار مأخوذ من أقدم Responses.CreatedAt غير محذوف وفق تعريف المستخدم، بتاريخ المصدر المحلي. عند غياب الاستجابة يظل الإخطار غير متاح. راجع مطابقة بداية الحدث لتعريف النشوء قبل الحفظ. الجلب لا ينشئ قيداً موثقاً تلقائياً ولا يحدد تواريخ الإجراءات السبعة. "+responseNotice,response);
        }
        catch(SqlException){throw new BusinessRuleException("تعذر قراءة تواريخ EgyptEBS. راجع اتصال المصدر وصلاحيات القراءة؛ التواريخ المحفوظة لم تتغير.");}
        catch(OperationCanceledException){throw new BusinessRuleException("انتهت مهلة قراءة تواريخ EgyptEBS؛ التواريخ المحفوظة لم تتغير.");}
    }
}
