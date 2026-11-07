using System.ComponentModel.DataAnnotations;
namespace PHEOC.OMS.Application.Contracts;
public class EgyptEbsFilter
{
    [StringLength(120)] public string? Search {get;set;}
    [StringLength(120)] public string? Governorate {get;set;}
    [Range(1,int.MaxValue)] public int? TypeId {get;set;}
    public string Receipt {get;set;}="all";
    public DateOnly? From {get;set;}
    public DateOnly? To {get;set;}
    [Range(1,1000000)] public int Page {get;set;}=1;
    public int PageSize {get;set;}=50;
    public string Display {get;set;}="table";
}
public record EgyptEbsSignal(int SignalId,int VerificationId,string Type,string Category,string Governorate,string Location,string Description,DateTime EventStartLocal,DateTime DetectionLocal,DateTime VerificationLocal,bool Imported,DateTime ChangedLocal,DateTime? ReceivedAtUtc=null);
public record EgyptEbsBoard(bool Configured,IReadOnlyList<EgyptEbsSignal> Signals,bool Automatic=false,DateTime? LastSuccessAt=null,string LastError="",EgyptEbsFilter? Filter=null,int TotalCount=0,IReadOnlyList<string>? Governorates=null,IReadOnlyList<Lookup>? SignalTypes=null,int? Before=null)
{
    public int PageCount=>Math.Max(1,(int)Math.Ceiling((double)TotalCount/(Filter?.PageSize??50)));
}
public record EgyptEbsStatus(bool Automatic,DateTime? LastSuccessAt,string LastError,DateTime ServerUtcNow);
public interface IEgyptEbsReader
{
    Task<EgyptEbsBoard> Candidates(int? before=null);
    Task<EgyptEbsBoard> Browse(EgyptEbsFilter filter,int? before=null,bool includeLookups=true);
    Task<IntakeResult> Import(int signalId);
    Task SetAutomatic(bool enabled);
    Task<EgyptEbsStatus> Status();
}

// Background synchronization port; interactive access remains authorized by IEgyptEbsReader.
public interface IEgyptEbsSyncSource
{
    Task<IReadOnlyList<EgyptEbsSignal>> ReadChanges(DateTime afterChange, int afterVerification);
    Task<IntakeResult> ImportSignal(int signalId);
}
