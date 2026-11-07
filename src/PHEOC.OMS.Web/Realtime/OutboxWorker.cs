using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PHEOC.OMS.Infrastructure.Data;
namespace PHEOC.OMS.Web.Realtime;
public class OutboxWorker(IServiceScopeFactory scopes,IHubContext<OperationsHub> hub,IHttpClientFactory clients,IConfiguration config,ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<OmsDbContext>();var now=DateTime.UtcNow;
                var messages=await db.OutboxMessages.AsNoTracking().Include(x=>x.Event).Where(x=>x.Status=="Pending"&&x.NextAttemptAt<=now).OrderBy(x=>x.Id).Take(20).ToListAsync(stoppingToken);
                foreach(var m in messages)
                {
                    var lease=now.AddMinutes(2);var claimed=await db.OutboxMessages.Where(x=>x.Id==m.Id&&x.NextAttemptAt<=now&&x.Status=="Pending").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NextAttemptAt,lease),stoppingToken);if(claimed==0)continue;
                    await hub.Clients.Groups("central","governorate:"+m.Event.GovernorateId).SendAsync("Changed",new{m.EventId},stoppingToken);
                    await EventArrivalBroadcast.Publish(hub,m.Event,m.Payload,stoppingToken);
                    var endpoint=config["Integration:Ebs:CallbackUrl"];
                    if(string.IsNullOrWhiteSpace(endpoint)){await db.OutboxMessages.Where(x=>x.Id==m.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NextAttemptAt,now.AddMinutes(1)),stoppingToken);continue;}
                    if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri)||uri.Scheme!="https")throw new InvalidOperationException("EBS callback requires HTTPS.");
                    using var request=new HttpRequestMessage(HttpMethod.Post,uri);request.Content=new StringContent(m.Payload,System.Text.Encoding.UTF8,"application/json");request.Headers.TryAddWithoutValidation("Idempotency-Key",m.MessageId.ToString());
                    var key=config["Integration:Ebs:CallbackKey"];if(!string.IsNullOrWhiteSpace(key))request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
                    try
                    {
                        using var response=await clients.CreateClient("EbsCallback").SendAsync(request,stoppingToken);var success=response.IsSuccessStatusCode;var attempts=m.Attempts+1;var state=success?"Sent":attempts>=5?"Failed":"Pending";
                        await db.OutboxMessages.Where(x=>x.Id==m.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,state).SetProperty(x=>x.Attempts,attempts).SetProperty(x=>x.SentAt,success?(DateTime?)now:null).SetProperty(x=>x.NextAttemptAt,now.AddMinutes(Math.Pow(2,attempts))),stoppingToken);
                    }
                    catch(Exception ex)when(ex is HttpRequestException||(ex is TaskCanceledException&&!stoppingToken.IsCancellationRequested)){var attempts=m.Attempts+1;await db.OutboxMessages.Where(x=>x.Id==m.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Attempts,attempts).SetProperty(x=>x.Status,attempts>=5?"Failed":"Pending").SetProperty(x=>x.NextAttemptAt,now.AddMinutes(Math.Pow(2,attempts))),stoppingToken);}
                }
            }
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogWarning("Outbox iteration failed: {ErrorType}",ex.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromSeconds(10),stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
