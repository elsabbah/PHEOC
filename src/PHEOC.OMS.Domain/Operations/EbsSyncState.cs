using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Operations;
public class EbsSyncState : Record
{
    [MaxLength(40)] public string ConnectorCode { get; set; } = "EgyptEBS";
    public bool Enabled { get; set; }
    public DateTime CursorLocal { get; set; }
    public int CursorVerificationId { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    [MaxLength(200)] public string LastError { get; set; } = "";
    [MaxLength(450)] public string EnabledBy { get; set; } = "";
}
