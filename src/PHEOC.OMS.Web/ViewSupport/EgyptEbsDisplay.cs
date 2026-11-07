using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.ViewSupport;
public static class EgyptEbsDisplay
{
    public static void DefaultDates(EgyptEbsFilter filter,bool datesSpecified,DateTime utcNow)
    {
        if(datesSpecified)return;
        var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow,TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")));
        filter.From=today.AddDays(-1);filter.To=today;
    }
}
