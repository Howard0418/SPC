using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Auditing;

public sealed record AuditRetentionWindow(int OnlineDays, int TotalDays);

public static class AuditRetentionPolicy
{
    public static AuditRetentionWindow For(string category) => category is "Navigation" or "Read"
        ? new(180, 365)
        : new(730, 1825);
}

public sealed record AuditRetentionPreview(long OnlineCount, long ArchiveCount, long ActiveLegalHolds,
    long DueForArchive, long PastTotalRetention, DateTime? EarliestOnlineUtc, DateTime? LatestOnlineUtc);

public sealed record AuditRetentionResult(Guid BatchId, int Archived, string Checksum);
public sealed record AuditLegalHoldRequest(DateTime DateFromUtc, DateTime DateToUtc, string ReasonCode,
    string? EventCode = null, string? PageKey = null, string? TargetType = null, string? TargetId = null);

public sealed class AuditRetentionException(string code, Exception? inner = null) : Exception(code, inner)
{
    public string Code { get; } = code;
}

public sealed record AuditArchiveEnvelope(long OriginalEventId, Guid EventId, DateTime OccurredAtUtc, DateTime ReceivedAtUtc,
    string Environment, string Category, string EventCode, string Outcome, string? ReasonCode, string ActorType,
    string ActorAssurance, int? ActorOperatorId, string? ActorAccountSnapshot, string? ActorOperatorCodeSnapshot,
    string? ActorDisplayNameSnapshot, string? ActorRoleSnapshot, string? ActorServiceCode, string? AttemptedAccountHash,
    string? SessionIdHash, string? CorrelationId, string? TraceId, string? PageKey, string? RouteTemplate,
    string? TargetType, string? TargetId, string? HttpMethod, int? HttpStatus, string Source, string DetailsJson,
    string DeduplicationHash);

public sealed class AuditArchiveCryptography(IConfiguration configuration)
{
    internal bool CorruptAfterEncryptForTest { get; init; }

    public string KeyId => configuration["Audit:Retention:ArchiveEncryptionKeyId"]?.Trim()
        ?? throw new AuditRetentionException("AUDIT_ARCHIVE_KEY_ID_MISSING");

    public (byte[] Nonce, byte[] Ciphertext, byte[] Tag, string Hash) Encrypt(byte[] payload)
    {
        var hash = Convert.ToHexString(SHA256.HashData(payload));
        var nonce = RandomNumberGenerator.GetBytes(12); var cipher = new byte[payload.Length]; var tag = new byte[16];
        using var aes = new AesGcm(Key(), tag.Length); aes.Encrypt(nonce, payload, cipher, tag);
        if (CorruptAfterEncryptForTest && cipher.Length > 0) cipher[0] ^= 0x01;
        return (nonce, cipher, tag, hash);
    }

    public byte[] Decrypt(AuditEventArchive archive)
    {
        var result = new byte[archive.Ciphertext.Length];
        using var aes = new AesGcm(Key(), archive.Tag.Length);
        aes.Decrypt(archive.Nonce, archive.Ciphertext, archive.Tag, result);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(archive.PayloadHash), SHA256.HashData(result)))
            throw new AuditRetentionException("AUDIT_ARCHIVE_HASH_MISMATCH");
        return result;
    }

    private byte[] Key()
    {
        var text = configuration[$"Audit:Retention:ArchiveEncryptionKeys:{KeyId}"];
        byte[] key;
        try { key = Convert.FromBase64String(text ?? ""); }
        catch (FormatException ex) { throw new AuditRetentionException("AUDIT_ARCHIVE_KEY_INVALID", ex); }
        if (key.Length != 32) throw new AuditRetentionException("AUDIT_ARCHIVE_KEY_INVALID");
        return key;
    }
}

