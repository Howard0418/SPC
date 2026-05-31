using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var pendingMessages = await dbContext.MesSyncMessages
                    .Where(m => m.SyncStatus == "Pending")
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in pendingMessages)
                {
                    logger.LogInformation("Processing MES Sync Message: {MessageId} of type {MessageType}", message.MessageId, message.MessageType);
                    
                    try 
                    {
                        // TODO: Phase 2/3 - Implement parsing logic based on MessageType
                        // Example: 
                        // if (message.MessageType == "WorkOrder") { var wo = JsonSerializer.Deserialize<WorkOrderDto>(message.PayloadJson); ... }

                        message.SyncStatus = "Processed";
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process message {MessageId}", message.MessageId);
                        message.SyncStatus = "Failed";
                        message.ErrorMessage = ex.Message;
                    }
                }
                
                if (pendingMessages.Any())
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
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
