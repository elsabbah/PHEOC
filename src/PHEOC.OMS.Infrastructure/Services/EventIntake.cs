using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Domain.Events;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Infrastructure.Services;

public class EventIntake(OmsDbContext db,IDataProtectionProvider protection) : IEventIntake
{
    public async Task<IntakeResult> Receive(string source,string key,string externalId,EventInput input)
    {
        Validator.ValidateObject(input,new(input),true);
        Require(source is "EBS" or "EgyptEBS"&&key.Length is >0 and <=128&&externalId.Length is >0 and <=128,"مصدر أو معرف رسالة غير صالح.");
        Require(input.ConfirmedAt.HasValue,"التكامل يقبل إشارات مؤكدة فقط.");
        Require(input.Latitude.HasValue==input.Longitude.HasValue,"إحداثيات غير مكتملة.");
        Require(!input.DeathsCount.HasValue||!input.CasesCount.HasValue||input.DeathsCount<=input.CasesCount,"أعداد غير صحيحة.");
        Require(new[]{input.OccurredAt,input.DetectedAt,input.NotifiedAt,input.VerifiedAt,input.ConfirmedAt}.Select(OmsService.Utc).All(d=>d==null||d<=DateTime.UtcNow.AddMinutes(5)),"تاريخ مستقبلي غير صالح.");
        Require(input.OccurredAt==null||input.DetectedAt==null||OmsService.Utc(input.DetectedAt)>=OmsService.Utc(input.OccurredAt),"الاكتشاف يسبق بداية الحدث.");
        Require(input.DetectedAt==null||input.NotifiedAt==null||OmsService.Utc(input.NotifiedAt)>=OmsService.Utc(input.DetectedAt),"الإخطار يسبق الاكتشاف.");
        Require(await db.Governorates.AnyAsync(x=>x.Id==input.GovernorateId),"المحافظة غير معروفة.");
        var payload=JsonSerializer.Serialize(input); var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        for(var attempt=0;attempt<4;attempt++)
        {
            try
            {
                await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted):null;
                var resources=new[]{source+":key:"+key,source+":event:"+externalId}.Select(x=>"PHEOC:Inbox:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(x)))).OrderBy(x=>x,StringComparer.Ordinal);
                foreach(var resource in resources)
                    await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @lockResult int; EXEC @lockResult=sys.sp_getapplock @Resource={resource}, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000; IF @lockResult<0 THROW 50001, 'Integration lock unavailable', 1;");
                var matches=await db.InboxMessages.Include(x=>x.Event).Where(x=>x.Source==source&&(x.IdempotencyKey==key||x.ExternalEventId==externalId)).OrderBy(x=>x.Id).Take(2).ToListAsync();
                if(matches.Count>1)throw new PHEOC.OMS.Domain.Rules.IdempotencyConflictException("مفتاح الرسالة ومعرف الحدث مرتبطان بسجلين مختلفين.");
                var original=matches.SingleOrDefault();
                if(original!=null)
                {
                    if(original.ExternalEventId!=externalId||original.PayloadHash!=hash)throw new PHEOC.OMS.Domain.Rules.IdempotencyConflictException("مفتاح الرسالة أو معرف الحدث مستخدم لمحتوى مختلف.");
                    if(tx!=null)await tx.CommitAsync();return new(original.EventId,original.Event.PublicId,true);
                }
                var e=OmsService.MapEvent(input);db.Events.Add(e);
                await SlaBinding.Bind(db,e);
                db.InboxMessages.Add(new(){Source=source,ExternalEventId=externalId,IdempotencyKey=key,PayloadHash=hash,ProtectedPayload=protection.CreateProtector("EBS.Inbox.v1").Protect(payload),Event=e});
                db.EventTimeline.Add(new(){Event=e,Action="Received",Summary="استلام إشارة مؤكدة من EBS",Actor="integration:EBS"});
                if(!e.IsTraining)db.Notifications.Add(new PHEOC.OMS.Domain.Operations.Notification{Event=e,Severity=EventArrival.NotificationKind,Text="تم استقبال حدث جديد من "+source});
                db.OutboxMessages.Add(new(){Event=e,Payload=JsonSerializer.Serialize(new{e.PublicId,Status="Received",Action="Received"})});await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();return new(e.Id,e.PublicId,false);
            }
            catch(Exception ex) when(attempt<3&&Retryable(ex)) { db.ChangeTracker.Clear(); await Task.Delay(50*(attempt+1)); }
        }
        throw new InvalidOperationException("تعذر حفظ الرسالة.");
    }
    private static bool Retryable(Exception ex)
    {
        for(Exception? current=ex;current!=null;current=current.InnerException)if(current is SqlException {Number:1205 or 2601 or 2627 or 50001})return true;
        return false;
    }
}
