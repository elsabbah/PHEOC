using PHEOC.OMS.Infrastructure.Services;
namespace PHEOC.OMS.Web.Realtime;
public class EgyptEbsWorker(IServiceScopeFactory scopes,ILogger<EgyptEbsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<EgyptEbsSync>().Tick(stoppingToken);}
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogWarning("EBS sync iteration failed: {ErrorType}",ex.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
