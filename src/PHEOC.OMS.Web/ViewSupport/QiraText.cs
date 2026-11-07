using PHEOC.OMS.Domain.Qira;
namespace PHEOC.OMS.Web.ViewSupport;
public static class QiraText
{
    public static string Risk(QiraRisk? value)=>value switch{QiraRisk.VeryLow=>"منخفض جداً",QiraRisk.Low=>"منخفض",QiraRisk.Moderate=>"متوسط",QiraRisk.High=>"مرتفع",QiraRisk.VeryHigh=>"مرتفع جداً",_=>"لم يُحدد بعد"};
    public static string State(QiraState value)=>value switch{QiraState.New=>"جديد",QiraState.InProgress=>"قيد الإعداد",QiraState.Completed=>"مكتمل — بانتظار المراجعة",QiraState.Reviewed=>"تمت المراجعة — بانتظار الاعتماد",QiraState.Approved=>"معتمد",_=>""};
    public static string Confidence(QiraConfidence? value)=>value switch{QiraConfidence.High=>"عالية",QiraConfidence.Moderate=>"متوسطة",QiraConfidence.Low=>"منخفضة",_=>"لم تُحدد"};
    public static string Choice(QiraChoice value)=>value switch{QiraChoice.Yes=>"نعم",QiraChoice.No=>"لا",QiraChoice.Unknown=>"غير معروف",_=>""};
}
