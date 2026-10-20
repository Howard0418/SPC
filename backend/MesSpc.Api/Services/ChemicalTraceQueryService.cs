using System.Text.Json;
using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Services;

public sealed record ChemicalSavedFCell(string Address,string? NumericValue,string? TextValue);
public sealed record ChemicalSavedSpecification(string? SegmentId,string? Lsl,string? Usl,string? Target);
public sealed record ChemicalTraceCalculation(string Phase,int PhaseVersion,string EvidenceStatus,
 long? FormulaVersionId=null,int? FormulaVersionNo=null,string? FormulaConfigJson=null,string? Reason=null,long? FTableHistoryId=null,
 DateTime? EffectiveFromUtc=null,DateTime? EffectiveUntilUtcExclusive=null,long? RevisionId=null,
 string? Primary=null,string? Secondary=null,string? RawResult=null,string? StoredResult=null,string? Judgment=null,
 string? Actor=null,string? EngineVersion=null,string? PrecisionPolicy=null,string? InputHash=null,
 ChemicalSavedSpecification? Specification=null,List<ChemicalSavedFCell>? FCells=null)
{public bool IsCurrent {get;init;} public DateTime MeasuredAtUtc {get;init;}}
public sealed record ChemicalTraceMeasurement(long MeasurementId,DateTime MeasuredAt,double InitialStored,double? RecheckStored,
 string SamplingPhase,string SamplingStage,string Status,string StatisticsStatus,List<ChemicalTraceCalculation> Calculations);
public sealed record ChemicalTracePage(bool Applicable,int TotalMeasurements,int UnknownMeasurements,bool MultipleFormulaVersions,
 List<ChemicalTraceMeasurement> Rows,bool HasMore,bool HasMoreTrace);

