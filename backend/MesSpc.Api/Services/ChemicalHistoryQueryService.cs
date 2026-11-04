using System.Globalization;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Services;

public sealed record ChemicalHistoryPresentation(double EffectiveValue, bool HasRecheck, string InitialJudgment,
    string Judgment, string JudgmentSource, string PrecisionNote, string? JudgmentReasonCode = null,
    string RepairEligibility = "NotRequired", string? RepairSuggestion = null);

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
        var byMeasurement=traces.ToLookup(x=>x.VariableMeasurementId);
        var saved=calculations.ToDictionary(x=>x.VariableMeasurementId!.Value);
        var output=new Dictionary<long,ChemicalHistoryPresentation>();
        foreach(var row in rows)
        {
            var value=row.RecheckValue??row.MeasuredValue;
            var own=byMeasurement[row.Id].ToList();
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
                output[row.Id]=final is null
                    ? new(value,row.RecheckValue.HasValue,Normalize(initial?.Judgment),judgment,"Unavailable",
                        "缺少有效的保存追溯，無法判定","TRACE_EVIDENCE_INVALID","SourceEvidenceRequired",
                        "補回量測當時的Receipt／Trace、公式F與規格版本後重新核對；不可套用目前主檔。")
                    : judgment=="Unknown"
                        ? new(value,row.RecheckValue.HasValue,Normalize(initial?.Judgment),judgment,"SavedTrace",
                            "保存紀錄缺少有效規格或判定，無法判定","TRACE_JUDGMENT_MISSING","SourceEvidenceRequired",
                            "補齊保存追溯中的當時規格與判定證據後重新核對；不可套用目前主檔。")
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
            else output[row.Id]=new(value,row.RecheckValue.HasValue,"Unknown","Unknown","Unavailable",
                "缺少相符的歷史判定或規格紀錄，無法判定","HISTORICAL_EVIDENCE_MISSING","SourceEvidenceRequired",
                "補回量測當時的Receipt／Trace、公式F與規格版本後重新核對；不可套用目前主檔。");
        }
        return output;
    }
    private static string Normalize(string? value)=>value is "OK" or "NG"?value:"Unknown";
}
