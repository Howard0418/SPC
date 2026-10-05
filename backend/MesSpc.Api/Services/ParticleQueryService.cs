using System.Globalization;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.SpcEngine.Calculators;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record ParticleMeasurementItem(long Id, Guid UploadBatchId, DateTime MeasurementTime, string Location,
    decimal ParticleSize, long Count, decimal? SamplingVolume, string? SamplingVolumeUnit, decimal? SamplingDurationSeconds, string? DeviceCode,
    string? Remark, string SourceSheet, int SourceRow, string SourceColumn);
public sealed record ParticlePage(IReadOnlyList<ParticleMeasurementItem> Items, int Total, int Page, int PageSize);
public sealed record ParticleTrendPoint(long MeasurementId, DateTime Time, long Count);
public sealed record ParticleTrendResult(string SeriesKey, string Unit, IReadOnlyList<ParticleTrendPoint> Points, int PointCount, object? Specification);
public sealed record ParticleComparisonCandidate(Guid UploadBatchId, string SourceSheet, int SourceRow, string? DeviceCode);
public sealed record ParticleComparisonItem(string Location, long? Count, long? MeasurementId, bool IsMissing);
public sealed record ParticleComparisonResult(bool IsAmbiguous, IReadOnlyList<ParticleComparisonCandidate> Candidates, IReadOnlyList<ParticleComparisonItem> Items);
public sealed record ParticleSpcResult(string SeriesKey, string ChartType, string SamplingBasis, string Warning, int PointCount,
    string ControlStatus, ParticleControlLimits? StatControlLimits, object? Specification, IReadOnlyList<ParticleChartPoint> Points,
    string? SamplingVolumeUnit = null);

