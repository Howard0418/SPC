using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class MesSyncMessageBatchProcessorTests
{
    [Fact]
    public async Task ProcessPendingAsync_ShouldFailUnsupportedMessageType()
    {
        await using var db = CreateDbContext();
        db.MesSyncMessages.Add(new MesSyncMessage
        {
            MessageType = "WorkOrder",
            PayloadJson = """{"workOrderNo":"WO-TEST-001"}""",
            SyncStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var processor = new MesSyncMessageBatchProcessor(db, [], NullLogger<MesSyncMessageBatchProcessor>.Instance);

        var result = await processor.ProcessPendingAsync();

        var message = await db.MesSyncMessages.SingleAsync();
        Assert.Equal(1, result.Failed);
        Assert.Equal("Failed", message.SyncStatus);
        Assert.Contains("Unsupported MES sync message type", message.ErrorMessage);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingAsync_ShouldFailInvalidJson()
    {
        await using var db = CreateDbContext();
        db.MesSyncMessages.Add(new MesSyncMessage
        {
            MessageType = "WorkOrder",
            PayloadJson = "{not-json",
            SyncStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var processor = new MesSyncMessageBatchProcessor(db, [new TestMesSyncHandler("WorkOrder")], NullLogger<MesSyncMessageBatchProcessor>.Instance);

        var result = await processor.ProcessPendingAsync();

        var message = await db.MesSyncMessages.SingleAsync();
        Assert.Equal(1, result.Failed);
        Assert.Equal("Failed", message.SyncStatus);
        Assert.Contains("Invalid MES sync payload JSON", message.ErrorMessage);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingAsync_ShouldProcessSupportedValidMessage()
    {
        await using var db = CreateDbContext();
        var handler = new TestMesSyncHandler("WorkOrder");
        db.MesSyncMessages.Add(new MesSyncMessage
        {
            MessageType = "WorkOrder",
            PayloadJson = """{"workOrderNo":"WO-TEST-001"}""",
            SyncStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var processor = new MesSyncMessageBatchProcessor(db, [handler], NullLogger<MesSyncMessageBatchProcessor>.Instance);

        var result = await processor.ProcessPendingAsync();

        var message = await db.MesSyncMessages.SingleAsync();
        Assert.Equal(1, result.Processed);
        Assert.Equal("Processed", message.SyncStatus);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.ErrorMessage);
        Assert.Single(handler.HandledMessageIds);
        Assert.Equal(message.MessageId, handler.HandledMessageIds[0]);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private sealed class TestMesSyncHandler(string messageType) : IMesSyncMessageHandler
    {
        public List<Guid> HandledMessageIds { get; } = [];

        public bool CanHandle(string candidateMessageType)
        {
            return string.Equals(candidateMessageType, messageType, StringComparison.OrdinalIgnoreCase);
        }

        public Task HandleAsync(MesSyncMessage message, CancellationToken ct)
        {
            HandledMessageIds.Add(message.MessageId);
            return Task.CompletedTask;
        }
    }
}
