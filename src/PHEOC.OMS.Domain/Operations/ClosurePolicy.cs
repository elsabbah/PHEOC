using System.ComponentModel.DataAnnotations;
using PHEOC.OMS.Domain.Events;
namespace PHEOC.OMS.Domain.Operations;
public class ClosurePolicy : Record
{
    [MaxLength(120)] public string Name { get; set; } = "";
    public int Version { get; set; }
    public bool RequireCompletedTasks { get; set; }
    public bool RequireApprovedReport { get; set; }
    public bool RequireResourcesSettled { get; set; }
    public bool RequireEpidemiologyReview { get; set; }
    public bool RequireFieldReview { get; set; }
    public bool AllowExceptional { get; set; }
    [MaxLength(450)] public string ApprovedBy { get; set; } = "";
}
