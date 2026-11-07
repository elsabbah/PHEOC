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
public record KpiEventDetails(long EventId,string Title,IReadOnlyList<ActivityRow> Dates,IReadOnlyList<ActivityRow> Milestones,bool IsTraining);
public interface IAnalyticsService
{
    Task<IReadOnlyList<KpiEventRow>> SevenOneSevenBoard();
    Task<KpiEventDetails> KpiDetails(long eventId);
    Task RecordKpiDates(long eventId,KpiDatesInput input);
    Task RecordMilestone(long eventId,MilestoneInput input);
}
