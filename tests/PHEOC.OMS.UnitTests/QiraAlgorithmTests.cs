using PHEOC.OMS.Domain.Qira;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.UnitTests;
public class QiraAlgorithmTests
{
    // Independent transcription of WHO Figure 2; seven concepts, not numeric weights.
    private static QiraRisk Oracle(QiraChoice[] c)
    {
        var capacity=c[6]==QiraChoice.Yes;
        if(c[0]==QiraChoice.Yes)return capacity?QiraRisk.High:QiraRisk.VeryHigh;
        if(c[1]==QiraChoice.No){if(c[3]==QiraChoice.No)return QiraRisk.VeryLow;return capacity?QiraRisk.VeryLow:QiraRisk.Low;}
        if(c[2]==QiraChoice.No){if(c[4]==QiraChoice.No)return capacity?QiraRisk.VeryLow:QiraRisk.Low;return capacity?QiraRisk.Low:QiraRisk.Moderate;}
        if(c[4]==QiraChoice.No)return capacity?QiraRisk.Low:QiraRisk.Moderate;
        if(c[5]==QiraChoice.No)return capacity?QiraRisk.Moderate:QiraRisk.High;
        return capacity?QiraRisk.High:QiraRisk.VeryHigh;
    }
    [Theory][InlineData(2,15)][InlineData(3,221)]
    public void AllRoutesAgreeWithIndependentWhoFigure2(int choices,int expectedPaths)
    {
        var routes=new HashSet<string>();var risks=new HashSet<QiraRisk>();
        for(var code=0;code<(int)Math.Pow(choices,7);code++)
        {
            var rest=code;var concepts=new QiraChoice[7];for(var i=0;i<7;i++){concepts[i]=(QiraChoice)(rest%choices+1);rest/=choices;}
            var answers=new Dictionary<string,QiraChoice>{{"Q1",concepts[0]},{"Q2",concepts[1]},{"Q3",concepts[2]},{"Q41",concepts[3]},{"Q42-L",concepts[4]},{"Q42-H",concepts[4]},{"Q51",concepts[5]}};
            foreach(var key in new[]{"C52-VL","C52-LM","C52-MH","C52-HV"})answers[key]=concepts[6];
            var result=QiraAlgorithm.Evaluate(QiraAlgorithm.Current,answers);
            Assert.Equal(Oracle(concepts),result.Risk);Assert.Null(result.Next);Assert.InRange(result.Path.Count,2,6);
            Assert.Equal(result.Path.Where(id=>answers[id]==QiraChoice.Unknown),result.UnknownNodes);
            risks.Add(result.Risk!.Value);routes.Add(string.Join("|",result.Path.Select(id=>id+":"+answers[id])));
        }
        Assert.Equal(expectedPaths,routes.Count);Assert.Equal(5,risks.Count);
    }
    [Fact]public void MissingAnswerDoesNotProduceRiskOrSkipQuestion()
    {
        var result=QiraAlgorithm.Evaluate(QiraAlgorithm.Current,new Dictionary<string,QiraChoice>{{"Q1",QiraChoice.Unknown}});
        Assert.Equal("Q2",result.Next);Assert.Null(result.Risk);Assert.Equal(new[]{"Q1","Q2"},result.Path);
    }
    [Fact]public void UnknownExposureAndUnknownSeverityFollowConservativeBranches()
    {
        var answers=new Dictionary<string,QiraChoice>{{"Q1",QiraChoice.Unknown},{"Q2",QiraChoice.Unknown},{"Q3",QiraChoice.Unknown},{"Q42-H",QiraChoice.Unknown},{"Q51",QiraChoice.Unknown},{"C52-HV",QiraChoice.Unknown}};
        Assert.Equal(QiraRisk.VeryHigh,QiraAlgorithm.Evaluate(QiraAlgorithm.Current,answers).Risk);
        answers["Q2"]=QiraChoice.No;Assert.Equal("Q41",QiraAlgorithm.Evaluate(QiraAlgorithm.Current,answers).Next);
    }
    [Fact]public void InvalidChoiceIsRejected()=>Assert.Throws<BusinessRuleException>(()=>QiraAlgorithm.Evaluate(QiraAlgorithm.Current,new Dictionary<string,QiraChoice>{{"Q1",(QiraChoice)0}}));
    [Fact]public void StoredAlgorithmSnapshotRetainsItsBranches()
    {
        var saved=QiraAlgorithm.Read(QiraAlgorithm.Snapshot);Assert.Equal(QiraAlgorithm.Version,saved.Version);
        Assert.Equal(11,saved.Nodes.Count);Assert.Equal(5,saved.Axes.Count);
        Assert.Equal(QiraAlgorithm.Evaluate(QiraAlgorithm.Current,new Dictionary<string,QiraChoice>{{"Q1",QiraChoice.Yes}}).Next,QiraAlgorithm.Evaluate(saved,new Dictionary<string,QiraChoice>{{"Q1",QiraChoice.Yes}}).Next);
    }
}
