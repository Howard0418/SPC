using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/spc")]
public class SpcController(SpcService spcService, AppDbContext db) : ControllerBase
{
    [HttpGet("chart")]
    public async Task<IActionResult> GetChart(
        [FromQuery] int ppcId,
        [FromQuery] Guid? uploadBatchId,
        [FromQuery] string? batchNo,
        [FromQuery] int? partId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var useAlternateFilter = !string.IsNullOrWhiteSpace(batchNo) || partId.HasValue;
        var start = useAlternateFilter ? startDate : startDate ?? DateTime.Today.AddMonths(-3);
        var end = useAlternateFilter ? endDate : endDate ?? DateTime.Today;

        if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
        {
            return BadRequest("量測起日不可晚於量測迄日。");
        }

        if (start.HasValue && end.HasValue && (end.Value.Date - start.Value.Date).TotalDays > 93)
        {
            return BadRequest("查詢時間範圍最多不可超過 3 個月。");
        }

        var chart = await spcService.GetInteractiveChartAsync(ppcId, uploadBatchId, start, end, batchNo, partId);
        if (chart is null) return NotFound("Chart data not found or invalid part process characteristic.");
        return Ok(chart);
    }

    [HttpGet("chart/raw-data")]
    public async Task<IActionResult> DownloadRawData(
        [FromQuery] int ppcId,
        [FromQuery] Guid? uploadBatchId,
        [FromQuery] string? batchNo,
        [FromQuery] int? partId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var useAlternateFilter = !string.IsNullOrWhiteSpace(batchNo) || partId.HasValue;
        var start = useAlternateFilter ? startDate : startDate ?? DateTime.Today.AddMonths(-3);
        var end = useAlternateFilter ? endDate : endDate ?? DateTime.Today;

        if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
        {
            return BadRequest("量測起日不可晚於量測迄日。");
        }

        if (start.HasValue && end.HasValue && (end.Value.Date - start.Value.Date).TotalDays > 93)
        {
            return BadRequest("查詢時間範圍最多不可超過 3 個月。");
        }

        var mapping = await db.PartProcessCharacteristics
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Include(x => x.Machine)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ppcId && x.IsEnabled);
        if (mapping is null) return NotFound("Part process characteristic not found.");

        var chart = await spcService.GetInteractiveChartAsync(ppcId, uploadBatchId, start, end, batchNo, partId);
        if (chart?.RawDataPoints is null) return NotFound("Raw data not found.");

        var rows = ToEnumerable(chart.RawDataPoints).ToList();
        if (rows.Count == 0) return NotFound("Raw data not found.");

        var csv = BuildRawDataCsv(rows, mapping);
        var fileName = $"SPC_RawData_{ppcId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetChartSummary(
        [FromQuery] string dimension,
        [FromQuery] string? groupType,
        [FromQuery] Guid? uploadBatchId,
        [FromQuery] string? batchNo,
        [FromQuery] int? partId,
        [FromQuery] int? processId,
        [FromQuery] string? comparisonPeriod,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var useAlternateFilter = !string.IsNullOrWhiteSpace(batchNo) || partId.HasValue;
        var start = useAlternateFilter ? startDate : startDate ?? DateTime.Today.AddMonths(-3);
        var end = useAlternateFilter ? endDate : endDate ?? DateTime.Today;

        if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
        {
            return BadRequest("量測起日不可晚於量測迄日。");
        }

        if (start.HasValue && end.HasValue && (end.Value.Date - start.Value.Date).TotalDays > 93)
        {
            return BadRequest("查詢時間範圍最多不可超過 3 個月。");
        }

