using System.Data;
using System.Globalization;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MesSpc.Api.Services;

public sealed record ParticlePreviewRow(
    string MeasurementTime,
    string Location,
    string ParticleSize,
    string Count,
    string? SamplingVolume,
    string? SamplingDurationSeconds,
    string? DeviceCode,
    string? Remark,
    string SourceSheet,
    int SourceRow,
    string SourceColumn,
    string RawValue,
    string? SamplingVolumeUnit = null);

public sealed record ParticlePreviewRequest(
    Guid? ClientBatchId,
    string OriginalFileName,
    string FileHashSha256,
    string SourceType,
    IReadOnlyList<ParticlePreviewRow> Rows);

public sealed record ParticlePreviewResult(
    Guid UploadBatchId,
    string ImportStatus,
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    int DuplicateRows);

public sealed record ParticleConfirmResult(
    Guid UploadBatchId,
    int InsertedRows,
    int SkippedRows,
    int ErrorRows,
    bool AlreadyImported = false,
    string? ConflictCode = null);

internal sealed record ParticleStagedRow(
    DateTime MeasurementTime,
    string Location,
    decimal ParticleSize,
    long Count,
    decimal? SamplingVolume,
    string? SamplingVolumeUnit,
    decimal? SamplingDurationSeconds,
    string? DeviceCode,
    string? Remark,
    string SourceSheet,
    int SourceRow,
    string SourceColumn,
    string RawValue,
    bool IsDuplicate);

public sealed class ParticleUploadService(AppDbContext db, ILogger<ParticleUploadService> logger)
{
    private static readonly HashSet<string> Locations = Enumerable.Range(1, 9).Select(number => $"R{number}").ToHashSet();
    private static readonly HashSet<decimal> ParticleSizes = [0.5m, 1m, 5m, 10m];

    public async Task<bool> IsParticleBatchAsync(Guid uploadBatchId, CancellationToken ct = default) =>
        await db.UploadBatches.AsNoTracking().AnyAsync(batch => batch.UploadBatchId == uploadBatchId && batch.UploadType == "Particle", ct);

