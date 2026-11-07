using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Qira;
namespace PHEOC.OMS.Application.Contracts;
public class QiraScopeInput:VersionInput
{
    [Required,StringLength(2000)]public string Scope{get;set;}="";
    [Required,StringLength(1000)]public string Population{get;set;}="";
    [Required,StringLength(500)]public string TimeWindow{get;set;}="";
    [StringLength(160)]public string Hazard{get;set;}="";
    public bool NovelHazard{get;set;}
    [Range(0,int.MaxValue)]public int? Hospitalizations{get;set;}
    [Range(0,int.MaxValue)]public int? IcuAdmissions{get;set;}
}
public class QiraAnswerInput:VersionInput
{
    [Required,StringLength(20)]public string Node{get;set;}="";
    public QiraChoice Choice{get;set;}
    [StringLength(2000)]public string Notes{get;set;}="";
    [StringLength(2000)]public string Source{get;set;}="";
    [StringLength(4000)]public string Evidence{get;set;}="";
    public long? AttachmentId{get;set;}
}
public class QiraCompleteInput:VersionInput
{
    public QiraConfidence Confidence{get;set;}
    [Required,StringLength(2000)]public string ConfidenceReason{get;set;}="";
    [StringLength(4000)]public string Uncertainties{get;set;}="";
}
public class QiraReviewInput:VersionInput{[Required,StringLength(2000)]public string Note{get;set;}="";}
public class QiraPolicyInput
{
    [Required,StringLength(160)]public string Name{get;set;}="";
    [Required,StringLength(2000)]public string Reference{get;set;}="";
    [StringLength(12000)]public string Hazards{get;set;}="";
    public List<QiraTemplateInput> Actions{get;set;}=[];
}
public class QiraTemplateInput
{
    public QiraRisk? Level{get;set;}
    [StringLength(1000)]public string Title{get;set;}="";
    [StringLength(450)]public string Organization{get;set;}="";
    public int Priority{get;set;}
    public int DueHours{get;set;}
}
public class QiraActionUpdate:VersionInput
{
    [StringLength(2000)]public string Notes{get;set;}="";
    public PHEOC.OMS.Domain.Events.TaskState State{get;set;}
    [Range(0,100)]public int Progress{get;set;}
    [Required]public string TaskVersion{get;set;}="";
}
public record QiraAssessmentRow(long Id,int Revision,QiraState State,QiraRisk? Risk,QiraConfidence? Confidence,string Assessor,DateTime StartedAt,DateTime? CompletedAt,string Algorithm,long? PreviousId,string Version,bool CanDelete=false);
public record QiraEventBoard(EventRow Event,IReadOnlyList<QiraAssessmentRow> Assessments,IReadOnlyList<ActivityRow> Legacy);
public record QiraEventSummary(long EventId,QiraAssessmentRow? Latest,QiraAssessmentRow? Approved);
public record QiraAnswerRow(string Node,QiraChoice Choice,string Notes,string Source,string Evidence,long? AttachmentId,bool Active);
public record QiraActionRow(long Id,string Title,string Organization,int Priority,DateTime DueAt,long? TaskId,string State,string Notes,string Version,string TaskVersion="",int Progress=0);
public record QiraPolicyRow(long Id,int Revision,string Name,string Reference,IReadOnlyList<string> Hazards,IReadOnlyList<QiraActionTemplate> Actions,string ApprovedBy,DateTime At);
public record QiraDetail(QiraAssessmentRow Assessment,EventRow Event,string Scope,string Population,string TimeWindow,string Hazard,bool NovelHazard,int? Hospitalizations,int? IcuAdmissions,QiraDocument Definition,QiraEvaluation Evaluation,IReadOnlyList<QiraAnswerRow> Answers,IReadOnlyList<string> HighThreatHazards,string? PolicyName,IReadOnlyList<AttachmentRow> Attachments,IReadOnlyList<QiraActionRow> Actions,IReadOnlyList<ActivityRow> History,string ConfidenceReason,string Uncertainties,string ReviewNote,string ApprovalNote,bool Editable,string Location="",string AssessedBy="",IReadOnlyList<PHEOC.OMS.Domain.Qira.QiraActionTemplate>? ActionTemplates=null);
public interface IQiraService
{
    Task<QiraEventBoard> QiraForEvent(long eventId);
    Task<long> StartQira(long eventId,long? previousId=null);
    Task<QiraDetail> Qira(long id);
    Task SaveQiraScope(long id,QiraScopeInput input);
    Task SaveQiraAnswer(long id,QiraAnswerInput input);
    Task UndoQiraAnswer(long id,VersionInput input);
    Task<long> DeleteQiraDraft(long id,VersionInput input);
    Task CompleteQira(long id,QiraCompleteInput input);
    Task ReviewQira(long id,QiraReviewInput input);
    Task ApproveQira(long id,QiraReviewInput input);
    Task ShareQiraActions(long id,VersionInput input);
    Task UpdateQiraAction(long id,QiraActionUpdate input);
    Task<IReadOnlyList<QiraPolicyRow>> QiraPolicies();
    Task AddQiraPolicy(QiraPolicyInput input);
}
