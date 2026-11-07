using System.ComponentModel.DataAnnotations;
namespace PHEOC.OMS.Application.Contracts;
public class ClosurePolicyInput
{
    [Required,StringLength(120)] public string Name { get; set; } = "";
    public bool RequireCompletedTasks { get; set; }
    public bool RequireApprovedReport { get; set; }
    public bool RequireResourcesSettled { get; set; }
    public bool RequireEpidemiologyReview { get; set; }
    public bool RequireFieldReview { get; set; }
    public bool AllowExceptional { get; set; }
}
public interface IPolicyService { Task<IReadOnlyList<ActivityRow>> ClosurePolicies(); Task AddClosurePolicy(ClosurePolicyInput input); }
