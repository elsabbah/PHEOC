using PHEOC.OMS.Web.Realtime;
namespace PHEOC.OMS.IntegrationTests;

public class EventArrivalBroadcastTests
{
    [Theory]
    [InlineData("{\"Action\":\"Received\",\"Status\":\"Received\"}",true)]
    [InlineData("{\"Status\":\"Received\"}",true)]
    [InlineData("{\"Action\":\"TaskUpdated\",\"Status\":\"Received\"}",false)]
    [InlineData("{\"Action\":\"Activated\"}",false)]
    [InlineData("[]",false)]
    [InlineData("invalid",false)]
    public void OnlyArrivalMessagesTriggerArrivalBroadcast(string payload,bool expected) =>
        Assert.Equal(expected,EventArrivalBroadcast.IsArrival(payload));
}
