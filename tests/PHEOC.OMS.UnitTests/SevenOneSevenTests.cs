using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.UnitTests;
public class SevenOneSevenTests
{
    private static readonly DateOnly Start=new(2026,1,1);
    [Fact]public void AllApplicableActionsDetermineResponseDate()
    {
        var actions=Enum.GetValues<EarlyResponseAction>().Select(a=>new ActionMilestone(a,Start.AddDays(9),false)).ToList();
        actions[6]=new(EarlyResponseAction.Coordination,Start.AddDays(12),false);
        var result=SevenOneSeven.Calculate(Start,Start.AddDays(7),Start.AddDays(8),actions);Assert.Equal(7,result.DetectionDays);Assert.Equal(1,result.NotificationDays);Assert.Equal(4,result.ResponseDays);Assert.True(result.OverallMet);
    }
    [Fact]public void FirstActionDoesNotStandInForAllSeven(){var result=SevenOneSeven.Calculate(Start,Start,Start,[new(EarlyResponseAction.Investigation,Start,false)]);Assert.Null(result.ResponseDays);Assert.Null(result.OverallMet);}
    [Fact]public void NotApplicableIsDifferentFromMissing()
    {
        var actions=Enum.GetValues<EarlyResponseAction>().Select(a=>new ActionMilestone(a,null,true)).ToList();actions[0]=new(EarlyResponseAction.Investigation,Start.AddDays(2),false);
        Assert.Equal(2,SevenOneSeven.Calculate(Start,Start,Start,actions).ResponseDays);actions[1]=new(EarlyResponseAction.EpidemiologyAndRisk,null,false);Assert.Null(SevenOneSeven.Calculate(Start,Start,Start,actions).ResponseDays);
    }
    [Fact]public void MissingAndReversedDatesAreExcluded(){var result=SevenOneSeven.Calculate(null,Start,Start.AddDays(-1),[]);Assert.Null(result.DetectionMet);Assert.Null(result.NotificationMet);}
    [Fact]public void AllNotApplicableDoesNotImplyResponseSuccess(){var result=SevenOneSeven.Calculate(Start,Start,Start,Enum.GetValues<EarlyResponseAction>().Select(a=>new ActionMilestone(a,null,true)).ToList());Assert.Null(result.ResponseMet);}
    [Fact]public void SameDayCountsAsZeroAndMeetsTarget(){var result=SevenOneSeven.Calculate(Start,Start,Start,Enum.GetValues<EarlyResponseAction>().Select(a=>new ActionMilestone(a,Start,false)).ToList());Assert.Equal(0,result.ResponseDays);Assert.True(result.OverallMet);}
}
