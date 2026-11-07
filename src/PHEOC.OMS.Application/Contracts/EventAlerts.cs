namespace PHEOC.OMS.Application.Contracts;

public record EventAlertItem(long Id, long EventId, string Title, string Governorate, DateTime ReceivedAtUtc);
public record EventAlertFeed(int UnreadCount, long LatestId, int NewCount, IReadOnlyList<EventAlertItem> Items);

public interface IEventAlertService
{
    Task<EventAlertFeed> Feed(long afterId = 0);
    Task Read(IReadOnlyList<long> ids);
}
