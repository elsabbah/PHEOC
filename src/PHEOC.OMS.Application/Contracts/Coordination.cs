using System.ComponentModel.DataAnnotations;
namespace PHEOC.OMS.Application.Contracts;
public class MessageInput { [Required,StringLength(2000)] public string Text { get; set; } = ""; }
public class SlaInput { [Required,StringLength(100)] public string Name { get; set; } = ""; [Required] public string Stage { get; set; } = ""; [Range(1,525600)] public int TargetMinutes { get; set; } [Range(1,525600)] public int EscalateEveryMinutes { get; set; } }
public record SlaRow(long Id,long EventId,string EventTitle,string Name,int Version,int TargetMinutes,DateTime StartedAt,DateTime? CompletedAt,bool? Compliant,int EscalationLevel);
public record NotificationRow(long Id,long EventId,string EventTitle,string Text,string Severity,DateTime At,bool Read);
public record AttachmentRow(long Id,long EventId,string Name,long Size,string UploadedBy,DateTime At);
public record AttachmentDownload(AttachmentRow Metadata,byte[] Content,string ContentType);
public record CommunicationBoard(IReadOnlyList<NotificationRow> Notifications,IReadOnlyList<SlaRow> Slas,IReadOnlyList<ActivityRow> Definitions);
public interface ICoordinationService
{
    Task<IReadOnlyList<ActivityRow>> Messages(long eventId);
    Task AddMessage(long eventId,MessageInput input);
    Task<CommunicationBoard> Communications();
    Task ReadNotification(long id);
    Task AddSla(SlaInput input);
    Task<IReadOnlyList<AttachmentRow>> Attachments(long eventId);
    Task UploadAttachment(long eventId,string name,byte[] content);
    Task<AttachmentDownload> DownloadAttachment(long id);
    Task<IReadOnlyList<EventRow>> MapEvents();
    Task Sos(long deploymentId,NoteInput input);
}
