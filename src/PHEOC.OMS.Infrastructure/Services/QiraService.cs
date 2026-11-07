using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Domain.Operations;
using PHEOC.OMS.Domain.Qira;
using static PHEOC.OMS.Domain.Rules.OperationalRules;
namespace PHEOC.OMS.Infrastructure.Services;

public partial class OmsService:IQiraService
{
    private static QiraAssessmentRow QiraRow(QiraAssessment a)=>new(a.Id,a.Revision,a.State,a.Risk,a.Confidence,a.AssessorName,a.CreatedAt,a.CompletedAt,a.Definition.Code,a.PreviousId,Version(a));
    private async Task<QiraAssessment> LoadQira(long id)
    {
        Demand(Permissions.EventsView);
        var a=await db.QiraAssessments.Include(x=>x.Definition).Include(x=>x.Policy).SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();
        await Load(a.EventId);return a;
    }
    private async Task<QiraAssessment> EditQira(long id,string version)
    {
        Demand(Permissions.RiskCreate);var a=await LoadQira(id);var e=await Open(a.EventId);e.ModifiedAt=DateTime.UtcNow;
        Require(a.AssessedBy==user.Id,"مسودة التقييم تخص مستخدماً آخر؛ أنشئ تقييمك الخاص.");
        Require(a.State<QiraState.Completed,"التقييم مكتمل؛ استخدم إعادة التقييم بإصدار جديد.");CheckVersion(a,version);a.ModifiedAt=DateTime.UtcNow;return a;
    }
    private void QiraLog(QiraAssessment assessment,string action,object details)=>db.QiraHistories.Add(new(){Assessment=assessment,Actor=user.Id,Action=action,Details=JsonSerializer.Serialize(details,new JsonSerializerOptions{Encoder=System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)})});
    private async Task<List<QiraAnswer>> Answers(long id)=>await db.QiraAnswers.Where(x=>x.AssessmentId==id).ToListAsync();
    private static QiraEvaluation Evaluate(QiraAssessment a,IEnumerable<QiraAnswer> answers)=>QiraAlgorithm.Evaluate(QiraAlgorithm.Read(a.Definition.DocumentJson),answers.Where(x=>x.Active).ToDictionary(x=>x.Node,x=>x.Choice));
    private async Task<QiraEventSummary> QiraSummary(long eventId)
    {
        var rows=await db.QiraAssessments.Include(x=>x.Definition).AsNoTracking().Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Revision).ToListAsync();
        return new(eventId,rows.Count>0?QiraRow(rows[0]):null,rows.FirstOrDefault(x=>x.State==QiraState.Approved) is {} approved?QiraRow(approved):null);
    }
    public async Task<QiraEventBoard> QiraForEvent(long eventId)
    {
        Demand(Permissions.EventsView);var e=await Load(eventId);await db.Entry(e).Reference(x=>x.Governorate).LoadAsync();
        var rows=await db.QiraAssessments.Include(x=>x.Definition).AsNoTracking().Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Revision).ToListAsync();
        var legacy=await db.RiskAssessments.AsNoTracking().Where(x=>x.EventId==eventId).OrderByDescending(x=>x.Id).Select(x=>new ActivityRow(x.Id,"تقييم مؤرشف",x.Level+" — "+x.Rationale,x.AssessedBy,x.CreatedAt)).ToListAsync();
        return new(Row(e),rows.Select(QiraRow).ToList(),legacy);
    }
    public async Task<long> StartQira(long eventId,long? previousId=null)
    {
        Demand(Permissions.RiskCreate);if(previousId.HasValue)Demand(Permissions.QiraReassess);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var e=await Open(eventId);
        var pending=await db.QiraAssessments.FirstOrDefaultAsync(x=>x.EventId==eventId&&x.AssessedBy==user.Id&&x.State<QiraState.Completed);
        if(pending!=null)return pending.Id;
        QiraAssessment? prior=null;
        if(previousId.HasValue){prior=await LoadQira(previousId.Value);Require(prior.EventId==eventId&&prior.State>=QiraState.Completed,"اختر تقييماً مكتملاً للحدث نفسه لإعادة التقييم.");}
        var document=QiraAlgorithm.Snapshot;var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document)));
        var definition=await db.QiraDefinitions.SingleOrDefaultAsync(x=>x.Code==QiraAlgorithm.Version);
        if(definition==null){definition=new(){Code=QiraAlgorithm.Version,SourceUrl=QiraAlgorithm.Source,DocumentJson=document,Hash=hash};db.QiraDefinitions.Add(definition);}
        else Require(definition.Hash==hash,"إصدار الخوارزمية المسجل لا يطابق تعريفه؛ يلزم إصدار جديد دون استبدال التاريخ.");
        var policy=await db.QiraNationalPolicies.OrderByDescending(x=>x.Version).FirstOrDefaultAsync();
        var name=await db.Users.Where(x=>x.Id==user.Id).Select(x=>x.UserName).FirstOrDefaultAsync()??user.Id;
        var a=new QiraAssessment{Event=e,Definition=definition,Policy=policy,Revision=(await db.QiraAssessments.Where(x=>x.EventId==eventId).MaxAsync(x=>(int?)x.Revision)??0)+1,PreviousId=previousId,AssessedBy=user.Id,AssessorName=name,
            Scope=prior?.Scope??("تقييم المخاطر المرتبطة بالحدث: "+e.Title),Population=prior?.Population??"",TimeWindow=prior?.TimeWindow??"",Hazard=prior?.Hazard??e.ThreatType,NovelHazard=prior?.NovelHazard??false};
        if(e.Status is EventStatus.Received or EventStatus.Monitoring){e.Status=EventStatus.UnderAssessment;e.AcknowledgedAt??=DateTime.UtcNow;}
        db.QiraAssessments.Add(a);QiraLog(a,previousId.HasValue?"بدء إعادة تقييم":"بدء تقييم",new{previousId,Algorithm=definition.Code,Policy=policy?.Version});
        Timeline(e,"QiraStarted","بدء تقييم QIRA إصدار "+a.Revision);await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();return a.Id;
    }
    public async Task<QiraDetail> Qira(long id)
    {
        var a=await LoadQira(id);var e=await Load(a.EventId);await db.Entry(e).Reference(x=>x.Governorate).LoadAsync();
        var answers=await Answers(id);var actions=await db.QiraActions.AsNoTracking().Where(x=>x.AssessmentId==id).OrderBy(x=>x.Priority).ThenBy(x=>x.Id).ToListAsync();
        var taskIds=actions.Where(x=>x.TaskId!=null).Select(x=>x.TaskId!.Value).ToArray();var tasks=await db.Tasks.AsNoTracking().Where(x=>taskIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var history=await db.QiraHistories.AsNoTracking().Where(x=>x.AssessmentId==id).OrderByDescending(x=>x.Id).Select(x=>new ActivityRow(x.Id,x.Action,x.Details,x.Actor,x.CreatedAt)).ToListAsync();
        var attachments=await db.EventAttachments.AsNoTracking().Where(x=>x.EventId==a.EventId).Select(x=>new AttachmentRow(x.Id,x.EventId,x.OriginalName,x.SizeBytes,x.UploadedBy,x.CreatedAt)).ToListAsync();
        return new(QiraRow(a),Row(e),a.Scope,a.Population,a.TimeWindow,a.Hazard,a.NovelHazard,a.Hospitalizations,a.IcuAdmissions,QiraAlgorithm.Read(a.Definition.DocumentJson),Evaluate(a,answers),answers.Select(x=>new QiraAnswerRow(x.Node,x.Choice,x.Notes,x.Source,x.Evidence,x.AttachmentId,x.Active)).ToList(),
            a.Policy==null?[]:JsonSerializer.Deserialize<string[]>(a.Policy.HazardsJson)??[],a.Policy?.Name,attachments,
            actions.Select(x=>new QiraActionRow(x.Id,x.Title,x.Organization,x.Priority,x.DueAt,x.TaskId,x.TaskId.HasValue&&tasks.TryGetValue(x.TaskId.Value,out var t)?t.State.ToString():"Proposed",x.Notes,Version(x),x.TaskId.HasValue&&tasks.TryGetValue(x.TaskId.Value,out var linked)?Version(linked):"",x.TaskId.HasValue&&tasks.TryGetValue(x.TaskId.Value,out var progress)?progress.Progress:0)).ToList(),history,a.ConfidenceReason,a.Uncertainties,a.ReviewNote,a.ApprovalNote,a.AssessedBy==user.Id&&a.State<QiraState.Completed&&e.Status!=EventStatus.Closed&&user.Can(Permissions.RiskCreate),e.Location,a.AssessedBy,a.Policy==null?[]:JsonSerializer.Deserialize<QiraActionTemplate[]>(a.Policy.ActionsJson)??[]);
    }
    public async Task SaveQiraScope(long id,QiraScopeInput input)
    {
        Validate(input);var a=await EditQira(id,input.Version);
        Require(!input.IcuAdmissions.HasValue||!input.Hospitalizations.HasValue||input.IcuAdmissions<=input.Hospitalizations,"عدد الرعاية المركزة لا يتجاوز إجمالي الدخول للمستشفى عند توفر العددين.");
        var changed=a.NovelHazard!=input.NovelHazard||a.Hazard!=input.Hazard;
        a.Scope=input.Scope;a.Population=input.Population;a.TimeWindow=input.TimeWindow;a.Hazard=input.Hazard;a.NovelHazard=input.NovelHazard;a.Hospitalizations=input.Hospitalizations;a.IcuAdmissions=input.IcuAdmissions;a.State=QiraState.InProgress;
        if(changed){foreach(var answer in await Answers(id))answer.Active=false;}
        QiraLog(a,"تحديث نطاق التقييم",new{input.Scope,input.Population,input.TimeWindow,input.Hazard,input.NovelHazard,input.Hospitalizations,input.IcuAdmissions,AnswersCleared=changed});await db.SaveChangesAsync();
    }
    public async Task SaveQiraAnswer(long id,QiraAnswerInput input)
    {
        Validate(input);var a=await EditQira(id,input.Version);Require(Enum.IsDefined(input.Choice),"اختر نعم أو لا أو غير معروف.");
        Require(!string.IsNullOrWhiteSpace(a.Population)&&!string.IsNullOrWhiteSpace(a.TimeWindow),"احفظ نطاق التقييم والفئة السكانية والمدة أولاً.");
        var answers=await Answers(id);var before=Evaluate(a,answers);Require(before.Path.Contains(input.Node),"السؤال خارج المسار الحالي؛ أكمل السؤال السابق أولاً.");
        if(input.Node=="Q1")
        {
            var hazards=a.Policy==null?[]:JsonSerializer.Deserialize<string[]>(a.Policy.HazardsJson)??[];
            Require(hazards.Length>0||a.NovelHazard||input.Choice==QiraChoice.Unknown,"لا توجد قائمة معتمدة للأخطار؛ اختر غير معروف، أو وثق قرار الفريق لمسار خطر مستجد.");
            if(a.NovelHazard&&input.Choice!=QiraChoice.Unknown)Require(!string.IsNullOrWhiteSpace(input.Notes),"وثّق مبرر الفريق لمسار الخطر المستجد.");
            if(input.Choice==QiraChoice.Yes&&!a.NovelHazard)Require(hazards.Contains(a.Hazard),"اختر اسم الخطر من القائمة المعتمدة في نطاق التقييم.");
        }
        if(input.AttachmentId.HasValue)Require(await db.EventAttachments.AnyAsync(x=>x.Id==input.AttachmentId&&x.EventId==a.EventId),"المرفق غير تابع للحدث أو خارج النطاق.");
        var answer=answers.SingleOrDefault(x=>x.Node==input.Node);
        if(answer==null){answer=new(){Assessment=a,Node=input.Node};db.QiraAnswers.Add(answer);answers.Add(answer);}
        answer.Choice=input.Choice;answer.Notes=input.Notes;answer.Source=input.Source;answer.Evidence=input.Evidence;answer.AttachmentId=input.AttachmentId;answer.Active=true;
        // Discard every downstream answer when revisiting a branch. The audit snapshot preserves it.
        var position=before.Path.ToList().IndexOf(input.Node);
        foreach(var downstream in answers.Where(x=>x.Node!=input.Node&&(!before.Path.Contains(x.Node)||before.Path.ToList().IndexOf(x.Node)>position)))downstream.Active=false;
        a.State=QiraState.InProgress;QiraLog(a,"حفظ إجابة",new{input.Node,input.Choice,input.Notes,input.Source,input.Evidence,input.AttachmentId,ActivePath=Evaluate(a,answers).Path});await db.SaveChangesAsync();
    }
    public async Task UndoQiraAnswer(long id,VersionInput input)
    {
        Validate(input);var a=await EditQira(id,input.Version);var answers=await Answers(id);
        var result=Evaluate(a,answers);
        var last=result.Path.Reverse().Select(node=>answers.SingleOrDefault(x=>x.Active&&x.Node==node)).FirstOrDefault(x=>x!=null);
        Require(last!=null,"لا توجد إجابة محفوظة للتراجع عنها.");
        last!.Active=false;a.State=QiraState.InProgress;
        QiraLog(a,"إلغاء آخر اختيار",new{last.Node,last.Choice,last.Notes,last.Source,last.Evidence,last.AttachmentId});
        await db.SaveChangesAsync();
    }
    public async Task CompleteQira(long id,QiraCompleteInput input)
    {
        Validate(input);var a=await EditQira(id,input.Version);var result=Evaluate(a,await Answers(id));
        Require(result.Risk.HasValue&&result.Next==null,"أكمل أسئلة المسار قبل إنهاء التقييم.");Require(Enum.IsDefined(input.Confidence),"اختر مستوى الثقة.");
        Require(result.UnknownNodes.Count==0||!string.IsNullOrWhiteSpace(input.Uncertainties),"وثق المعلومات الناقصة وعدم اليقين المرتبط بإجابات غير معروف.");
        a.Risk=result.Risk;a.Confidence=input.Confidence;a.ConfidenceReason=input.ConfidenceReason;a.Uncertainties=input.Uncertainties;a.ResultJson=JsonSerializer.Serialize(result);a.CompletedAt=DateTime.UtcNow;a.State=QiraState.Completed;
        var templates=a.Policy==null?[]:JsonSerializer.Deserialize<QiraActionTemplate[]>(a.Policy.ActionsJson)??[];
        foreach(var template in templates.Where(x=>x.Level==a.Risk))db.QiraActions.Add(new(){Assessment=a,Title=template.Title,Organization=template.Organization,Priority=template.Priority,DueAt=DateTime.UtcNow.AddHours(template.DueHours)});
        QiraLog(a,"إكمال التقييم",new{a.Risk,a.Confidence,a.ConfidenceReason,a.Uncertainties,Result=result});Timeline(await Load(a.EventId),"QiraCompleted","إكمال QIRA إصدار "+a.Revision+" — "+a.Risk);await db.SaveChangesAsync();
    }
    public async Task ReviewQira(long id,QiraReviewInput input)
    {
        Demand(Permissions.QiraReview);Validate(input);var a=await LoadQira(id);await Open(a.EventId);CheckVersion(a,input.Version);
        Require(a.State==QiraState.Completed&&a.AssessedBy!=user.Id,"المراجعة تتطلب تقييماً مكتملاً ومراجعاً غير القائم بالتقييم.");
        a.State=QiraState.Reviewed;a.ReviewedBy=user.Id;a.ReviewedAt=DateTime.UtcNow;a.ModifiedAt=DateTime.UtcNow;a.ReviewNote=input.Note;(await Load(a.EventId)).ModifiedAt=DateTime.UtcNow;QiraLog(a,"مراجعة التقييم",new{input.Note});await db.SaveChangesAsync();
    }
    public async Task ApproveQira(long id,QiraReviewInput input)
    {
        Demand(Permissions.RiskApprove);Validate(input);var a=await LoadQira(id);await Open(a.EventId);CheckVersion(a,input.Version);
        Require(a.State==QiraState.Reviewed&&a.AssessedBy!=user.Id,"الاعتماد يتطلب مراجعة التقييم ومستخدماً غير القائم به.");
        a.State=QiraState.Approved;a.ApprovedBy=user.Id;a.ApprovedAt=DateTime.UtcNow;a.ApprovalNote=input.Note;QiraLog(a,"اعتماد التقييم",new{input.Note});Timeline(await Load(a.EventId),"QiraApproved","اعتماد QIRA إصدار "+a.Revision+" — "+a.Risk);await db.SaveChangesAsync();
    }
    public async Task ShareQiraActions(long id,VersionInput input)
    {
        Demand(Permissions.RiskApprove);Demand(Permissions.EventsUpdate);Validate(input);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        var a=await LoadQira(id);var e=await Open(a.EventId);CheckVersion(a,input.Version);Require(a.State==QiraState.Approved,"اعتمد التقييم قبل إسناد إجراءاته.");
        var actions=await db.QiraActions.Where(x=>x.AssessmentId==id&&x.TaskId==null).ToListAsync();
        foreach(var action in actions)
        {
            var task=new OperationalTask{EventId=e.Id,Title=action.Title,AssignedTo=action.Organization,DueAt=action.DueAt,State=TaskState.Assigned};db.Tasks.Add(task);await db.SaveChangesAsync();
            action.TaskId=task.Id;action.SharedAt=DateTime.UtcNow;action.SharedBy=user.Id;
        }
        if(actions.Count>0){a.ModifiedAt=DateTime.UtcNow;QiraLog(a,"إسناد إجراءات الاستجابة",new{Count=actions.Count});db.EventMessages.Add(new(){EventId=e.Id,Sender=user.Id,Text="إسناد إجراءات QIRA الإصدار "+a.Revision+" إلى الجهات المسؤولة في مهام الحدث."});Timeline(e,"QiraActions","إسناد إجراءات QIRA إلى مهام الاستجابة");await db.SaveChangesAsync();}
        if(tx!=null)await tx.CommitAsync();
    }
    public async Task UpdateQiraAction(long id,QiraActionUpdate input)
    {
        Demand(Permissions.EventsUpdate);Validate(input);
        var action=await db.QiraActions.SingleOrDefaultAsync(x=>x.Id==id)??throw new KeyNotFoundException();
        var a=await LoadQira(action.AssessmentId);await Open(a.EventId);CheckVersion(action,input.Version);
        Require(a.State==QiraState.Approved&&action.TaskId.HasValue,"أسند الإجراء المعتمد إلى مهام الحدث أولاً.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        await UpdateTask(action.TaskId!.Value,new(){Version=input.TaskVersion,State=input.State,Progress=input.Progress});
        action.Notes=input.Notes;action.ModifiedAt=DateTime.UtcNow;QiraLog(a,"متابعة إجراء الاستجابة",new{ActionId=id,input.Notes,input.State,input.Progress});
        await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
    public async Task<IReadOnlyList<QiraPolicyRow>> QiraPolicies()
    {
        Demand(Permissions.AdminSettings);Require(user.GovernorateId==null,"إعداد المرجع الوطني يتطلب نطاقاً مركزياً.");
        var policies=await db.QiraNationalPolicies.AsNoTracking().OrderByDescending(x=>x.Version).ToListAsync();
        return policies.Select(p=>new QiraPolicyRow(p.Id,p.Version,p.Name,p.Reference,JsonSerializer.Deserialize<string[]>(p.HazardsJson)??[],JsonSerializer.Deserialize<QiraActionTemplate[]>(p.ActionsJson)??[],p.ApprovedBy,p.CreatedAt)).ToList();
    }
    public async Task AddQiraPolicy(QiraPolicyInput input)
    {
        Demand(Permissions.AdminSettings);Demand(Permissions.RiskApprove);Require(user.GovernorateId==null,"إعداد المرجع الوطني يتطلب نطاقاً مركزياً.");Validate(input);
        var hazards=input.Hazards.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();Require(hazards.All(h=>h.Length<=160),"اسم الخطر لا يتجاوز 160 حرفاً.");
        var templates=new List<QiraActionTemplate>();Require(input.Actions.Count<=50,"يمكن تعريف حتى 50 إجراء في الإصدار.");
        foreach(var action in input.Actions.Where(x=>!string.IsNullOrWhiteSpace(x.Title)))
        {Validate(action);Require(action.Level.HasValue&&Enum.IsDefined(action.Level.Value)&&action.Priority is >=1 and <=3&&action.DueHours is >=1 and <=8760&&!string.IsNullOrWhiteSpace(action.Organization),"حدد المستوى والجهة والأولوية والاستحقاق لكل إجراء.");templates.Add(new(action.Level!.Value,action.Title.Trim(),action.Organization.Trim(),action.Priority,action.DueHours));}
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(IsolationLevel.Serializable):null;
        db.QiraNationalPolicies.Add(new(){Name=input.Name,Reference=input.Reference,Version=(await db.QiraNationalPolicies.MaxAsync(x=>(int?)x.Version)??0)+1,HazardsJson=JsonSerializer.Serialize(hazards),ActionsJson=JsonSerializer.Serialize(templates),ApprovedBy=user.Id});
        await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
}
