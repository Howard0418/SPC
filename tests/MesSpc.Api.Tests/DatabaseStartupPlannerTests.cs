using MesSpc.Api.Infrastructure.Data;

namespace MesSpc.Api.Tests;

public class DatabaseStartupPlannerTests
{
    [Theory]
    [InlineData("sqlite")]
    [InlineData("SQLite")]
    [InlineData(" sqlite ")]
    public void CreatePlan_ShouldUseEnsureCreated_ForSqlite(string provider)
    {
        var plan = DatabaseStartupPlanner.CreatePlan(provider);

        Assert.Equal(DatabaseStartupPlanner.SqliteProvider, plan.Provider);
        Assert.Equal(DatabaseStartupAction.EnsureCreated, plan.Action);
        Assert.False(plan.RequiresSqlServerBaseline);
        Assert.True(plan.IsSqlite);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sqlserver")]
    [InlineData("SqlServer")]
    [InlineData("unknown")]
    public void CreatePlan_ShouldDefaultToSqlServerMigration(string? provider)
    {
        var plan = DatabaseStartupPlanner.CreatePlan(provider);

        Assert.Equal(DatabaseStartupPlanner.SqlServerProvider, plan.Provider);
        Assert.Equal(DatabaseStartupAction.Migrate, plan.Action);
        Assert.True(plan.RequiresSqlServerBaseline);
        Assert.False(plan.IsSqlite);
    }
}
