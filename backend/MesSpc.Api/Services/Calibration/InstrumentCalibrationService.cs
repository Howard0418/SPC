using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Calibration;

public record InstrumentInput(string Code, string Name, string Department, int CustodianOperatorId, int CycleMonths,
    DateOnly? LastCalibrationDate, DateOnly? NextCalibrationDate, string UsageStatus, int[] RecipientOperatorIds,
    bool IncludeCustodian, Guid? Version, string? Reason, string? Location = null, string? CalibrationMethod = null, string? MeasurementSpecification = null, string? Precision = null, string? Remarks = null, string? CalibrationStandard = null, string? AcceptanceCriteria = null);
public record CalibrationInput(DateOnly CalibrationDate, string Result, DateOnly? NextDueDate, string? Reason,
    int? CorrectsRecordId, Guid RequestId, Guid Version);
public sealed class CalibrationException(string message, int status = 400, string code = "VALIDATION") : Exception(message)
{ public int Status { get; } = status; public string Code { get; } = code; }

/// <summary>儀器主檔及追加式校正紀錄；所有異動與待寄工作取消在同一交易。</summary>
public sealed class InstrumentCalibrationService(AppDbContext db, TimeProvider clock)
{
    public static readonly string[] Statuses = ["Active", "InCalibration", "Inactive", "Retired"];
    public void Audit(int? id, string actor, string action, object? before, object? after, string? reason)
        => db.Add(new CalibrationAuditLog { InstrumentId=id, Actor=actor, Action=action,
            BeforeJson=JsonSerializer.Serialize(before), AfterJson=JsonSerializer.Serialize(after), Reason=reason ?? "", CreatedAt=clock.GetUtcNow().UtcDateTime });

    /// <summary>單筆與批次共用主檔欄位及有效保管人/收件人驗證，不開始交易或寫入。</summary>
    public async Task ValidateInputAsync(InstrumentInput req, CancellationToken ct)
    {
        var code = req.Code?.Trim().ToUpperInvariant() ?? "";
        if (code.Length is < 1 or > 80 || string.IsNullOrWhiteSpace(req.Name) || req.Name.Length > 200 || string.IsNullOrWhiteSpace(req.Department) || req.Department.Length > 100)
            throw new CalibrationException("請填寫儀器編號、名稱與部門，且勿超過欄位長度。");
        if (!string.IsNullOrWhiteSpace(req.Location) && req.Location.Trim().Length > 100)
            throw new CalibrationException("放置地點最多 100 字。");
        if (!string.IsNullOrWhiteSpace(req.CalibrationMethod) && req.CalibrationMethod.Trim().Length > 100)
            throw new CalibrationException("校驗方式最多 100 字。");
        if (req.MeasurementSpecification?.Trim().Length > 2000) throw new CalibrationException("量測規格最多 2000 字。");
        if (req.Precision?.Trim().Length > 2000) throw new CalibrationException("精度最多 2000 字。");
        if (req.Remarks?.Trim().Length > 2000) throw new CalibrationException("備註最多 2000 字。");
        if (req.CalibrationStandard?.Trim().Length > 2000) throw new CalibrationException("校驗規範最多 2000 字。");
        if (req.AcceptanceCriteria?.Trim().Length > 2000) throw new CalibrationException("允收標準最多 2000 字。");
        if (!Statuses.Contains(req.UsageStatus) || req.CycleMonths is < 1 or > 120)
            throw new CalibrationException("使用狀態或校正週期無效。");
        if (req.NextCalibrationDate == default(DateOnly))
            throw new CalibrationException("下次日期無效。");
        if (req.LastCalibrationDate > CalibrationRules.Today(clock.GetUtcNow()))
            throw new CalibrationException("上次校正不可為未來日期。");
        if (req.LastCalibrationDate.HasValue && req.NextCalibrationDate.HasValue && req.LastCalibrationDate >= req.NextCalibrationDate)
            throw new CalibrationException("下次日期必須晚於上次日期。");
        if (!await db.Operators.AnyAsync(x => x.Id==req.CustodianOperatorId && x.IsActive, ct))
            throw new CalibrationException("保管人必須為啟用中的 SPC 人員。");
        var ids = (req.RecipientOperatorIds ?? []).Distinct().ToArray();
        var recipients = await db.Operators.Where(x => ids.Contains(x.Id) && x.IsActive).ToListAsync(ct);
        if (recipients.Count != ids.Length || recipients.Any(x => !ValidEmail(x.Email))) throw new CalibrationException("收件人須啟用且有有效 Email。");
    }

