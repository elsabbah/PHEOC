using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class PoliciesController(IPolicyService service) : OmsController
{
    public async Task<IActionResult> Index()=>View(await service.ClosurePolicies());
    [HttpPost]public Task<IActionResult> Closure(ClosurePolicyInput input)=>Command(()=>service.AddClosurePolicy(input),"Policies");
}