        var list = await spcService.GetChartSummaryListAsync(dimension, uploadBatchId, start, end, batchNo, partId, groupType, processId, comparisonPeriod);
        return Ok(list);
    }

    [HttpPost("exclude-batch/{uploadBatchId:guid}")]
    public async Task<IActionResult> ToggleExcludeUploadBatch(Guid uploadBatchId, CancellationToken ct = default)
    {
        var batch = await db.UploadBatches.FindAsync([uploadBatchId], ct);
        if (batch is null) return NotFound("Upload batch not found.");

        batch.IsExcluded = !batch.IsExcluded;
        await db.SaveChangesAsync(ct);

        return Ok(new { batch.UploadBatchId, batch.IsExcluded });
    }

    /// <summary>
    /// 查詢目前條件下已設定的 SPC 單一圖點排除狀態。
    /// </summary>
    [HttpGet("point-exclusions")]
    public async Task<IActionResult> GetPointExclusions(
        [FromQuery] int ppcId,
        [FromQuery] string? pointScope,
        [FromQuery] long? variableMeasurementId,
        [FromQuery] long? attributeMeasurementId,
        [FromQuery] string? pointKey,
        CancellationToken ct = default)
    {
        if (ppcId <= 0) return BadRequest(new { message = "ppcId 為必填。" });

        var query = db.SpcPointExclusions
            .AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == ppcId && x.IsActive);

        if (!string.IsNullOrWhiteSpace(pointScope))
        {
            query = query.Where(x => x.PointScope == NormalizePointScope(pointScope));
        }

        if (variableMeasurementId.HasValue)
        {
            query = query.Where(x => x.VariableMeasurementId == variableMeasurementId.Value);
        }

        if (attributeMeasurementId.HasValue)
        {
            query = query.Where(x => x.AttributeMeasurementId == attributeMeasurementId.Value);
        }

        if (!string.IsNullOrWhiteSpace(pointKey))
        {
            query = query.Where(x => x.PointKey == pointKey.Trim());
        }

        var rows = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.PartProcessCharacteristicId,
                x.PointScope,
                x.VariableMeasurementId,
                x.AttributeMeasurementId,
                x.MeasurementBatchId,
                x.PointKey,
                x.State,
                x.Reason,
                x.CreatedBy,
                x.CreatedAt,
                x.UpdatedBy,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = rows, message = "已取得點位排除清單。" });
    }

    /// <summary>
    /// 查詢目前條件下已設定的 SPC 單一圖點備註。
    /// </summary>
    [HttpGet("point-remarks")]
    public async Task<IActionResult> GetPointRemarks(
        [FromQuery] int ppcId,
        [FromQuery] string? pointScope,
        [FromQuery] long? variableMeasurementId,
        [FromQuery] long? attributeMeasurementId,
        [FromQuery] string? pointKey,
        CancellationToken ct = default)
    {
        if (ppcId <= 0) return BadRequest(new { message = "ppcId 為必填。" });

        var query = db.SpcPointRemarks
            .AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == ppcId && x.IsActive);

        if (!string.IsNullOrWhiteSpace(pointScope))
            query = query.Where(x => x.PointScope == NormalizePointScope(pointScope));

        if (variableMeasurementId.HasValue)
            query = query.Where(x => x.VariableMeasurementId == variableMeasurementId.Value);

        if (attributeMeasurementId.HasValue)
            query = query.Where(x => x.AttributeMeasurementId == attributeMeasurementId.Value);

        if (!string.IsNullOrWhiteSpace(pointKey))
            query = query.Where(x => x.PointKey == pointKey.Trim());

        var rows = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.PartProcessCharacteristicId,
                x.PointScope,
                x.VariableMeasurementId,
                x.AttributeMeasurementId,
                x.MeasurementBatchId,
                x.PointKey,
                x.Remark,
                x.CreatedBy,
                x.CreatedAt,
                x.UpdatedBy,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = rows, message = "已取得點位備註清單。" });
    }

    /// <summary>
    /// 設定單一 SPC 圖點為顯示但不列入計算，或隱藏且不列入計算。
    /// </summary>
    [HttpPut("point-exclusions")]
    [Authorize(Roles = "Admin," + UserRoles.Editor)]
    public async Task<IActionResult> UpsertPointExclusion([FromBody] UpsertSpcPointExclusionReq req, CancellationToken ct = default)
    {
        var validation = await ValidatePointExclusionRequestAsync(req, ct);
        if (validation is not null) return validation;

        var pointScope = NormalizePointScope(req.PointScope);
        var state = NormalizeExclusionState(req.State);
        var pointKey = string.IsNullOrWhiteSpace(req.PointKey) ? null : req.PointKey.Trim();
        var entity = await FindActivePointExclusionAsync(
            req.PartProcessCharacteristicId,
            pointScope,
            req.VariableMeasurementId,
            req.AttributeMeasurementId,
            pointKey,
            ct);

        if (entity is null)
        {
            entity = new SpcPointExclusion
            {
                PartProcessCharacteristicId = req.PartProcessCharacteristicId,
                PointScope = pointScope,
                VariableMeasurementId = req.VariableMeasurementId,
                AttributeMeasurementId = req.AttributeMeasurementId,
                MeasurementBatchId = req.MeasurementBatchId,
                PointKey = pointKey,
                IsActive = true
            };
            db.SpcPointExclusions.Add(entity);
        }

        entity.State = state;
        entity.Reason = string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason.Trim();
        entity.UpdatedBy = CurrentUserName();
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            data = ToPointExclusionDto(entity),
            message = state == "ExcludedHidden" ? "點位已設定為隱藏且不列入計算。" : "點位已設定為顯示但不列入計算。"
        });
    }

    /// <summary>
    /// 新增、編輯或清空單一 SPC 圖點備註；備註不影響計算與點位排除狀態。
    /// </summary>
    [HttpPut("point-remarks")]
    [Authorize(Roles = "Admin," + UserRoles.Editor)]
    public async Task<IActionResult> UpsertPointRemark([FromBody] UpsertSpcPointRemarkReq req, CancellationToken ct = default)
    {
        var validation = await ValidatePointRemarkRequestAsync(req, ct);
        if (validation is not null) return validation;

        var pointScope = NormalizePointScope(req.PointScope);
        var pointKey = string.IsNullOrWhiteSpace(req.PointKey) ? null : req.PointKey.Trim();
        var entity = await FindActivePointRemarkAsync(
            req.PartProcessCharacteristicId,
            pointScope,
            req.VariableMeasurementId,
            req.AttributeMeasurementId,
            pointKey,
            ct);

        var remark = string.IsNullOrWhiteSpace(req.Remark) ? null : req.Remark.Trim();
        if (remark is null)
        {
            if (entity is not null)
            {
                entity.IsActive = false;
                entity.UpdatedBy = CurrentUserName();
                entity.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            return Ok(new { success = true, data = entity is null ? null : ToPointRemarkDto(entity), message = "點位備註已清空。" });
        }

        if (entity is null)
        {
            entity = new SpcPointRemark
            {
                PartProcessCharacteristicId = req.PartProcessCharacteristicId,
                PointScope = pointScope,
                VariableMeasurementId = req.VariableMeasurementId,
                AttributeMeasurementId = req.AttributeMeasurementId,
                MeasurementBatchId = req.MeasurementBatchId,
                PointKey = pointKey,
                IsActive = true
            };
            db.SpcPointRemarks.Add(entity);
        }

        entity.Remark = remark;
        entity.UpdatedBy = CurrentUserName();
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, data = ToPointRemarkDto(entity), message = "點位備註已儲存。" });
    }

    /// <summary>
    /// 恢復單一 SPC 圖點為正常顯示並列入計算。
    /// </summary>
    [HttpDelete("point-exclusions/{id:long}")]
    [Authorize(Roles = "Admin," + UserRoles.Editor)]
    public async Task<IActionResult> RestorePointExclusion(long id, CancellationToken ct = default)
    {
        var entity = await db.SpcPointExclusions.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (entity is null) return NotFound(new { success = false, data = (object?)null, message = "找不到有效的點位排除狀態。" });

        entity.IsActive = false;
        entity.UpdatedBy = CurrentUserName();
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, data = ToPointExclusionDto(entity), message = "點位已恢復列入計算。" });
    }

    /// <summary>
    /// 清空單一 SPC 圖點備註。
    /// </summary>
    [HttpDelete("point-remarks/{id:long}")]
    [Authorize(Roles = "Admin," + UserRoles.Editor)]
    public async Task<IActionResult> DeletePointRemark(long id, CancellationToken ct = default)
    {
        var entity = await db.SpcPointRemarks.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (entity is null) return NotFound(new { success = false, data = (object?)null, message = "找不到有效的點位備註。" });

        entity.IsActive = false;
        entity.UpdatedBy = CurrentUserName();
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, data = ToPointRemarkDto(entity), message = "點位備註已清空。" });
    }

    [HttpPut("control-limits/{ppcId:int}")]
    public async Task<IActionResult> UpdateControlLimits(int ppcId, [FromBody] UpdateControlLimitsReq req, CancellationToken ct = default)
    {
        var mapping = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.Id == ppcId && x.IsEnabled, ct);
        if (mapping is null) return NotFound("Part process characteristic not found.");

        mapping.UCL = req.Ucl;
        mapping.CL = req.Cl;
        mapping.LCL = req.Lcl;

        await db.SaveChangesAsync(ct);
        return Ok(new { mapping.Id, mapping.UCL, mapping.CL, mapping.LCL });
    }

    [HttpPost("trial-calculate")]
    public async Task<IActionResult> TrialCalculate([FromBody] TrialCalculateReq req, CancellationToken ct = default)
    {
        if (req.PartProcessCharacteristicId <= 0) return BadRequest("PartProcessCharacteristicId 為必填。");
        if (!req.StartDate.HasValue || !req.EndDate.HasValue) return BadRequest("量測起迄時間為必填。");
        if (req.StartDate.Value > req.EndDate.Value) return BadRequest("起日不可晚於迄日。");

        var result = await spcService.TrialCalculateLimitsAsync(
            req.PartProcessCharacteristicId,
            req.StartDate.Value,
            req.EndDate.Value,
            ct);

        if (result is null) return NotFound("找不到該 SPC 管制項目。");
        return Ok(result);
    }

    [HttpPost("ocap")]
    public async Task<IActionResult> SaveOcap([FromBody] SaveOcapReq req, CancellationToken ct = default)
    {
        if (req.PpcId <= 0) return BadRequest("PpcId is required.");

        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.PpcId && x.IsEnabled, ct);
        if (mapping is null) return NotFound("Part process characteristic not found.");

        VariableMeasurement? measurement = null;
        if (req.VariableMeasurementId.HasValue)
        {
            measurement = await db.VariableMeasurements.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == req.VariableMeasurementId.Value, ct);
        }

        AlertEvent? alert = null;
        if (req.AlertId.HasValue)
        {
            alert = await db.AlertEvents.FirstOrDefaultAsync(x => x.Id == req.AlertId.Value, ct);
        }
        else if (req.VariableMeasurementId.HasValue)
        {
            alert = await db.AlertEvents
                .Where(x => x.VariableMeasurementId == req.VariableMeasurementId.Value)
                .OrderByDescending(x => x.OccurredAt)
                .FirstOrDefaultAsync(ct);
        }

        var actualValue = req.ActualValue ?? measurement?.MeasuredValue;
        if (alert is null)
        {
            if (!actualValue.HasValue) return BadRequest("ActualValue or VariableMeasurementId is required when creating OCAP.");

            var isOutOfSpec = (mapping.USL.HasValue && actualValue.Value > mapping.USL.Value)
                || (mapping.LSL.HasValue && actualValue.Value < mapping.LSL.Value);

            alert = new AlertEvent
            {
                OccurredAt = measurement?.MeasuredAt ?? DateTime.UtcNow,
                PartId = measurement?.PartId ?? mapping.PartId ?? 0,
                ProcessId = measurement?.ProcessId ?? mapping.ProcessId,
                CharacteristicId = measurement?.CharacteristicId ?? mapping.CharacteristicId,
                ActualValue = actualValue,
                AlertType = isOutOfSpec ? AlertType.OutOfSpec : AlertType.OutOfControl,
                UploadBatchId = measurement?.UploadBatchId,
                VariableMeasurementId = measurement?.Id ?? req.VariableMeasurementId,
                Status = "Open",
                Message = isOutOfSpec ? "圖表監控 OCAP：量測值超出規格界限" : "圖表監控 OCAP：量測點觸發管制規則或管制界限"
            };
            db.AlertEvents.Add(alert);
        }

        alert.RootCause = JoinOcapText(req.CauseCategory, req.CauseType, req.RootCause);
        alert.CorrectiveAction = JoinOcapText(req.ActionType, null, req.CorrectiveAction);
        alert.ResponsibleUser = req.ResponsibleUser;
        alert.Status = string.IsNullOrWhiteSpace(req.Status) ? alert.Status : req.Status;
        if (string.Equals(alert.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            alert.IsAcknowledged = true;
            alert.ClosedAt ??= DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            alert.Id,
            alert.Status,
            alert.RootCause,
            alert.CorrectiveAction,
            alert.ResponsibleUser,
            alert.IsAcknowledged,
            alert.ClosedAt
        });
    }

    private static string? JoinOcapText(string? prefix, string? type, string? text)
    {
        var parts = new[] { prefix, type, text }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();
        return parts.Count == 0 ? null : string.Join(" / ", parts);
    }

    private async Task<IActionResult?> ValidatePointExclusionRequestAsync(UpsertSpcPointExclusionReq req, CancellationToken ct)
    {
        if (req.PartProcessCharacteristicId <= 0)
            return BadRequest(new { success = false, data = (object?)null, message = "PartProcessCharacteristicId 為必填。" });

        if (!IsValidExclusionState(req.State))
            return BadRequest(new { success = false, data = (object?)null, message = "State 僅允許 ExcludedVisible 或 ExcludedHidden。" });

        var pointScope = NormalizePointScope(req.PointScope);
        if (!IsValidPointScope(pointScope))
            return BadRequest(new { success = false, data = (object?)null, message = "PointScope 僅允許 VariableMeasurement、AttributeMeasurement 或 Subgroup。" });

        if (!req.VariableMeasurementId.HasValue
            && !req.AttributeMeasurementId.HasValue
            && string.IsNullOrWhiteSpace(req.PointKey))
        {
            return BadRequest(new { success = false, data = (object?)null, message = "需提供 VariableMeasurementId、AttributeMeasurementId 或 PointKey 其中之一。" });
        }

        var ppcExists = await db.PartProcessCharacteristics.AsNoTracking()
            .AnyAsync(x => x.Id == req.PartProcessCharacteristicId && x.IsEnabled, ct);
        if (!ppcExists)
            return NotFound(new { success = false, data = (object?)null, message = "找不到 SPC 管制項目。" });

        if (req.VariableMeasurementId.HasValue)
        {
            var measurement = await db.VariableMeasurements.AsNoTracking()
                .Where(x => x.Id == req.VariableMeasurementId.Value)
                .Select(x => new { x.PartProcessCharacteristicId, x.UploadBatchId })
                .FirstOrDefaultAsync(ct);
            if (measurement is null || measurement.PartProcessCharacteristicId != req.PartProcessCharacteristicId)
                return NotFound(new { success = false, data = (object?)null, message = "找不到符合管制項目的 VariableMeasurement。" });
        }

        if (req.AttributeMeasurementId.HasValue)
        {
            var measurement = await db.AttributeMeasurements.AsNoTracking()
                .Where(x => x.Id == req.AttributeMeasurementId.Value)
                .Select(x => new { x.PartProcessCharacteristicId, x.UploadBatchId })
                .FirstOrDefaultAsync(ct);
            if (measurement is null || measurement.PartProcessCharacteristicId != req.PartProcessCharacteristicId)
                return NotFound(new { success = false, data = (object?)null, message = "找不到符合管制項目的 AttributeMeasurement。" });
        }

        return null;
    }

    private async Task<IActionResult?> ValidatePointRemarkRequestAsync(UpsertSpcPointRemarkReq req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.Remark) && req.Remark.Trim().Length > 500)
            return BadRequest(new { success = false, data = (object?)null, message = "Remark 不可超過 500 字。" });

        return await ValidatePointReferenceAsync(
            req.PartProcessCharacteristicId,
            req.PointScope,
            req.VariableMeasurementId,
            req.AttributeMeasurementId,
            req.PointKey,
            ct);
    }

    private async Task<IActionResult?> ValidatePointReferenceAsync(
        int partProcessCharacteristicId,
        string? pointScopeValue,
        long? variableMeasurementId,
        long? attributeMeasurementId,
        string? pointKey,
        CancellationToken ct)
    {
        if (partProcessCharacteristicId <= 0)
            return BadRequest(new { success = false, data = (object?)null, message = "PartProcessCharacteristicId 為必填。" });

        var pointScope = NormalizePointScope(pointScopeValue);
        if (!IsValidPointScope(pointScope))
            return BadRequest(new { success = false, data = (object?)null, message = "PointScope 僅允許 VariableMeasurement、AttributeMeasurement 或 Subgroup。" });

        if (!variableMeasurementId.HasValue
            && !attributeMeasurementId.HasValue
            && string.IsNullOrWhiteSpace(pointKey))
        {
            return BadRequest(new { success = false, data = (object?)null, message = "需提供 VariableMeasurementId、AttributeMeasurementId 或 PointKey 其中之一。" });
        }

        var ppcExists = await db.PartProcessCharacteristics.AsNoTracking()
            .AnyAsync(x => x.Id == partProcessCharacteristicId && x.IsEnabled, ct);
        if (!ppcExists)
            return NotFound(new { success = false, data = (object?)null, message = "找不到 SPC 管制項目。" });

        if (variableMeasurementId.HasValue)
        {
            var measurement = await db.VariableMeasurements.AsNoTracking()
                .Where(x => x.Id == variableMeasurementId.Value)
                .Select(x => new { x.PartProcessCharacteristicId, x.UploadBatchId })
                .FirstOrDefaultAsync(ct);
            if (measurement is null || measurement.PartProcessCharacteristicId != partProcessCharacteristicId)
                return NotFound(new { success = false, data = (object?)null, message = "找不到符合管制項目的 VariableMeasurement。" });
        }

        if (attributeMeasurementId.HasValue)
        {
            var measurement = await db.AttributeMeasurements.AsNoTracking()
                .Where(x => x.Id == attributeMeasurementId.Value)
                .Select(x => new { x.PartProcessCharacteristicId, x.UploadBatchId })
                .FirstOrDefaultAsync(ct);
            if (measurement is null || measurement.PartProcessCharacteristicId != partProcessCharacteristicId)
                return NotFound(new { success = false, data = (object?)null, message = "找不到符合管制項目的 AttributeMeasurement。" });
        }

        return null;
    }

    private async Task<SpcPointExclusion?> FindActivePointExclusionAsync(
        int partProcessCharacteristicId,
        string pointScope,
        long? variableMeasurementId,
        long? attributeMeasurementId,
        string? pointKey,
        CancellationToken ct)
    {
        var query = db.SpcPointExclusions.Where(x =>
            x.PartProcessCharacteristicId == partProcessCharacteristicId
            && x.PointScope == pointScope
            && x.IsActive);

        if (variableMeasurementId.HasValue)
            return await query.FirstOrDefaultAsync(x => x.VariableMeasurementId == variableMeasurementId.Value, ct);

        if (attributeMeasurementId.HasValue)
            return await query.FirstOrDefaultAsync(x => x.AttributeMeasurementId == attributeMeasurementId.Value, ct);

        return await query.FirstOrDefaultAsync(x => x.PointKey == pointKey, ct);
    }

    private async Task<SpcPointRemark?> FindActivePointRemarkAsync(
        int partProcessCharacteristicId,
        string pointScope,
        long? variableMeasurementId,
        long? attributeMeasurementId,
        string? pointKey,
        CancellationToken ct)
    {
        var query = db.SpcPointRemarks.Where(x =>
            x.PartProcessCharacteristicId == partProcessCharacteristicId
            && x.PointScope == pointScope
            && x.IsActive);

        if (variableMeasurementId.HasValue)
            return await query.FirstOrDefaultAsync(x => x.VariableMeasurementId == variableMeasurementId.Value, ct);

        if (attributeMeasurementId.HasValue)
            return await query.FirstOrDefaultAsync(x => x.AttributeMeasurementId == attributeMeasurementId.Value, ct);

        return await query.FirstOrDefaultAsync(x => x.PointKey == pointKey, ct);
    }

    private static bool IsValidPointScope(string pointScope) =>
        pointScope is "VariableMeasurement" or "AttributeMeasurement" or "Subgroup";

    private static string NormalizePointScope(string? pointScope)
    {
        if (string.IsNullOrWhiteSpace(pointScope)) return "VariableMeasurement";
        if (string.Equals(pointScope, "variablemeasurement", StringComparison.OrdinalIgnoreCase)) return "VariableMeasurement";
        if (string.Equals(pointScope, "attributemeasurement", StringComparison.OrdinalIgnoreCase)) return "AttributeMeasurement";
        if (string.Equals(pointScope, "subgroup", StringComparison.OrdinalIgnoreCase)) return "Subgroup";
        return string.Empty;
    }

    private static bool IsValidExclusionState(string? state) =>
        NormalizeExclusionState(state) is "ExcludedVisible" or "ExcludedHidden";

    private static string NormalizeExclusionState(string? state) =>
        string.Equals(state, "ExcludedHidden", StringComparison.OrdinalIgnoreCase)
            ? "ExcludedHidden"
            : string.Equals(state, "ExcludedVisible", StringComparison.OrdinalIgnoreCase)
                ? "ExcludedVisible"
                : string.Empty;

    private string CurrentUserName() =>
        User.Identity?.Name
        ?? User.FindFirst("name")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? "System";

    private static object ToPointExclusionDto(SpcPointExclusion entity) => new
    {
        entity.Id,
        entity.PartProcessCharacteristicId,
        entity.PointScope,
        entity.VariableMeasurementId,
        entity.AttributeMeasurementId,
        entity.MeasurementBatchId,
        entity.PointKey,
        entity.State,
        entity.Reason,
        entity.IsActive,
        entity.CreatedBy,
        entity.CreatedAt,
        entity.UpdatedBy,
        entity.UpdatedAt
    };

    private static object ToPointRemarkDto(SpcPointRemark entity) => new
    {
        entity.Id,
        entity.PartProcessCharacteristicId,
        entity.PointScope,
        entity.VariableMeasurementId,
        entity.AttributeMeasurementId,
        entity.MeasurementBatchId,
        entity.PointKey,
        entity.Remark,
        entity.IsActive,
        entity.CreatedBy,
        entity.CreatedAt,
        entity.UpdatedBy,
        entity.UpdatedAt
    };

    private static IEnumerable<object> ToEnumerable(object source) =>
        source is System.Collections.IEnumerable items
            ? items.Cast<object>()
            : [];

    private static string BuildRawDataCsv(IEnumerable<object> rows, PartProcessCharacteristic mapping)
    {
        var sb = new StringBuilder();
        AppendCsvRow(sb,
            "PartProcessCharacteristicId",
            "Process",
            "Characteristic",
            "Machine",
            "MeasuredAt",
            "PortalDailyDate",
            "SamplingPhase",
            "SamplingStage",
            "Value",
            "LotNo",
            "SerialNo",
            "Operator",
            "LineId",
            "TankId",
            "SlotId",
            "SideCode",
            "IsExcluded",
            "IsOutOfSpec",
            "IsOutOfControl",
            "VariableMeasurementId");

        foreach (var row in rows)
        {
            AppendCsvRow(sb,
                mapping.Id,
                mapping.Process?.ProcessName ?? mapping.Process?.ProcessCode ?? "",
                mapping.Characteristic?.CharacteristicName ?? mapping.Characteristic?.CharacteristicCode ?? "",
                mapping.Machine?.MachineName ?? mapping.Machine?.MachineCode ?? "",
                FormatValue(ReadProperty(row, "MeasuredAt")),
                FormatValue(ReadProperty(row, "PortalDailyDate")),
                ReadProperty(row, "SamplingPhase") ?? "",
                ReadProperty(row, "SamplingStage") ?? "",
                FormatValue(ReadProperty(row, "Value")),
                ReadProperty(row, "LotNo") ?? "",
                ReadProperty(row, "SerialNo") ?? "",
                ReadProperty(row, "Operator") ?? "",
                ReadProperty(row, "LineId") ?? "",
                ReadProperty(row, "TankId") ?? "",
                ReadProperty(row, "SlotId") ?? "",
                ReadProperty(row, "SideCode") ?? "",
                ReadProperty(row, "IsExcluded") ?? "",
                ReadProperty(row, "IsOutOfSpec") ?? "",
                ReadProperty(row, "IsOutOfControl") ?? "",
                ReadProperty(row, "VariableMeasurementId") ?? "");
        }

        return sb.ToString();
    }

    private static string? ReadProperty(object source, string name)
    {
        var prop = source.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(source) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
    }

    private static string FormatValue(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : value ?? "";

    private static void AppendCsvRow(StringBuilder sb, params object?[] values)
    {
        sb.AppendLine(string.Join(",", values.Select(value =>
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        })));
    }
}