    public async Task<CalibrationInstrument> SaveInstrumentAsync(int? id, InstrumentInput req, string actor, CancellationToken ct)
    {
        await ValidateInputAsync(req, ct);
        var code = req.Code.Trim().ToUpperInvariant();
        var ids = (req.RecipientOperatorIds ?? []).Distinct().ToArray();
        if (await db.Set<CalibrationInstrument>().AnyAsync(x => x.Code==code && x.Id != id, ct)) throw new CalibrationException("儀器編號已存在。",409,"DUPLICATE_CODE");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = id.HasValue ? await db.Set<CalibrationInstrument>().SingleOrDefaultAsync(x => x.Id==id, ct) : new CalibrationInstrument();
        if (row is null) throw new CalibrationException("儀器不存在。",404,"NOT_FOUND");
        if (id.HasValue && req.Version != row.Version) throw new CalibrationException("資料已更新，請重新載入。",409,"VERSION_CONFLICT");
        var before = id.HasValue ? JsonSerializer.SerializeToElement(row) : (JsonElement?)null;
        // Master dates may be corrected directly; the existing audit captures both values.
        // This does not register a calibration result, rewrite history, or start a new cycle.
        if (id.HasValue && row.NextCalibrationDate.HasValue && req.NextCalibrationDate != row.NextCalibrationDate && string.IsNullOrWhiteSpace(req.Reason)) throw new CalibrationException("人工調整下次日期需填理由。");
        if (id.HasValue && (row.NextCalibrationDate != req.NextCalibrationDate || row.UsageStatus != req.UsageStatus || row.RecipientOperatorIdsJson != JsonSerializer.Serialize(ids) || row.IncludeCustodian != req.IncludeCustodian || row.CustodianOperatorId != req.CustodianOperatorId))
            await CancelPendingAsync(row.Id, "InstrumentChanged", ct);
        row.Code=code; row.Name=req.Name.Trim(); row.Department=req.Department.Trim();
        row.Location=string.IsNullOrWhiteSpace(req.Location) ? null : req.Location.Trim();
        row.CalibrationMethod=string.IsNullOrWhiteSpace(req.CalibrationMethod) ? null : req.CalibrationMethod.Trim();
        row.MeasurementSpecification=string.IsNullOrWhiteSpace(req.MeasurementSpecification) ? null : req.MeasurementSpecification.Trim();
        row.Precision=string.IsNullOrWhiteSpace(req.Precision) ? null : req.Precision.Trim();
        row.Remarks=string.IsNullOrWhiteSpace(req.Remarks) ? null : req.Remarks.Trim();
        row.CalibrationStandard=string.IsNullOrWhiteSpace(req.CalibrationStandard) ? null : req.CalibrationStandard.Trim();
        row.AcceptanceCriteria=string.IsNullOrWhiteSpace(req.AcceptanceCriteria) ? null : req.AcceptanceCriteria.Trim();
        row.CustodianOperatorId=req.CustodianOperatorId;
        row.CycleMonths=req.CycleMonths; row.LastCalibrationDate=req.LastCalibrationDate; row.NextCalibrationDate=req.NextCalibrationDate;
        row.UsageStatus=req.UsageStatus; row.RecipientOperatorIdsJson=JsonSerializer.Serialize(ids); row.IncludeCustodian=req.IncludeCustodian;
        row.Version=Guid.NewGuid(); row.NotificationIssue=null;
        if (!id.HasValue) db.Add(row);
        await db.SaveChangesAsync(ct);
        Audit(row.Id, actor, id.HasValue ? "InstrumentUpdated" : "InstrumentCreated", before, row, req.Reason);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return row;
    }

