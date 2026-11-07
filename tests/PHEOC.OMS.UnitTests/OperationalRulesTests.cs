using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.UnitTests;
public class OperationalRulesTests
{
    [Theory][InlineData(EventStatus.Received,EventStatus.UnderAssessment)][InlineData(EventStatus.UnderAssessment,EventStatus.Monitoring)][InlineData(EventStatus.UnderAssessment,EventStatus.Activated)][InlineData(EventStatus.Activated,EventStatus.ResponseOngoing)]
    public void ValidStateTransitions(EventStatus from,EventStatus to)=>OperationalRules.Transition(from,to);
    [Theory][InlineData(EventStatus.Received,EventStatus.Activated)][InlineData(EventStatus.Closed,EventStatus.UnderAssessment)][InlineData(EventStatus.ResponseOngoing,EventStatus.Closed)]
    public void BypassingWorkflowIsRejected(EventStatus from,EventStatus to)=>Assert.Throws<BusinessRuleException>(()=>OperationalRules.Transition(from,to));
    [Fact] public void StockCannotGoNegative()=>Assert.Throws<BusinessRuleException>(()=>OperationalRules.StockBalance(10,-11));
    [Fact] public void StockCanBeFullyIssued()=>Assert.Equal(0,OperationalRules.StockBalance(10,-10));
    [Fact] public void StockRejectsZeroMovement()=>Assert.Throws<BusinessRuleException>(()=>OperationalRules.StockBalance(10,0));
    [Fact] public void MissingDatesDoNotBecomeZero(){Assert.Null(OperationalRules.ElapsedDays(null,DateTime.UtcNow));Assert.Null(OperationalRules.Within(null,DateTime.UtcNow,7));}
    [Fact] public void ReversedDatesAreNotValidMeasurements(){var now=DateTime.UtcNow;Assert.Null(OperationalRules.ElapsedDays(now,now.AddDays(-1)));}
    [Fact] public void ThresholdIncludesExactBoundary(){var now=DateTime.UtcNow;Assert.True(OperationalRules.Within(now,now.AddDays(7),7));Assert.False(OperationalRules.Within(now,now.AddDays(7).AddSeconds(1),7));}
}
