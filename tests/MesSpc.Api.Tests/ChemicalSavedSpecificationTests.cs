using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ChemicalSavedSpecificationTests
{
    private static async Task<ChemicalPortalMeasurementTests.Fixture> DirectAsync(bool recordOnly = false, bool hasSpec = true)
    {
        var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        f.Db.ChemicalFormulaPolicies.RemoveRange(await f.Db.ChemicalFormulaPolicies.ToListAsync());
        (await f.Db.QualityCharacteristics.SingleAsync()).InputMode = recordOnly ? "RECORD_ONLY" : "DIRECT";
        var ppc = await f.Db.PartProcessCharacteristics.SingleAsync();
        ppc.LSL = hasSpec ? 0 : null; ppc.USL = hasSpec ? 10 : null;
        await f.Db.SaveChangesAsync(); return f;
    }

    private static Dictionary<string, string?> DirectRow(string value, string? recheck = null) => new()
    {
        ["PpcId"]="1",["ControlScope"]="CHEM",["ProcessCode"]="C1",["MachineCode"]="C1",["TankCode"]="T1",["CharacteristicCode"]="CONC",
        ["MeasuredAt"]="2026-10-20T12:00:00+08:00",["MeasuredValue"]=value,["RecheckValue"]=recheck,["SampleNo"]="1",["SamplingPhase"]="OPEN",["SamplingStage"]="GENERAL",["Operator"]="editor"
    };

    [Theory]
    [InlineData(false, 0, "OK", "DIRECT")]
    [InlineData(false, 10, "OK", "DIRECT")]
    [InlineData(false, 11, "NG", "DIRECT")]
    [InlineData(true, 11, "NG", "DIRECT")]
    [InlineData(false, 11, "NG", "FORMULA")]
    public async Task NewDirectSaveRetainsSpecJudgmentIndependentlyOfChart(bool chart, double value, string expected, string inputMode)
    {
        await using var f = await ChemicalPortalMeasurementTests.Fixture.Create();
        var input = await f.Row("1", null);
        f.Db.ChemicalFormulaPolicies.RemoveRange(await f.Db.ChemicalFormulaPolicies.ToListAsync());
        var ppc = await f.Db.PartProcessCharacteristics.SingleAsync();
        ppc.LSL = 0; ppc.USL = 10;
        (await f.Db.QualityCharacteristics.SingleAsync()).InputMode = inputMode;
        if (chart)
        {
            f.Db.Add(new ControlChartType { Id = 1, ChartTypeCode = "I_MR" });
            ppc.ChartTypeId = 1;
        }
        await f.Db.SaveChangesAsync();
        input["MeasuredValue"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (inputMode == "FORMULA") input["TitrationValue"] = "5.5"; // Existing formula is Primary*2.
        input.Remove("ChemicalPreviewToken");
        await f.Service.ConfirmAsync(new(Guid.NewGuid(), [input]), "editor", default);
        // Later master edits cannot replace the specification at save time.
        ppc.USL = 100; await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        var rows = await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        var result = (await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value;
        result.Judgment.Should().Be(expected);
        result.InitialJudgment.Should().Be(expected);
        if (!chart) (await f.Db.SpcCalculationResults.CountAsync()).Should().Be(0);
        f.Db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task RecheckZeroAndDailyUpdateUseOnlyTheLatestBatchSnapshotAndRetryIsIdempotent()
    {
        await using var f=await DirectAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[DirectRow("11")]),"editor",default);
        var oldAudit=await f.Db.UploadDetails.AsNoTracking().Where(x=>x.PayloadJson.Contains("PortalChemicalSpecificationSavedV1")).Select(x=>x.PayloadJson).SingleAsync();
        var id=Guid.NewGuid(); var request=new ChemicalPortalConfirmRequest(id,[DirectRow("11","0")]);
        await f.Service.ConfirmAsync(request,"editor",default);await f.Service.ConfirmAsync(request,"editor",default);
        var rows=await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        rows.Should().ContainSingle();
        var result=(await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value;
        result.Judgment.Should().Be("OK");result.InitialJudgment.Should().Be("NG");result.EffectiveValue.Should().Be(0);
        (await f.Db.UploadDetails.CountAsync(x=>x.UploadBatchId==id && x.PayloadJson.Contains("PortalChemicalSpecificationSavedV1"))).Should().Be(1);
        (await f.Db.UploadDetails.CountAsync(x=>x.PayloadJson==oldAudit)).Should().Be(1);
    }

    [Theory]
    [InlineData(true,true,"僅紀錄","SAVED_SPEC_RECORD_ONLY","NotApplicable")]
    [InlineData(false,false,"未設定規格","SAVED_SPEC_LIMITS_MISSING","SourceEvidenceRequired")]
    public async Task RecordOnlyAndMissingSpecificationNeverBecomeOk(bool recordOnly,bool spec,string reason,string reasonCode,string eligibility)
    {
        await using var f=await DirectAsync(recordOnly,spec);
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[DirectRow("5")]),"editor",default);
        var result=(await new ChemicalHistoryQueryService(f.Db).GetAsync(await f.Db.VariableMeasurements.AsNoTracking().ToListAsync())).Single().Value;
        result.Judgment.Should().Be("Unknown");result.PrecisionNote.Should().Contain(reason);
        result.JudgmentReasonCode.Should().Be(reasonCode);result.RepairEligibility.Should().Be(eligibility);
        result.RepairSuggestion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SavesApplicableSegmentAndRejectsStaleValuesInsteadOfUsingCurrentMaster()
    {
        await using var f=await DirectAsync();
        f.Db.ControlLimitSegments.Add(new ControlLimitSegment{PartProcessCharacteristicId=1,StartDate=new DateTime(2026,10,1),LSL=5,USL=8});
        await f.Db.SaveChangesAsync();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[DirectRow("9")]),"editor",default);
        var row=await f.Db.VariableMeasurements.AsNoTracking().SingleAsync();var query=new ChemicalHistoryQueryService(f.Db);
        (await query.GetAsync([row]))[row.Id].Judgment.Should().Be("NG");
        row.MeasuredValue=7;
        var mismatch=(await query.GetAsync([row]))[row.Id];
        mismatch.Judgment.Should().Be("Unknown");mismatch.PrecisionNote.Should().Contain("不一致");
        mismatch.JudgmentReasonCode.Should().Be("SAVED_SPEC_VALUE_MISMATCH");
        mismatch.RepairEligibility.Should().Be("EvidenceConflict");
    }

    [Fact]
    public async Task ClientSuppliedJudgmentCannotReplaceServerSpecification()
    {
        await using var f=await DirectAsync();var input=DirectRow("11");
        input["Judgment"]="OK";input["action"]="PortalChemicalSpecificationSavedV1";input["details"]="[{\"Usl\":999}]";
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[input]),"editor",default);
        var rows=await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        (await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value.Judgment.Should().Be("NG");
        // A second conflicting server record is not silently chosen as the authoritative one.
        await new ChemicalSavedSpecificationService(f.Db).CaptureAsync(rows.Single().UploadBatchId,"editor",default);
        (await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value.Judgment.Should().Be("Unknown");
    }

    [Theory]
    [InlineData("[\"PortalChemicalSpecificationSavedV1\"]")]
    [InlineData("{\"action\":\"PortalChemicalSpecificationSavedV1\",\"details\":\"forged\"}")]
    [InlineData("{\"action\":\"PortalChemicalSpecificationSavedV1\",\"details\":broken}")]
    public async Task MalformedOrNonObjectAuditCannotCrashHistoryOrBecomeEvidence(string json)
    {
        await using var f=await DirectAsync();var batch=Guid.NewGuid();
        f.Db.AddRange(new VariableMeasurement{Id=900,UploadBatchId=batch,PartProcessCharacteristicId=1,MeasuredValue=5},
            new UploadDetail{UploadBatchId=batch,IsValid=false,PayloadJson=json});
        await f.Db.SaveChangesAsync();
        var rows=await f.Db.VariableMeasurements.AsNoTracking().ToListAsync();
        (await new ChemicalHistoryQueryService(f.Db).GetAsync(rows)).Single().Value.Judgment.Should().Be("Unknown");
    }
}
