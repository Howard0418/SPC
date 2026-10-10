using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public interface IMesSyncMessageHandler
{
    bool CanHandle(string messageType);
    Task HandleAsync(MesSyncMessage message, CancellationToken ct);
}

public sealed record MesSyncBatchResult(int Total, int Processed, int Failed);

public sealed class MesSyncMessageBatchProcessor(
    AppDbContext dbContext,
    IEnumerable<IMesSyncMessageHandler> handlers,
    ILogger<MesSyncMessageBatchProcessor> logger)
{
    public async Task<MesSyncBatchResult> ProcessPendingAsync(CancellationToken ct = default)
    {
        var pendingMessages = await dbContext.MesSyncMessages
            .Where(m => m.SyncStatus == "Pending")
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var processed = 0;
        var failed = 0;

        foreach (var message in pendingMessages)
        {
            logger.LogInformation("Processing MES Sync Message: {MessageId} of type {MessageType}", message.MessageId, message.MessageType);

            try
            {
                ValidatePayloadJson(message.PayloadJson);
                var handler = handlers.FirstOrDefault(h => h.CanHandle(message.MessageType));
                if (handler is null)
                {
                    throw new InvalidOperationException($"Unsupported MES sync message type '{message.MessageType}'. No handler is registered.");
                }

                await handler.HandleAsync(message, ct);
                message.SyncStatus = "Processed";
                message.ProcessedAt = DateTime.UtcNow;
                message.ErrorMessage = null;
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process MES sync message {MessageId}", message.MessageId);
                message.SyncStatus = "Failed";
                message.ProcessedAt = null;
                message.ErrorMessage = BuildErrorMessage(ex);
                failed++;
            }
        }

        if (pendingMessages.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }

        return new MesSyncBatchResult(pendingMessages.Count, processed, failed);
    }

    private static void ValidatePayloadJson(string payloadJson)
    {
        try
        {
            using var _ = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid MES sync payload JSON.", ex);
        }
    }

    private static string BuildErrorMessage(Exception ex)
    {
        var message = ex.Message;
        return message.Length <= 500 ? message : message[..500];
    }
}