    public async Task<InstrumentCalibrationRecord> RecordAsync(int id, CalibrationInput req, string actor, CancellationToken ct)
    {
        if (req.RequestId==Guid.Empty || req.CalibrationDate==default || req.CalibrationDate>CalibrationRules.Today(clock.GetUtcNow()) || req.Result is not ("Passed" or "Failed"))
            throw new CalibrationException("校正日期、結果或請求識別碼無效。");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req))));
        var existing = await db.Set<InstrumentCalibrationRecord>().AsNoTracking().SingleOrDefaultAsync(x=>x.InstrumentId==id && x.RequestId==req.RequestId, ct);
        if (existing is not null)
        {
            if (existing.RequestHash!=hash) throw new CalibrationException("相同請求識別碼的內容不同。",409,"IDEMPOTENCY_CONFLICT");
            return existing;
        }
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.Set<CalibrationInstrument>().SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new CalibrationException("儀器不存在。",404,"NOT_FOUND");
        if (row.Version!=req.Version) throw new CalibrationException("資料已更新，請重新載入。",409,"VERSION_CONFLICT");
        if (req.CorrectsRecordId.HasValue && (!await db.Set<InstrumentCalibrationRecord>().AnyAsync(x=>x.Id==req.CorrectsRecordId && x.InstrumentId==id,ct) || string.IsNullOrWhiteSpace(req.Reason)))
            throw new CalibrationException("更正需指定同儀器的原紀錄並填寫理由。");
        var before = JsonSerializer.SerializeToElement(row);
        var due = req.Result=="Passed"
            ? req.NextDueDate ?? CalibrationRules.NextDue(req.CalibrationDate,row.CycleMonths)
            : row.NextCalibrationDate ?? req.CalibrationDate;
        if (req.Result=="Passed" && due<=req.CalibrationDate) throw new CalibrationException("下次日期須晚於校正日期。");
        if (req.Result=="Passed" && due!=CalibrationRules.NextDue(req.CalibrationDate,row.CycleMonths) && string.IsNullOrWhiteSpace(req.Reason)) throw new CalibrationException("人工調整日期需填理由。");
        var record = new InstrumentCalibrationRecord { InstrumentId=id, CalibrationDate=req.CalibrationDate, Result=req.Result,
            PreviousDueDate=row.NextCalibrationDate ?? req.CalibrationDate, NextDueDate=due, Reason=req.Reason ?? "", CorrectsRecordId=req.CorrectsRecordId,
            RequestId=req.RequestId, RequestHash=hash, CreatedBy=actor, CreatedAt=clock.GetUtcNow().UtcDateTime };
        var latest = await db.Set<InstrumentCalibrationRecord>().Where(x=>x.InstrumentId==id).MaxAsync(x=>(DateOnly?)x.CalibrationDate,ct);
        var currentDate = latest ?? row.LastCalibrationDate;
        if (!currentDate.HasValue || req.CalibrationDate>=currentDate)
        {
            row.LatestResult=req.Result;
            if (req.Result=="Passed")
            {
                row.LastCalibrationDate=req.CalibrationDate; row.NextCalibrationDate=due; row.CurrentCycleId=Guid.NewGuid();
                if (row.UsageStatus=="InCalibration") row.UsageStatus="Active";
                await CancelPendingAsync(id,"CalibrationCompleted",ct);
            }
        }
        row.Version=Guid.NewGuid(); db.Add(record);
        Audit(id,actor,"CalibrationRecorded",before,new { Instrument=row, Record=record },req.Reason);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return record;
    }

    public async Task CancelPendingAsync(int id,string reason,CancellationToken ct)
    {
        var pending = await db.Set<CalibrationNotification>().Where(x=>x.InstrumentId==id && (x.State=="Pending" || x.State=="Failed")).ToListAsync(ct);
        foreach(var n in pending) { n.State="Cancelled"; n.ErrorCode=reason; n.NextAttemptAt=null; n.Version=Guid.NewGuid(); }
    }
    public static bool ValidEmail(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length<=254 && System.Net.Mail.MailAddress.TryCreate(value,out var mail) && mail.Address==value.Trim();
}
