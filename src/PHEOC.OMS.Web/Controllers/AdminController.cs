using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
namespace PHEOC.OMS.Web.Controllers;
public class AdminController(IOmsService oms,IUserAdministration users) : OmsController
{
    public async Task<IActionResult> Users(){ViewBag.Governorates=await oms.Governorates();return View(await users.Users());}
    [Microsoft.AspNetCore.Authorization.Authorize(Policy=PHEOC.OMS.Application.Security.Permissions.AdminUsers)]public async Task<IActionResult> EditPermissions(string id){ViewBag.Governorates=await oms.Governorates();return View(await users.EditPermissions(id));}
    [HttpPost,Microsoft.AspNetCore.Authorization.Authorize(Policy=PHEOC.OMS.Application.Security.Permissions.AdminUsers)]public Task<IActionResult> UpdatePermissions(string id,UserPermissionsInput input)=>Command(()=>users.UpdatePermissions(id,input),"Admin","Users");
    [HttpGet,Microsoft.AspNetCore.Authorization.Authorize(Policy=PHEOC.OMS.Application.Security.Permissions.AdminUsers)]public async Task<IActionResult> DeleteUser(string id)=>View(await users.EditPermissions(id));
    [HttpPost,ActionName("DeleteUser"),Microsoft.AspNetCore.Authorization.Authorize(Policy=PHEOC.OMS.Application.Security.Permissions.AdminUsers)]public Task<IActionResult> ConfirmDeleteUser(string id,UserAccountDeleteInput input)=>Command(()=>users.Delete(id,input),"Admin","Users");
    [HttpPost]public Task<IActionResult> CreateUser(UserInput input)=>Command(()=>users.Create(input),"Admin","Users");
    [HttpPost]public Task<IActionResult> DisableUser(string id,bool disabled)=>Command(()=>users.Disable(id,disabled),"Admin","Users");
    [HttpPost]public Task<IActionResult> ResetPassword(string id,PasswordResetInput input)=>Command(()=>users.ResetPassword(id,input),"Admin","Users");
    [Microsoft.AspNetCore.Authorization.Authorize(Policy=PHEOC.OMS.Application.Security.Permissions.AdminSettings)]public IActionResult Settings()=>View();
    public async Task<IActionResult> Audit()=>View(await oms.Audit());
    public async Task<IActionResult> Integration()=>View(await oms.Integration());
}
