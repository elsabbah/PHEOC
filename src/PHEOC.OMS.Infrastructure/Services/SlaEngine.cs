using System.Data;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Infrastructure.Services;
public static class SlaBinding
{
    public static async Task Bind(OmsDbContext db,EmergencyEvent e)
    {
        if(e.IsTraining)return;var definitions=await db.SlaDefinitions.ToListAsync();
        foreach(var rule in definitions.GroupBy(x=>x.Stage).Select(g=>g.MaxBy(x=>x.Version)!))db.EventSlas.Add(new(){Event=e,DefinitionId=rule.Id,StartedAt=e.CreatedAt});
    }
}
public class SlaEngine(OmsDbContext db)
{
    public async Task Tick(CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,token);var now=DateTime.UtcNow;
        var rules=await db.EventSlas.Include(x=>x.Event).Include(x=>x.Definition).Where(x=>x.CompletedAt==null&&!x.Event.IsTraining).OrderBy(x=>x.LastEvaluatedAt??x.StartedAt).ThenBy(x=>x.Id).Take(100).ToListAsync(token);
        foreach(var rule in rules)
        {
            rule.LastEvaluatedAt=now;var e=rule.Event;var stage=rule.Definition.Stage;
            DateTime? complete=stage switch{
                "Acknowledge"=>e.AcknowledgedAt,
                "Assessment"=>await db.QiraAssessments.Where(x=>x.EventId==e.Id).MinAsync(x=>x.CompletedAt,token) ?? await db.RiskAssessments.Where(x=>x.EventId==e.Id).MinAsync(x=>(DateTime?)x.CreatedAt,token),
                "Activation"=>await db.ActivationDecisions.Where(x=>x.EventId==e.Id).MinAsync(x=>(DateTime?)x.CreatedAt,token),
                "Deployment"=>await db.Deployments.Where(x=>x.EventId==e.Id).MinAsync(x=>(DateTime?)x.CreatedAt,token),
                "FirstResponse"=>e.FirstResponseAt,_=>null};
            if(complete.HasValue){rule.CompletedAt=complete;rule.Compliant=complete.Value<=rule.StartedAt.AddMinutes(rule.Definition.TargetMinutes);continue;}
            if(e.Status==EventStatus.Closed){rule.CompletedAt=e.ClosedAt??now;rule.Compliant=false;continue;}
            var overdue=(now-rule.StartedAt).TotalMinutes-rule.Definition.TargetMinutes;if(overdue<0)continue;
            var level=Math.Min(3,1+(int)(overdue/rule.Definition.EscalateEveryMinutes));
            if(await db.SlaEscalations.AnyAsync(x=>x.EventSlaId==rule.Id&&x.Level==level,token))continue;
            db.SlaEscalations.Add(new(){EventSlaId=rule.Id,Level=level});db.Notifications.Add(new(){EventId=e.Id,Severity="Warning",Text=$"تجاوز {rule.Definition.Name} المدة المحددة — تصعيد مستوى {level}"});
        }
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);
    }
}
