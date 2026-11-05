namespace ChemicalFormulaMigration;

/// <summary>Validates the approved production target and rejects config drift before writes.</summary>
public static class UnsetProductionGuard
{
    public static void ValidateTarget(string? environment, string server, string database, bool apply, bool productionReviewed)
    {
        var allowed = server.Equals("172.16.110.16", StringComparison.OrdinalIgnoreCase)
            && ((environment == "test" && database == "PMR_SPC_TEST" && !productionReviewed)
                || (environment == "production" && database == "PMR_SPC_2026" && (!apply || productionReviewed)));
        if (!allowed) throw new InvalidOperationException("Environment, database, server or production review flag is invalid.");
    }

    public static void ValidateRows(IReadOnlyCollection<UnsetFormulaChange> review, IReadOnlyCollection<UnsetFormulaRow> current)
    {
        foreach (var approved in review)
        {
            var matches = current.Where(x => x.PpcId == approved.PpcId).ToList();
            if (matches.Count != 1 || (matches[0].ConfigJson != approved.BeforeJson && matches[0].ConfigJson != approved.AfterJson))
                throw new InvalidOperationException($"Reviewed PPC {approved.PpcId} is missing or has drifted.");
        }
    }
}
