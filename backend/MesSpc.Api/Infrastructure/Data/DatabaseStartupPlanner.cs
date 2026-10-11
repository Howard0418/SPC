namespace MesSpc.Api.Infrastructure.Data;

public enum DatabaseStartupAction
{
    EnsureCreated,
    Migrate
}

public sealed record DatabaseStartupPlan(string Provider, DatabaseStartupAction Action, bool RequiresSqlServerBaseline)
{
    public bool IsSqlite => string.Equals(Provider, DatabaseStartupPlanner.SqliteProvider, StringComparison.Ordinal);
}

public static class DatabaseStartupPlanner
{
    public const string SqliteProvider = "sqlite";
    public const string SqlServerProvider = "sqlserver";

    public static DatabaseStartupPlan CreatePlan(string? provider)
    {
        var normalized = NormalizeProvider(provider);
        return normalized == SqliteProvider
            ? new DatabaseStartupPlan(normalized, DatabaseStartupAction.EnsureCreated, RequiresSqlServerBaseline: false)
            : new DatabaseStartupPlan(normalized, DatabaseStartupAction.Migrate, RequiresSqlServerBaseline: true);
    }

    public static string NormalizeProvider(string? provider)
    {
        var normalized = provider?.Trim().ToLowerInvariant();
        return normalized == SqliteProvider ? SqliteProvider : SqlServerProvider;
    }
}
