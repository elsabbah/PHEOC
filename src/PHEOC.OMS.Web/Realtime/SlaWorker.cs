using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.Web.Realtime;
public class SlaWorker(IServiceScopeFactory scopes,ILogger<SlaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<SlaEngine>().Tick(stoppingToken);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogWarning("SLA iteration failed: {ErrorType}",ex.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromSeconds(30),stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
