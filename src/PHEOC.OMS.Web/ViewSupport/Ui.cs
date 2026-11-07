using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Web.ViewSupport;
public static class Ui
{
    public static string Time(DateTime? utc)=>utc.HasValue?TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value,DateTimeKind.Utc),TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")).ToString("yyyy/MM/dd  HH:mm"):"غير مسجل";
    public static string Status(EventStatus status)=>status switch{EventStatus.Received=>"وارد",EventStatus.UnderAssessment=>"قيد التقييم",EventStatus.Monitoring=>"تحت المراقبة",EventStatus.Activated=>"استجابة مفعّلة",EventStatus.ResponseOngoing=>"استجابة جارية",EventStatus.Closed=>"مغلق",_=>"غير معروف"};
    public static string Task(TaskState state)=>state switch{TaskState.New=>"جديدة",TaskState.Assigned=>"مسندة",TaskState.InProgress=>"قيد التنفيذ",TaskState.Pending=>"معلّقة",TaskState.Completed=>"مكتملة",TaskState.Cancelled=>"ملغاة",_=>""};
    public static string Team(TeamState state)=>state switch{TeamState.Ready=>"جاهز",TeamState.Assigned=>"مكلف",TeamState.Mobilizing=>"تجهيز",TeamState.EnRoute=>"في الطريق",TeamState.OnSite=>"في الموقع",TeamState.Returning=>"عائد",TeamState.Resting=>"راحة",_=>"غير متاح"};
}
