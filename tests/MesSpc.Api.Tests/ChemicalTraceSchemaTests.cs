using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace MesSpc.Api.Tests;

public class ChemicalTraceSchemaTests
{
    [Fact]
    public async Task ReceiptAndTraceAreImmutableAndPoliciesDefaultDisabled()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var receipt = new ChemicalCalculationReceipt { Id = Guid.NewGuid(), PayloadJson = "{}" };
        db.Add(receipt); await db.SaveChangesAsync();
        new ChemicalFormulaPolicy().IsEnabled.Should().BeFalse();
        receipt.PayloadJson = "{\"tampered\":true}";
        Func<Task> save = () => db.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>();
        db.ChangeTracker.Clear();
        var trace = new ChemicalCalculationTrace { ReceiptId = receipt.Id, Phase = "Initial" };
        db.Add(trace); await db.SaveChangesAsync();
        db.Remove(trace); save = () => db.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void ModelHasScopedForeignKeysAndNullableLegacyMeasurementLinks()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options);
        var policy = db.Model.FindEntityType(typeof(ChemicalFormulaPolicy))!;
        policy.GetForeignKeys().Should().Contain(x => x.Properties.Count == 2);
        var effectivity = db.Model.FindEntityType(typeof(ChemicalFormulaEffectivity))!;
        effectivity.GetForeignKeys().Count(x => x.Properties.Count == 2).Should().Be(2);
        db.Model.FindEntityType(typeof(VariableMeasurement))!.FindProperty("ChemicalReceiptId")!.IsNullable.Should().BeTrue();
        var receipt = db.Model.FindEntityType(typeof(ChemicalCalculationReceipt))!;
        receipt.GetForeignKeys().Should().Contain(x => x.Properties.Count == 2);
        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public async Task SqliteChecksRangeAndCrossItemReferencesAndDoesNotBackfill()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var process = new Process { ProcessCode = "SCHEMA" };
        var characteristic = new QualityCharacteristic { CharacteristicCode = "SCHEMA" };
        db.AddRange(process, characteristic); await db.SaveChangesAsync();
        var a = new PartProcessCharacteristic { ProcessId = process.Id, CharacteristicId = characteristic.Id, ControlScope = "CHEM" };
        var b = new PartProcessCharacteristic { ProcessId = process.Id, CharacteristicId = characteristic.Id, ControlScope = "CHEM", Unit = "OTHER" };
        db.AddRange(a, b); await db.SaveChangesAsync();
        var formula = new ChemicalAnalysisFormulaVersion { PartProcessCharacteristicId = a.Id, VersionNo = 1, ConfigJson = "{}" };
        var revision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId = a.Id, RevisionNo = 1 };
        db.AddRange(formula, revision); await db.SaveChangesAsync();
        var bad = new ChemicalFormulaEffectivity { PartProcessCharacteristicId = b.Id, FormulaVersionId = formula.Id,
            ScheduleRevisionId = revision.Id, EffectiveFromUtc = new(2026, 10, 1) };
        db.Add(bad); Func<Task> save = () => db.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>(); db.Entry(bad).State = EntityState.Detached;
        bad = new() { PartProcessCharacteristicId = a.Id, FormulaVersionId = formula.Id, ScheduleRevisionId = revision.Id,
            EffectiveFromUtc = new(2026, 10, 2), EffectiveUntilUtcExclusive = new(2026, 10, 1) };
        db.Add(bad); await save.Should().ThrowAsync<DbUpdateException>(); db.Entry(bad).State = EntityState.Detached;
        (await db.ChemicalFormulaPolicies.CountAsync()).Should().Be(0);
        (await db.ChemicalCalculationTraces.CountAsync()).Should().Be(0);
    }
}
