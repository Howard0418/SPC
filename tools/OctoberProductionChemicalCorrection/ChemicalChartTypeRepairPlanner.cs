namespace OctoberProductionChemicalCorrection;

public sealed record ChemicalChartTypeRepairCandidate(
    int PpcId,
    string? CurrentChartTypeCode,
    IReadOnlyCollection<string> EvidenceChartTypeCodes,
    int FailedJobCount);

public sealed record ChemicalChartTypeRepairPlan(
    IReadOnlyList<int> ToUpdate,
    IReadOnlyList<int> AlreadyCorrect,
    int FailedJobCount);

public static class ChemicalChartTypeRepairPlanner
{
    private const string RequiredChartType = "I_MR";

    public static ChemicalChartTypeRepairPlan Build(IReadOnlyCollection<ChemicalChartTypeRepairCandidate> candidates)
    {
        if (candidates.Count == 0)
            throw new InvalidOperationException("No chart type repair candidates were supplied.");

        if (candidates.GroupBy(x => x.PpcId).Any(x => x.Count() != 1))
            throw new InvalidOperationException("Duplicate PPC repair candidate detected.");

        var updates = new List<int>();
        var alreadyCorrect = new List<int>();
        var failedJobs = 0;

        foreach (var candidate in candidates.OrderBy(x => x.PpcId))
        {
            if (candidate.FailedJobCount <= 0)
                throw new InvalidOperationException($"PPC {candidate.PpcId} has no failed jobs.");

            var evidence = candidate.EvidenceChartTypeCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (evidence.Length != 1 || evidence[0] != RequiredChartType)
                throw new InvalidOperationException($"PPC {candidate.PpcId} does not have unique I_MR evidence.");

            var current = candidate.CurrentChartTypeCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(current)) updates.Add(candidate.PpcId);
            else if (current == RequiredChartType) alreadyCorrect.Add(candidate.PpcId);
            else throw new InvalidOperationException($"PPC {candidate.PpcId} already has a different chart type and cannot be overwritten.");

            checked { failedJobs += candidate.FailedJobCount; }
        }

        return new(updates, alreadyCorrect, failedJobs);
    }
}
