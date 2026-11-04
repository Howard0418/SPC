using ClosedXML.Excel;
using System.Text.Json;
using FluentAssertions;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MesSpc.Api.Tests;

public sealed class ChemicalGenericImportEvidenceTests
{
    [Fact]
    public void GenericVariableTemplateDoesNotAdvertiseChemicalImport()
    {
        var controller = new UploadsController(null!, null!);

        var result = Assert.IsType<FileContentResult>(controller.GetVariableTemplate());
        using var workbook = new XLWorkbook(new MemoryStream(result.FileContents));
        var sheet = workbook.Worksheet("計量型資料匯入範本");

        sheet.Range("A2:A4").Cells().Select(x => x.GetString()).Should().OnlyContain(x => x == "PROCESS");
        sheet.Cell("A6").GetString().Should().Contain("Portal").And.Contain("TransFiles");
    }

    [Theory]
    [InlineData("Api")]
    [InlineData("File")]
    [InlineData("TransFiles")]
    [InlineData("PortalDaily")]
    public async Task GenericPreviewIsolatesChemicalRowsWithActionableSourceLocation(string sourceType)
    {
        await using var fixture = await Fixture.CreateAsync();
        var row = fixture.ChemicalRow();

        var batch = await fixture.Service.CreateVariableBatchAsync([row], sourceType, "generic-user", "history.xlsx");

        batch.ValidRows.Should().Be(0);
        batch.ErrorRows.Should().Be(1);
        (await fixture.Db.VariableMeasurements.CountAsync()).Should().Be(0);
        var preview = JsonSerializer.SerializeToElement(await fixture.Service.GetPreviewAsync(batch.UploadBatchId));
        var error = preview.GetProperty("errors").EnumerateArray().Single(x =>
            x.GetProperty("ErrorCode").GetString() == "CHEMICAL_EVIDENCE_REQUIRED");
        error.GetProperty("FieldName").GetString().Should().Be("ChemicalEvidence");
        error.GetProperty("SourceFile").GetString().Should().Be("history.xlsx");
        error.GetProperty("SourceSheet").GetString().Should().Be("C5");
        error.GetProperty("SourceRow").GetInt32().Should().Be(27);
        error.GetProperty("Suggestion").GetString().Should().Contain("Portal").And.Contain("TransFiles");
    }

    [Fact]
    public async Task ConfirmRejectsPreDeploymentValidChemicalBatchBeforeAnyWrite()
    {
        await using var fixture = await Fixture.CreateAsync();
        var batch = new UploadBatch
        {
            UploadBatchId = Guid.NewGuid(), UploadType = "Variable", SourceType = "File",
            ImportStatus = "PreviewReady", TotalRows = 1, ValidRows = 1
        };
        fixture.Db.AddRange(batch, new UploadDetail
        {
            UploadBatchId = batch.UploadBatchId, RowNo = 1, IsValid = true,
            PayloadJson = JsonSerializer.Serialize(fixture.ChemicalRow())
        });
        await fixture.Db.SaveChangesAsync();

        Func<Task> confirm = async () => await fixture.Service.ConfirmAsync(batch.UploadBatchId);
        await confirm.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*藥液*Portal*專用*TransFiles*");

        fixture.Db.ChangeTracker.Clear();
        (await fixture.Db.VariableMeasurements.CountAsync()).Should().Be(0);
        (await fixture.Db.UploadBatches.SingleAsync()).ImportStatus.Should().Be("PreviewReady");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public AppDbContext Db { get; }
        public UploadService Service { get; }

        private Fixture(SqliteConnection connection, AppDbContext db)
        {
            this.connection = connection;
            Db = db;
            Service = new UploadService(db, new SpcService(db,
                new ChemicalPortalMeasurementTests.FakeEmail(), new ConfigurationBuilder().Build()));
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            db.AddRange(
                new Process { Id = 1, ProcessCode = "C5", ProcessName = "C5", ControlScope = "CHEM" },
                new Machine { Id = 1, MachineCode = "C5", MachineName = "C5", ProcessId = 1 },
                new ProductionLine { Id = 1, LineCode = "C5", LineName = "C5" },
                new Tank { Id = 1, LineId = 1, TankCode = "T1", TankName = "清潔" },
                new QualityCharacteristic
                {
                    Id = 1, CharacteristicCode = "H2SO4", CharacteristicName = "硫酸",
                    ControlScope = "CHEM", DataCategory = "Variable", InputMode = "FORMULA"
                },
                new ControlChartGroup
                {
                    Id = 1, GroupCode = "CHEMICAL", GroupName = "Chemical",
                    BusinessScopeCode = "CHEM", GroupType = "CONTROL_CHART",
                    RequiresMachine = true, RequiresTank = true, IsEnabled = true
                },
                new PartProcessCharacteristic
                {
                    Id = 1, ProcessId = 1, MachineId = 1, TankId = 1, CharacteristicId = 1,
                    ControlScope = "CHEM", Unit = "ml/L", LSL = 80, USL = 120, IsEnabled = true
                });
            await db.SaveChangesAsync();
            return new Fixture(connection, db);
        }

        public Dictionary<string, string?> ChemicalRow() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["__SourceSheet"] = "C5", ["__SourceRow"] = "27",
            ["ControlScope"] = "CHEM", ["ProcessCode"] = "C5", ["MachineCode"] = "C5",
            ["TankCode"] = "T1", ["CharacteristicCode"] = "H2SO4", ["Unit"] = "ml/L",
            ["MeasuredValue"] = "86.13", ["MeasuredAt"] = "2026-10-30T12:00:00+08:00",
            ["SamplingPhase"] = "OPEN", ["SamplingStage"] = "GENERAL", ["SampleNo"] = "1",
            ["Operator"] = "generic-user"
        };

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
