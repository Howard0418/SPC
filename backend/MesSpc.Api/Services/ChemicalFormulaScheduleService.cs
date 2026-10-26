using MesSpc.Api.Services.Localization;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MesSpc.Api.Services;

public sealed record ChemicalPublishSchedule(long? BaseRevisionId, string Reason, IReadOnlyList<ChemicalSchedulePeriod> Periods);
public sealed record ChemicalPreviewInput(string MeasuredAt, string Primary, string? Secondary, string SourceKey);
public sealed record ChemicalPrepareRequest(ChemicalPreviewInput Input, string Token);
public sealed record ChemicalPreviewResult(long RevisionId, long FormulaVersionId, int FormulaVersionNo, long? FTableHistoryId,
    DateTime MeasuredAtUtc, ChemicalCalculatedValue Result, string EngineVersion, string PrecisionPolicy, string Token, DateTime ExpiresAtUtc);
public sealed class ChemicalPreviewConflictException(string message) : Exception(message);
internal sealed record ChemicalPreviewTicket(int PpcId, string Actor, string PayloadHash, DateTime ExpiresAtUtc);
internal sealed record ChemicalResolvedCalculation(long RevisionId, ChemicalFormulaEffectivity Period, ChemicalAnalysisFormulaVersion Formula,
    DateTime MeasuredAtUtc, ChemicalCalculatedValue Result, string PayloadJson);

