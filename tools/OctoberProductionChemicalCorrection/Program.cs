namespace OctoberProductionChemicalCorrection;

internal static class Program
{
    public static Task<int> Main(string[] args) => ChemicalCorrectionRunner.RunAsync(args);
}
