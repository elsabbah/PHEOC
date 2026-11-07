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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.IntegrationTests;

public class KpiSourceBrowserTests
{
    private sealed class Actor:ICurrentUser{public string Id=>"qa.kpi.browser";public int? GovernorateId=>null;public bool Can(string p)=>true;}
    private sealed class Authentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,"qa.kpi.browser"),new(ClaimTypes.Name,"qa.kpi.browser")};claims.AddRange(Permissions.All.Select(p=>new Claim("permission",p)));
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));
        }
    }
    private sealed class Host(string root,SqlConnection connection,SqlTransaction transaction):WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.UseEnvironment("Development").UseContentRoot(Path.Combine(root,"src/PHEOC.OMS.Web")).UseWebRoot(Path.Combine(root,"src/PHEOC.OMS.Web/wwwroot"))
            .ConfigureTestServices(services=>{
                services.RemoveAll<IHostedService>();services.RemoveAll<OmsDbContext>();services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<IEgyptEbsKpiDateSource>();services.AddSingleton<IEgyptEbsKpiDateSource>(new KpiSourceDatesTests.Source());
                services.AddScoped(sp=>{var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(connection,s=>s.UseCompatibilityLevel(130).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options,sp.GetRequiredService<ICurrentUser>());db.Database.UseTransaction(transaction);return db;});
                services.AddAuthentication(o=>{o.DefaultAuthenticateScheme="KpiReview";o.DefaultChallengeScheme="KpiReview";}).AddScheme<AuthenticationSchemeOptions,Authentication>("KpiReview",_=>{});
            });
    }
    [BrowserReviewFact]public async Task PreviewFillSaveReloadAndManualValuesWorkAcrossResponsiveWidths()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"PHEOC.OMS.sln")))dir=dir.Parent;Assert.NotNull(dir);var root=dir.FullName;
        await using var connection=new SqlConnection(new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PHEOC_TEST_SQL")){MultipleActiveResultSets=true}.ConnectionString);await connection.OpenAsync();await using var tx=(SqlTransaction)await connection.BeginTransactionAsync();
        await using var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(connection,s=>s.UseCompatibilityLevel(130)).Options,new Actor());await db.Database.UseTransactionAsync(tx);var eventId=await KpiSourceDatesTests.CreateImported(db);
        await using var host=new Host(root,connection,tx);host.UseKestrel(0);using var client=host.CreateClient();
        var start=new ProcessStartInfo("node"){WorkingDirectory=root,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add("scripts/browser-kpi-source-check.mjs");start.ArgumentList.Add(client.BaseAddress!.ToString());start.ArgumentList.Add(eventId.ToString());
        using var process=Process.Start(start)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(100));
        try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
        Assert.True(process.ExitCode==0,(await output)+Environment.NewLine+(await errors));db.ChangeTracker.Clear();var records=await db.KpiDateRecords.Where(x=>x.EventId==eventId).ToListAsync();Assert.Single(records);Assert.Equal(new DateOnly(2026,1,4),records[0].Notification);Assert.True(records[0].Excluded);await tx.RollbackAsync();
    }
}
