using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Infrastructure.Services;
public partial class OmsService
{
    public async Task<IReadOnlyList<ActivityRow>> ClosurePolicies()
    {
        Demand(Permissions.AdminSettings);return await db.ClosurePolicies.OrderByDescending(x=>x.Version).Select(x=>new ActivityRow(x.Id,x.Name,"مهام مكتملة: "+x.RequireCompletedTasks+" | تقرير معتمد: "+x.RequireApprovedReport+" | تسوية موارد: "+x.RequireResourcesSettled+" | مراجعة وبائية: "+x.RequireEpidemiologyReview+" | مراجعة ميدانية: "+x.RequireFieldReview+" | استثنائي: "+x.AllowExceptional,x.ApprovedBy,x.CreatedAt,"v"+x.Version,"")).ToListAsync();
    }
    public async Task AddClosurePolicy(ClosurePolicyInput input)
    {
        Demand(Permissions.AdminSettings);Demand(Permissions.EventsClose);Validate(input);
        var version=(await db.ClosurePolicies.MaxAsync(x=>(int?)x.Version)??0)+1;
        db.ClosurePolicies.Add(new(){Name=input.Name,Version=version,RequireCompletedTasks=input.RequireCompletedTasks,RequireApprovedReport=input.RequireApprovedReport,RequireResourcesSettled=input.RequireResourcesSettled,RequireEpidemiologyReview=input.RequireEpidemiologyReview,RequireFieldReview=input.RequireFieldReview,AllowExceptional=input.AllowExceptional,ApprovedBy=user.Id});await db.SaveChangesAsync();
    }
}