    public async Task<ParticlePreviewResult> CreatePreviewAsync(ParticlePreviewRequest request, string? createdBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileHashSha256))
            throw new ArgumentException("fileHashSha256 is required.", nameof(request));

        var priorBatches = await db.UploadBatches
            .Where(batch => batch.UploadType == "Particle" && batch.FileHash == request.FileHashSha256)
            .ToListAsync(ct);
        if (priorBatches.Any(batch => batch.ImportStatus == "Imported"))
            throw new InvalidOperationException("DUPLICATE_FILE");

        foreach (var stale in priorBatches.Where(batch => batch.ImportStatus == "PreviewReady"))
        {
            db.UploadErrors.RemoveRange(db.UploadErrors.Where(error => error.UploadBatchId == stale.UploadBatchId));
            db.UploadDetails.RemoveRange(db.UploadDetails.Where(detail => detail.UploadBatchId == stale.UploadBatchId));
            db.UploadBatches.Remove(stale);
        }
        if (priorBatches.Count > 0) await db.SaveChangesAsync(ct);

        var batch = new UploadBatch
        {
            UploadBatchId = request.ClientBatchId ?? Guid.NewGuid(),
            UploadType = "Particle",
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "TransFiles" : request.SourceType.Trim(),
            ImportStatus = "PreviewReady",
            OriginalFileName = request.OriginalFileName?.Trim(),
            FileHash = request.FileHashSha256.Trim(),
            CreatedBy = createdBy
        };
        db.UploadBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        var sourceCoordinates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateRows = 0;
        for (var index = 0; index < request.Rows.Count; index++)
        {
            var input = request.Rows[index];
            var detail = new UploadDetail { UploadBatchId = batch.UploadBatchId, RowNo = index + 1 };
            var errors = ValidateAndNormalize(input, sourceCoordinates, out var staged);

            if (staged is not null && errors.Count == 0)
            {
                var duplicate = await IsExistingMeasurementAsync(staged, ct);
                staged = staged with { IsDuplicate = duplicate };
                if (duplicate) duplicateRows++;
            }

            detail.IsValid = errors.Count == 0;
            detail.PayloadJson = JsonSerializer.Serialize(staged is not null ? (object)staged : input);
            db.UploadDetails.Add(detail);
            await db.SaveChangesAsync(ct);

            foreach (var error in errors)
            {
                db.UploadErrors.Add(new UploadError
                {
                    UploadBatchId = batch.UploadBatchId,
                    UploadDetailId = detail.Id,
                    RowNo = detail.RowNo,
                    FieldName = error.Field,
                    ErrorCode = error.Code,
                    ErrorMessage = error.Message
                });
            }
        }

        batch.TotalRows = request.Rows.Count;
        batch.ValidRows = await db.UploadDetails.CountAsync(detail => detail.UploadBatchId == batch.UploadBatchId && detail.IsValid, ct);
        batch.ErrorRows = batch.TotalRows - batch.ValidRows;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Particle preview {UploadBatchId}: {ValidRows} valid, {ErrorRows} errors, {DuplicateRows} duplicates.",
            batch.UploadBatchId, batch.ValidRows, batch.ErrorRows, duplicateRows);
        return new(batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows, duplicateRows);
    }

    public async Task<ParticleConfirmResult?> ConfirmAsync(Guid uploadBatchId, string duplicateMode, CancellationToken ct = default)
    {
        if (duplicateMode is not ("reject" or "skip")) throw new ArgumentException("duplicateMode must be reject or skip.");
        var batch = await db.UploadBatches.FirstOrDefaultAsync(item => item.UploadBatchId == uploadBatchId && item.UploadType == "Particle", ct);
        if (batch is null) return null;
        if (batch.ImportStatus == "Imported")
        {
            var existingCount = await db.ParticleMeasurements.CountAsync(item => item.UploadBatchId == uploadBatchId, ct);
            return new(uploadBatchId, existingCount, 0, 0, true);
        }
        if (batch.ErrorRows > 0) return new(uploadBatchId, 0, 0, batch.ErrorRows, ConflictCode: "VALIDATION_ERRORS");

        var details = await db.UploadDetails.AsNoTracking()
            .Where(detail => detail.UploadBatchId == uploadBatchId && detail.IsValid)
            .OrderBy(detail => detail.RowNo)
            .ToListAsync(ct);
        var rows = details.Select(detail => JsonSerializer.Deserialize<ParticleStagedRow>(detail.PayloadJson)!).ToList();
        var duplicateFlags = new List<bool>(rows.Count);
        foreach (var row in rows) duplicateFlags.Add(await IsExistingMeasurementAsync(row, ct));
        var duplicateCount = duplicateFlags.Count(flag => flag);
        if (duplicateMode == "reject" && duplicateCount > 0)
            return new(uploadBatchId, 0, duplicateCount, 0, ConflictCode: "DUPLICATE_MEASUREMENT");

        IDbContextTransaction? transaction = null;
        if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var measurements = rows.Where((_, index) => !duplicateFlags[index]).Select(row => new ParticleMeasurement
            {
                UploadBatchId = uploadBatchId,
                MeasurementTime = DateTime.SpecifyKind(row.MeasurementTime, DateTimeKind.Utc),
                Location = row.Location,
                ParticleSize = row.ParticleSize,
                Count = row.Count,
                SamplingVolume = row.SamplingVolume,
                SamplingVolumeUnit = row.SamplingVolumeUnit,
                SamplingDurationSeconds = row.SamplingDurationSeconds,
                DeviceCode = row.DeviceCode,
                Remark = row.Remark,
                SourceSheet = row.SourceSheet,
                SourceRow = row.SourceRow,
                SourceColumn = row.SourceColumn,
                RawValue = row.RawValue
            }).ToList();
            db.ParticleMeasurements.AddRange(measurements);
            batch.ImportStatus = "Imported";
            batch.ConfirmedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return new(uploadBatchId, measurements.Count, duplicateCount, 0);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private async Task<bool> IsExistingMeasurementAsync(ParticleStagedRow row, CancellationToken ct) =>
        await db.ParticleMeasurements.AsNoTracking().AnyAsync(item =>
            item.MeasurementTime == row.MeasurementTime && item.Location == row.Location &&
            item.ParticleSize == row.ParticleSize && item.DeviceCode == row.DeviceCode, ct);

    private static List<(string Field, string Code, string Message)> ValidateAndNormalize(
        ParticlePreviewRow input, HashSet<string> sourceCoordinates, out ParticleStagedRow? staged)
    {
        var errors = new List<(string, string, string)>();
        var location = input.Location?.Trim().ToUpperInvariant() ?? string.Empty;
        var sourceSheet = input.SourceSheet?.Trim() ?? string.Empty;
        var sourceColumn = input.SourceColumn?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!DateTimeOffset.TryParse(input.MeasurementTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var measuredAt))
            errors.Add(("measurementTime", "MEASUREMENT_TIME_INVALID", "Measurement time must be ISO 8601 with an offset."));
        if (!Locations.Contains(location)) errors.Add(("location", "LOCATION_UNSUPPORTED", "Location must be R1 through R9."));
        if (!decimal.TryParse(input.ParticleSize, NumberStyles.Number, CultureInfo.InvariantCulture, out var particleSize) || !ParticleSizes.Contains(particleSize))
            errors.Add(("particleSize", "PARTICLE_SIZE_UNSUPPORTED", "Particle size must be 0.5, 1, 5, or 10 micrometers."));
        if (!long.TryParse(input.Count, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            errors.Add(("count", "COUNT_INVALID", "Count must be an integer."));
        else if (count < 0) errors.Add(("count", "COUNT_NEGATIVE", "Count cannot be negative."));
        if (input.SourceRow <= 0 || sourceSheet.Length == 0 || sourceColumn.Length == 0)
            errors.Add(("source", "SOURCE_COORDINATE_INVALID", "Source sheet, row, and column are required."));
        else if (!sourceCoordinates.Add($"{sourceSheet}|{input.SourceRow}|{sourceColumn}"))
            errors.Add(("source", "DUPLICATE_SOURCE_CELL", "The source cell appears more than once in this batch."));

        decimal? samplingVolume = ParseOptionalDecimal(input.SamplingVolume, "samplingVolume", errors);
        decimal? duration = ParseOptionalDecimal(input.SamplingDurationSeconds, "samplingDurationSeconds", errors);
        staged = errors.Count == 0 ? new ParticleStagedRow(
            measuredAt.UtcDateTime, location, particleSize, count, samplingVolume, NormalizeOptional(input.SamplingVolumeUnit)?.ToUpperInvariant(), duration,
            NormalizeOptional(input.DeviceCode)?.ToUpperInvariant(), NormalizeOptional(input.Remark), sourceSheet,
            input.SourceRow, sourceColumn, input.RawValue ?? string.Empty, false) : null;
        return errors;
    }

    private static decimal? ParseOptionalDecimal(string? value, string field, List<(string, string, string)> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0) return parsed;
        errors.Add((field, "INVALID_POSITIVE_DECIMAL", $"{field} must be greater than zero."));
        return null;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