/// <summary>Projects immutable calculation evidence. Querying never selects a current formula or performs calculations.</summary>
public sealed class ChemicalTraceQueryService(AppDbContext db)
{
 public const int PageSize=20;
 public async Task<ChemicalTracePage?> GetAsync(int ppcId,int? segmentId,long? measurementId,int skip,int traceSkip,CancellationToken ct)
 {
  if(skip<0 || traceSkip<0 || segmentId is <=0 || measurementId is <=0)throw new ArgumentException("無效的分段／量測／分頁範圍。");
  var mapping=await db.PartProcessCharacteristics.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==ppcId && x.IsEnabled,ct);
  if(mapping is null)return null;
  if(ControlScopeCodes.Normalize(mapping.ControlScope)!=ControlScopeCodes.Chemical)return new(false,0,0,false,[],false,false);
  var query=db.VariableMeasurements.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==ppcId);
  if(segmentId.HasValue)
  {
   var segment=await db.ControlLimitSegments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==segmentId && x.PartProcessCharacteristicId==ppcId,ct);
   if(segment is null)return null;
   query=query.Where(x=>x.MeasuredAt>=segment.StartDate && (segment.EndDate==null || x.MeasuredAt<=segment.EndDate));
  }
  else if(!measurementId.HasValue)query=query.Where(x=>!db.ControlLimitSegments.Any(s=>s.PartProcessCharacteristicId==ppcId && s.StartDate<=x.MeasuredAt && (s.EndDate==null || s.EndDate>=x.MeasuredAt)));
  if(measurementId.HasValue)query=query.Where(x=>x.Id==measurementId);
  var count=await query.CountAsync(ct);if(measurementId.HasValue && count==0)return null;
  var unknown=await query.CountAsync(x=>x.ChemicalReceiptId==null,ct);
  var multiple=await (from t in db.ChemicalCalculationTraces.AsNoTracking()
    join r in db.ChemicalCalculationReceipts.AsNoTracking() on t.ReceiptId equals r.Id
    join e in db.ChemicalFormulaEffectivities.AsNoTracking() on r.EffectivityId equals e.Id
    where query.Any(v=>v.Id==t.VariableMeasurementId) && r.PartProcessCharacteristicId==ppcId
    select e.FormulaVersionId).Distinct().Take(2).CountAsync(ct)>1;
  var measurements=await query.OrderByDescending(x=>x.MeasuredAt).ThenByDescending(x=>x.Id).Skip(skip).Take(PageSize+1).ToListAsync(ct);
  var more=measurements.Count>PageSize;measurements=measurements.Take(PageSize).ToList();var ids=measurements.Select(x=>x.Id).ToList();
  var traceQuery=db.ChemicalCalculationTraces.AsNoTracking().Where(x=>ids.Contains(x.VariableMeasurementId));
  var latest=await traceQuery.GroupBy(x=>new{x.VariableMeasurementId,x.Phase}).Select(g=>new{g.Key.VariableMeasurementId,g.Key.Phase,Version=g.Max(x=>x.VersionNo)}).ToListAsync(ct);
  var traces=measurementId.HasValue
    ? await traceQuery.OrderByDescending(x=>x.Id).Skip(traceSkip).Take(PageSize+1).ToListAsync(ct)
    : await traceQuery.GroupBy(x=>new{x.VariableMeasurementId,x.Phase}).Select(g=>g.OrderByDescending(x=>x.VersionNo).First()).ToListAsync(ct);
  var moreTrace=measurementId.HasValue && traces.Count>PageSize;if(measurementId.HasValue)traces=traces.Take(PageSize).ToList();
  var linkedInitialIds=await db.ChemicalCalculationTraces.AsNoTracking().Where(t=>ids.Contains(t.VariableMeasurementId) && t.Phase=="Initial" && db.VariableMeasurements.Any(v=>v.Id==t.VariableMeasurementId && v.ChemicalReceiptId==t.ReceiptId)).Select(t=>t.VariableMeasurementId).Distinct().ToListAsync(ct);
  var receiptIds=traces.Select(x=>x.ReceiptId).ToList();
  var receipts=await db.ChemicalCalculationReceipts.AsNoTracking().Where(x=>receiptIds.Contains(x.Id) && x.PartProcessCharacteristicId==ppcId).ToDictionaryAsync(x=>x.Id,ct);
  var effectIds=receipts.Values.Select(x=>x.EffectivityId).ToList();
  var effects=await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x=>effectIds.Contains(x.Id) && x.PartProcessCharacteristicId==ppcId).ToDictionaryAsync(x=>x.Id,ct);
  var formulaIds=effects.Values.Select(x=>x.FormulaVersionId).ToList();
  var formulas=await db.ChemicalAnalysisFormulaVersions.AsNoTracking().Where(x=>formulaIds.Contains(x.Id) && x.PartProcessCharacteristicId==ppcId).ToDictionaryAsync(x=>x.Id,ct);
  var jobs=await db.ChemicalStatisticsJobs.AsNoTracking().Where(x=>ids.Contains(x.VariableMeasurementId)).ToListAsync(ct);
  var calculatedIds=await db.SpcCalculationResults.AsNoTracking().Where(x=>x.VariableMeasurementId.HasValue && ids.Contains(x.VariableMeasurementId.Value)).Select(x=>x.VariableMeasurementId!.Value).Distinct().ToListAsync(ct);
  var rows=new List<ChemicalTraceMeasurement>();
  foreach(var vm in measurements)
  {
   var evidence=traces.Where(x=>x.VariableMeasurementId==vm.Id).Select(t=>Read(t,receipts,effects,formulas,ppcId) with {
    IsCurrent=t.Phase=="Initial"?t.ReceiptId==vm.ChemicalReceiptId:vm.RecheckValue.HasValue && latest.Any(l=>l.VariableMeasurementId==vm.Id && l.Phase=="Recheck" && l.Version==t.VersionNo)
   }).ToList();
   var status=vm.ChemicalReceiptId==null && evidence.Count==0?"Unknown":!linkedInitialIds.Contains(vm.Id) || evidence.Any(x=>x.EvidenceStatus!="Recorded")?"Incomplete":"Recorded";
   var statistics=jobs.Where(x=>x.VariableMeasurementId==vm.Id && x.ReceiptId==vm.ChemicalReceiptId).OrderByDescending(x=>x.CreatedAtUtc).FirstOrDefault()?.Status
       ?? (calculatedIds.Contains(vm.Id)?"Completed":"NotRecorded");
   rows.Add(new(vm.Id,Utc(vm.MeasuredAt),vm.MeasuredValue,vm.RecheckValue,vm.SamplingPhase,vm.SamplingStage,status,statistics,evidence));
  }
  return new(true,count,unknown,multiple,rows,more,moreTrace);
 }
 private static DateTime Utc(DateTime at)=>DateTime.SpecifyKind(at,DateTimeKind.Utc);
 private static ChemicalTraceCalculation Read(ChemicalCalculationTrace trace,Dictionary<Guid,ChemicalCalculationReceipt> receipts,
  Dictionary<long,ChemicalFormulaEffectivity> effects,Dictionary<long,ChemicalAnalysisFormulaVersion> formulas,int ppcId)
 {
  var unavailable=new ChemicalTraceCalculation(trace.Phase,trace.VersionNo,"Unavailable");
  if(!receipts.TryGetValue(trace.ReceiptId,out var r) || !effects.TryGetValue(r.EffectivityId,out var e) || !formulas.TryGetValue(e.FormulaVersionId,out var f))return unavailable;
  try
  {
   using var json=JsonDocument.Parse(r.PayloadJson);var root=json.RootElement;
   if(root.GetProperty("ppcId").GetInt32()!=ppcId || root.GetProperty("effectivityId").GetInt64()!=e.Id || root.GetProperty("formulaVersionId").GetInt64()!=f.Id
      || root.GetProperty("formulaVersionNo").GetInt32()!=f.VersionNo || root.GetProperty("formulaConfigJson").GetString()!=f.ConfigJson
      || root.GetProperty("result").GetProperty("Raw").GetString()!=trace.RawResultCanonical || root.GetProperty("result").GetProperty("Stored").GetString()!=trace.StoredResultCanonical)return unavailable;
   string? Text(JsonElement value,string name)=>value.TryGetProperty(name,out var item) && item.ValueKind!=JsonValueKind.Null?item.ToString():null;
   var input=root.GetProperty("originalInput");var config=root.GetProperty("formulaConfigJson").GetString();var result=root.GetProperty("result");
   if(string.IsNullOrWhiteSpace(Text(input,"Primary")))return unavailable;
   var cells=new List<ChemicalSavedFCell>();var fJson=Text(root,"fCellsJson");
   var formula=JsonSerializer.Deserialize<ChemicalCalculationConfig>(config??"{}",new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
   var refs=ChemicalFTableService.ExtractCellReferences(string.Join(" ",formula?.ConcentrationFormula,formula?.AdjustmentFormula,formula?.AdjustmentAmountFormula)).Select(x=>x.Replace("F!","",StringComparison.OrdinalIgnoreCase).Replace("$","")).ToHashSet(StringComparer.OrdinalIgnoreCase);
   if(refs.Count>0 && (!e.FTableHistoryId.HasValue || string.IsNullOrWhiteSpace(fJson)))return unavailable;
   if(e.FTableHistoryId.HasValue && fJson is not null)
   {
    using var table=JsonDocument.Parse(fJson);
    foreach(var cell in table.RootElement.EnumerateArray())
    {
     var address=Text(cell,"CellAddress")??Text(cell,"cellAddress")??"";
     if(refs.Contains(address.Replace("$","").Replace("F!","",StringComparison.OrdinalIgnoreCase)))
      cells.Add(new(address,Text(cell,"NumericValue")??Text(cell,"numericValue"),Text(cell,"TextValue")??Text(cell,"textValue")));
    }
   }
   if(refs.Any(address=>!cells.Any(c=>c.NumericValue!=null && string.Equals(c.Address.Replace("$","").Replace("F!","",StringComparison.OrdinalIgnoreCase),address,StringComparison.OrdinalIgnoreCase))))return unavailable;
   return new(trace.Phase,trace.VersionNo,"Recorded",f.Id,f.VersionNo,config,f.Reason,e.FTableHistoryId,Utc(e.EffectiveFromUtc),
    e.EffectiveUntilUtcExclusive.HasValue?Utc(e.EffectiveUntilUtcExclusive.Value):null,e.ScheduleRevisionId,Text(input,"Primary"),Text(input,"Secondary"),
    trace.RawResultCanonical,trace.StoredResultCanonical,Text(result,"Judgment"),r.Actor,r.EngineVersion,r.PrecisionPolicy,r.InputHash,
    new(Text(root,"segmentId"),Text(root,"lsl"),Text(root,"usl"),Text(root,"target")),cells){MeasuredAtUtc=Utc(r.MeasuredAtUtc)};
  }
  catch(Exception ex) when(ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException){return unavailable;}
 }
}
