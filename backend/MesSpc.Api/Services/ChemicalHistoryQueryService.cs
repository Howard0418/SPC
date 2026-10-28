using System.Globalization;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Services;

public sealed record ChemicalHistoryPresentation(double EffectiveValue, bool HasRecheck, string InitialJudgment,
    string Judgment, string JudgmentSource, string PrecisionNote);

/// <summary>Read saved judgment evidence in bounded batches; never recalculate formulas or use current master limits.</summary>
public sealed class ChemicalHistoryQueryService(AppDbContext db)
{
    public async Task<Dictionary<long,ChemicalHistoryPresentation>> GetAsync(IReadOnlyList<VariableMeasurement> rows)
    {
        if(rows.Count==0)return [];
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
                output[row.Id]=new(value,row.RecheckValue.HasValue,Normalize(initial?.Judgment),judgment,
                    final is null?"Unavailable":"SavedTrace",final is null?"缺少有效的保存追溯，無法判定"
                    :judgment=="Unknown"?"保存紀錄缺少有效規格或判定，無法判定":rounded?"依保存的原始計算結果判定（顯示值已取位）":"");
            }
            else if(saved.TryGetValue(row.Id,out var result) && result.StatisticValue==value && (result.LSL.HasValue || result.USL.HasValue))
            {
                var initial=(result.LSL.HasValue && row.MeasuredValue<result.LSL.Value) || (result.USL.HasValue && row.MeasuredValue>result.USL.Value)?"NG":"OK";
                output[row.Id]=new(value,row.RecheckValue.HasValue,initial,result.IsOutOfSpec?"NG":"OK","SavedCalculation","舊資料：依保存量測值及SPC計算紀錄的規格判定");
            }
            else output[row.Id]=new(value,row.RecheckValue.HasValue,"Unknown","Unknown","Unavailable","缺少相符的歷史判定或規格紀錄，無法判定");
        }
        return output;
    }
    private static string Normalize(string? value)=>value is "OK" or "NG"?value:"Unknown";
}
