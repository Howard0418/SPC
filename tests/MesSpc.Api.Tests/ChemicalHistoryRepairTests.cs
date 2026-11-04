using System.Text.Json;
using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MesSpc.Api.Tests;

public sealed class ChemicalHistoryRepairTests
{
    [Fact]
    public async Task Unique_existing_trace_can_be_relinked_and_rerun_is_no_change()
    {
        await using var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        await AddLimits(f);
        await f.Service.ConfirmAsync(new(Guid.NewGuid(), [await f.Row("1.25", null)]), "editor", default);
        var row = await f.Db.VariableMeasurements.SingleAsync();
        var expectedReceipt = row.ChemicalReceiptId!.Value;
        row.ChemicalReceiptId = null;
        row.ChemicalSourceKeyHash = null;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();

        var service = new ChemicalHistoryRepairService(f.Db);
        var unlinked = await f.Db.VariableMeasurements.AsNoTracking().SingleAsync();
        unlinked.ChemicalReceiptId.Should().BeNull();
        var presentation = (await new ChemicalHistoryQueryService(f.Db).GetAsync([unlinked]))[unlinked.Id];
        presentation.JudgmentSource.Should().Be("Unavailable");
        var dryRun = await service.DryRunAsync(default);
        dryRun.TotalUnknown.Should().Be(1);
        dryRun.Blocked.Should().BeEmpty(string.Join(";", dryRun.Blocked.Select(x => x.ReasonCode)));
        dryRun.Manifest.Items.Should().ContainSingle();

        var applied = await service.ApplyAsync(dryRun.Manifest, "repairer", default);
        applied.Updated.Should().Be(1);
        applied.NoChange.Should().Be(0);
        f.Db.ChangeTracker.Clear();
        var repaired = await f.Db.VariableMeasurements.AsNoTracking().SingleAsync();
        repaired.ChemicalReceiptId.Should().Be(expectedReceipt);
        (await new ChemicalHistoryQueryService(f.Db).GetAsync([repaired]))[repaired.Id].JudgmentSource.Should().Be("SavedTrace");

        var rerun = await service.ApplyAsync(dryRun.Manifest, "repairer", default);
        rerun.Updated.Should().Be(0);
        rerun.NoChange.Should().Be(1);
        (await RepairAuditCount(f.Db)).Should().Be(1);

        var changed = await f.Db.VariableMeasurements.SingleAsync();
        changed.MeasuredValue += 1;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var staleRerun = () => service.ApplyAsync(dryRun.Manifest, "repairer", default);
        await staleRerun.Should().ThrowAsync<InvalidOperationException>().WithMessage("*manifest*");
        (await RepairAuditCount(f.Db)).Should().Be(1);
    }

    [Fact]
    public async Task Missing_trace_is_blocked_even_when_current_master_has_limits()
    {
        await using var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        var ppc = await f.Db.PartProcessCharacteristics.SingleAsync();
        ppc.LSL = 0;
        ppc.USL = 100;
        f.Db.VariableMeasurements.Add(new VariableMeasurement
        {
            Id = 991, PartProcessCharacteristicId = 1, ProcessId = 1, MachineId = 1,
            CharacteristicId = 1, TankId = 1, MeasuredAt = new DateTime(2026, 10, 30), MeasuredValue = 12
        });
        await f.Db.SaveChangesAsync();

        var result = await new ChemicalHistoryRepairService(f.Db).DryRunAsync(default);
        result.Manifest.Items.Should().BeEmpty();
        result.Blocked.Should().ContainSingle(x => x.MeasurementId == 991 && x.ReasonCode == "NO_TRACE_EVIDENCE");
    }

    [Fact]
    public async Task Duplicate_phase_trace_is_blocked()
    {
        await using var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        await AddLimits(f);
        await f.Service.ConfirmAsync(new(Guid.NewGuid(), [await f.Row("1.25", null)]), "editor", default);
        var row = await f.Db.VariableMeasurements.SingleAsync();
        row.ChemicalReceiptId = null;
        row.ChemicalSourceKeyHash = null;
        var trace = await f.Db.ChemicalCalculationTraces.SingleAsync();
        var receipt = await f.Db.ChemicalCalculationReceipts.SingleAsync();
        var duplicateReceipt = new ChemicalCalculationReceipt
        {
            Id = Guid.NewGuid(), PartProcessCharacteristicId = receipt.PartProcessCharacteristicId,
            EffectivityId = receipt.EffectivityId, MeasuredAtUtc = receipt.MeasuredAtUtc,
            CreatedAtUtc = receipt.CreatedAtUtc, ExpiresAtUtc = receipt.ExpiresAtUtc, Actor = receipt.Actor,
            InputHash = receipt.InputHash, SourceKeyHash = receipt.SourceKeyHash, EngineVersion = receipt.EngineVersion,
            PrecisionPolicy = receipt.PrecisionPolicy, PayloadJson = receipt.PayloadJson
        };
        f.Db.ChemicalCalculationReceipts.Add(duplicateReceipt);
        f.Db.ChemicalCalculationTraces.Add(new ChemicalCalculationTrace
        {
            VariableMeasurementId = trace.VariableMeasurementId, ReceiptId = duplicateReceipt.Id, Phase = trace.Phase,
            VersionNo = 2, RawResultCanonical = trace.RawResultCanonical, StoredResultCanonical = trace.StoredResultCanonical
        });
        await f.Db.SaveChangesAsync();

        var result = await new ChemicalHistoryRepairService(f.Db).DryRunAsync(default);
        result.Manifest.Items.Should().BeEmpty();
        result.Blocked.Should().ContainSingle(x => x.ReasonCode == "TRACE_NOT_UNIQUE");
    }

