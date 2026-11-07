using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
using PHEOC.OMS.Reporting;
using PHEOC.OMS.Web.Security;
using PHEOC.OMS.Web.Realtime;
using Serilog;

var builder=WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_,log)=>log.MinimumLevel.Information().MinimumLevel.Override("Microsoft",Serilog.Events.LogEventLevel.Warning).MinimumLevel.Override("Microsoft.Hosting.Lifetime",Serilog.Events.LogEventLevel.Information).WriteTo.Console());
builder.Services.AddHttpContextAccessor();builder.Services.AddScoped<ICurrentUser,CurrentUser>();
var connection=builder.Configuration.GetConnectionString("Oms") ?? "Server=localhost;Database=PHEOC_OMS;Integrated Security=true;TrustServerCertificate=true;Encrypt=false";
builder.Services.AddDbContext<OmsDbContext>(o=>o.UseSqlServer(connection,sql=>sql.UseCompatibilityLevel(130).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
builder.Services.AddIdentity<AppUser,IdentityRole>(options=>{options.Password.RequiredLength=12;options.Password.RequireNonAlphanumeric=true;options.Lockout.MaxFailedAccessAttempts=5;options.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);options.User.RequireUniqueEmail=false;})
    .AddEntityFrameworkStores<OmsDbContext>().AddDefaultTokenProviders().AddClaimsPrincipalFactory<UserClaimsFactory>();
builder.Services.ConfigureApplicationCookie(o=>{o.LoginPath="/Account/Login";o.AccessDeniedPath="/Account/Denied";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;o.ExpireTimeSpan=TimeSpan.FromMinutes(30);o.SlidingExpiration=true;});
builder.Services.Configure<SecurityStampValidatorOptions>(o=>o.ValidationInterval=TimeSpan.FromMinutes(1));
builder.Services.AddAuthorization(options=>{foreach(var p in Permissions.All)options.AddPolicy(p,policy=>policy.RequireAuthenticatedUser().RequireClaim("permission",p));});
builder.Services.AddControllersWithViews(options=>{options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=true;options.ModelMetadataDetailsProviders.Add(new PHEOC.OMS.Web.ViewSupport.StringMetadata());});
builder.Services.AddScoped<IOmsService,OmsService>();builder.Services.AddScoped<IQiraService,OmsService>();builder.Services.AddScoped<IUserAdministration,UserAdministration>();builder.Services.AddScoped<IEventIntake,EventIntake>();builder.Services.AddSingleton<IReportExporter,ReportExporter>();
builder.Services.AddScoped<ICoordinationService,OmsService>();builder.Services.AddScoped<SlaEngine>();builder.Services.AddHostedService<SlaWorker>();
builder.Services.AddScoped<EgyptEbsReader>();builder.Services.AddScoped<IEgyptEbsReader>(sp=>sp.GetRequiredService<EgyptEbsReader>());builder.Services.AddScoped<IEgyptEbsSyncSource>(sp=>sp.GetRequiredService<EgyptEbsReader>());builder.Services.AddScoped<EgyptEbsSync>();builder.Services.AddHostedService<EgyptEbsWorker>();
builder.Services.AddSingleton<IEgyptEbsServerStore>(new EgyptEbsServerFileStore(Path.Combine(builder.Environment.ContentRootPath,".local","settings","egypt-ebs-server.json")));
builder.Services.AddSingleton<IEgyptEbsConnectionSource,EgyptEbsConnectionSource>();
builder.Services.AddSingleton<IEgyptEbsConnectionProbe,EgyptEbsConnectionProbe>();
builder.Services.AddScoped<IEgyptEbsServerSettings,EgyptEbsServerSettings>();
builder.Services.AddScoped<IEgyptEbsKpiDateSource,EgyptEbsKpiDateSource>();
builder.Services.AddScoped<IAnalyticsService,OmsService>();
builder.Services.AddScoped<IPolicyService,OmsService>();
builder.Services.AddScoped<ILogisticsService,OmsService>();
builder.Services.AddScoped<IIntegrationStatus,IntegrationStatus>();builder.Services.AddScoped<IEventAlertService,EventAlertService>();
var keys=builder.Configuration["DataProtection:Path"]??Path.Combine(builder.Environment.ContentRootPath,".local","keys");
var dp=builder.Services.AddDataProtection().SetApplicationName("PHEOC-OMS").PersistKeysToFileSystem(new DirectoryInfo(keys));
if(OperatingSystem.IsWindows())dp.ProtectKeysWithDpapi();
builder.Services.AddSignalR();builder.Services.AddHttpClient("EbsCallback",c=>c.Timeout=TimeSpan.FromSeconds(15));builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddPolicy("login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new(){PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));o.AddPolicy("ebs",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new(){PermitLimit=120,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
var app=builder.Build();
if(args.Contains("--initialize-database"))
{
    var adminName=app.Configuration["Bootstrap:UserName"]??throw new InvalidOperationException("Bootstrap__UserName is required.");
    var password=app.Configuration["Bootstrap:Password"]??throw new InvalidOperationException("Bootstrap__Password is required.");
    using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();await db.Database.MigrateAsync();
    await InitialData.Seed(db,scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),adminName,password);
    Console.WriteLine("Database migrated; reference data and administrator initialized. No operational demo data inserted.");return;
}
if(args.Contains("--db-check"))
{
    using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();Console.WriteLine("SQL connection: "+await db.Database.CanConnectAsync());Console.WriteLine("Events: "+await db.Events.CountAsync());return;
}
app.Use(async(context,next)=>{
    context.Response.Headers["X-Content-Type-Options"]="nosniff";context.Response.Headers["X-Frame-Options"]="DENY";context.Response.Headers["Referrer-Policy"]="same-origin";
    context.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self' ws: wss:; frame-ancestors 'none'; form-action 'self'; base-uri 'self'";
    if(!context.Request.Path.StartsWithSegments("/css")&&!context.Request.Path.StartsWithSegments("/js")){context.Response.Headers.CacheControl="no-cache, no-store";context.Response.Headers.Pragma="no-cache";}
    try{await next();}
    catch(Exception ex) when(ex is PHEOC.OMS.Domain.Rules.BusinessRuleException or System.ComponentModel.DataAnnotations.ValidationException or DbUpdateConcurrencyException or KeyNotFoundException or AccessDeniedException or DbUpdateException)
    {
        if(context.Response.HasStarted)throw;
        var code=ex is AccessDeniedException?403:ex is KeyNotFoundException?404:ex is DbUpdateException or PHEOC.OMS.Domain.Rules.IdempotencyConflictException?409:400;
        var message=ex is DbUpdateConcurrencyException?"تعارض تعديل؛ أعد تحميل الصفحة.":ex is DbUpdateException?"تعذر الحفظ بسبب تعارض أو بيانات مكررة.":ex is AccessDeniedException?"ليست لديك صلاحية لهذا الإجراء.":ex.Message;
        context.Response.StatusCode=code;
        if(context.Request.Path.StartsWithSegments("/api"))await context.Response.WriteAsJsonAsync(new{error=message});
        else{context.Response.ContentType="text/html; charset=utf-8";await context.Response.WriteAsync("<!doctype html><html lang='ar' dir='rtl'><meta charset='utf-8'><link rel='stylesheet' href='/css/site.css'><main class='error-page'><h1>تعذر إتمام الإجراء</h1><p>"+System.Net.WebUtility.HtmlEncode(message)+"</p><a href='/'>العودة إلى لوحة القيادة</a><p>للعودة إلى البيانات المدخلة استخدم زر الرجوع في المتصفح.</p></main></html>");}
    }
    catch(Exception ex)
    {
        Log.Error("Unhandled request failure {ErrorType} {TraceId}",ex.GetType().Name,context.TraceIdentifier);
        if(context.Response.HasStarted)throw;context.Response.StatusCode=503;context.Response.ContentType="text/html; charset=utf-8";
        await context.Response.WriteAsync("<html lang='ar' dir='rtl'><meta charset='utf-8'><h1>الخدمة غير متاحة مؤقتاً</h1><p>تحقق من إعداد اتصال قاعدة البيانات وسجل التشغيل. رقم المتابعة: "+context.TraceIdentifier+"</p></html>");
    }
});
if(!app.Environment.IsDevelopment()){app.UseHsts();app.UseHttpsRedirection();}
app.UseStaticFiles();app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
app.MapControllerRoute("default","{controller=Dashboard}/{action=Index}/{id?}");app.MapHub<OperationsHub>("/hubs/operations");
app.MapGet("/health/live",()=>Results.Ok(new{status="running"})).AllowAnonymous();
await app.RunAsync();
public partial class Program { }
