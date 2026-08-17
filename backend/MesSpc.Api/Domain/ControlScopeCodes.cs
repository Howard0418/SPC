namespace MesSpc.Api.Domain;

public static class ControlScopeCodes
{
    public const string Product = "PRODUCT";
    public const string Process = "PROCESS";
    public const string Chemical = "CHEM";

    public static string Normalize(string? scope, string fallback = Product)
    {
        var value = string.IsNullOrWhiteSpace(scope)
            ? fallback
            : scope.Trim().ToUpperInvariant();
        return value switch
        {
            "PROD" => Product,
            "PROC" => Process,
            "CHEMICAL" => Chemical,
            _ => value
        };
    }
}
