using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Web.ViewSupport;
namespace PHEOC.OMS.IntegrationTests;
public class EgyptEbsDisplayTests
{
    [Fact]public void DefaultsUseEgyptDateAcrossUtcMidnight()
    {
        var filter=new EgyptEbsFilter();
        EgyptEbsDisplay.DefaultDates(filter,false,new DateTime(2026,11,5,22,30,0,DateTimeKind.Utc));
        Assert.Equal(new DateOnly(2026,11,5),filter.From);Assert.Equal(new DateOnly(2026,11,6),filter.To);
    }
    [Fact]public void ExplicitEmptyDatesAndCustomRangeRemainUnchanged()
    {
        var empty=new EgyptEbsFilter();EgyptEbsDisplay.DefaultDates(empty,true,DateTime.UtcNow);Assert.Null(empty.From);Assert.Null(empty.To);
        var custom=new EgyptEbsFilter{From=new(2026,10,1),To=new(2026,10,31)};EgyptEbsDisplay.DefaultDates(custom,true,DateTime.UtcNow);
        Assert.Equal(new DateOnly(2026,10,1),custom.From);Assert.Equal(new DateOnly(2026,10,31),custom.To);
    }
}
