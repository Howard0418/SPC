using MesSpc.Api.Services.Localization;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public sealed record ChemicalPortalPreviewRequest(int PpcId,string MeasuredAt,string Primary,string? Secondary,string SamplingPhase,string SamplingStage);
public sealed record ChemicalPortalConfirmRequest(Guid RequestId,List<Dictionary<string,string?>> Rows);
public sealed record ChemicalPortalEvidence(ChemicalCalculationReceipt Initial,ChemicalCalculationReceipt? Recheck);
public sealed record ChemicalPortalConfirmed(Guid UploadBatchId,bool AlreadyProcessed);

/// <summary>Portal-only atomic orchestration, reusing staging, daily upsert and statistics. Does not replace TransFiles DB import.</summary>
public sealed class ChemicalPortalMeasurementService(AppDbContext db,ChemicalFormulaScheduleService schedule,UploadService uploads,
    ILogger<ChemicalPortalMeasurementService> logger)
{
    private const string Marker="PortalChemicalTrace-v1";
    private static string? Get(Dictionary<string,string?> row,string key)=>row.GetValueOrDefault(key);

    public async Task<object> GetSchedulesAsync(CancellationToken ct) => await db.ChemicalFormulaPolicies.AsNoTracking()
        .Where(x=>x.ActiveScheduleRevisionId!=null).Select(x=>new{x.PartProcessCharacteristicId,revisionId=x.ActiveScheduleRevisionId}).ToListAsync(ct);

    private async Task<ChemicalPreviewInput> InputAsync(ChemicalPortalPreviewRequest request,CancellationToken ct)
    {
        var mapping=await db.PartProcessCharacteristics.AsNoTracking().Include(x=>x.Machine)
            .SingleOrDefaultAsync(x=>x.Id==request.PpcId && x.IsEnabled,ct) ?? throw ApiUiException.Attach(new ArgumentException("管制項目不存在或已停用。"), "spc.api.chemicalItemDisabled");
        if(ControlScopeCodes.Normalize(mapping.ControlScope)!=ControlScopeCodes.Chemical)throw ApiUiException.Attach(new ArgumentException("此入口只支援藥液日報。"), "spc.api.chemicalDailyOnly");
        var at=ChemicalFormulaCalculation.ParseMeasuredAt(request.MeasuredAt);
        if(request.SamplingPhase is not ("OPEN" or "MIDDLE"))throw ApiUiException.Attach(new ArgumentException("請選早班或中班。"), "spc.api.chemicalShiftRequired");
        if(!ChemicalSamplingStage.TryNormalize(mapping.Machine?.MachineCode,request.SamplingStage,out var stage))throw ApiUiException.Attach(new ArgumentException(ChemicalSamplingStage.Error), "spc.api.chemicalSamplingStageInvalid");
        return new(request.MeasuredAt,request.Primary,request.Secondary,$"PortalDaily|{request.PpcId}|{at.AddHours(8):yyyyMMdd}|{request.SamplingPhase}|{stage}");
    }

    /// <summary>Dates and original decimal strings are resolved by SPC; no custom expression from the browser is trusted.</summary>
    public async Task<ChemicalPreviewResult> PreviewAsync(ChemicalPortalPreviewRequest request,string actor,CancellationToken ct)
        => await schedule.PreviewAsync(request.PpcId,await InputAsync(request,ct),actor,ct);

    /// <summary>All rows, receipts, initial/recheck traces and statistics commit together; a request ID provides safe retry.</summary>
    public async Task<ChemicalPortalConfirmed> ConfirmAsync(ChemicalPortalConfirmRequest request,string actor,CancellationToken ct)
    {
        if(request.RequestId==Guid.Empty || request.Rows is null || request.Rows.Count is <1 or >200 || request.Rows.Any(x=>x is null))
            throw ApiUiException.Attach(new ArgumentException("請提供唯一requestId及1至200筆日報。"), "spc.api.chemicalBatchRequired");
        if(string.IsNullOrWhiteSpace(actor) || actor.Length>100)throw ApiUiException.Attach(new ArgumentException("缺少有效操作者。"), "spc.api.actorRequired");
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actor+"|"+JsonSerializer.Serialize(request.Rows))));
        var rows=request.Rows.Select(x=>new Dictionary<string,string?>(x,StringComparer.Ordinal)).ToList();
        var ids=rows.Select(x=>int.TryParse(Get(x,"PpcId"),out var id) && id>0?id:throw ApiUiException.Attach(new ArgumentException("每筆須有有效PpcId。"), "spc.api.chemicalRowIdRequired")).ToList();
        if(ids.Distinct().Count()!=ids.Count)throw ApiUiException.Attach(new ArgumentException("同批每個管制項目只可有一筆日報。"), "spc.api.chemicalBatchDuplicate");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var committed=false;
        try
        {
            if(db.Database.IsSqlServer())
            {
                var resource="PortalChemicalBatch:"+request.RequestId.ToString("N");
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW 51200,'Chemical batch is busy.',1;",ct);
                foreach(var id in ids.Order())await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}",ct);
            }
            var existing=await db.UploadBatches.AsNoTracking().SingleOrDefaultAsync(x=>x.UploadBatchId==request.RequestId,ct);
            if(existing is not null)
            {
                if(existing.FileHash!=hash || existing.OriginalFileName!=Marker || existing.ImportStatus!="Imported")
                    throw ApiUiException.Attach(new ChemicalPreviewConflictException("requestId已用於其他內容或狀態，請重新確認資料。"), "spc.api.chemicalRequestUsed");
                await tx.CommitAsync(ct);return new(existing.UploadBatchId,true);
            }
            var evidence=new Dictionary<int,ChemicalPortalEvidence>();
            foreach(var row in rows)
            {
                var id=int.Parse(row["PpcId"]!,System.Globalization.CultureInfo.InvariantCulture);
                // Validate date/stage even for legacy rows; no system-date fallback on this new entry point.
                var input=await InputAsync(new(id,Get(row,"MeasuredAt")??"",Get(row,"TitrationValue")??"",Get(row,"SecondaryTitrationValue"),Get(row,"SamplingPhase")??"",Get(row,"SamplingStage")??"GENERAL"),ct);
                if(ControlScopeCodes.Normalize(Get(row,"ControlScope"))!=ControlScopeCodes.Chemical || await uploads.ResolveMappingIdAsync(row,ct)!=id)
                    throw ApiUiException.Attach(new ArgumentException("PpcId與線別／槽位／檢驗特性不一致，請重新載入主檔。"), "spc.api.chemicalIdentityMismatch");
                var policy=await db.ChemicalFormulaPolicies.AsNoTracking().SingleOrDefaultAsync(x=>x.PartProcessCharacteristicId==id,ct);
                if(policy?.ActiveScheduleRevisionId is null)continue;
                if(Get(row,"SampleNo") is not (null or "" or "1"))throw ApiUiException.Attach(new ArgumentException("公式日報只支援單筆樣本。"), "spc.api.chemicalSingleSampleOnly");
                if(string.IsNullOrWhiteSpace(Get(row,"ChemicalPreviewToken")))throw ApiUiException.Attach(new ChemicalPreviewConflictException("初驗尚未依量測日期預覽，請重新計算。"), "spc.api.chemicalInitialPreviewRequired");
                var initial=await schedule.PrepareAsync(id,new(input,Get(row,"ChemicalPreviewToken")!),actor,ct);
                ChemicalCalculationReceipt? recheck=null;
                var hasRecheck=!string.IsNullOrWhiteSpace(Get(row,"RecheckTitrationValue"));
                if(!hasRecheck && !string.IsNullOrWhiteSpace(Get(row,"RecheckValue")))throw ApiUiException.Attach(new ArgumentException("日期公式複驗須提供原始滴定值，不可只填濃度。"), "spc.api.chemicalRecheckInputRequired");
                if(hasRecheck)
                {
                    if(string.IsNullOrWhiteSpace(Get(row,"ChemicalRecheckPreviewToken")))throw ApiUiException.Attach(new ChemicalPreviewConflictException("複驗尚未預覽，請重新計算。"), "spc.api.chemicalRecheckPreviewRequired");
                    recheck=await schedule.PrepareAsync(id,new(input with{Primary=Get(row,"RecheckTitrationValue")!,Secondary=Get(row,"RecheckSecondaryTitrationValue")},Get(row,"ChemicalRecheckPreviewToken")!),actor,ct);
                }
                evidence[id]=new(initial,recheck);
                var value=Result(initial);var final=recheck is null?value:Result(recheck);
                row["MeasuredValue"]=value.Stored;row["RecheckValue"]=recheck is null?"":final.Stored;
                row["AdjustAction"]=final.Adjustment;row["AdjustAmount"]=final.AdjustmentAmount;

            }
            foreach(var row in rows){row.Remove("ChemicalPreviewToken");row.Remove("ChemicalRecheckPreviewToken");}
            var batch=await uploads.CreateVariableBatchAsync(rows,"PortalDaily",actor,Marker,ct:ct,uploadBatchId:request.RequestId);
            if(batch.ErrorRows>0)
            {
                var sourceErrors=await db.UploadErrors.AsNoTracking().Where(x=>x.UploadBatchId==batch.UploadBatchId).OrderBy(x=>x.RowNo).Take(5)
                    .Select(x=>new { x.RowNo,x.FieldName,x.ErrorCode,x.ErrorMessage }).ToListAsync(ct);
                var errors=sourceErrors.Select(x=>x.RowNo+"："+x.FieldName+" "+x.ErrorMessage).ToList();
                var failure=new ArgumentException("日報格式不符，未寫入："+string.Join("；",errors));
                var children=sourceErrors.Select(x=>UploadErrorUi.Describe(x.ErrorCode,x.ErrorMessage,x.FieldName)).ToArray();
                if(children.All(x=>x is not null))
                {
                    var details=sourceErrors.Select((x,index)=>ApiUiError.Create("IMPORT_ROW_INVALID","spc.api.importRowInvalid",new Dictionary<string,string>{{"row",x.RowNo?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}}) with { Details = new[] { children[index]! } });
                    throw ApiUiException.AttachDetails(failure,"spc.api.dailyBatchInvalid",ApiUiMessageCatalog.EmptyArguments,details);
                }
                throw failure;
            }
            batch.FileHash=hash;
            await uploads.ConfirmAsync(batch.UploadBatchId,ct:ct,chemicalEvidence:evidence,deferVariableNotifications:true);
            await tx.CommitAsync(ct); committed=true;
            logger.LogInformation("Portal chemical batch committed {BatchId}, traced items {Count}",batch.UploadBatchId,evidence.Count);
            await tx.DisposeAsync();
            try { await uploads.NotifyCommittedVariableBatchAsync(batch.UploadBatchId,ct); }
            catch(Exception ex) { logger.LogWarning(ex,"Committed chemical batch notification failed {BatchId}",batch.UploadBatchId); }
            return new(batch.UploadBatchId,false);
        }
        catch
        {
            if(!committed)await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();throw;
        }
    }

    public static ChemicalCalculatedValue Result(ChemicalCalculationReceipt receipt)
    {
        using var payload=JsonDocument.Parse(receipt.PayloadJson);
        return payload.RootElement.GetProperty("result").Deserialize<ChemicalCalculatedValue>() ?? throw new InvalidOperationException("Receipt result missing.");
    }
}
