using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Rules;
namespace PHEOC.OMS.Web.Controllers;
[Authorize(Policy=Permissions.AdminSettings)]
public class EgyptEbsServerController(IEgyptEbsServerSettings settings) : OmsController
{
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Index()
    {
        var current=await settings.Get();ViewBag.Current=current;
        return View(new EgyptEbsServerInput{Server=current.CurrentServer,Revision=current.Revision});
    }
    [HttpPost] public Task<IActionResult> Test(EgyptEbsServerInput input)=>Apply(input,false);
    [HttpPost] public Task<IActionResult> Save(EgyptEbsServerInput input)=>Apply(input,true);
    private async Task<IActionResult> Apply(EgyptEbsServerInput input,bool save)
    {
        // Demand permission and central scope even when model binding failed.
        ViewBag.Current=await settings.Get();
        if(ModelState.IsValid)
        {
            try
            {
                if(save){await settings.Save(input);TempData["Success"]="تم اختبار القراءة وحفظ عنوان خادم EgyptEBS. يُستخدم تلقائياً في الاتصالات التالية.";return RedirectToAction(nameof(Index));}
                await settings.Test(input);ViewBag.TestSuccess="نجح الاتصال وقراءة مخطط جداول المصدر. لم يُحفظ أي تغيير. إذا أدخلت حساباً جديداً، أعد إدخال كلمة مروره عند الحفظ.";
            }
            catch(BusinessRuleException ex){ModelState.AddModelError("",ex.Message);}
        }
        input.Password=null;ModelState.Remove(nameof(input.Password));
        return View("Index",input);
    }
}
