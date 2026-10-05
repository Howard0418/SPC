using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/equipment-points")]
public class EquipmentPointsController(AppDbContext db, ChameleonStatusService chameleon) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string sourceId, [FromQuery] string equipmentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(equipmentId)) return BadRequest(new { message = "sourceId 與 equipmentId 為必填。" });
        var read = await chameleon.GetLatestPointsAsync(sourceId, equipmentId, cancellationToken);
        var mappings = await db.EquipmentPointMappings.AsNoTracking().Where(x => x.SourceId == read.SourceId && x.EquipmentId == read.EquipmentId).ToDictionaryAsync(x => x.ChannelId, cancellationToken);
        return Ok(new { read.SourceId, read.SourceName, read.EquipmentId, read.LastUpdatedAt, points = read.Points.Select(x => new { x.ChannelId, x.ValueType, x.Value, x.ChannelName, x.Category, displayName = mappings.GetValueOrDefault(x.ChannelId)?.DisplayName ?? x.ChannelName, unit = mappings.GetValueOrDefault(x.ChannelId)?.Unit, isEnabled = mappings.GetValueOrDefault(x.ChannelId)?.IsEnabled ?? true, isPinned = mappings.GetValueOrDefault(x.ChannelId)?.IsPinned ?? false, warningLow = mappings.GetValueOrDefault(x.ChannelId)?.WarningLow, warningHigh = mappings.GetValueOrDefault(x.ChannelId)?.WarningHigh, sortOrder = mappings.GetValueOrDefault(x.ChannelId)?.SortOrder ?? 0, statusRole = mappings.GetValueOrDefault(x.ChannelId)?.StatusRole, activeWhen = mappings.GetValueOrDefault(x.ChannelId)?.ActiveWhen ?? "NONZERO" }) });
    }

    [HttpPut("mapping")]
    public async Task<IActionResult> UpsertMapping(EquipmentPointMapping request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceId) || string.IsNullOrWhiteSpace(request.EquipmentId) || string.IsNullOrWhiteSpace(request.ChannelId)) return BadRequest(new { message = "來源、設備與點位 ID 為必填。" });
        var entity = await db.EquipmentPointMappings.FirstOrDefaultAsync(x => x.SourceId == request.SourceId && x.EquipmentId == request.EquipmentId && x.ChannelId == request.ChannelId, cancellationToken);
        if (entity is null) { entity = new EquipmentPointMapping { SourceId = request.SourceId.Trim(), EquipmentId = request.EquipmentId.Trim(), ChannelId = request.ChannelId.Trim() }; db.EquipmentPointMappings.Add(entity); }
        var statusRole = request.StatusRole?.Trim().ToUpperInvariant();
        if (statusRole is not null and not "RUNNING" and not "ALARM") return BadRequest(new { message = "狀態用途只允許 RUNNING 或 ALARM。" });
        var activeWhen = request.ActiveWhen?.Trim().ToUpperInvariant() ?? "NONZERO";
        if (activeWhen is not "NONZERO" and not "ZERO") return BadRequest(new { message = "訊號成立條件只允許 NONZERO 或 ZERO。" });
        if (statusRole == "RUNNING")
        {
            var previous = await db.EquipmentPointMappings.Where(x => x.SourceId == request.SourceId && x.EquipmentId == request.EquipmentId && x.StatusRole == "RUNNING" && x.ChannelId != request.ChannelId).ToListAsync(cancellationToken);
            previous.ForEach(x => x.StatusRole = null);
        }
        entity.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(); entity.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim(); entity.IsEnabled = request.IsEnabled; entity.IsPinned = request.IsPinned; entity.WarningLow = request.WarningLow; entity.WarningHigh = request.WarningHigh; entity.SortOrder = request.SortOrder; entity.StatusRole = statusRole; entity.ActiveWhen = statusRole is null ? null : activeWhen;
        await db.SaveChangesAsync(cancellationToken); return Ok(entity);
    }
}
