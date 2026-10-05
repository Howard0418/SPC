namespace MesSpc.Api.Services;

/// <summary>Keep sampling stage separate from the legacy SamplingPhase shift codes.</summary>
public static class ChemicalSamplingStage
{
    public const string Error = "只有 N1／N2 可選開線或收線取樣階段。";
    public static bool TryNormalize(string? machineCode,string? value,out string stage)
    {
        stage=string.IsNullOrWhiteSpace(value)?"GENERAL":value.Trim().ToUpperInvariant();
        if(stage=="GENERAL")return true; // Preserve unclassified historical/legacy daily records.
        return (machineCode?.Trim().ToUpperInvariant() is "N1" or "N2") && (stage is "OPEN" or "CLOSE");
    }
}
