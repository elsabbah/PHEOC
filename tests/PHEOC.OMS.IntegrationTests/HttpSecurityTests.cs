using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
namespace PHEOC.OMS.IntegrationTests;
public class SecurityHost : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.UseEnvironment("Development").ConfigureTestServices(services=>services.RemoveAll<IHostedService>());
}
public class HttpSecurityTests(SecurityHost host) : IClassFixture<SecurityHost>
{
    private HttpClient Client()=>host.CreateClient(new(){AllowAutoRedirect=false});
    [Theory][InlineData("/")][InlineData("/Events")][InlineData("/Admin/Users")][InlineData("/Inventory")][InlineData("/Health")]
    public async Task ProtectedPagesRedirectAnonymousUsers(string path){using var c=Client();var r=await c.GetAsync(path);Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Contains("/Account/Login",r.Headers.Location!.OriginalString);}
    [Fact]public async Task LoginRendersArabicAndSecurityHeaders(){using var c=Client();var r=await c.GetAsync("/Account/Login");var html=await r.Content.ReadAsStringAsync();Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Contains("dir=\"rtl\"",html);Assert.Contains("__RequestVerificationToken",html);Assert.Contains("frame-ancestors 'none'",r.Headers.GetValues("Content-Security-Policy").Single());}
    [Fact]public async Task CookiePostRequiresAntiForgery(){using var c=Client();var r=await c.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"UserName","test"},{"Password","irrelevant"}}));Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);}
    [Fact]public async Task EbsRejectsMissingApiCredentialBeforeDatabaseAccess(){using var c=Client();var r=await c.PostAsJsonAsync("/api/integration/ebs/events",new{externalEventId="security-test",@event=new{title="security test",eventType="test",threatType="test",governorateId=1}});Assert.True(r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest);}
    [Fact]public async Task HealthEndpointDoesNotDiscloseDatabase(){using var c=Client();var r=await c.GetAsync("/health/live");Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.DoesNotContain("connection",await r.Content.ReadAsStringAsync());}
}
