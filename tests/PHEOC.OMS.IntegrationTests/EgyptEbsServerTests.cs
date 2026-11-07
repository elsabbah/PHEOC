using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;
using PHEOC.OMS.Domain.Rules;
using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.IntegrationTests;

public class EgyptEbsServerTests
{
    private const string Original="Server=original;Database=EgyptEBS;User Id=reader;Password=synthetic-test-password;Encrypt=true;TrustServerCertificate=false";
    private sealed class Actor(bool allowed=true,int? governorate=null):ICurrentUser
    {public string Id=>"server-settings-review";public int? GovernorateId=>governorate;public bool Can(string permission)=>allowed;}
    private sealed class Probe:IEgyptEbsConnectionProbe
    {
        public string? Last {get;private set;}
        public Task Check(string value)
        {
            Last=value;
            if(new SqlConnectionStringBuilder(value).DataSource=="offline")throw new OperationCanceledException("sensitive diagnostic");
            return Task.CompletedTask;
        }
    }
    private static IConfiguration Config()=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:EgyptEbsReader",Original}}).Build();
    [Theory]
    [InlineData("SERVER\\INSTANCE")][InlineData("192.168.1.10,1433")][InlineData("tcp:sql-host,1433")][InlineData("[::1],1433")]
    public void OnlyAddressChangesAndSecurityCredentialsStay(string server)
    {
        var before=new SqlConnectionStringBuilder(Original);var after=new SqlConnectionStringBuilder(EgyptEbsConnectionSource.Build(Original,server));
        Assert.Equal(server,after.DataSource);after.DataSource=before.DataSource;Assert.Equal(before.ConnectionString,after.ConnectionString);
    }
    [Theory][InlineData("host;Password=override")][InlineData("https://host")][InlineData("")]
    public void ConnectionStringInjectionAndUrlsAreRejected(string server)=>Assert.Throws<BusinessRuleException>(()=>EgyptEbsConnectionSource.Build(Original,server));

    [Theory]
    [InlineData(18456,"رفض حساب القراءة")][InlineData(4060,"فتح قاعدة EgyptEBS")]
    [InlineData(229,"SELECT")][InlineData(208,"جداول EgyptEBS")][InlineData(207,"مخطط أعمدة")]
    public void FailuresExplainTheActualCategoryWithoutRawDiagnostics(int code,string expected)=>Assert.Contains(expected,EgyptEbsConnectionProbe.Failure(false,code));
    [Theory][InlineData(5)][InlineData(10013)]
    public void LocalNetworkDenialExplainsTheApplicationContext(int code)
    {
        var text=EgyptEbsConnectionProbe.Failure(true,code);
        Assert.Contains("Access denied",text);Assert.Contains("عملية التطبيق",text);
        Assert.DoesNotContain("شهادة الاتصال",text);Assert.DoesNotContain("رفض حساب القراءة",text);
    }
    [Fact] public void TimeoutDistinguishesOpeningFromReading()
    {
        Assert.Contains("المصافحة",EgyptEbsConnectionProbe.Failure(true,-2));
        Assert.Contains("بعد فتح الاتصال",EgyptEbsConnectionProbe.Failure(false,-2));
    }

    [Fact]public async Task AlternateCredentialsAreEncryptedReusableAndCanReturnToOriginalAccount()
    {
        var folder=Path.Combine(Path.GetTempPath(),"pheoc-credentials-"+Guid.NewGuid().ToString("N"));
        var path=Path.Combine(folder,"server.json");var keys=Path.Combine(folder,"keys");
        try
        {
            var protection=DataProtectionProvider.Create(new DirectoryInfo(keys));
            var store=new EgyptEbsServerFileStore(path);var probe=new Probe();
            var service=new EgyptEbsServerSettings(Config(),store,probe,new Actor(),protection);
            await service.Save(new(){Server="alternate",UserName="alternate-reader",Password="alternate-secret-for-test"});
            var json=await File.ReadAllTextAsync(path);Assert.DoesNotContain("alternate-reader",json);Assert.DoesNotContain("alternate-secret-for-test",json);
            var secondProtection=DataProtectionProvider.Create(new DirectoryInfo(keys));
            var source=new EgyptEbsConnectionSource(Config(),new EgyptEbsServerFileStore(path),secondProtection);
            var cs=new SqlConnectionStringBuilder(await source.ConnectionString());
            Assert.Equal("alternate-reader",cs.UserID);Assert.Equal("alternate-secret-for-test",cs.Password);Assert.False(cs.IntegratedSecurity);
            var revision=(await store.Read())!.Revision;
            await service.Test(new(){Server="alternate",Revision=revision});
            Assert.Equal("alternate-reader",new SqlConnectionStringBuilder(probe.Last).UserID);
            await Assert.ThrowsAsync<BusinessRuleException>(()=>service.Save(new(){Server="alternate",Revision=revision,UserName="partial"}));
            await Assert.ThrowsAsync<BusinessRuleException>(()=>service.Save(new(){Server="offline",Revision=revision,UserName="new-reader",Password="new-test-secret"}));
            Assert.Equal(json,await File.ReadAllTextAsync(path));
            await service.Save(new(){Server="alternate",Revision=revision,UseOriginalAccount=true});
            Assert.Null((await store.Read())!.ProtectedCredentials);
            Assert.Equal("reader",new SqlConnectionStringBuilder(await source.ConnectionString()).UserID);
        }
        finally
        {
            if(File.Exists(path))File.Delete(path);
            if(Directory.Exists(keys)){foreach(var file in Directory.GetFiles(keys))File.Delete(file);Directory.Delete(keys);}
            if(Directory.Exists(folder))Directory.Delete(folder);
        }
    }

