using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Operations;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;
public partial class OmsService
{
    public async Task Sos(long deploymentId,NoteInput input)
    {
        Demand(Permissions.FieldManage);Validate(input);var d=await db.Deployments.SingleOrDefaultAsync(x=>x.Id==deploymentId)??throw new KeyNotFoundException();var e=await Open(d.EventId);Require(d.ReturnedAt==null,"أمر النشر منتهٍ.");
        db.Notifications.Add(new(){EventId=e.Id,Severity="Critical",Text="نداء طوارئ SOS من أمر النشر #"+d.Id+": "+input.Text});
        db.EventMessages.Add(new(){EventId=e.Id,Sender=user.Id,Text="SOS — "+input.Text});Timeline(e,"EmergencySOS","تم تسجيل نداء طوارئ لأمر النشر #"+d.Id);await db.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<ActivityRow>> Messages(long eventId)
    {
        Demand(Permissions.EventsView);await Load(eventId);return await db.EventMessages.Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Id).Take(200).Select(x=>new ActivityRow(x.Id,"رسالة",x.Text,x.Sender,x.CreatedAt,"","")).ToListAsync();
    }
    public async Task AddMessage(long eventId,MessageInput input)
    {
        Demand(Permissions.EventsUpdate);Validate(input);var e=await Open(eventId);db.EventMessages.Add(new(){EventId=eventId,Text=input.Text,Sender=user.Id});Timeline(e,"Message","رسالة جديدة في غرفة الحدث");await db.SaveChangesAsync();
    }
    public async Task<CommunicationBoard> Communications()
    {
        Demand(Permissions.EventsView);
        var notifications=await db.Notifications.Where(x=>(user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId)&&!x.Event.IsTraining).OrderByDescending(x=>x.Id).Take(100).Select(x=>new NotificationRow(x.Id,x.EventId,x.Event.Title,x.Text,x.Severity,x.CreatedAt,db.NotificationReads.Any(r=>r.NotificationId==x.Id&&r.UserId==user.Id))).ToListAsync();
        var slas=await db.EventSlas.Where(x=>(user.GovernorateId==null||x.Event.GovernorateId==user.GovernorateId)&&!x.Event.IsTraining).OrderByDescending(x=>x.Id).Take(200).Select(x=>new SlaRow(x.Id,x.EventId,x.Event.Title,x.Definition.Name,x.Definition.Version,x.Definition.TargetMinutes,x.StartedAt,x.CompletedAt,x.Compliant,db.SlaEscalations.Where(s=>s.EventSlaId==x.Id).Max(s=>(int?)s.Level)??0)).ToListAsync();
        var definitions=user.Can(Permissions.AdminSettings)?await db.SlaDefinitions.OrderByDescending(x=>x.Id).Select(x=>new ActivityRow(x.Id,x.Stage,x.Name+" / "+x.TargetMinutes+" دقيقة، تصعيد كل "+x.EscalateEveryMinutes,x.ApprovedBy,x.CreatedAt,"v"+x.Version,"")).ToListAsync():[];
        return new(notifications,slas,definitions);
    }
    public async Task ReadNotification(long id)
    {
        Demand(Permissions.EventsView);var n=await db.Notifications.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();await Load(n.EventId);
        if(!await db.NotificationReads.AnyAsync(x=>x.NotificationId==id&&x.UserId==user.Id)){db.NotificationReads.Add(new(){NotificationId=id,UserId=user.Id});await db.SaveChangesAsync();}
    }
    public async Task AddSla(SlaInput input)
    {
        Demand(Permissions.AdminSettings);Validate(input);Require(new[]{"Acknowledge","Assessment","Activation","Deployment","FirstResponse"}.Contains(input.Stage),"مرحلة SLA غير معروفة.");
        var version=(await db.SlaDefinitions.Where(x=>x.Stage==input.Stage).MaxAsync(x=>(int?)x.Version)??0)+1;
        db.SlaDefinitions.Add(new(){Name=input.Name,Stage=input.Stage,Version=version,TargetMinutes=input.TargetMinutes,EscalateEveryMinutes=input.EscalateEveryMinutes,ApprovedBy=user.Id});await db.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<AttachmentRow>> Attachments(long eventId)
    {
        Demand(Permissions.EventsView);await Load(eventId);return await db.EventAttachments.Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Id).Select(x=>new AttachmentRow(x.Id,x.EventId,x.OriginalName,x.SizeBytes,x.UploadedBy,x.CreatedAt)).ToListAsync();
    }
    private string UploadDirectory()=>Path.GetFullPath(config?["Storage:Attachments"]??Path.Combine(Directory.GetCurrentDirectory(),".local","attachments"));
    public async Task UploadAttachment(long eventId,string name,byte[] content)
    {
        Demand(Permissions.EventsUpdate);var e=await Open(eventId);Require(content.Length is >0 and <=5242880,"حجم المرفق يجب أن يكون من بايت واحد إلى 5 MB.");
        name=Path.GetFileName(name);Require(name.Length is >0 and <=240,"اسم مرفق غير صالح.");
        var extension=Path.GetExtension(name).ToLowerInvariant();var type=extension switch{".pdf"=>"application/pdf",".png"=>"image/png",".jpg" or ".jpeg"=>"image/jpeg",".txt"=>"text/plain",_=>""};Require(type!="","الأنواع المتاحة: PDF، PNG، JPEG، TXT.");
        var valid=extension switch{".pdf"=>content.AsSpan().StartsWith("%PDF-"u8),".png"=>content.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}),".jpg" or ".jpeg"=>content.Length>3&&content[0]==255&&content[1]==216&&content[2]==255,".txt"=>ValidText(content),_=>false};Require(valid,"محتوى الملف لا يطابق نوعه.");
        var key=Guid.NewGuid().ToString("N")+extension;var folder=UploadDirectory();Directory.CreateDirectory(folder);var target=Path.Combine(folder,key);await File.WriteAllBytesAsync(target,content);
        try{db.EventAttachments.Add(new(){EventId=eventId,OriginalName=name,StorageKey=key,ContentType=type,SizeBytes=content.Length,Sha256=Convert.ToHexString(SHA256.HashData(content)),UploadedBy=user.Id});Timeline(e,"Attachment","رفع مرفق");await db.SaveChangesAsync();}
        catch{File.Delete(target);throw;}
    }
    private static bool ValidText(byte[] content){try{var text=new UTF8Encoding(false,true).GetString(content);return !text.Contains('\0');}catch(DecoderFallbackException){return false;}}
    public async Task<AttachmentDownload> DownloadAttachment(long id)
    {
        Demand(Permissions.EventsView);var a=await db.EventAttachments.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();await Load(a.EventId);
        var content=await File.ReadAllBytesAsync(Path.Combine(UploadDirectory(),a.StorageKey));return new(new(a.Id,a.EventId,a.OriginalName,a.SizeBytes,a.UploadedBy,a.CreatedAt),content,a.ContentType);
    }
    public async Task<IReadOnlyList<EventRow>> MapEvents()
    {
        Demand(Permissions.EventsView);var events=await ScopedEvents().Include(x=>x.Governorate).Where(x=>!x.IsTraining&&x.Latitude!=null&&x.Longitude!=null).OrderByDescending(x=>x.Id).Take(1000).ToListAsync();return events.Select(Row).ToList();
    }
}
