using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PHEOC.OMS.Domain.Events;

namespace PHEOC.OMS.Web.Realtime;

public static class EventArrivalBroadcast
{
    public static bool IsArrival(string payload)
    {
        try
        {
            using var json = JsonDocument.Parse(payload);
            if (json.RootElement.ValueKind != JsonValueKind.Object) return false;
            // Action takes priority: an unrelated update may retain Status=Received.
            if (json.RootElement.TryGetProperty("Action", out var action))
                return action.ValueKind == JsonValueKind.String && action.GetString() == "Received";
            return json.RootElement.TryGetProperty("Status", out var status) &&
                status.ValueKind == JsonValueKind.String && status.GetString() == "Received";
        }
        catch (JsonException) { return false; }
    }

    public static Task Publish(IHubContext<OperationsHub> hub, EmergencyEvent item, string payload, CancellationToken token) =>
        !item.IsTraining && IsArrival(payload)
            ? hub.Clients.Groups("central", "governorate:" + item.GovernorateId)
                .SendAsync("EventReceived", new { EventId = item.Id }, token)
            : Task.CompletedTask;
}
