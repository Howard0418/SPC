namespace OctoberProductionChemicalCorrection;

public readonly record struct CorrectionNaturalKey(int PpcId, DateOnly Day, string SamplingPhase, string SamplingStage);
public sealed record CorrectionSource(string SourceId, int PpcId, DateOnly Day, string SamplingPhase, string SamplingStage,
    double Initial, double? Recheck, string FileName, string Sheet, int SourceRow)
{
    public CorrectionNaturalKey Key => new(PpcId, Day, SamplingPhase, SamplingStage);
}
public sealed record CorrectionCurrent(long MeasurementId, int PpcId, DateOnly Day, string SamplingPhase, string SamplingStage,
    double Initial, double? Recheck, string? ChemicalSourceKeyHash, string RowVersion)
{
    public CorrectionNaturalKey Key => new(PpcId, Day, SamplingPhase, SamplingStage);
}
public sealed record PlannedCorrection(string SourceId, long MeasurementId, string RowVersion,
    double BeforeInitial, double? BeforeRecheck, double AfterInitial, double? AfterRecheck,
    CorrectionNaturalKey Key, string FileName, string Sheet, int SourceRow);
public sealed record ChemicalCorrectionPlan(IReadOnlyList<PlannedCorrection> Corrections, IReadOnlyList<CorrectionCurrent> AlreadyCorrect);

public static class ChemicalCorrectionPlanner
{
    private const double Epsilon = 0.00000001;

    public static ChemicalCorrectionPlan Build(IReadOnlyCollection<CorrectionSource> sources, IReadOnlyCollection<CorrectionCurrent> current)
    {
        if (sources.Count == 0) throw new ArgumentException("Correction sources are required.");
        var duplicateSource = sources.GroupBy(x => x.Key).FirstOrDefault(x => x.Count() != 1);
        if (duplicateSource is not null) throw new InvalidOperationException("Duplicate source natural key: " + duplicateSource.Key);
        var currentByKey = current.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList());
        var corrections = new List<PlannedCorrection>();
        var already = new List<CorrectionCurrent>();
        foreach (var source in sources.OrderBy(x => x.Day).ThenBy(x => x.PpcId).ThenBy(x => x.SamplingPhase).ThenBy(x => x.SamplingStage))
        {
            if (!currentByKey.TryGetValue(source.Key, out var matches) || matches.Count != 1)
                throw new InvalidOperationException("Expected exactly one formal measurement for " + source.Key);
            var row = matches[0];
            if (!string.IsNullOrWhiteSpace(row.ChemicalSourceKeyHash))
                throw new InvalidOperationException("Existing formal measurement already owns a source key: " + row.MeasurementId);
            if (Same(row.Initial, source.Initial) && Same(row.Recheck, source.Recheck)) { already.Add(row); continue; }
            corrections.Add(new(source.SourceId, row.MeasurementId, row.RowVersion, row.Initial, row.Recheck,
                source.Initial, source.Recheck, source.Key, source.FileName, source.Sheet, source.SourceRow));
        }
        return new(corrections, already);
    }

    private static bool Same(double left, double right) => Math.Abs(left - right) <= Epsilon;
    private static bool Same(double? left, double? right) => left.HasValue == right.HasValue && (!left.HasValue || Same(left.Value, right!.Value));
}