public record TrialCalculateReq(int PartProcessCharacteristicId, DateTime? StartDate, DateTime? EndDate);
public record UpdateControlLimitsReq(double? Ucl, double? Cl, double? Lcl);
public record UpsertSpcPointExclusionReq(
    int PartProcessCharacteristicId,
    string PointScope,
    long? VariableMeasurementId,
    long? AttributeMeasurementId,
    int? MeasurementBatchId,
    string? PointKey,
    string State,
    string? Reason);
public record UpsertSpcPointRemarkReq(
    int PartProcessCharacteristicId,
    string PointScope,
    long? VariableMeasurementId,
    long? AttributeMeasurementId,
    int? MeasurementBatchId,
    string? PointKey,
    string? Remark);
public record SaveOcapReq(
    int PpcId,
    long? VariableMeasurementId,
    int? AlertId,
    double? ActualValue,
    string? Status,
    string? CauseCategory,
    string? CauseType,
    string? ActionType,
    string? RootCause,
    string? CorrectiveAction,
    string? ResponsibleUser);

public class ChartSummaryDto
{
    public int PartProcessCharacteristicId { get; set; }
    public string ControlCategory { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public string ChartKind { get; set; } = string.Empty;
    public string LineOrProcessName { get; set; } = string.Empty;
    public string SlotName { get; set; } = string.Empty;
    public string ChartName { get; set; } = string.Empty;
    public string ChartType { get; set; } = string.Empty;
    public double? Usl { get; set; }
    public double? Lsl { get; set; }
    public double? Ucl { get; set; }
    public double? Lcl { get; set; }
    public string LimitCalculationMethod { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int OosCount { get; set; }
    public double OosPercentage { get; set; }
    public int PreviousMonthOosCount { get; set; }
    public double PreviousMonthOosPercentage { get; set; }
    public int OocCount { get; set; }
    public double OocPercentage { get; set; }
    public double? Ca { get; set; }
    public double? Pp { get; set; }
    public double? Ppk { get; set; }
    public double? PreviousMonthPpk { get; set; }
    public string ResponsibleUser { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/formulas")]
public class FormulaController(AppDbContext db, FormulaEngineService formulaEngine) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.FormulaDefinitions.OrderBy(x => x.FormulaCode).ToListAsync());

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Domain.Entities.FormulaDefinition req)
    {
        var x = await db.FormulaDefinitions.FindAsync(id);
        if (x is null) return NotFound();
        x.DisplayName = req.DisplayName;
        x.Expression = req.Expression;
        x.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate(EvaluateFormulaReq req)
    {
        var formula = await db.FormulaDefinitions.FirstOrDefaultAsync(x => x.FormulaCode == req.FormulaCode && x.IsActive);
        if (formula is null) return NotFound($"Formula '{req.FormulaCode}' not found or inactive.");
        var value = formulaEngine.EvaluateByExpression(formula.Expression, req.Values, req.Usl, req.Lsl);
        return Ok(new { req.FormulaCode, formula.Expression, value });
    }

    public record EvaluateFormulaReq(string FormulaCode, List<double> Values, double? Usl, double? Lsl);
}

[ApiController]
[Route("api/v1/chemical-f-table")]
public class ChemicalFTableController(ChemicalFTableService fTableService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var detail = await fTableService.GetActiveDetailAsync(ct);
        return detail is null ? NotFound(new { message = "尚未啟用 F 表版本。" }) : Ok(detail);
    }

    [HttpPost("sync-default")]
    public async Task<IActionResult> SyncDefault([FromBody] ChemicalFTableSyncRequest req, CancellationToken ct)
    {
        var result = await fTableService.SyncDefaultAsync(req.Apply, req.VersionCode, req.DisplayName, ct);
        return Ok(result);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ChemicalFTableApplyRequest req, CancellationToken ct)
    {
        if (req.Cells is null || req.Cells.Count == 0)
            return BadRequest(new { message = "F 表儲存格不可空白。" });

        try
        {
            var cells = req.Cells
                .Select(x => new ChemicalFTableSeedCell(x.CellAddress, x.StandardSolution ?? "", x.NumericValue))
                .ToList();
            var result = await fTableService.SyncCellsAsync(
                cells,
                req.Apply,
                req.VersionCode,
                req.DisplayName,
                "SPC_F_TABLE_MAINTENANCE",
                ct,
                User?.Identity?.Name,
                req.Reason);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("impact")]
    public async Task<IActionResult> GetImpact([FromQuery] string cellAddress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cellAddress))
            return BadRequest(new { message = "請指定 F 表儲存格。" });

        try
        {
            var result = await fTableService.GetImpactAsync(cellAddress, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] ChemicalFTableEvaluateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Expression))
            return BadRequest(new { message = "公式不可空白。" });

