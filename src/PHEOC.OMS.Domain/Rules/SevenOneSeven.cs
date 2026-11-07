namespace PHEOC.OMS.Domain.Rules;
public enum EarlyResponseAction { Investigation=1, EpidemiologyAndRisk=2, LaboratoryConfirmation=3, CaseManagementAndIpc=4, PublicHealthMeasures=5, RiskCommunication=6, Coordination=7 }
public record ActionMilestone(EarlyResponseAction Action,DateOnly? Date,bool NotApplicable);
public record SevenOneSevenResult(int? DetectionDays,int? NotificationDays,int? ResponseDays,bool? DetectionMet,bool? NotificationMet,bool? ResponseMet,bool? OverallMet,string DataIssue);
public static class SevenOneSeven
{
    public const string Version="717-alliance-2024/date-difference-v1";
    public static readonly IReadOnlyDictionary<EarlyResponseAction,string> Names=new Dictionary<EarlyResponseAction,string>{
        [EarlyResponseAction.Investigation]="بدء الاستقصاء أو نشر فريق الاستجابة",
        [EarlyResponseAction.EpidemiologyAndRisk]="التحليل الوبائي والتقييم الأولي للمخاطر",
        [EarlyResponseAction.LaboratoryConfirmation]="تأكيد المسبب معملياً",
        [EarlyResponseAction.CaseManagementAndIpc]="بدء تدبير الحالات ومكافحة العدوى بالمنشآت",
        [EarlyResponseAction.PublicHealthMeasures]="بدء تدابير الصحة العامة بالمجتمع",
        [EarlyResponseAction.RiskCommunication]="بدء التواصل بشأن المخاطر وإشراك المجتمع",
        [EarlyResponseAction.Coordination]="إنشاء آلية تنسيق"};
    public static SevenOneSevenResult Calculate(DateOnly? emergence,DateOnly? detection,DateOnly? notification,IReadOnlyList<ActionMilestone> milestones)
    {
        int? Difference(DateOnly? a,DateOnly? b)=>a.HasValue&&b.HasValue&&b>=a?b.Value.DayNumber-a.Value.DayNumber:null;
        var d=Difference(emergence,detection);var n=Difference(detection,notification);
        var complete=Enum.GetValues<EarlyResponseAction>().All(action=>milestones.Any(x=>x.Action==action&&(x.NotApplicable||x.Date.HasValue)));
        var applicable=milestones.Where(x=>!x.NotApplicable&&x.Date.HasValue).Select(x=>x.Date!.Value).ToList();
        var r=complete&&applicable.Count>0?Difference(notification,applicable.Max()):null;
        bool? Met(int? value,int threshold)=>value.HasValue?value<=threshold:null;
        var dm=Met(d,7);var nm=Met(n,1);var rm=Met(r,7);bool? overall=dm.HasValue&&nm.HasValue&&rm.HasValue?dm.Value&&nm.Value&&rm.Value:null;
        return new(d,n,r,dm,nm,rm,overall,overall.HasValue?"":"تواريخ ناقصة أو غير متسلسلة، أو إجراءات استجابة غير موثقة بالكامل.");
    }
}
