namespace OctoberProductionChemicalCorrection;

internal static class Program
{
    public static Task<int> Main(string[] args) =>
        args.Length > 0 && args[0] == "chart-type-repair"
            ? ChemicalChartTypeRepairRunner.RunAsync(args[1..])
            : ChemicalCorrectionRunner.RunAsync(args);
}