        try
        {
            var result = await fTableService.EvaluateChemicalFormulaAsync(req.Expression, req.Variables, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("references")]
    public IActionResult ExtractReferences([FromBody] ChemicalFTableReferenceRequest req)
    {
        var refs = ChemicalFTableService.ExtractCellReferences(req.Expression);
        return Ok(new { references = refs });
    }

    [HttpGet("versions")]
    public async Task<IActionResult> Versions([FromQuery] int take, CancellationToken ct)
    {
        var result = await fTableService.GetVersionHistoryAsync(take <= 0 ? 50 : take, ct);
        return Ok(result);
    }

    [HttpGet("versions/{id:long}")]
    public async Task<IActionResult> VersionDetail(long id, CancellationToken ct)
    {
        var result = await fTableService.GetVersionHistoryDetailAsync(id, ct);
        return result is null ? NotFound(new { message = "找不到指定的 F 表版本記錄。" }) : Ok(result);
    }

    [HttpPost("versions/{id:long}/restore")]
    public async Task<IActionResult> RestoreVersion(long id, [FromBody] ChemicalFTableRestoreRequest req, CancellationToken ct)
    {
        try
        {
            var result = await fTableService.RestoreVersionHistoryAsync(id, User?.Identity?.Name, req.Reason, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public record ChemicalFTableSyncRequest(bool Apply, string? VersionCode, string? DisplayName, string? Reason = null);
    public record ChemicalFTableApplyRequest(bool Apply, string? VersionCode, string? DisplayName, List<ChemicalFTableApplyCell> Cells, string? Reason = null);
    public record ChemicalFTableApplyCell(string CellAddress, string? StandardSolution, decimal NumericValue);
    public record ChemicalFTableEvaluateRequest(string Expression, Dictionary<string, decimal>? Variables);
    public record ChemicalFTableReferenceRequest(string? Expression);
    public record ChemicalFTableRestoreRequest(string? Reason);
}