public sealed class ParticleQueryService(AppDbContext db)
{
    public async Task<ParticlePage> GetMeasurementsAsync(DateTime from, DateTime to, IReadOnlyCollection<string> locations,
        IReadOnlyCollection<decimal> particleSizes, string? deviceCode, Guid? uploadBatchId, int page, int pageSize, bool ascending, CancellationToken ct = default)
    {
        var query = db.ParticleMeasurements.AsNoTracking().Where(item => item.MeasurementTime >= from && item.MeasurementTime < to);
        if (locations.Count > 0) query = query.Where(item => locations.Contains(item.Location));
        if (particleSizes.Count > 0) query = query.Where(item => particleSizes.Contains(item.ParticleSize));
        if (!string.IsNullOrWhiteSpace(deviceCode)) query = query.Where(item => item.DeviceCode == deviceCode);
        if (uploadBatchId.HasValue) query = query.Where(item => item.UploadBatchId == uploadBatchId.Value);
        var total = await query.CountAsync(ct);
        query = ascending ? query.OrderBy(item => item.MeasurementTime).ThenBy(item => item.Id) : query.OrderByDescending(item => item.MeasurementTime).ThenByDescending(item => item.Id);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(item => new ParticleMeasurementItem(
            item.Id, item.UploadBatchId, item.MeasurementTime, item.Location, item.ParticleSize, item.Count,
            item.SamplingVolume, item.SamplingVolumeUnit, item.SamplingDurationSeconds, item.DeviceCode, item.Remark, item.SourceSheet, item.SourceRow, item.SourceColumn)).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    public async Task<ParticleTrendResult> GetTrendAsync(DateTime from, DateTime to, string location, decimal particleSize, string? deviceCode, CancellationToken ct = default)
    {
        var query = db.ParticleMeasurements.AsNoTracking().Where(item => item.MeasurementTime >= from && item.MeasurementTime < to && item.Location == location && item.ParticleSize == particleSize);
        query = string.IsNullOrWhiteSpace(deviceCode) ? query.Where(item => item.DeviceCode == null) : query.Where(item => item.DeviceCode == deviceCode);
        var count = await query.CountAsync(ct);
        if (count > 10_000) throw new InvalidOperationException("TOO_MANY_POINTS");
        var points = await query.OrderBy(item => item.MeasurementTime).ThenBy(item => item.Id)
            .Select(item => new ParticleTrendPoint(item.Id, item.MeasurementTime, item.Count)).ToListAsync(ct);
        var key = $"{location}|{particleSize.ToString(CultureInfo.InvariantCulture)}|{(string.IsNullOrWhiteSpace(deviceCode) ? "DEFAULT" : deviceCode)}";
        return new(key, "count", points, count, null);
    }

    public async Task<ParticleComparisonResult> GetLocationComparisonAsync(DateTime measurementTime, decimal particleSize, string? deviceCode,
        Guid? uploadBatchId, string? sourceSheet, int? sourceRow, CancellationToken ct = default)
    {
        var query = db.ParticleMeasurements.AsNoTracking().Where(item => item.MeasurementTime == measurementTime && item.ParticleSize == particleSize);
        if (!string.IsNullOrWhiteSpace(deviceCode)) query = query.Where(item => item.DeviceCode == deviceCode);
        if (uploadBatchId.HasValue) query = query.Where(item => item.UploadBatchId == uploadBatchId.Value);
        if (!string.IsNullOrWhiteSpace(sourceSheet)) query = query.Where(item => item.SourceSheet == sourceSheet);
        if (sourceRow.HasValue) query = query.Where(item => item.SourceRow == sourceRow.Value);
        var rows = await query.OrderBy(item => item.Location).ThenBy(item => item.Id).ToListAsync(ct);
        var groups = rows.GroupBy(item => new { item.UploadBatchId, item.SourceSheet, item.SourceRow, item.DeviceCode }).ToList();
        if (groups.Count > 1)
        {
            var candidates = groups.Select(group => new ParticleComparisonCandidate(group.Key.UploadBatchId, group.Key.SourceSheet, group.Key.SourceRow, group.Key.DeviceCode)).ToList();
            return new(true, candidates, []);
        }
        var selected = groups.SingleOrDefault()?.ToDictionary(item => item.Location, item => item);
        var items = Enumerable.Range(1, 9).Select(number =>
        {
            var location = $"R{number}";
            var found = selected?.GetValueOrDefault(location);
            return new ParticleComparisonItem(location, found?.Count, found?.Id, found is null);
        }).ToList();
        return new(false, [], items);
    }

    public async Task<ParticleSpcResult> GetSpcAsync(DateTime from, DateTime to, string location, decimal particleSize, string? deviceCode,
        string chartType = "C", CancellationToken ct = default)
    {
        var query = db.ParticleMeasurements.AsNoTracking().Where(item => item.MeasurementTime >= from && item.MeasurementTime < to &&
            item.Location == location && item.ParticleSize == particleSize);
        query = string.IsNullOrWhiteSpace(deviceCode) ? query.Where(item => item.DeviceCode == null) : query.Where(item => item.DeviceCode == deviceCode);
        var count = await query.CountAsync(ct);
        if (count > 10_000) throw new InvalidOperationException("TOO_MANY_POINTS");
        var rows = await query.OrderBy(item => item.MeasurementTime).ThenBy(item => item.Id).ToListAsync(ct);
        var normalizedChartType = chartType.Trim().ToUpperInvariant();
        ParticleChartResult chart = normalizedChartType switch
        {
            "C" or "C_CHART" => ParticleCChartCalculator.Calculate(rows.Select(item => new ParticleCChartInput(item.Id, item.MeasurementTime, item.Count)).ToList()),
            "U" or "U_CHART" => ParticleUChartCalculator.Calculate(rows.Select(item => new ParticleUChartInput(
                item.Id, item.MeasurementTime, item.Count, item.SamplingVolume, item.SamplingVolumeUnit)).ToList()),
            _ => throw new ArgumentException("CHART_TYPE_UNSUPPORTED", nameof(chartType))
        };
        var key = $"{location}|{particleSize.ToString(CultureInfo.InvariantCulture)}|{(string.IsNullOrWhiteSpace(deviceCode) ? "DEFAULT" : deviceCode)}";
        var samplingBasis = chart.ChartType == "U_CHART" ? "sampling-volume" : "constant-assumed";
        var warning = chart.ChartType == "U_CHART" ? "U-chart 已依 SamplingVolume 正規化。" : "目前採固定採樣基準，尚未依採樣體積正規化。";
        return new(key, chart.ChartType, samplingBasis, warning, count, chart.ControlStatus, chart.StatControlLimits, null, chart.Points, chart.SamplingVolumeUnit);
    }
}