/// <summary>Publishes immutable schedules and issues calculation evidence without importing any measurement.</summary>
public sealed class ChemicalFormulaScheduleService(AppDbContext db, IDataProtectionProvider protection, TimeProvider clock,
    ILogger<ChemicalFormulaScheduleService> logger)
{
    private readonly IDataProtector protector = protection.CreateProtector("SPC.ChemicalPreview.v1");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Serializable PPC lock is shared by schedule publication and receipt preparation.</summary>
    private async Task<IDbContextTransaction?> LockAsync(int ppcId, CancellationToken ct)
    {
        if (!db.Database.IsRelational()) return null; // Test-only InMemory provider; SQL concurrency is tested separately.
        var ownsTransaction = db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            if (db.Database.IsSqlServer())
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM dbo.PartProcessCharacteristics WITH (UPDLOCK,HOLDLOCK) WHERE Id={ppcId}", ct);
            return tx;
        }
        catch { if (tx is not null) await tx.DisposeAsync(); throw; }
    }

    private async Task<PartProcessCharacteristic> MappingAsync(int id, CancellationToken ct)
    {
        var mapping = await db.PartProcessCharacteristics.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiUiException.Attach(new KeyNotFoundException("找不到指定管制項目。"), "spc.api.scheduleItemMissing");
        if (ControlScopeCodes.Normalize(mapping.ControlScope) != ControlScopeCodes.Chemical || !mapping.IsEnabled)
            throw ApiUiException.Attach(new ArgumentException("僅啟用中的藥液管制項目可設定公式排程。"), "spc.api.scheduleChemicalOnly");
        return mapping;
    }

    /// <summary>Returns current schedule and explicit gaps; does not infer historical effectiveness from ChangedAt.</summary>
    public async Task<object> GetAsync(int id, CancellationToken ct)
    {
        await MappingAsync(id, ct);
        var policy = await db.ChemicalFormulaPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.PartProcessCharacteristicId == id, ct);
        var activeRevisionId = policy?.ActiveScheduleRevisionId;
        var periods = await db.ChemicalFormulaEffectivities.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == id && x.ScheduleRevisionId == activeRevisionId)
            .OrderBy(x => x.EffectiveFromUtc).ToListAsync(ct);
        var gaps = periods.Zip(periods.Skip(1), (a,b) => new { FromUtc=a.EffectiveUntilUtcExclusive, UntilUtc=b.EffectiveFromUtc })
            .Where(x => x.FromUtc < x.UntilUtc).ToList();
        return new { revisionId=policy?.ActiveScheduleRevisionId, isEnabled=policy?.IsEnabled ?? false,
            periods=periods.Select(p => new { p.Id,p.FormulaVersionId,p.FTableHistoryId, effectiveFromUtc=DateTime.SpecifyKind(p.EffectiveFromUtc,DateTimeKind.Utc),
                effectiveUntilUtcExclusive=p.EffectiveUntilUtcExclusive.HasValue ? (DateTime?)DateTime.SpecifyKind(p.EffectiveUntilUtcExclusive.Value,DateTimeKind.Utc) : null }),
            gaps=gaps.Select(g => new { fromUtc=g.FromUtc.HasValue ? (DateTime?)DateTime.SpecifyKind(g.FromUtc.Value,DateTimeKind.Utc) : null, untilUtc=DateTime.SpecifyKind(g.UntilUtc,DateTimeKind.Utc) }),
            message="目前僅排程與試算；既有寫入尚未切換日期選版。首段之前、末段之後及空缺日期皆拒絕試算。",
            messageKey="spc.api.chemicalSchedulePreviewOnly", args=ApiUiMessageCatalog.EmptyArguments };
    }

    /// <summary>Publishes a complete replacement schedule using optimistic revision matching and a serialized PPC transaction.</summary>
    public async Task<long> PublishAsync(int id, ChemicalPublishSchedule request, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100) throw ApiUiException.Attach(new ArgumentException("缺少有效操作者。"), "spc.api.actorRequired");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) throw ApiUiException.Attach(new ArgumentException("請提供500字以內發布依據。"), "spc.api.scheduleReasonRequired");
        if (request.Periods is null || request.Periods.Any(x => x is null)) throw ApiUiException.Attach(new ArgumentException("請提供有效排程期間。"), "spc.api.schedulePeriodsRequired");
        var periods = ChemicalFormulaCalculation.ValidatePeriods(request.Periods);
        await using var tx = await LockAsync(id, ct);
        await MappingAsync(id, ct);
        var policy = await db.ChemicalFormulaPolicies.SingleOrDefaultAsync(x => x.PartProcessCharacteristicId == id, ct);
        if (policy?.ActiveScheduleRevisionId != request.BaseRevisionId)
            throw ApiUiException.Attach(new ChemicalPreviewConflictException("排程已被其他人發布，請重新載入後再操作。"), "spc.api.scheduleAlreadyPublished");
        foreach (var period in periods)
        {
            var formula = await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == period.FormulaVersionId && x.PartProcessCharacteristicId == id, ct)
                ?? throw ApiUiException.Attach(new ArgumentException("公式版本不存在或不屬於此管制項目。"), "spc.api.scheduleFormulaMissing");
            ChemicalCalculationConfig? config;
            try { config = JsonSerializer.Deserialize<ChemicalCalculationConfig>(formula.ConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive=true }); }
            catch (JsonException) { throw ApiUiException.Attach(new ArgumentException("公式歷史配置不是有效JSON，請先在公式維護建立有效版本。"), "spc.api.scheduleFormulaJsonInvalid"); }
            if (config is not { Enabled:true } || string.IsNullOrWhiteSpace(config.ConcentrationFormula) || config.DecimalPlaces is <0 or >8)
                throw ApiUiException.Attach(new ArgumentException("公式版本必須啟用，具有濃度公式及有效精度。"), "spc.api.scheduleFormulaInvalid");
            var refs = ChemicalFTableService.ExtractCellReferences(string.Join(" ", config.ConcentrationFormula, config.AdjustmentFormula, config.AdjustmentAmountFormula));
            ChemicalFTableVersionHistory? f = null;
            if (period.FTableHistoryId.HasValue)
                f = await db.ChemicalFTableVersionHistories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == period.FTableHistoryId, ct)
                    ?? throw ApiUiException.Attach(new ArgumentException("F表歷史快照不存在。"), "spc.api.scheduleSnapshotMissing");
            if (refs.Count > 0)
            {
                if (f is null) throw ApiUiException.Attach(new ArgumentException("含F表公式須指定F表歷史快照。"), "spc.api.scheduleSnapshotRequired");
                var cells = JsonSerializer.Deserialize<List<ChemicalFTableHistoryCell>>(f.CellsJson) ?? [];
                if (refs.Any(r => !cells.Any(c => c.NumericValue.HasValue && string.Equals(c.CellAddress.Replace("$", "").Replace("F!", "", StringComparison.OrdinalIgnoreCase), r.Replace("F!", "").Replace("$", ""), StringComparison.OrdinalIgnoreCase))))
                    throw ApiUiException.Attach(new ArgumentException("F表歷史快照缺少公式所需儲存格。"), "spc.api.scheduleSnapshotCellsMissing");
            }
        }
        var used = await (from receipt in db.ChemicalCalculationReceipts.AsNoTracking()
            join effect in db.ChemicalFormulaEffectivities.AsNoTracking() on receipt.EffectivityId equals effect.Id
            where receipt.PartProcessCharacteristicId == id
            select new { receipt.MeasuredAtUtc, effect.FormulaVersionId, effect.FTableHistoryId }).Distinct().ToListAsync(ct);
        if (used.Any(old => !periods.Any(p => p.FormulaVersionId == old.FormulaVersionId && p.FTableHistoryId == old.FTableHistoryId &&
            p.FromUtc <= old.MeasuredAtUtc && (p.UntilUtc == null || old.MeasuredAtUtc < p.UntilUtc))))
            throw ApiUiException.Attach(new ArgumentException("已發行計算憑證的量測時點不可改用其他公式或F快照；可在未使用日期切換未來版本。"), "spc.api.scheduleReceiptImmutable");
        var number = (await db.ChemicalFormulaScheduleRevisions.Where(x => x.PartProcessCharacteristicId == id).MaxAsync(x => (int?)x.RevisionNo, ct) ?? 0) + 1;
        var revision = new ChemicalFormulaScheduleRevision { PartProcessCharacteristicId=id, RevisionNo=number, CreatedAtUtc=Now, Actor=actor, Reason=request.Reason.Trim() };
        db.Add(revision); await db.SaveChangesAsync(ct);
        db.AddRange(periods.Select(p => new ChemicalFormulaEffectivity { PartProcessCharacteristicId=id, ScheduleRevisionId=revision.Id,
            FormulaVersionId=p.FormulaVersionId, FTableHistoryId=p.FTableHistoryId, EffectiveFromUtc=p.FromUtc, EffectiveUntilUtcExclusive=p.UntilUtc }));
        await db.SaveChangesAsync(ct);
        revision.PublishedAtUtc=Now; await db.SaveChangesAsync(ct);
        if (policy is null) { policy = new() { PartProcessCharacteristicId=id }; db.Add(policy); }
        policy.ActiveScheduleRevisionId=revision.Id; // IsEnabled is deliberately unchanged; clients are not yet migrated.
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        logger.LogInformation("Published chemical schedule PPC {PpcId} revision {RevisionId}", id, revision.Id);
        return revision.Id;
    }

    private async Task<ChemicalResolvedCalculation> ResolveAsync(int id, ChemicalPreviewInput input, CancellationToken ct)
    {
        if (input is null) throw ApiUiException.Attach(new ArgumentException("請提供原始輸入。"), "spc.api.chemicalInputRequired");
        var mapping = await MappingAsync(id, ct);
        var at = ChemicalFormulaCalculation.ParseMeasuredAt(input.MeasuredAt);
        if (string.IsNullOrWhiteSpace(input.SourceKey) || input.SourceKey.Length > 1000) throw ApiUiException.Attach(new ArgumentException("請提供1000字內來源唯一鍵。"), "spc.api.chemicalSourceKeyRequired");
        var policy = await db.ChemicalFormulaPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.PartProcessCharacteristicId == id, ct);
        if (policy?.ActiveScheduleRevisionId is not long revisionId) throw ApiUiException.Attach(new ArgumentException("尚未發布公式排程，請先設定適用期間。"), "spc.api.scheduleNotConfigured");
        if (!await db.ChemicalFormulaScheduleRevisions.AsNoTracking().AnyAsync(x => x.Id==revisionId && x.PublishedAtUtc!=null, ct))
            throw ApiUiException.Attach(new ArgumentException("排程尚未發布。"), "spc.api.scheduleNotPublished");
        var matches = await db.ChemicalFormulaEffectivities.AsNoTracking().Where(x => x.PartProcessCharacteristicId==id && x.ScheduleRevisionId==revisionId &&
            x.EffectiveFromUtc<=at && (x.EffectiveUntilUtcExclusive==null || at<x.EffectiveUntilUtcExclusive)).Take(2).ToListAsync(ct);
        if (matches.Count!=1) throw ApiUiException.Attach(new ArgumentException(matches.Count==0 ? "量測日期沒有適用公式，請補齊排程空缺後重新預覽。" : "公式期間衝突，請修正排程後重試。"), matches.Count==0 ? "spc.api.scheduleDateGap" : "spc.api.scheduleDateConflict");
        var period=matches[0];
        var formula=await db.ChemicalAnalysisFormulaVersions.AsNoTracking().SingleAsync(x => x.Id==period.FormulaVersionId && x.PartProcessCharacteristicId==id,ct);
        var f=period.FTableHistoryId.HasValue ? await db.ChemicalFTableVersionHistories.AsNoTracking().SingleAsync(x=>x.Id==period.FTableHistoryId,ct) : null;
        // Same inclusive segment date precedence as existing chart service; never manufacture independent effective spec versions.
        var segments=await db.ControlLimitSegments.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==id && x.StartDate<=at && (x.EndDate==null || x.EndDate>=at))
            .OrderByDescending(x=>x.StartDate).ThenByDescending(x=>x.Id).Take(2).ToListAsync(ct);
        if (segments.Count>1) throw ApiUiException.Attach(new ArgumentException("量測日期有重疊規格分段，請先修正後再試算。"), "spc.api.chemicalSpecificationOverlap");
        var segment=segments.FirstOrDefault();
        decimal? ConvertLimit(double? value) => value.HasValue ? Convert.ToDecimal(value.Value, System.Globalization.CultureInfo.InvariantCulture) : null;
        var lsl=ConvertLimit(segment?.LSL ?? mapping.LSL); var usl=ConvertLimit(segment?.USL ?? mapping.USL); var target=ConvertLimit(segment?.TargetValue ?? mapping.TargetValue);
        var result=ChemicalFormulaCalculation.Calculate(formula.ConfigJson,input.Primary,input.Secondary,f?.CellsJson ?? "[]",lsl,usl,target);
        var payload=JsonSerializer.Serialize(new { ppcId=id, revisionId, effectivityId=period.Id, formulaVersionId=formula.Id, formulaVersionNo=formula.VersionNo,
            formulaConfigJson=formula.ConfigJson, fTableHistoryId=f?.Id, fCellsJson=f?.CellsJson, measuredAtUtc=at, originalInput=input,
            primaryCanonical=ChemicalFormulaCalculation.Canonical(ChemicalFormulaCalculation.ReadDecimal(input.Primary,"Primary")),
            secondaryCanonical=string.IsNullOrWhiteSpace(input.Secondary)?null:ChemicalFormulaCalculation.Canonical(ChemicalFormulaCalculation.ReadDecimal(input.Secondary,"Secondary")),
            sourceKeyHash=Hash(input.SourceKey), unit=mapping.Unit, segmentId=segment?.Id, lsl,usl,target,result,
            engineVersion=ChemicalFormulaCalculation.EngineVersion, precisionPolicy=ChemicalFormulaCalculation.PrecisionPolicy });
        return new(revisionId,period,formula,at,result,payload);
    }

    /// <summary>Pure preview: no business data or receipt is persisted.</summary>
    public async Task<ChemicalPreviewResult> PreviewAsync(int id, ChemicalPreviewInput input, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 100) throw ApiUiException.Attach(new ArgumentException("缺少有效操作者。"), "spc.api.actorRequired");
        var value=await ResolveAsync(id,input,ct); var expires=Now.AddMinutes(10);
        var token=protector.Protect(JsonSerializer.Serialize(new ChemicalPreviewTicket(id,actor,Hash(value.PayloadJson),expires)));
        return new(value.RevisionId,value.Formula.Id,value.Formula.VersionNo,value.Period.FTableHistoryId,value.MeasuredAtUtc,value.Result,
            ChemicalFormulaCalculation.EngineVersion,ChemicalFormulaCalculation.PrecisionPolicy,token,expires);
    }

    /// <summary>Checks protected preview against current input/schedule under lock; prepares immutable evidence only.</summary>
    public async Task<ChemicalCalculationReceipt> PrepareAsync(int id, ChemicalPrepareRequest request, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 20000) throw ApiUiException.Attach(new ChemicalPreviewConflictException("請重新預覽。"), "spc.api.chemicalPreviewRequired");
        ChemicalPreviewTicket ticket;
        try { ticket=JsonSerializer.Deserialize<ChemicalPreviewTicket>(protector.Unprotect(request.Token)) ?? throw new CryptographicException(); }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        { throw ApiUiException.Attach(new ChemicalPreviewConflictException("預覽憑證無效，請重新預覽。"), "spc.api.chemicalPreviewInvalid"); }
        if (ticket.PpcId!=id || ticket.Actor!=actor || ticket.ExpiresAtUtc<=Now)
            throw ApiUiException.Attach(new ChemicalPreviewConflictException("預覽已過期或操作者不同，請重新預覽。"), "spc.api.chemicalPreviewExpired");
        await using var tx=await LockAsync(id,ct);
        var value=await ResolveAsync(id,request.Input,ct);
        if (ticket.ExpiresAtUtc<=Now || ticket.PayloadHash!=Hash(value.PayloadJson)) throw ApiUiException.Attach(new ChemicalPreviewConflictException("輸入、規格或公式排程已變更，請重新預覽。"), "spc.api.chemicalPreviewChanged");
        var receipt=new ChemicalCalculationReceipt { Id=Guid.NewGuid(),PartProcessCharacteristicId=id,EffectivityId=value.Period.Id,
            MeasuredAtUtc=value.MeasuredAtUtc,CreatedAtUtc=Now,ExpiresAtUtc=Now.AddMinutes(10),Actor=actor,InputHash=Hash(value.PayloadJson),
            SourceKeyHash=Hash(request.Input.SourceKey),EngineVersion=ChemicalFormulaCalculation.EngineVersion,
            PrecisionPolicy=ChemicalFormulaCalculation.PrecisionPolicy,PayloadJson=value.PayloadJson };
        db.Add(receipt); await db.SaveChangesAsync(ct); if(tx is not null) await tx.CommitAsync(ct);
        logger.LogInformation("Prepared chemical receipt {ReceiptId} PPC {PpcId}",receipt.Id,id);
        return receipt;
    }
}
