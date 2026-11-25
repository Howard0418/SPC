using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MesSpc.Api.Tests;

public sealed class MeasurementImportOperatorIdentityTests
{
    [Theory]
    [InlineData(false, "作業員測試文字", "作業員測試文字")]
    [InlineData(true, "未登記作業員", "未登記作業員")]
    [InlineData(false, "E-100", "E-100")]
    [InlineData(true, "  ", null)]
    public async Task ConfirmAndRetryKeepMeasurementLabelWithoutChangingOperators(
        bool isAttribute, string sourceOperator, string? expectedOperator)
    {
        await using var connection = await OpenDatabaseAsync();
        await using var db = CreateContext(connection);
        await SeedAsync(db, isAttribute);
        var service = CreateService(db);

        var batch = await PreviewAsync(service, isAttribute, sourceOperator);
        Assert.Equal(1, batch.ValidRows);
        await service.ConfirmAsync(batch.UploadBatchId);
        await service.ConfirmAsync(batch.UploadBatchId);
        db.ChangeTracker.Clear();

        Assert.Equal("Imported", (await db.UploadBatches.SingleAsync()).ImportStatus);
        var savedOperator = isAttribute
            ? (await db.AttributeMeasurements.SingleAsync()).Operator
            : (await db.VariableMeasurements.SingleAsync()).Operator;
        Assert.Equal(expectedOperator, savedOperator);
        AssertRegisteredOperator(Assert.Single(await db.Operators.IgnoreQueryFilters().ToListAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfirmAcceptsLabelThatMatchesAHiddenOperatorCode(bool isAttribute)
    {
        const string label = "作業員測試文字";
        await using var connection = await OpenDatabaseAsync();
        await using var db = CreateContext(connection);
        await SeedAsync(db, isAttribute);
        db.Operators.Add(new Operator
        {
            Id = 2, OperatorCode = label, OperatorName = label,
            IsDeleted = true, IsActive = false, Role = UserRoles.Editor
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var batch = await PreviewAsync(service, isAttribute, label);
        Assert.Equal(1, batch.ValidRows);
        await service.ConfirmAsync(batch.UploadBatchId);
        db.ChangeTracker.Clear();

        Assert.Equal("Imported", (await db.UploadBatches.SingleAsync()).ImportStatus);
        var savedOperator = isAttribute
            ? (await db.AttributeMeasurements.SingleAsync()).Operator
            : (await db.VariableMeasurements.SingleAsync()).Operator;
        Assert.Equal(label, savedOperator);
        AssertRegisteredOperator(await db.Operators.SingleAsync());
        var all = await db.Operators.IgnoreQueryFilters().OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(label, all[1].OperatorCode);
        Assert.True(all[1].IsDeleted);
        Assert.False(all[1].IsActive);
        Assert.Null(all[1].Username);
    }

    private static async Task<UploadBatch> PreviewAsync(UploadService service, bool isAttribute, string sourceOperator)
    {
        var row = new Dictionary<string, string?>
        {
            ["ControlScope"] = "PROCESS", ["ProcessCode"] = "P1", ["MachineCode"] = "M1",
            ["CharacteristicCode"] = "QC1", ["MeasuredAt"] = "2026-11-25T02:00:00Z",
            ["SampleNo"] = "1", ["LotNo"] = "identity-regression-lot", ["Operator"] = sourceOperator
        };
        if (isAttribute)
        {
            row["InspectedQty"] = "10";
            row["DefectQty"] = "1";
            return await service.CreateAttributeBatchAsync([row], "File", "test-editor", "attribute.xlsx");
        }

        row["MeasuredValue"] = "10";
        return await service.CreateVariableBatchAsync([row], "Api", "test-editor", null);
    }

    private static void AssertRegisteredOperator(Operator op)
    {
        Assert.Equal(1, op.Id);
        Assert.Equal("E-100", op.OperatorCode);
        Assert.Equal("registered-ad", op.Username);
        Assert.Equal(UserRoles.Viewer, op.Role);
        Assert.Equal("[\"analysis.spc\"]", op.PagePermissionsJson);
        Assert.True(op.IsActive);
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await connection.OpenAsync();
        return connection;
    }

    private static AppDbContext CreateContext(SqliteConnection connection) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    private static UploadService CreateService(AppDbContext db) => new(db,
        new SpcService(db, new ChemicalPortalMeasurementTests.FakeEmail(), new ConfigurationBuilder().Build()));

    private static async Task SeedAsync(AppDbContext db, bool isAttribute)
    {
        await db.Database.EnsureCreatedAsync();
        db.AddRange(
            new Process { Id = 1, ProcessCode = "P1", ProcessName = "製程測試" },
            new Machine { Id = 1, ProcessId = 1, MachineCode = "M1", MachineName = "線別測試" },
            new ControlChartGroup
            {
                Id = 1, GroupCode = "PROCESS", GroupName = "製程", GroupType = "CONTROL_CHART",
                BusinessScopeCode = "PROCESS", RequiresMachine = true, IsEnabled = true
            },
            new QualityCharacteristic
            {
                Id = 1, CharacteristicCode = "QC1", CharacteristicName = "量測測試", ControlScope = "PROCESS",
                DataCategory = isAttribute ? "Attribute" : "Variable"
            },
            new PartProcessCharacteristic
            {
                Id = 1, ProcessId = 1, MachineId = 1, CharacteristicId = 1, ControlScope = "PROCESS", IsEnabled = true
            },
            new Operator
            {
                Id = 1, OperatorCode = "E-100", OperatorName = "作業員測試文字", Username = "registered-ad",
                IsActive = true, Role = UserRoles.Viewer, PagePermissionsJson = "[\"analysis.spc\"]"
            });
        await db.SaveChangesAsync();
    }
}
