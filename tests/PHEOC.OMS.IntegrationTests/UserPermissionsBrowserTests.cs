using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Infrastructure.Data;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;
public class UserPermissionsBrowserTests
{
    private sealed class Actor:ICurrentUser{public string Id=>"qa.permission-editor";public int? GovernorateId=>null;public bool Can(string p)=>true;}
    private sealed class Host(string root,SqlConnection connection,SqlTransaction transaction):WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.UseEnvironment("Development").UseContentRoot(Path.Combine(root,"src/PHEOC.OMS.Web")).UseWebRoot(Path.Combine(root,"src/PHEOC.OMS.Web/wwwroot"))
        .ConfigureTestServices(services=>{
            services.RemoveAll<IHostedService>();services.RemoveAll<OmsDbContext>();services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddScoped(sp=>{var db=new OmsDbContext(new DbContextOptionsBuilder<OmsDbContext>().UseSqlServer(connection,s=>s.UseCompatibilityLevel(130).UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options,sp.GetRequiredService<ICurrentUser>());db.Database.UseTransaction(transaction);return db;});
            // Exercise an actual cookie validation without waiting for the production one-minute interval.
            services.Configure<SecurityStampValidatorOptions>(o=>o.ValidationInterval=TimeSpan.Zero);
        });
    }
    [BrowserReviewFact]public async Task EditPermissionsThroughRealCookiesReloadAndRevokePreviousSession()
    {
        var cs=Environment.GetEnvironmentVariable("PHEOC_TEST_SQL");Assert.False(string.IsNullOrWhiteSpace(cs));var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"PHEOC.OMS.sln")))dir=dir.Parent;Assert.NotNull(dir);var root=dir.FullName;
        await using var connection=new SqlConnection(new SqlConnectionStringBuilder(cs){MultipleActiveResultSets=true}.ConnectionString);await connection.OpenAsync();await using var transaction=(SqlTransaction)await connection.BeginTransactionAsync();
        await using var host=new Host(root,connection,transaction);host.UseKestrel(0);using var client=host.CreateClient();
        var suffix=Guid.NewGuid().ToString("N");var password="Qa!9Aa"+suffix;
        using(var scope=host.Services.CreateScope()){
            var service=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();var admin=new UserAdministration(db,service,new Actor());
            await admin.Create(new(){UserName="qa.editor."+suffix,Password=password,Role="System Administrator",Permissions=[Permissions.EventsView,Permissions.AdminUsers]});
            await admin.Create(new(){UserName="qa.target."+suffix,Password=password,Role="Risk Assessment Officer",Permissions=[Permissions.EventsView,Permissions.ReportsView]});
        }
        var start=new ProcessStartInfo("node"){WorkingDirectory=root,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add("scripts/browser-edit-user-permissions-check.mjs");start.ArgumentList.Add(client.BaseAddress!.ToString());
        start.Environment["PHEOC_EDIT_ADMIN"]="qa.editor."+suffix;start.Environment["PHEOC_EDIT_TARGET"]="qa.target."+suffix;start.Environment["PHEOC_EDIT_PASSWORD"]=password;
        using var process=Process.Start(start)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(100));
        try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
        Assert.True(process.ExitCode==0,(await output)+Environment.NewLine+(await errors));await transaction.RollbackAsync();
    }
}
