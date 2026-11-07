using PHEOC.OMS.Domain.Qira;
namespace PHEOC.OMS.Web.ViewSupport;
public static class QiraText
{
    public static string Risk(QiraRisk? value)=>value switch{QiraRisk.VeryLow=>"منخفض جداً",QiraRisk.Low=>"منخفض",QiraRisk.Moderate=>"متوسط",QiraRisk.High=>"مرتفع",QiraRisk.VeryHigh=>"مرتفع جداً",_=>"لم يُحدد بعد"};
    public static string State(QiraState value)=>value switch{QiraState.New=>"جديد",QiraState.InProgress=>"قيد الإعداد",QiraState.Completed=>"مكتمل — بانتظار المراجعة",QiraState.Reviewed=>"تمت المراجعة — بانتظار الاعتماد",QiraState.Approved=>"معتمد",_=>""};
    public static string Confidence(QiraConfidence? value)=>value switch{QiraConfidence.High=>"عالية",QiraConfidence.Moderate=>"متوسطة",QiraConfidence.Low=>"منخفضة",_=>"لم تُحدد"};
    public static string StageClass(QiraState value)=>value switch{QiraState.New=>"preparation",QiraState.InProgress=>"assessment",QiraState.Completed=>"review",QiraState.Reviewed=>"approval",QiraState.Approved=>"approved",_=>"deleted"};
    public static string StageLabel(QiraState value)=>value switch{QiraState.New=>"الإعداد · مسودة جديدة",QiraState.InProgress=>"التقييم · جارٍ استكمال الأسئلة",QiraState.Completed=>"المراجعة · بانتظار المراجع",QiraState.Reviewed=>"الاعتماد · بانتظار المعتمد",QiraState.Approved=>"معتمد",_=>"محذوفة"};
    public static string Choice(QiraChoice value)=>value switch{QiraChoice.Yes=>"نعم",QiraChoice.No=>"لا",QiraChoice.Unknown=>"غير معروف",_=>""};
}
