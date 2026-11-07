using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Qira;
public class QiraDefinition:Record
{
    [MaxLength(80)]public string Code{get;set;}="";
    [MaxLength(600)]public string SourceUrl{get;set;}="";
    [MaxLength(64)]public string Hash{get;set;}="";
    public string DocumentJson{get;set;}="";
}
public class QiraNationalPolicy:Record
{
    public int Version{get;set;}
    [MaxLength(160)]public string Name{get;set;}="";
    [MaxLength(2000)]public string Reference{get;set;}="";
    public string HazardsJson{get;set;}="[]";
    public string ActionsJson{get;set;}="[]";
    [MaxLength(450)]public string ApprovedBy{get;set;}="";
}
public class QiraAssessment:Record
{
    public long EventId{get;set;}
    public EmergencyEvent Event{get;set;}=null!;
    public long DefinitionId{get;set;}
    public QiraDefinition Definition{get;set;}=null!;
    public long? PolicyId{get;set;}
    public QiraNationalPolicy? Policy{get;set;}
    public long? PreviousId{get;set;}
    public int Revision{get;set;}
    public QiraState State{get;set;}
    [MaxLength(450)]public string AssessedBy{get;set;}="";
    [MaxLength(160)]public string AssessorName{get;set;}="";
    [MaxLength(2000)]public string Scope{get;set;}="";
    [MaxLength(1000)]public string Population{get;set;}="";
    [MaxLength(500)]public string TimeWindow{get;set;}="";
    [MaxLength(160)]public string Hazard{get;set;}="";
    public bool NovelHazard{get;set;}
    public int? Hospitalizations{get;set;}
    public int? IcuAdmissions{get;set;}
    public QiraRisk? Risk{get;set;}
    public QiraConfidence? Confidence{get;set;}
    [MaxLength(2000)]public string ConfidenceReason{get;set;}="";
    [MaxLength(4000)]public string Uncertainties{get;set;}="";
    public string ResultJson{get;set;}="";
    public DateTime? CompletedAt{get;set;}
    [MaxLength(450)]public string? ReviewedBy{get;set;}
    public DateTime? ReviewedAt{get;set;}
    [MaxLength(2000)]public string ReviewNote{get;set;}="";
    [MaxLength(450)]public string? ApprovedBy{get;set;}
    public DateTime? ApprovedAt{get;set;}
    [MaxLength(2000)]public string ApprovalNote{get;set;}="";
}
public class QiraAnswer:Record
{
    public long AssessmentId{get;set;}
    public QiraAssessment Assessment{get;set;}=null!;
    [MaxLength(20)]public string Node{get;set;}="";
    public QiraChoice Choice{get;set;}
    public bool Active{get;set;}=true;
    [MaxLength(2000)]public string Notes{get;set;}="";
    [MaxLength(2000)]public string Source{get;set;}="";
    [MaxLength(4000)]public string Evidence{get;set;}="";
    public long? AttachmentId{get;set;}
}
public class QiraHistory:Record
{
    public long AssessmentId{get;set;}
    public QiraAssessment Assessment{get;set;}=null!;
    [MaxLength(450)]public string Actor{get;set;}="";
    [MaxLength(80)]public string Action{get;set;}="";
    public string Details{get;set;}="";
}
public class QiraAction:Record
{
    public long AssessmentId{get;set;}
    public QiraAssessment Assessment{get;set;}=null!;
    [MaxLength(1000)]public string Title{get;set;}="";
    [MaxLength(450)]public string Organization{get;set;}="";
    public int Priority{get;set;}
    public DateTime DueAt{get;set;}
    public long? TaskId{get;set;}
    [MaxLength(2000)]public string Notes{get;set;}="";
    [MaxLength(450)]public string? SharedBy{get;set;}
    public DateTime? SharedAt{get;set;}
}
public record QiraActionTemplate(QiraRisk Level,string Title,string Organization,int Priority,int DueHours);
