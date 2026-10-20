using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

/// <summary>Reads current segment settings and audit history without inferring historical applicability.</summary>
public sealed class ChartSegmentDetailsService(AppDbContext db)
{
    public const int PageSize = 20;

    /// <summary>Returns only the specified mapping's segment; modification dates never select a formula.</summary>
    public async Task<ChartSegmentDetails?> GetAsync(int ppcId, int? segmentId, int formulaSkip,
        int specificationSkip, CancellationToken ct)
    {
        if (formulaSkip < 0 || specificationSkip < 0)
            throw new ArgumentException("無效的分頁範圍。");
        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ppcId && x.IsEnabled, ct);
        if (mapping is null) return null;
        ControlLimitSegment? segment = null;
        if (segmentId.HasValue)
        {
            segment = await db.ControlLimitSegments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == segmentId && x.PartProcessCharacteristicId == ppcId, ct);
            if (segment is null) return null;
        }

        var chemical = ControlScopeCodes.Normalize(mapping.ControlScope) == ControlScopeCodes.Chemical;
        var formulaRows = chemical
            ? await db.ChemicalAnalysisFormulaVersions.AsNoTracking()
                .Where(x => x.PartProcessCharacteristicId == ppcId)
                .OrderByDescending(x => x.VersionNo).ThenByDescending(x => x.Id)
                .Skip(formulaSkip).Take(PageSize + 1)
                .Select(x => new SegmentFormulaHistory(x.Id, x.VersionNo, x.ConfigJson, x.Reason,
                    x.ChangedBy, x.ChangedAt, x.ChangeType)).ToListAsync(ct)
            : [];
        var specRows = await db.SpecificationLimitVersions.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == ppcId &&
                (segmentId.HasValue ? x.SourceKind == "Segment" && x.SourceSegmentId == segmentId : x.SourceKind == "Base"))
            .OrderByDescending(x => x.VersionNo).ThenByDescending(x => x.Id)
            .Skip(specificationSkip).Take(PageSize + 1)
            .Select(x => new SegmentSpecificationHistory(x.Id, x.VersionNo, x.SourceSegmentId,
                x.SnapshotJson, x.ChangedBy, x.ChangedAt, x.ChangeType)).ToListAsync(ct);

        SegmentLimitValue Limit(double? value, double? fallback) => new(value ?? fallback,
            segment is not null && value.HasValue ? "Segment" : "Base");
        return new(ppcId, segmentId, segment?.StartDate, segment?.EndDate, segment?.Note,
            new Dictionary<string, SegmentLimitValue>
            {
                ["USL"] = Limit(segment?.USL, mapping.USL), ["LSL"] = Limit(segment?.LSL, mapping.LSL),
                ["Target"] = Limit(segment?.TargetValue, mapping.TargetValue),
                ["UCL"] = Limit(segment?.UCL, mapping.UCL), ["CL"] = Limit(segment?.CL, mapping.CL),
                ["LCL"] = Limit(segment?.LCL, mapping.LCL)
            }, chemical ? mapping.ChemicalAnalysisConfigJson : null, mapping.FormulaConfigJson,
            chemical ? "Unknown" : "NotApplicable", chemical ? "Unknown" : "NotApplicable", "NotRecorded",
            formulaRows.Take(PageSize).Select(x => x with { ChangedAt = AsUtc(x.ChangedAt) }).ToList(),
            formulaRows.Count > PageSize,
            specRows.Take(PageSize).Select(x => x with { ChangedAt = AsUtc(x.ChangedAt) }).ToList(),
            specRows.Count > PageSize);
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

public sealed record SegmentLimitValue(double? Value, string Source);
public sealed record SegmentFormulaHistory(long Id, int VersionNo, string ConfigJson, string? Reason,
    string? ChangedBy, DateTime ChangedAt, string ChangeType);
public sealed record SegmentSpecificationHistory(long Id, int VersionNo, int? SourceSegmentId,
    string? SnapshotJson, string ChangedBy, DateTime ChangedAt, string ChangeType);
public sealed record ChartSegmentDetails(int PartProcessCharacteristicId, int? SegmentId,
    DateTime? StartDate, DateTime? EndDate, string? Note, Dictionary<string, SegmentLimitValue> Limits,
    string? CurrentFormulaConfigJson, string? CurrentStatisticalConfigJson,
    string FormulaVersionStatus, string FTableVersionStatus, string ControlLimitVersionStatus,
    List<SegmentFormulaHistory> FormulaHistory, bool HasMoreFormula,
    List<SegmentSpecificationHistory> SpecificationHistory, bool HasMoreSpecification);
