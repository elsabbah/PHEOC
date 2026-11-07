using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Rules;

public static class OperationalRules
{
    public static void Require(bool condition, string message) { if (!condition) throw new BusinessRuleException(message); }
    public static void Transition(EventStatus from, EventStatus to)
    {
        var allowed = from switch {
            EventStatus.Received => to == EventStatus.UnderAssessment,
            EventStatus.UnderAssessment => to is EventStatus.Monitoring or EventStatus.Activated,
            EventStatus.Monitoring => to is EventStatus.UnderAssessment or EventStatus.Activated,
            EventStatus.Activated => to == EventStatus.ResponseOngoing,
            _ => false
        };
        Require(allowed, "انتقال حالة غير مسموح. استخدم إجراءات التقييم والتفعيل والإغلاق المخصصة.");
    }
    public static decimal StockBalance(decimal current, decimal delta)
    {
        Require(delta != 0 && current + delta >= 0, "الكمية غير صحيحة أو تتجاوز الرصيد المتاح.");
        return current + delta;
    }
    public static double? ElapsedDays(DateTime? start, DateTime? end) =>
        start.HasValue && end.HasValue && end >= start ? (end.Value - start.Value).TotalDays : null;
    public static bool? Within(DateTime? start, DateTime? end, double days) => ElapsedDays(start, end) is double elapsed ? elapsed <= days : null;
}
public class BusinessRuleException(string message) : Exception(message);
public class IdempotencyConflictException(string message) : BusinessRuleException(message);
