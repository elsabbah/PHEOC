using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PHEOC.OMS.Application.Contracts;
using PHEOC.OMS.Application.Security;

namespace PHEOC.OMS.IntegrationTests;

public class AutomaticReceptionTests
{
    private sealed class Reader(bool enabled) : IEgyptEbsReader
    {
        public bool Enabled { get; private set; } = enabled;
        public int Changes { get; private set; }
        public EgyptEbsFilter? LastFilter {get;private set;}
        public bool IncludeLookups {get;private set;}
        public IReadOnlyList<EgyptEbsSignal> Signals {get;set;}=[];
        public Task<EgyptEbsBoard> Candidates(int? before = null) => Task.FromResult(new EgyptEbsBoard(true, [], Enabled));
        public Task<EgyptEbsBoard> Browse(EgyptEbsFilter filter,int? before=null,bool includeLookups=true){LastFilter=filter;IncludeLookups=includeLookups;return Task.FromResult(new EgyptEbsBoard(true,Signals,Enabled,Filter:filter,TotalCount:Signals.Count,Before:before));}
        public Task<IntakeResult> Import(int signalId) => throw new NotSupportedException();
        public Task SetAutomatic(bool enabled) { Enabled = enabled; Changes++; return Task.CompletedTask; }
        public Task<EgyptEbsStatus> Status()=>Task.FromResult(new EgyptEbsStatus(Enabled,DateTime.UtcNow,"",DateTime.UtcNow));
    }

    private sealed class AdminAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "render-test-admin"),
                new Claim(ClaimTypes.Name, "render-test-admin"),
                new Claim("permission", Permissions.EventsView),
                new Claim("permission", Permissions.AdminSettings)
            }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class Host(Reader reader) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development")
            .ConfigureTestServices(services => {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IEgyptEbsReader>();
                services.AddSingleton<IEgyptEbsReader>(reader);
                services.AddAuthentication(o => {
                    o.DefaultAuthenticateScheme = "RenderTest";
                    o.DefaultChallengeScheme = "RenderTest";
                }).AddScheme<AuthenticationSchemeOptions, AdminAuthentication>("RenderTest", _ => { });
            });
    }

    [Theory]
    [InlineData(false, "true")]
    [InlineData(true, "false")]
    public async Task RenderedButtonPostsValidBooleanAndRedirects(bool initial, string expected)
    {
        var reader = new Reader(initial);
        await using var host = new Host(reader);
        using var client = host.CreateClient(new() { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/EgyptEbs");
        var form = Regex.Match(html, "<form[^>]*action=\"/EgyptEbs/Automatic\"[^>]*>(.*?)</form>", RegexOptions.Singleline).Groups[1].Value;
        Assert.NotEmpty(form);
        var value = Regex.Match(form, "name=\"enabled\"[^>]*value=\"([^\"]*)\"").Groups[1].Value;
        Assert.Equal(expected, value);
        var token = WebUtility.HtmlDecode(Regex.Match(form, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        var response = await client.PostAsync("/EgyptEbs/Automatic", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["enabled"] = value, ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(!initial, reader.Enabled);
        Assert.Equal(1, reader.Changes);
    }

    [Fact] public async Task StatusIsFreshUtcAndReadOnlyAndSignalsKeepPagination()
    {
        var reader=new Reader(true);
        await using var host=new Host(reader);
        using var client=host.CreateClient();
        var response=await client.GetAsync("/EgyptEbs/Status");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        var status=await response.Content.ReadFromJsonAsync<EgyptEbsStatus>();
        Assert.NotNull(status);Assert.True(status.Automatic);
        Assert.Equal(DateTimeKind.Utc,status.ServerUtcNow.Kind);
        Assert.InRange((DateTime.UtcNow-status.ServerUtcNow).TotalSeconds,-2,10);
        Assert.Equal(0,reader.Changes);
        var signals=await client.GetStringAsync("/EgyptEbs/Signals?before=100");
        Assert.Contains("table-scroll",signals);Assert.DoesNotContain("<!doctype",signals);
    }
    [Fact] public async Task FullPageAndLiveFragmentForwardTheSameFilters()
    {
        var reader=new Reader(true);await using var host=new Host(reader);using var client=host.CreateClient();
        var query="?Search=1158553&Governorate="+Uri.EscapeDataString("القاهرة")+"&Receipt=received&TypeId=2&From=2026-10-01&To=2026-10-31&Page=2&PageSize=20&Display=cards";
        (await client.GetAsync("/EgyptEbs"+query)).EnsureSuccessStatusCode();Assert.True(reader.IncludeLookups);
        (await client.GetAsync("/EgyptEbs/Signals"+query)).EnsureSuccessStatusCode();Assert.False(reader.IncludeLookups);
        var filter=reader.LastFilter;Assert.NotNull(filter);Assert.Equal("1158553",filter.Search);Assert.Equal("القاهرة",filter.Governorate);
        Assert.Equal("received",filter.Receipt);Assert.Equal(2,filter.TypeId);Assert.Equal(2,filter.Page);Assert.Equal(20,filter.PageSize);
        Assert.Equal("cards",filter.Display);
        Assert.Equal(new DateOnly(2026,10,1),filter.From);Assert.Equal(new DateOnly(2026,10,31),filter.To);Assert.Equal(0,reader.Changes);
    }
    [Fact] public async Task InitialDatesDefaultAndExplicitClearingSurvivesBothEndpoints()
    {
        var reader=new Reader(true);await using var host=new Host(reader);using var client=host.CreateClient();
        (await client.GetAsync("/EgyptEbs")).EnsureSuccessStatusCode();Assert.NotNull(reader.LastFilter!.From);Assert.Equal(reader.LastFilter.From.Value.AddDays(1),reader.LastFilter.To);
        (await client.GetAsync("/EgyptEbs?From=&To=&Display=cards")).EnsureSuccessStatusCode();Assert.Null(reader.LastFilter!.From);Assert.Null(reader.LastFilter.To);
        (await client.GetAsync("/EgyptEbs/Signals?From=&To=&Display=cards")).EnsureSuccessStatusCode();Assert.Null(reader.LastFilter!.From);Assert.Null(reader.LastFilter.To);
    }
    [Fact] public async Task CardsUseSameIntakeActionEncodeSourceTextAndRetainEmptyDateKeys()
    {
        var now=DateTime.UtcNow;
        var reader=new Reader(true){Signals=[new(91,81,"نوع اختبار","فئة","القاهرة","موقع اختبار","<img src=x onerror=alert(1)>",now,now,now,false,now)]};
        await using var host=new Host(reader);using var client=host.CreateClient();
        var html=await client.GetStringAsync("/EgyptEbs?From=&To=&Display=cards&Search=test");
        Assert.Contains("<article class=\"ebs-signal-card\"",html);Assert.DoesNotContain("<tbody>",html);
        Assert.Contains("&lt;img",html);Assert.DoesNotContain("<img src=x",html);
        Assert.Contains("action=\"/EgyptEbs/Import/91\"",html);Assert.Contains("__RequestVerificationToken",html);
        Assert.Contains("From=&amp;To=",html);Assert.Contains("Display=table",html);Assert.Contains("Display=cards",html);
    }
}
