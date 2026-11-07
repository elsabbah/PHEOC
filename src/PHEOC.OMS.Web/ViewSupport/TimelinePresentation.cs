using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.ViewSupport;
public record TimelineAppearance(string Label,string Tone,string Icon);
public static class TimelinePresentation
{
    public static DateTime EgyptLocal(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc,DateTimeKind.Utc),TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
    public static TimelineAppearance For(ActivityRow item)=>item.Type switch
    {
        "Assignment" when item.Text.Contains("نائب",StringComparison.Ordinal)=>new("تعيين مسؤول","green","hierarchy"),
        "Assignment" when item.Text.Contains("تعيين مدير",StringComparison.Ordinal)=>new("تعيين مسؤول","gold","shield"),
        "Assignment" when item.Text.Contains("رئيس",StringComparison.Ordinal)=>new("تعيين مسؤول","blue","team"),
        "Assignment"=>new("تعيين مسؤول","blue","person"),
        "Received"=>new("استقبال الحدث","blue","inbox"),
        "UnderAssessment" or "RiskAssessed"=>new("تقييم المخاطر","gold","shield"),
        "QiraStarted"=>new("بدء تقييم QIRA","gold","shield"),
        "QiraDraftDeleted"=>new("حذف مسودة QIRA","neutral","clipboard"),
        "QiraCompleted"=>new("إكمال تقييم QIRA","teal","clipboard"),
        "QiraApproved"=>new("اعتماد تقييم QIRA","green","shield"),
        "QiraActions"=>new("إجراءات QIRA","blue","clipboard"),
        "Activated"=>new("تفعيل الاستجابة","green","shield"),
        "Monitoring"=>new("تحت المراقبة","blue","clock"),
        "ResponseOngoing"=>new("استجابة جارية","green","team"),
        "TaskCreated" or "TaskUpdated"=>new("متابعة مهمة","blue","clipboard"),
        "Deployed" or "TeamArrived" or "TeamReturned"=>new(item.Type=="Deployed"?"تكليف فريق":item.Type=="TeamArrived"?"وصول فريق":"عودة فريق","green","team"),
        "Decision"=>new("تسجيل قرار","gold","shield"),
        "FieldReport" or "SitRepCreated" or "SitRepApproved" or "AAR"=>new("تقارير ومراجعة","teal","clipboard"),
        "KpiDates" or "EarlyResponseMilestone"=>new("توثيق 7-1-7","teal","calendar"),
        "ClosureRequested" or "Closed"=>new(item.Type=="Closed"?"إغلاق الحدث":"طلب إغلاق","green","shield"),
        "ContactRegistered" or "ContactFollowUp"=>new("متابعة مخالط","teal","person"),
        "SampleRegistered" or "SampleTransferred" or "SampleResult"=>new("متابعة عينة","teal","clipboard"),
        "Message"=>new("اتصالات الحدث","blue","message"),
        "Attachment"=>new("إضافة مرفق","blue","clipboard"),
        "EmergencySOS"=>new("نداء طوارئ","red","shield"),
        _=>new(string.IsNullOrWhiteSpace(item.Type)?"إجراء":item.Type,"neutral","clock")
    };
}