    [Fact]
    public async Task Stale_manifest_is_rejected_before_any_write()
    {
        await using var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        await AddLimits(f);
        await f.Service.ConfirmAsync(new(Guid.NewGuid(), [await f.Row("1.25", null)]), "editor", default);
        var row = await f.Db.VariableMeasurements.SingleAsync();
        row.ChemicalReceiptId = null;
        row.ChemicalSourceKeyHash = null;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var service = new ChemicalHistoryRepairService(f.Db);
        var manifest = (await service.DryRunAsync(default)).Manifest;
        row = await f.Db.VariableMeasurements.SingleAsync();
        row.MeasuredValue += 1;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();

        var action = () => service.ApplyAsync(manifest, "repairer", default);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*manifest*");
        (await f.Db.VariableMeasurements.AsNoTracking().SingleAsync()).ChemicalReceiptId.Should().BeNull();
        (await RepairAuditCount(f.Db)).Should().Be(0);
    }

    [Fact]
    public async Task Late_database_failure_rolls_back_whole_manifest()
    {
        await using var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        await AddLimits(f);
        await f.Service.ConfirmAsync(new(Guid.NewGuid(), [await f.Row("1.25", null)]), "editor", default);
        var first = await f.Db.VariableMeasurements.SingleAsync();
        first.ChemicalReceiptId = null;
        first.ChemicalSourceKeyHash = null;
        await f.Db.SaveChangesAsync();
        var firstId = first.Id;

        await f.Service.ConfirmAsync(new(Guid.NewGuid(), [await RowAt(f, "1.75", "2026-10-21T12:00:00+08:00")]), "editor", default);
        var second = await f.Db.VariableMeasurements.OrderBy(x => x.Id).LastAsync();
        second.ChemicalReceiptId = null;
        second.ChemicalSourceKeyHash = null;
        await f.Db.SaveChangesAsync();
        var secondId = second.Id;
        f.Db.ChangeTracker.Clear();
        var service = new ChemicalHistoryRepairService(f.Db);
        var manifest = (await service.DryRunAsync(default)).Manifest;
        manifest.Items.Should().HaveCount(2);
        var triggerSql = "CREATE TRIGGER reject_history_repair BEFORE UPDATE OF ChemicalReceiptId ON VariableMeasurements WHEN NEW.Id="
            + secondId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " BEGIN SELECT RAISE(ABORT, 'forced repair rollback'); END;";
        await f.Db.Database.ExecuteSqlRawAsync(triggerSql);

        var action = () => service.ApplyAsync(manifest, "repairer", default);
        await action.Should().ThrowAsync<DbUpdateException>();
        f.Db.ChangeTracker.Clear();
        var rows = await f.Db.VariableMeasurements.AsNoTracking().Where(x => x.Id == firstId || x.Id == secondId).ToListAsync();
        rows.Should().OnlyContain(x => x.ChemicalReceiptId == null && x.ChemicalSourceKeyHash == null);
        (await RepairAuditCount(f.Db)).Should().Be(0);
    }

    private static async Task<int> RepairAuditCount(MesSpc.Api.Infrastructure.Data.AppDbContext db)
    {
        var payloads = await db.UploadDetails.AsNoTracking().Where(x => !x.IsValid).Select(x => x.PayloadJson).ToListAsync();
        return payloads.Count(x =>
        {
            try
            {
                using var json = JsonDocument.Parse(x);
                return json.RootElement.TryGetProperty("action", out var action)
                    && action.GetString() == ChemicalHistoryRepairService.AuditAction;
            }
            catch (JsonException) { return false; }
        });
    }

    private static async Task AddLimits(ChemicalPortalMeasurementTests.Fixture f)
    {
        var ppc = await f.Db.PartProcessCharacteristics.SingleAsync();
        ppc.LSL = 0;
        ppc.USL = 100;
        await f.Db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, string?>> RowAt(ChemicalPortalMeasurementTests.Fixture f, string primary, string measuredAt)
    {
        var input = new ChemicalPortalPreviewRequest(1, measuredAt, primary, null, "OPEN", "GENERAL");
        var preview = await f.Service.PreviewAsync(input, "editor", default);
        return new()
        {
            ["PpcId"] = "1", ["ControlScope"] = "CHEM", ["ProcessCode"] = "C1", ["MachineCode"] = "C1",
            ["TankCode"] = "T1", ["CharacteristicCode"] = "CONC", ["MeasuredAt"] = measuredAt,
            ["TitrationValue"] = primary, ["MeasuredValue"] = "0", ["SampleNo"] = "1", ["SamplingPhase"] = "OPEN",
            ["SamplingStage"] = "GENERAL", ["Operator"] = "editor", ["ChemicalPreviewToken"] = preview.Token
        };
    }
}
