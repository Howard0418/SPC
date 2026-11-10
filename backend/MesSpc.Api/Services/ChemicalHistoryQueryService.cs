using System.Globalization;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Services;

public sealed record ChemicalHistoryPresentation(double EffectiveValue, bool HasRecheck, string InitialJudgment,
    string Judgment, string JudgmentSource, string PrecisionNote, string? JudgmentReasonCode = null,
    string RepairEligibility = "NotRequired", string? RepairSuggestion = null,
    int? SpecificationVersionNo = null, DateTime? SpecificationVersionAt = null);

/// <summary>Read saved judgment evidence in bounded batches; never recalculate formulas or use current master limits.</summary>
public sealed class ChemicalHistoryQueryService(AppDbContext db)
{
    public async Task<Dictionary<long,ChemicalHistoryPresentation>> GetAsync(IReadOnlyList<VariableMeasurement> rows)
    {
        if(rows.Count==0)return [];
        var savedSpecifications=await new ChemicalSavedSpecificationService(db).ReadAsync(rows);
        var ids=rows.Select(x=>x.Id).ToList();
        var traces=await db.ChemicalCalculationTraces.AsNoTracking().Where(x=>ids.Contains(x.VariableMeasurementId))
            .GroupBy(x=>new{x.VariableMeasurementId,x.Phase}).Select(g=>g.OrderByDescending(x=>x.VersionNo).First()).ToListAsync();
        var receiptIds=traces.Select(x=>x.ReceiptId).Distinct().ToList();
        var receipts=await db.ChemicalCalculationReceipts.AsNoTracking().Where(x=>receiptIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var effectIds=receipts.Values.Select(x=>x.EffectivityId).Distinct().ToList();
        var effects=await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x=>effectIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var formulaIds=effects.Values.Select(x=>x.FormulaVersionId).Distinct().ToList();
        var formulas=await db.ChemicalAnalysisFormulaVersions.AsNoTracking().Where(x=>formulaIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        var calculations=await db.SpcCalculationResults.AsNoTracking().Where(x=>x.VariableMeasurementId.HasValue && ids.Contains(x.VariableMeasurementId.Value))
            .GroupBy(x=>x.VariableMeasurementId!.Value).Select(g=>g.OrderByDescending(x=>x.CalculatedAt).ThenByDescending(x=>x.Id).First()).ToListAsync();
        var ppcIds=rows.Select(x=>x.PartProcessCharacteristicId).Distinct().ToList();
        var specificationVersions=await db.SpecificationLimitVersions.AsNoTracking()
            .Where(x=>ppcIds.Contains(x.PartProcessCharacteristicId)).ToListAsync();
        var versionsByPpc=specificationVersions.ToLookup(x=>x.PartProcessCharacteristicId);
        var currentSpecifications=await db.PartProcessCharacteristics.AsNoTracking()
            .Where(x=>ppcIds.Contains(x.Id)).Select(x=>new{x.Id,x.LSL,x.USL}).ToDictionaryAsync(x=>x.Id);
        var byMeasurement=traces.ToLookup(x=>x.VariableMeasurementId);
        var saved=calculations.ToDictionary(x=>x.VariableMeasurementId!.Value);
        var output=new Dictionary<long,ChemicalHistoryPresentation>();
        foreach(var row in rows)
        {
            var value=row.RecheckValue??row.MeasuredValue;
            var own=byMeasurement[row.Id].ToList();
            ChemicalHistoryPresentation Fallback()
            {
                var nearest=NearestVersion(versionsByPpc[row.PartProcessCharacteristicId],row.MeasuredAt);
                if(nearest is not null)return PresentNearestVersion(row,nearest);
                if(currentSpecifications.TryGetValue(row.PartProcessCharacteristicId,out var current))
                    return PresentCurrentSpecification(row,current.LSL,current.USL);
                return new(value,row.RecheckValue.HasValue,"Unknown","Unknown","Unavailable",
                    "缺少相符的歷史判定或規格紀錄，無法判定","HISTORICAL_EVIDENCE_MISSING","SourceEvidenceRequired",
                    "請補回有效規格範圍後重新核對。");
            }
            if(row.ChemicalReceiptId.HasValue || own.Count>0)
            {
                ChemicalTraceCalculation? Read(string phase,double stored)
                {
                    var trace=own.FirstOrDefault(x=>x.Phase==phase);
                    if(trace is null || (phase=="Initial" && trace.ReceiptId!=row.ChemicalReceiptId)
                        || !receipts.TryGetValue(trace.ReceiptId,out var receipt) || receipt.PartProcessCharacteristicId!=row.PartProcessCharacteristicId
                        || !effects.TryGetValue(receipt.EffectivityId,out var effect) || effect.PartProcessCharacteristicId!=row.PartProcessCharacteristicId
                        || !formulas.TryGetValue(effect.FormulaVersionId,out var formula) || formula.PartProcessCharacteristicId!=row.PartProcessCharacteristicId)return null;
                    var evidence=ChemicalTraceQueryService.Read(trace,receipts,effects,formulas,row.PartProcessCharacteristicId);
                    if(evidence.EvidenceStatus!="Recorded"
                        || !double.TryParse(evidence.StoredResult,NumberStyles.Float,CultureInfo.InvariantCulture,out var original) || original!=stored)return null;
                    return evidence;
                }
                var initial=Read("Initial",row.MeasuredValue);
                var final=row.RecheckValue.HasValue?Read("Recheck",value):initial;
                var judgment=Normalize(final?.Judgment);
                var rounded=final is not null && decimal.TryParse(final.RawResult,NumberStyles.Float,CultureInfo.InvariantCulture,out var raw)
                    && decimal.TryParse(final.StoredResult,NumberStyles.Float,CultureInfo.InvariantCulture,out var stored) && raw!=stored;
                output[row.Id]=final is null||judgment=="Unknown"
                    ? Fallback()
                    : new(value,row.RecheckValue.HasValue,Normalize(initial?.Judgment),judgment,"SavedTrace",
                        rounded?"依保存的原始計算結果判定（顯示值已取位）":"");
            }
            else if(savedSpecifications.TryGetValue(row.Id,out var specifications))
                output[row.Id]=ChemicalSavedSpecificationService.Present(row,specifications);
            else if(saved.TryGetValue(row.Id,out var result) && result.StatisticValue==value && (result.LSL.HasValue || result.USL.HasValue))
            {
                var initial=(result.LSL.HasValue && row.MeasuredValue<result.LSL.Value) || (result.USL.HasValue && row.MeasuredValue>result.USL.Value)?"NG":"OK";
                output[row.Id]=new(value,row.RecheckValue.HasValue,initial,result.IsOutOfSpec?"NG":"OK","SavedCalculation","舊資料：依保存量測值及SPC計算紀錄的規格判定");
            }
            else output[row.Id]=Fallback();
        }
        return output;
    }

    private static SpecificationLimitVersion? NearestVersion(IEnumerable<SpecificationLimitVersion> versions,DateTime measuredAt)
    {
        var date=measuredAt.Date;
        return versions.OrderBy(x=>Math.Abs((x.ChangedAt.Date-date).Ticks))
            .ThenBy(x=>x.ChangedAt.Date>date?1:0).ThenByDescending(x=>x.VersionNo).ThenByDescending(x=>x.Id).FirstOrDefault();
    }

    private static ChemicalHistoryPresentation PresentNearestVersion(VariableMeasurement row,SpecificationLimitVersion version)
    {
        var value=row.RecheckValue??row.MeasuredValue;
        var json=version.SnapshotJson??version.PreviousSnapshotJson;
        if(!TryReadLimits(json,out var lsl,out var usl) || (!lsl.HasValue && !usl.HasValue)
            || (lsl.HasValue && !double.IsFinite(lsl.Value)) || (usl.HasValue && !double.IsFinite(usl.Value)) || lsl>usl)
            return new(value,row.RecheckValue.HasValue,"Unknown","Unknown","NearestSpecificationVersion",
                $"最近的規格版本 {version.VersionNo}（{version.ChangedAt:yyyy-MM-dd}）上下限缺漏或無效，無法判定",
                "NEAREST_SPEC_LIMITS_INVALID","SourceEvidenceRequired","請修正該規格版本的LSL／USL後重新載入。",
                version.VersionNo,version.ChangedAt);
        string Judge(double input)=>(lsl.HasValue&&input<lsl.Value)||(usl.HasValue&&input>usl.Value)?"NG":"OK";
        return new(value,row.RecheckValue.HasValue,Judge(row.MeasuredValue),Judge(value),"NearestSpecificationVersion",
            $"量測日無對應保存規格；依最近的規格版本 {version.VersionNo}（{version.ChangedAt:yyyy-MM-dd}）LSL／USL判定",
            null,"NotRequired",null,version.VersionNo,version.ChangedAt);
    }

    private static ChemicalHistoryPresentation PresentCurrentSpecification(VariableMeasurement row,double? lsl,double? usl)
    {
        var value=row.RecheckValue??row.MeasuredValue;
        if((!lsl.HasValue&&!usl.HasValue)||(lsl.HasValue&&!double.IsFinite(lsl.Value))
            ||(usl.HasValue&&!double.IsFinite(usl.Value))||lsl>usl)
            return new(value,row.RecheckValue.HasValue,"Unknown","Unknown","CurrentSpecificationFallback",
                "找不到規格版本歷史，且目前項目上下限缺漏或無效，無法判定","CURRENT_SPEC_LIMITS_INVALID",
                "SourceEvidenceRequired","請到管制項目設定補齊有效的LSL／USL。");
        string Judge(double input)=>(lsl.HasValue&&input<lsl.Value)||(usl.HasValue&&input>usl.Value)?"NG":"OK";
        return new(value,row.RecheckValue.HasValue,Judge(row.MeasuredValue),Judge(value),"CurrentSpecificationFallback",
            "找不到可追溯的規格版本；依目前管制項目的LSL／USL範圍判定");
    }

    private static bool TryReadLimits(string? json,out double? lsl,out double? usl)
    {
        lsl=null;usl=null;
        if(string.IsNullOrWhiteSpace(json))return false;
        try
        {
            using var document=System.Text.Json.JsonDocument.Parse(json);
            lsl=Read(document.RootElement,"LSL");usl=Read(document.RootElement,"USL");return true;
        }
        catch(System.Text.Json.JsonException){return false;}
        static double? Read(System.Text.Json.JsonElement root,string name)
        {
            if(!root.TryGetProperty(name,out var element)||element.ValueKind==System.Text.Json.JsonValueKind.Null)return null;
            if(element.ValueKind==System.Text.Json.JsonValueKind.Number&&element.TryGetDouble(out var number))return number;
            return element.ValueKind==System.Text.Json.JsonValueKind.String
                && double.TryParse(element.GetString(),NumberStyles.Float,CultureInfo.InvariantCulture,out number)?number:null;
        }
    }
    private static string Normalize(string? value)=>value is "OK" or "NG"?value:"Unknown";
}
