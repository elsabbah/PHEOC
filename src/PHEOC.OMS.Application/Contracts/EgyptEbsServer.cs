using System.ComponentModel.DataAnnotations;
namespace PHEOC.OMS.Application.Contracts;
public class EgyptEbsServerInput
{
    [Required,MaxLength(255)] public string Server { get; set; } = "";
    public string Revision { get; set; } = "";
    [MaxLength(128)] public string? UserName { get; set; }
    [MaxLength(512),DataType(DataType.Password)] public string? Password { get; set; }
    public bool UseOriginalAccount { get; set; }
}
public record EgyptEbsServerView(string CurrentServer,string Revision,bool Configured);
public interface IEgyptEbsServerSettings
{
    Task<EgyptEbsServerView> Get();
    Task Test(EgyptEbsServerInput input);
    Task Save(EgyptEbsServerInput input);
}
