using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Security;
namespace PHEOC.OMS.Web.Controllers;
[Authorize(Policy=Permissions.EventsView)]
public abstract class OmsController : Controller
{
    protected void CheckInput(){if(!ModelState.IsValid)throw new ValidationException(string.Join("؛ ",ModelState.Values.SelectMany(x=>x.Errors).Select(x=>string.IsNullOrWhiteSpace(x.ErrorMessage)?"بيانات غير صالحة.":x.ErrorMessage)));}
    protected async Task<IActionResult> Command(Func<Task> action,string controller,string method="Index",long? id=null)
    {
        CheckInput();await action();TempData["Success"]="تم حفظ الإجراء بنجاح.";return RedirectToAction(method,controller,new{id});
    }
}
