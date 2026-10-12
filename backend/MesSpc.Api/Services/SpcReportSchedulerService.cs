namespace MesSpc.Api.Services;

public class SpcReportSchedulerService(IServiceProvider services, ILogger<SpcReportSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "SPC 報表排程執行失敗。"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<SpcReportScheduleProcessor>();
        await processor.ProcessAsync(DateTime.UtcNow, ct);
    }
}
