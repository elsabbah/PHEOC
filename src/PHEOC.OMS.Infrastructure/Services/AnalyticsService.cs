using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Rules;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;
public partial class OmsService
{
    public async Task<IReadOnlyList<KpiEventRow>> SevenOneSevenBoard()
    {
        Demand(Permissions.ReportsView);var events=await ScopedEvents().Where(x=>!x.IsTraining).OrderByDescending(x=>x.Id).Take(500).ToListAsync();var ids=events.Select(x=>x.Id).ToArray();
        var dates=await db.KpiDateRecords.Where(x=>ids.Contains(x.EventId)).ToListAsync();var milestones=await db.EarlyResponseMilestones.Where(x=>ids.Contains(x.EventId)).ToListAsync();
        return events.Select(e=>{var d=dates.Where(x=>x.EventId==e.Id).MaxBy(x=>x.Id);var actions=milestones.Where(x=>x.EventId==e.Id).GroupBy(x=>x.Action).Select(g=>g.MaxBy(x=>x.Id)!).Select(x=>new ActionMilestone(x.Action,x.Date,x.NotApplicable)).ToList();return new KpiEventRow(e.Id,e.Title,d?.Excluded??false,d?.Evidence??"لم توثق تواريخ المؤشر بعد",d?.MethodologyVersion??SevenOneSeven.Version,SevenOneSeven.Calculate(d?.Emergence,d?.Detection,d?.Notification,actions));}).ToList();
    }
    public async Task<KpiEventDetails> KpiDetails(long eventId)
    {
        Demand(Permissions.ReportsView);var e=await Load(eventId);
        var dates=await db.KpiDateRecords.Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Id).ToListAsync();var milestones=await db.EarlyResponseMilestones.Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Id).ToListAsync();
        var latest=dates.FirstOrDefault();var current=latest==null?null:new KpiDatesInput{Emergence=latest.Emergence,Detection=latest.Detection,Notification=latest.Notification,Excluded=latest.Excluded,Evidence=latest.Evidence};
        var hasSource=await db.InboxMessages.AnyAsync(x=>x.EventId==eventId&&x.Source=="EgyptEBS");
        return new(eventId,e.Title,dates.Select(x=>new ActivityRow(x.Id,x.MethodologyVersion,$"النشوء: {x.Emergence} | الاكتشاف: {x.Detection} | الإخطار: {x.Notification} — {x.Evidence}",x.RecordedBy,x.CreatedAt,x.Excluded?"مستبعد":"مشمول")).ToList(),milestones.Select(x=>new ActivityRow(x.Id,SevenOneSeven.Names[x.Action],$"{(x.NotApplicable?"لا ينطبق":x.Date?.ToString()??"غير معروف")} — {x.Evidence}",x.RecordedBy,x.CreatedAt)).ToList(),e.IsTraining,current,hasSource);
    }
    public async Task<EgyptEbsKpiDates> KpiSourceDates(long eventId)
    {
        Demand(Permissions.ReportsView);await Load(eventId);
        var externalId=await db.InboxMessages.Where(x=>x.EventId==eventId&&x.Source=="EgyptEBS").Select(x=>x.ExternalEventId).FirstOrDefaultAsync();
        Require(int.TryParse(externalId,out var signalId)&&signalId>0,"الحدث غير مرتبط بإشارة EgyptEBS قابلة للقراءة.");
        Require(kpiSource!=null,"قارئ تواريخ EgyptEBS غير مهيأ.");
        return await kpiSource!.Read(signalId);
    }
    public async Task RecordKpiDates(long eventId,KpiDatesInput input)
    {
        Demand(Permissions.ReportsCreate);Validate(input);var e=await Load(eventId);var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")));
        Require(new[]{input.Emergence,input.Detection,input.Notification}.All(x=>x==null||x<=today),"تاريخ مؤشر مستقبلي.");
        Require(input.Emergence==null||input.Detection==null||input.Detection>=input.Emergence,"الاكتشاف يسبق النشوء.");Require(input.Detection==null||input.Notification==null||input.Notification>=input.Detection,"الإخطار يسبق الاكتشاف.");
        db.KpiDateRecords.Add(new(){EventId=eventId,Emergence=input.Emergence,Detection=input.Detection,Notification=input.Notification,Excluded=input.Excluded,Evidence=input.Evidence,RecordedBy=user.Id});Timeline(e,"KpiDates","توثيق تواريخ 7-1-7 بقيد جديد");await db.SaveChangesAsync();
    }
    public async Task RecordMilestone(long eventId,MilestoneInput input)
    {
        Demand(Permissions.ReportsCreate);Validate(input);var e=await Load(eventId);Require(Enum.IsDefined(input.Action),"إجراء غير معروف.");Require(!input.NotApplicable||input.Date==null,"لا ينطبق لا يجمع مع تاريخ إجراء.");var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")));Require(input.Date==null||input.Date<=today,"تاريخ إجراء مستقبلي.");
        db.EarlyResponseMilestones.Add(new(){EventId=eventId,Action=input.Action,Date=input.Date,NotApplicable=input.NotApplicable,Evidence=input.Evidence,RecordedBy=user.Id});Timeline(e,"EarlyResponseMilestone","توثيق: "+SevenOneSeven.Names[input.Action]);await db.SaveChangesAsync();
    }
}
