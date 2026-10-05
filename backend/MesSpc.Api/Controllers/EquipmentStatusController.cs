using System.Globalization;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

/// <summary>
/// Provides a read-only, browser-safe summary of Chameleon equipment freshness.
/// </summary>
[ApiController]
[Route("api/equipment-status")]
[Route("api/v1/equipment-status")]
public class EquipmentStatusController(ChameleonStatusService chameleonStatusService, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await chameleonStatusService.GetStatusAsync(cancellationToken);
        var mappings = await db.EquipmentPointMappings.AsNoTracking()
            .Where(x => x.IsEnabled && (x.StatusRole == "RUNNING" || x.StatusRole == "ALARM"))
            .ToListAsync(cancellationToken);
        var groups = mappings.GroupBy(x => (x.SourceId, x.EquipmentId));
        var runtimeEntries = await Task.WhenAll(groups.Select(async group =>
        {
            try
            {
                var values = await chameleonStatusService.GetLatestValuesAsync(group.Key.SourceId, group.Key.EquipmentId, cancellationToken);
                var running = group.FirstOrDefault(x => x.StatusRole == "RUNNING");
                var alarms = group.Where(x => x.StatusRole == "ALARM").ToList();
                string? runningValue = null;
                var operatingState = running is null ? "unconfigured" : values.TryGetValue(running.ChannelId, out runningValue) && IsActive(runningValue, running.ActiveWhen) ? "running" : "stopped";
                var activeAlarms = alarms
                    .Where(x => values.TryGetValue(x.ChannelId, out var value) && IsActive(value, x.ActiveWhen))
                    .Select(x => new EquipmentAlarmInfo(x.ChannelId, x.DisplayName ?? x.ChannelId, values[x.ChannelId]))
                    .OrderBy(x => x.Name)
                    .ToList();
                return (group.Key, Info: new EquipmentRuntimeInfo(operatingState, running?.ChannelId, running?.DisplayName ?? running?.ChannelId, runningValue, alarms.Count, activeAlarms));
            }
            catch
            {
                return (group.Key, Info: new EquipmentRuntimeInfo("unknown", null, null, null, group.Count(x => x.StatusRole == "ALARM"), []));
            }
        }));
        var runtime = runtimeEntries.ToDictionary(x => x.Key, x => x.Info);
        return Ok(new
        {
            result.IsConfigured,
            result.FreshnessThresholdSeconds,
            result.RefreshedAt,
            result.Message,
            sources = result.Sources.Select(source => new
            {
                source.SourceId,
                source.DisplayName,
                source.Status,
                source.Message,
                devices = source.Devices.Select(device =>
                {
                    var info = runtime.GetValueOrDefault((device.SourceId, device.EquipmentId)) ?? new EquipmentRuntimeInfo("unconfigured", null, null, null, 0, []);
                    return new { device.SourceId, device.SourceName, device.EquipmentId, device.Status, device.LastUpdatedAt, device.AgeSeconds, device.ChannelCount, device.Message, info.OperatingState, info.OperatingChannelId, info.OperatingPointName, info.OperatingRawValue, activeAlarmCount = info.ActiveAlarms.Count, configuredAlarmCount = info.AlarmCount, info.ActiveAlarms };
                })
            })
        });
    }

    private static bool IsActive(string? value, string? activeWhen)
    {
        var nonzero = bool.TryParse(value, out var boolean) ? boolean :
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && Math.Abs(number) > double.Epsilon;
        return string.Equals(activeWhen, "ZERO", StringComparison.OrdinalIgnoreCase) ? !nonzero : nonzero;
    }

    private sealed record EquipmentRuntimeInfo(string OperatingState, string? OperatingChannelId, string? OperatingPointName, string? OperatingRawValue, int AlarmCount, IReadOnlyList<EquipmentAlarmInfo> ActiveAlarms);
    private sealed record EquipmentAlarmInfo(string ChannelId, string Name, string RawValue);
}
