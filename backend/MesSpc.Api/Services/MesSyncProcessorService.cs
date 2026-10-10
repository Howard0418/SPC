namespace MesSpc.Api.Services;

public class MesSyncProcessorService(IServiceProvider serviceProvider, ILogger<MesSyncProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<MesSyncMessageBatchProcessor>();
                var result = await processor.ProcessPendingAsync(stoppingToken);

                if (result.Total > 0)
                    logger.LogInformation("MES Sync batch completed. Total: {Total}, Processed: {Processed}, Failed: {Failed}", result.Total, result.Processed, result.Failed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in MesSyncProcessorService loop");
            }
            
            // Wait before checking again (e.g., 30 seconds)
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