public sealed class AuditRetentionService(AppDbContext db, DbContextOptions<AppDbContext> options,
    IConfiguration configuration, TimeProvider clock, AuditArchiveCryptography crypto,
    ILogger<AuditRetentionService> logger)
{
    private const int MaximumBatchSize = 1000;

    public async Task<AuditRetentionPreview> PreviewAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var due = DueQuery(now);
        return new(
            await db.AuditEvents.LongCountAsync(ct), await db.AuditEventArchives.LongCountAsync(ct),
            await db.AuditLegalHolds.LongCountAsync(x => x.ReleasedAtUtc == null, ct), await due.LongCountAsync(ct),
            await PastTotalRetentionQuery(now).LongCountAsync(ct),
            await db.AuditEvents.MinAsync(x => (DateTime?)x.OccurredAtUtc, ct),
            await db.AuditEvents.MaxAsync(x => (DateTime?)x.OccurredAtUtc, ct));
    }

    public async Task<AuditRetentionResult> ExecuteAsync(CancellationToken ct = default)
    {
        EnsureExecutionAllowed();
        var now = clock.GetUtcNow().UtcDateTime; var batchId = Guid.NewGuid();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var rows = await DueQuery(now).OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
                .Take(MaximumBatchSize).ToListAsync(ct);
            if (rows.Count == 0) { await tx.RollbackAsync(ct); return new(batchId, 0, ""); }
            var batch = new AuditArchiveBatch
            {
                BatchId = batchId, Environment = EnvironmentName(), StartedAtUtc = now,
                RangeFromUtc = rows.Min(x => x.OccurredAtUtc), RangeToUtc = rows.Max(x => x.OccurredAtUtc),
                RecordCount = rows.Count, EncryptionKeyId = crypto.KeyId, Status = "Preparing"
            };
            db.AuditArchiveBatches.Add(batch); await db.SaveChangesAsync(ct);
            var archives = rows.Select(x => Archive(x, batch.Id)).ToArray(); db.AuditEventArchives.AddRange(archives);
            await db.SaveChangesAsync(ct);
            foreach (var archive in archives)
            {
                var clear = crypto.Decrypt(archive);
                var envelope = JsonSerializer.Deserialize<AuditArchiveEnvelope>(clear)
                    ?? throw new AuditRetentionException("AUDIT_ARCHIVE_RESTORE_FAILED");
                if (envelope.OriginalEventId != archive.OriginalEventId || envelope.EventId != archive.EventId)
                    throw new AuditRetentionException("AUDIT_ARCHIVE_RESTORE_FAILED");
            }
            var checksumBytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", archives.OrderBy(x => x.OriginalEventId).Select(x => x.PayloadHash))));
            batch.Checksum = Convert.ToHexString(checksumBytes); batch.CompletedAtUtc = now; batch.Status = "Verified";
            var ids = rows.Select(x => x.Id).ToArray();
            var deleted = await db.AuditEvents.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(ct);
            if (deleted != rows.Count) throw new AuditRetentionException("AUDIT_ARCHIVE_DELETE_COUNT_MISMATCH");
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            logger.LogInformation("AUDIT_RETENTION_ARCHIVE_VERIFIED batch {BatchId} count {Count}", batchId, rows.Count);
            return new(batchId, rows.Count, batch.Checksum);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
            await RecordFailedBatchAsync(batchId, now, ex, ct);
            logger.LogError(ex, "AUDIT_RETENTION_ARCHIVE_FAILED batch {BatchId}", batchId);
            throw ex is AuditRetentionException ? ex : new AuditRetentionException("AUDIT_ARCHIVE_VERIFICATION_FAILED", ex);
        }
    }

    public async Task<AuditLegalHold> CreateLegalHoldAsync(AuditLegalHoldRequest request, int operatorId, CancellationToken ct = default)
    {
        EnsureExecutionAllowed(); ValidateHold(request);
        var hold = new AuditLegalHold
        {
            HoldId = Guid.NewGuid(), Environment = EnvironmentName(), DateFromUtc = Utc(request.DateFromUtc),
            DateToUtc = Utc(request.DateToUtc), EventCode = Clean(request.EventCode), PageKey = Clean(request.PageKey),
            TargetType = Clean(request.TargetType), TargetId = Clean(request.TargetId), ReasonCode = request.ReasonCode.Trim(),
            CreatedByOperatorId = operatorId, CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };
        db.AuditLegalHolds.Add(hold); await db.SaveChangesAsync(ct); return hold;
    }

    public async Task<bool> ReleaseLegalHoldAsync(Guid holdId, int operatorId, CancellationToken ct = default)
    {
        EnsureExecutionAllowed();
        var hold = await db.AuditLegalHolds.SingleOrDefaultAsync(x => x.HoldId == holdId && x.ReleasedAtUtc == null, ct);
        if (hold is null) return false;
        hold.ReleasedByOperatorId = operatorId; hold.ReleasedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct); return true;
    }

    private IQueryable<AuditEvent> DueQuery(DateTime now)
    {
        var generalCutoff = now.AddDays(-180); var criticalCutoff = now.AddDays(-730); var environment = EnvironmentName();
        return db.AuditEvents.Where(x => x.Environment == environment
            && ((x.Category == "Navigation" || x.Category == "Read") && x.OccurredAtUtc < generalCutoff
                || x.Category != "Navigation" && x.Category != "Read" && x.OccurredAtUtc < criticalCutoff)
            && !db.AuditEventArchives.Any(a => a.OriginalEventId == x.Id)
            && !db.AuditLegalHolds.Any(h => h.Environment == x.Environment && h.ReleasedAtUtc == null
                && h.DateFromUtc <= x.OccurredAtUtc && h.DateToUtc >= x.OccurredAtUtc
                && (h.EventCode == null || h.EventCode == x.EventCode) && (h.PageKey == null || h.PageKey == x.PageKey)
                && (h.TargetType == null || h.TargetType == x.TargetType) && (h.TargetId == null || h.TargetId == x.TargetId)));
    }

    private IQueryable<AuditEventArchive> PastTotalRetentionQuery(DateTime now)
    {
        var generalCutoff = now.AddDays(-365); var criticalCutoff = now.AddDays(-1825); var environment = EnvironmentName();
        return db.AuditEventArchives.Where(x => x.Environment == environment
            && ((x.Category == "Navigation" || x.Category == "Read") && x.OccurredAtUtc < generalCutoff
                || x.Category != "Navigation" && x.Category != "Read" && x.OccurredAtUtc < criticalCutoff)
            && !db.AuditLegalHolds.Any(h => h.Environment == x.Environment && h.ReleasedAtUtc == null
                && h.DateFromUtc <= x.OccurredAtUtc && h.DateToUtc >= x.OccurredAtUtc
                && (h.EventCode == null || h.EventCode == x.EventCode) && (h.PageKey == null || h.PageKey == x.PageKey)
                && (h.TargetType == null || h.TargetType == x.TargetType) && (h.TargetId == null || h.TargetId == x.TargetId)));
    }

    private AuditEventArchive Archive(AuditEvent row, long batchId)
    {
        var envelope = new AuditArchiveEnvelope(row.Id, row.EventId, row.OccurredAtUtc, row.ReceivedAtUtc, row.Environment,
            row.Category, row.EventCode, row.Outcome, row.ReasonCode, row.ActorType, row.ActorAssurance, row.ActorOperatorId,
            row.ActorAccountSnapshot, row.ActorOperatorCodeSnapshot, row.ActorDisplayNameSnapshot, row.ActorRoleSnapshot,
            row.ActorServiceCode, row.AttemptedAccountHash, row.SessionIdHash, row.CorrelationId, row.TraceId, row.PageKey,
            row.RouteTemplate, row.TargetType, row.TargetId, row.HttpMethod, row.HttpStatus, row.Source, row.DetailsJson, row.DeduplicationHash);
        var encrypted = crypto.Encrypt(JsonSerializer.SerializeToUtf8Bytes(envelope));
        return new AuditEventArchive
        {
            OriginalEventId = row.Id, EventId = row.EventId, ArchiveBatchId = batchId, OccurredAtUtc = row.OccurredAtUtc,
            Environment = row.Environment, Category = row.Category, EventCode = row.EventCode, PageKey = row.PageKey,
            TargetType = row.TargetType, TargetId = row.TargetId, Nonce = encrypted.Nonce, Ciphertext = encrypted.Ciphertext,
            Tag = encrypted.Tag, PayloadHash = encrypted.Hash
        };
    }

    private void EnsureExecutionAllowed()
    {
        if (!configuration.GetValue<bool>("Audit:Retention:Enabled")) throw new AuditRetentionException("AUDIT_RETENTION_DISABLED");
        if (EnvironmentName() != "Test") throw new AuditRetentionException("AUDIT_RETENTION_TEST_ONLY");
        if (!configuration.GetValue<bool>("Audit:Retention:AllowCurrentConnectionForMaintenance"))
            throw new AuditRetentionException("AUDIT_MAINTENANCE_IDENTITY_REQUIRED");
        _ = options;
    }

    private string EnvironmentName() => configuration["AppEnvironment"] == "Production" ? "Production" : "Test";

    private static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidateHold(AuditLegalHoldRequest request)
    {
        var from = Utc(request.DateFromUtc); var to = Utc(request.DateToUtc);
        if (from >= to || to - from > TimeSpan.FromDays(1825)) throw new AuditRetentionException("AUDIT_HOLD_RANGE_INVALID");
        if (string.IsNullOrWhiteSpace(request.ReasonCode) || request.ReasonCode.Length > 100
            || request.ReasonCode.Any(x => !(char.IsLetterOrDigit(x) || x is '-' or '_' or '.')))
            throw new AuditRetentionException("AUDIT_HOLD_REASON_INVALID");
        foreach (var value in new[] { request.EventCode, request.PageKey, request.TargetType })
            if (value?.Length > 100) throw new AuditRetentionException("AUDIT_HOLD_FILTER_INVALID");
        if (request.TargetId?.Length > 200) throw new AuditRetentionException("AUDIT_HOLD_FILTER_INVALID");
    }

    private async Task RecordFailedBatchAsync(Guid batchId, DateTime now, Exception exception, CancellationToken ct)
    {
        try
        {
            db.AuditArchiveBatches.Add(new AuditArchiveBatch
            {
                BatchId = batchId, Environment = EnvironmentName(), StartedAtUtc = now, CompletedAtUtc = now,
                RangeFromUtc = now, RangeToUtc = now, RecordCount = 0, EncryptionKeyId = crypto.KeyId,
                Checksum = "", Status = exception is AuditRetentionException e ? $"Failed:{e.Code}"[..Math.Min(32, 7 + e.Code.Length)] : "Failed"
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception failure) { logger.LogError(failure, "AUDIT_RETENTION_FAILURE_MANIFEST_UNAVAILABLE batch {BatchId}", batchId); }
    }
}
