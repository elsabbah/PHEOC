using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Infrastructure.Services;
public class EgyptEbsSync(OmsDbContext db,IEgyptEbsSyncSource reader)
{
    public async Task Tick(CancellationToken token)
    {
        var state=await db.EbsSyncStates.OrderBy(x=>x.Id).FirstOrDefaultAsync(token);if(state?.Enabled!=true)return;
        int? processingSignal=null;
        try
        {
            var records=await reader.ReadChanges(state.CursorLocal,state.CursorVerificationId);
            foreach(var record in records)
            {
                processingSignal=record.SignalId;token.ThrowIfCancellationRequested();await reader.ImportSignal(record.SignalId);
                // Intake retry may clear tracking after a SQL deadlock; reacquire the checkpoint before saving.
                if(db.Entry(state).State==EntityState.Detached)state=await db.EbsSyncStates.SingleAsync(x=>x.Id==state.Id,token);
                if(!state.Enabled)return;
                state.CursorLocal=record.ChangedLocal;state.CursorVerificationId=record.VerificationId;state.LastSuccessAt=DateTime.UtcNow;state.LastError="";await db.SaveChangesAsync(token);
            }
            state.LastSuccessAt=DateTime.UtcNow;state.LastError="";await db.SaveChangesAsync(token);
        }
        catch(Exception ex)when(ex is not OperationCanceledException)
        {
            // The checkpoint is not advanced for a failed import. No source data or credentials enter the log.
            db.ChangeTracker.Clear();state=await db.EbsSyncStates.OrderBy(x=>x.Id).FirstAsync(token);var detail=ex is BusinessRuleException?ex.Message:ex.GetType().Name;state.LastError=($"فشل الاستقبال التلقائي{(processingSignal.HasValue?$" للإشارة #{processingSignal}":"")}: "+detail);if(state.LastError.Length>200)state.LastError=state.LastError[..200];await db.SaveChangesAsync(token);
        }
    }
}
