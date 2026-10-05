namespace EtchTrialImport;

public static class EtchImportKey
{
    public static string Report(DateTime date, string lineCode) =>
        $"{date:yyyy-MM-dd}:{lineCode}";

    public static string SourceReference(DateTime date, string lineCode) =>
        $"ETCH:{Report(date, lineCode)}";
}
