using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;
public class QiraBrowserTests
{
    private sealed class Actor:ICurrentUser{public string Id=>"qa.qira.author";public int? GovernorateId=>null;public bool Can(string p)=>true;}
    private sealed class Authentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var id=Request.Cookies["qira-actor"]=="reviewer"?"qa.qira.reviewer":"qa.qira.author";
            var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,id),new(ClaimTypes.Name,id)};claims.AddRange(Permissions.All.Select(p=>new Claim("permission",p)));
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));
        }
    }
    private sealed class Host(string root,SqlConnection connection,SqlTransaction transaction):WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.UseEnvironment("Development").UseContentRoot(Path.Combine(root,"src/PHEOC.OMS.Web")).UseWebRoot(Path.Combine(root,"src/PHEOC.OMS.Web/wwwroot"))
            .ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"Storage:Attachments",Path.Combine(root,".local/qira-qa-attachments")}}))
            .ConfigureTestServices(services=>{
                services.RemoveAll<IHostedService>();services.RemoveAll<OmsDbContext>();services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddScoped(sp=>{var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(connection,s=>s.UseCompatibilityLevel(130).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options,sp.GetRequiredService<ICurrentUser>());db.Database.UseTransaction(transaction);return db;});
                services.AddAuthentication(o=>{o.DefaultAuthenticateScheme="QiraReview";o.DefaultChallengeScheme="QiraReview";}).AddScheme<AuthenticationSchemeOptions,Authentication>("QiraReview",_=>{});
            });
    }
    [BrowserReviewFact]public async Task RealQiraWorkflowPersistsAcrossRequestsAndRollsBackAfterBrowserReview()
    {
        var cs=Environment.GetEnvironmentVariable("PHEOC_TEST_SQL");Assert.False(string.IsNullOrWhiteSpace(cs));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"PHEOC.OMS.sln")))directory=directory.Parent;Assert.NotNull(directory);var root=directory.FullName;
        var builder=new SqlConnectionStringBuilder(cs){MultipleActiveResultSets=true};await using var connection=new SqlConnection(builder.ConnectionString);await connection.OpenAsync();await using var transaction=(SqlTransaction)await connection.BeginTransactionAsync();
        var actor=new Actor();await using var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(connection,s=>s.UseCompatibilityLevel(130)).Options,actor);await db.Database.UseTransactionAsync(transaction);
        var s=new OmsService(db,actor);await s.AddQiraPolicy(new(){Name="[QA rollback] isolated starting policy",Reference="Browser fixture only; rolled back"});
        var eventId=await s.CreateEvent(new(){Title="[QA rollback] واجهة تقييم QIRA",EventType="تدريب",ThreatType="QA",GovernorateId=1,IsTraining=true});
        await using var host=new Host(root,connection,transaction);host.UseKestrel(0);using var client=host.CreateClient();
        var start=new ProcessStartInfo("node"){WorkingDirectory=root,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add("scripts/browser-qira-check.mjs");start.ArgumentList.Add(client.BaseAddress!.ToString());start.ArgumentList.Add(eventId.ToString());
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(150));
        try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
        Assert.True(process.ExitCode==0,(await stdout)+Environment.NewLine+(await stderr));
        db.ChangeTracker.Clear();Assert.Equal(2,await db.QiraAssessments.CountAsync(x=>x.EventId==eventId));Assert.Single(await db.Tasks.Where(x=>x.EventId==eventId).ToListAsync());
        await transaction.RollbackAsync();
    }
}
