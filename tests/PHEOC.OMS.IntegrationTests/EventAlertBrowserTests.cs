using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Events;
using PHEOC.OMS.Web.Realtime;

namespace PHEOC.OMS.IntegrationTests;

public class BrowserReviewFactAttribute : FactAttribute
{
    public BrowserReviewFactAttribute() { if(Environment.GetEnvironmentVariable("PHEOC_BROWSER_REVIEW")!="1") Skip="Set PHEOC_BROWSER_REVIEW=1 to launch local Chrome against the isolated test host."; }
}

public class AlertBrowserState : IEventAlertService
{
    private readonly Dictionary<long,EventAlertItem> items=[];
    private readonly HashSet<long> read=[];
    private readonly object gate=new();
    public void Receive(long id,string title) { lock(gate) items.TryAdd(id,new(id,id+1000,title,"القاهرة",DateTime.UtcNow)); }
    public Task<EventAlertFeed> Feed(long afterId=0)
    {
        lock(gate) {
            var unread=items.Values.Where(n=>!read.Contains(n.Id)).OrderByDescending(n=>n.Id).ToList();
            return Task.FromResult(new EventAlertFeed(unread.Count,items.Keys.DefaultIfEmpty().Max(),unread.Count(n=>n.Id>afterId),unread.Take(20).ToList()));
        }
    }
    public Task Read(IReadOnlyList<long> ids) { lock(gate) foreach(var id in ids) read.Add(id); return Task.CompletedTask; }
}

// Discovered only by the factory below; this assembly is not part of the deployed web application.
[Route("test-event-alerts"),IgnoreAntiforgeryToken]
public class AlertBrowserHarnessController(AlertBrowserState state,IHubContext<OperationsHub> hub) : Controller
{
    public record Arrival(long Id,string Title);
    [HttpGet("page")] public IActionResult Page() => View("~/Views/Events/Improvements.cshtml",Array.Empty<ActivityRow>());
    [HttpGet("timeline")] public IActionResult Timeline(bool empty=false)
    {
        IReadOnlyList<ActivityRow> rows=empty?[]:[
            new(6,"Assignment","تعيين مسؤول المعلومات والاتصال: مسؤول نظم المعلومات","qa.timeline",new(2026,11,6,10,4,0,DateTimeKind.Utc)),
            new(5,"Assignment","تعيين رئيس العمليات: مسؤول العمليات على مستوى الإدارة العامة للترصد والمتابعة","qa.timeline",new(2026,11,6,7,52,0,DateTimeKind.Utc)),
            new(4,"Assignment","تعيين نائب مدير الحادث: نائب مدير الإدارة العامة للترصد والمتابعة","qa.timeline",new(2026,11,5,22,22,0,DateTimeKind.Utc)),
            new(3,"Assignment","تعيين مدير الحادث: مدير الإدارة العامة للترصد والمتابعة","qa.timeline",new(2026,11,5,22,22,0,DateTimeKind.Utc)),
            new(2,"Received","تم استقبال الحدث من المصدر وحفظ لقطة البيانات","system",new(2026,11,5,20,0,0,DateTimeKind.Utc)),
            new(1,"FutureAction","<img src=x onerror=window.__timelineXss=1> اختبار نص إجراء طويل مع الحفاظ على ترميز المحتوى وعدم تنفيذ أي وسم من بيانات المصدر","qa.timeline",new(2026,11,5,19,0,0,DateTimeKind.Utc))
        ];
        return View("~/Views/Events/_Timeline.cshtml",rows);
    }
    [HttpPost("receive")] public async Task<IActionResult> Receive([FromBody]Arrival input)
    {
        state.Receive(input.Id,input.Title);
        await EventArrivalBroadcast.Publish(hub,new EmergencyEvent{Id=input.Id+1000,GovernorateId=1},"{\"Action\":\"Received\"}",default);
        return Ok();
    }
}

public class EventAlertBrowserTests
{
    private sealed class Authentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,"alert-browser-review"),new Claim(ClaimTypes.Name,"alert-review"),new Claim("permission",Permissions.EventsView)},Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(identity),Scheme.Name)));
        }
    }
    private sealed class Host(string root) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development")
            .UseContentRoot(Path.Combine(root,"src/PHEOC.OMS.Web"))
            .UseWebRoot(Path.Combine(root,"src/PHEOC.OMS.Web/wwwroot"))
            .ConfigureTestServices(services=>{
                services.RemoveAll<IHostedService>();services.RemoveAll<IEventAlertService>();
                services.AddSingleton<AlertBrowserState>();services.AddSingleton<IEventAlertService>(sp=>sp.GetRequiredService<AlertBrowserState>());
                services.AddControllersWithViews().AddApplicationPart(typeof(AlertBrowserHarnessController).Assembly);
                services.AddAuthentication(o=>{o.DefaultAuthenticateScheme="AlertReview";o.DefaultChallengeScheme="AlertReview";})
                    .AddScheme<AuthenticationSchemeOptions,Authentication>("AlertReview",_=>{});
            });
    }
    [BrowserReviewFact] public async Task SignalRHeaderAnimationSoundReadingAndMobileWorkTogether()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"PHEOC.OMS.sln")))dir=dir.Parent;
        Assert.NotNull(dir);var root=dir.FullName;
        await using var factory=new Host(root);factory.UseKestrel(0);
        using var client=factory.CreateClient();Assert.True((await client.GetAsync("/test-event-alerts/page")).IsSuccessStatusCode);
        var start=new ProcessStartInfo("node"){WorkingDirectory=root,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add("scripts/browser-event-alerts-check.mjs");start.ArgumentList.Add(client.BaseAddress!.ToString());
        using var process=Process.Start(start)!;
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(100));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch(OperationCanceledException){process.Kill(true);throw;}
        var result=await stdout;var error=await stderr;
        Assert.True(process.ExitCode==0,result+Environment.NewLine+error);
    }
    [BrowserReviewFact] public async Task TimelineLayoutEgyptDatesEncodingAndMobileRenderTogether()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"PHEOC.OMS.sln")))dir=dir.Parent;
        Assert.NotNull(dir);var root=dir.FullName;await using var factory=new Host(root);factory.UseKestrel(0);
        using var client=factory.CreateClient();Assert.True((await client.GetAsync("/test-event-alerts/timeline")).IsSuccessStatusCode);
        var start=new ProcessStartInfo("node"){WorkingDirectory=root,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add("scripts/browser-event-timeline-check.mjs");start.ArgumentList.Add(client.BaseAddress!.ToString());
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(100));
        try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
        Assert.True(process.ExitCode==0,await stdout+Environment.NewLine+await stderr);
    }
}
