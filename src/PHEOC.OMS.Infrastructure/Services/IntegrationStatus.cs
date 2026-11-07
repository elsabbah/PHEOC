using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Infrastructure.Services;
public class IntegrationStatus(OmsDbContext db) : IIntegrationStatus
{
    public async Task<IntegrationEventStatus> Status(Guid id)
    {
        var source=await db.InboxMessages.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.Event.PublicId==id)??throw new KeyNotFoundException();return new(id,source.Source,source.ExternalEventId,source.Event.Status.ToString(),source.ReceivedAt);
    }
    public async Task QueueStatus(Guid id)
    {
        var source=await db.InboxMessages.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.Event.PublicId==id)??throw new KeyNotFoundException();
        db.OutboxMessages.Add(new(){EventId=source.EventId,Payload=JsonSerializer.Serialize(await Status(id))});await db.SaveChangesAsync();
    }
}
