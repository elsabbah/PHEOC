using System.Data;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Data;

namespace PHEOC.OMS.Infrastructure.Services;

public class EventAlertService(OmsDbContext db, ICurrentUser user) : IEventAlertService
{
    private void Demand() { if (!user.Can(Permissions.EventsView)) throw new AccessDeniedException(); }
    private IQueryable<Notification> Visible() => db.Notifications.Where(n =>
        n.Severity == EventArrival.NotificationKind && !n.Event.IsTraining &&
        (user.GovernorateId == null || n.Event.GovernorateId == user.GovernorateId));

    public async Task<EventAlertFeed> Feed(long afterId = 0)
    {
        Demand(); OperationalRules.Require(afterId >= 0, "معرف تنبيه غير صالح.");
        var visible = Visible();
        var latest = await visible.MaxAsync(n => (long?)n.Id) ?? 0;
        // Bound this response to its first observed maximum so a racing insert is picked up next time.
        var unread = visible.Where(n => n.Id <= latest && !db.NotificationReads.Any(r => r.NotificationId == n.Id && r.UserId == user.Id));
        var count = await unread.CountAsync();
        var newCount = afterId > 0 ? await unread.CountAsync(n => n.Id > afterId) : count;
        var rows = await unread.AsNoTracking().OrderByDescending(n => n.Id).Take(20)
            .Select(n => new { n.Id, n.EventId, n.Event.Title, Governorate = n.Event.Governorate.Name, n.CreatedAt }).ToListAsync();
        return new(count, latest, newCount, rows.Select(n => new EventAlertItem(n.Id, n.EventId, n.Title,
            n.Governorate, DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc))).ToList());
    }

    public async Task Read(IReadOnlyList<long> ids)
    {
        Demand(); OperationalRules.Require(ids.Count is > 0 and <= 20 && ids.All(id => id > 0), "اختر حتى 20 تنبيهاً صالحاً.");
        // Serialize competing acknowledgements for the same visible rows; reading is idempotent.
        await using var tx = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var requested = ids.Distinct().Order().ToArray();
        var visible = await Visible().Where(n => requested.Contains(n.Id)).OrderBy(n => n.Id).Select(n => n.Id).ToListAsync();
        if (visible.Count != requested.Length) throw new KeyNotFoundException("التنبيه غير موجود أو خارج نطاق صلاحيتك.");
        var read = await db.NotificationReads.Where(r => r.UserId == user.Id && requested.Contains(r.NotificationId))
            .Select(r => r.NotificationId).ToListAsync();
        foreach (var id in visible.Except(read)) db.NotificationReads.Add(new() { NotificationId = id, UserId = user.Id });
        await db.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
    }
}
