using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Integration.Ebs;
/// <summary>Proposed inbound contract v1. The external EBS adapter must map its approved contract to this DTO.</summary>
public class EbsEnvelope
{
    [Required,StringLength(128)] public string ExternalEventId { get; set; } = "";
    [Required] public EventInput Event { get; set; } = new();
}
