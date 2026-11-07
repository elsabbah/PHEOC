using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Web.Controllers;
public class LoginInput { [Required,StringLength(100)] public string UserName { get; set; } = ""; [Required,StringLength(128)] public string Password { get; set; } = ""; public string? ReturnUrl { get; set; } }
public class AccountController(SignInManager<AppUser> signIn) : Controller
{
    [AllowAnonymous,HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginInput{ReturnUrl=returnUrl});
    [AllowAnonymous,HttpPost,EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if(!ModelState.IsValid)return View(input);
        var result=await signIn.PasswordSignInAsync(input.UserName,input.Password,false,true);
        if(result.Succeeded)return LocalRedirect(Url.IsLocalUrl(input.ReturnUrl)?input.ReturnUrl!:"/");
        ModelState.AddModelError("","تعذر تسجيل الدخول. تحقق من البيانات أو حاول لاحقاً إذا كان الحساب مقفلاً."); input.Password="";return View(input);
    }
    [Authorize,HttpPost]public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return RedirectToAction(nameof(Login));}
    [AllowAnonymous]public IActionResult Denied(){Response.StatusCode=403;return View();}
}