    [Fact]public async Task FailedProbeDoesNotSaveAndSuccessPersistsAcrossInstancesWithConflictDetection()
    {
        var path=Path.Combine(Path.GetTempPath(),"pheoc-server-"+Guid.NewGuid().ToString("N"),"server.json");
        try
        {
            var store=new EgyptEbsServerFileStore(path);var probe=new Probe();var protection=new EphemeralDataProtectionProvider();var service=new EgyptEbsServerSettings(Config(),store,probe,new Actor(),protection);
            var error=await Assert.ThrowsAsync<BusinessRuleException>(()=>service.Save(new(){Server="offline"}));
            Assert.DoesNotContain("sensitive diagnostic",error.Message);Assert.False(File.Exists(path));
            await service.Test(new(){Server="new-server"});Assert.False(File.Exists(path));
            await service.Save(new(){Server="new-server"});
            var freshStore=new EgyptEbsServerFileStore(path);var state=await freshStore.Read();Assert.NotNull(state);Assert.Equal("server-settings-review",state.UpdatedBy);
            var source=new EgyptEbsConnectionSource(Config(),freshStore,protection);Assert.Equal("new-server",new SqlConnectionStringBuilder(await source.ConnectionString()).DataSource);
            var stored=await File.ReadAllTextAsync(path);Assert.DoesNotContain("synthetic-test-password",stored);Assert.DoesNotContain("User Id",stored);
            await Assert.ThrowsAsync<BusinessRuleException>(()=>service.Save(new(){Server="other-server",Revision="stale"}));
            Assert.Equal("new-server",(await freshStore.Read())!.Server);
            await service.Save(new(){Server="next-server",Revision=state.Revision});
            Assert.Equal("next-server",new SqlConnectionStringBuilder(await source.ConnectionString()).DataSource);
        }
        finally {if(File.Exists(path))File.Delete(path);if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path)!);}
    }
    [Theory][InlineData(false,null)][InlineData(true,1)]
    public async Task PermissionAndCentralScopeAreRequired(bool allowed,int? governorate)
    {
        var store=new EgyptEbsServerFileStore("unused");var probe=new Probe();var service=new EgyptEbsServerSettings(Config(),store,probe,new Actor(allowed,governorate),new EphemeralDataProtectionProvider());
        await Assert.ThrowsAsync<AccessDeniedException>(()=>service.Get());
        await Assert.ThrowsAsync<AccessDeniedException>(()=>service.Test(new(){Server="host"}));
        await Assert.ThrowsAsync<AccessDeniedException>(()=>service.Save(new(){Server="host"}));Assert.Null(probe.Last);
    }
    private sealed class Authentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()=>Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,"settings-review"),new Claim(ClaimTypes.Name,"settings-review"),new Claim("permission",Permissions.EventsView),new Claim("permission",Permissions.AdminSettings)},Scheme.Name)),Scheme.Name)));
    }
    private sealed class Host(string path):WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:EgyptEbsReader",Original}}))
            .ConfigureTestServices(s=>{s.RemoveAll<IHostedService>();s.RemoveAll<IEgyptEbsServerStore>();s.AddSingleton<IEgyptEbsServerStore>(new EgyptEbsServerFileStore(path));s.RemoveAll<IEgyptEbsConnectionProbe>();s.AddSingleton<IEgyptEbsConnectionProbe,Probe>();s.AddAuthentication(o=>{o.DefaultAuthenticateScheme="SettingsReview";o.DefaultChallengeScheme="SettingsReview";}).AddScheme<AuthenticationSchemeOptions,Authentication>("SettingsReview",_=>{});});
    }
    [Fact]public async Task RealSettingsFormTestsThenSavesAndRequiresAntiforgery()
    {
        var path=Path.Combine(Path.GetTempPath(),"pheoc-http-"+Guid.NewGuid().ToString("N"),"server.json");
        try
        {
            await using var host=new Host(path);using var client=host.CreateClient(new(){AllowAutoRedirect=false});
            var html=await client.GetStringAsync("/EgyptEbsServer");Assert.Contains("original",html);Assert.DoesNotContain("synthetic-test-password",html);
            var token=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);Assert.NotEmpty(token);
            var missing=await client.PostAsync("/EgyptEbsServer/Save",new FormUrlEncodedContent(new Dictionary<string,string>{{"Server","new-server"}}));Assert.Equal(HttpStatusCode.BadRequest,missing.StatusCode);
            var fields=new Dictionary<string,string>{{"Server","new-server"},{"Revision",""},{"__RequestVerificationToken",token},{"UserName","alternate-reader"},{"Password","ui-synthetic-secret"}};
            var test=await client.PostAsync("/EgyptEbsServer/Test",new FormUrlEncodedContent(fields));Assert.Equal(HttpStatusCode.OK,test.StatusCode);Assert.False(File.Exists(path));Assert.DoesNotContain("ui-synthetic-secret",await test.Content.ReadAsStringAsync());
            var save=await client.PostAsync("/EgyptEbsServer/Save",new FormUrlEncodedContent(fields));Assert.Equal(HttpStatusCode.Redirect,save.StatusCode);
            html=await client.GetStringAsync("/EgyptEbsServer");Assert.Contains("new-server",html);Assert.DoesNotContain("synthetic-test-password",html);
            Assert.DoesNotContain("ui-synthetic-secret",html);Assert.DoesNotContain("ui-synthetic-secret",await File.ReadAllTextAsync(path));
        }
        finally {if(File.Exists(path))File.Delete(path);if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path)!);}
    }
}
