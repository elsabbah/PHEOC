using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.Application.Contracts;
public class KpiDatesInput
{
    public DateOnly? Emergence { get; set; }
    public DateOnly? Detection { get; set; }
    public DateOnly? Notification { get; set; }
    public bool Excluded { get; set; }
    [Required,StringLength(2000)] public string Evidence { get; set; } = "";
}
public class MilestoneInput
{
    public EarlyResponseAction Action { get; set; }
    public DateOnly? Date { get; set; }
    public bool NotApplicable { get; set; }
    [Required,StringLength(2000)] public string Evidence { get; set; } = "";
}
public record KpiEventRow(long Id,string Title,bool Excluded,string Evidence,string Version,SevenOneSevenResult Result);
public record KpiEventDetails(long EventId,string Title,IReadOnlyList<ActivityRow> Dates,IReadOnlyList<ActivityRow> Milestones,bool IsTraining,KpiDatesInput? CurrentDates=null,bool HasEgyptEbsSource=false);
public record EgyptEbsFirstResponse(int Id,DateTime CreatedAtLocal,bool IsConfirmed);
public record EgyptEbsKpiDates(int SignalId,DateOnly? Emergence,DateOnly? Detection,DateOnly? Notification,string Evidence,DateTime ReadAtUtc,string Notice,EgyptEbsFirstResponse? FirstResponse=null);
public interface IEgyptEbsKpiDateSource
{
    Task<EgyptEbsKpiDates> Read(int signalId);
}
public interface IAnalyticsService
{
    Task<IReadOnlyList<KpiEventRow>> SevenOneSevenBoard();
    Task<KpiEventDetails> KpiDetails(long eventId);
    Task<EgyptEbsKpiDates> KpiSourceDates(long eventId);
    Task RecordKpiDates(long eventId,KpiDatesInput input);
    Task RecordMilestone(long eventId,MilestoneInput input);
}
