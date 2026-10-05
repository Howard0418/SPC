using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/particles")]
public sealed class ParticlesController(ParticleQueryService service) : ControllerBase
{
    /// <summary>依日期、位置、粒徑、儀器與上傳批次分頁查詢 Particle 原始量測。</summary>
    [HttpGet("measurements")]
    public async Task<IActionResult> Measurements([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        [FromQuery] string[]? locations, [FromQuery] decimal[]? particleSizes, [FromQuery] string? deviceCode,
        [FromQuery] Guid? uploadBatchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100,
        [FromQuery] string sort = "asc", CancellationToken ct = default)
    {
        if (!TryValidate(from, to, locations ?? [], particleSizes ?? [], out var error)) return BadRequest(new { message = error });
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 500);
        return Ok(await service.GetMeasurementsAsync(from.UtcDateTime, to.UtcDateTime, locations ?? [], particleSizes ?? [], Normalize(deviceCode), uploadBatchId, page, pageSize, sort != "desc", ct));
    }

    /// <summary>查詢單一 Location、ParticleSize 與 DeviceCode 的原始計數趨勢。</summary>
    [HttpGet("trend")]
    public async Task<IActionResult> Trend([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        [FromQuery] string location, [FromQuery] decimal particleSize, [FromQuery] string? deviceCode, CancellationToken ct)
    {
        if (!TryValidate(from, to, [location], [particleSize], out var error)) return BadRequest(new { message = error });
        try { return Ok(await service.GetTrendAsync(from.UtcDateTime, to.UtcDateTime, location.ToUpperInvariant(), particleSize, Normalize(deviceCode), ct)); }
        catch (InvalidOperationException ex) when (ex.Message == "TOO_MANY_POINTS") { return UnprocessableEntity(new { code = ex.Message, message = "資料點超過 10,000，請縮小日期範圍。" }); }
    }

    /// <summary>比較同一量測事件、同一粒徑的 R1 至 R9 Particle Count；重測歧義回 409 與候選事件。</summary>
    [HttpGet("location-comparison")]
    public async Task<IActionResult> LocationComparison([FromQuery] DateTimeOffset measurementTime, [FromQuery] decimal particleSize,
        [FromQuery] string? deviceCode, [FromQuery] Guid? uploadBatchId, [FromQuery] string? sourceSheet, [FromQuery] int? sourceRow, CancellationToken ct)
    {
        if (!AllowedSizes.Contains(particleSize)) return BadRequest(new { message = "ParticleSize 僅支援 0.5、1、5、10。" });
        var result = await service.GetLocationComparisonAsync(measurementTime.UtcDateTime, particleSize, Normalize(deviceCode), uploadBatchId, Normalize(sourceSheet), sourceRow, ct);
        return result.IsAmbiguous ? Conflict(new { code = "COMPARISON_AMBIGUOUS", result.Candidates }) : Ok(result);
    }

    /// <summary>以單一 Location、ParticleSize 與 DeviceCode sequence 計算 Particle C-chart。</summary>
    [HttpGet("spc")]
    public async Task<IActionResult> Spc([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        [FromQuery] string location, [FromQuery] decimal particleSize, [FromQuery] string? deviceCode,
        [FromQuery] string chartType = "C", CancellationToken ct = default)
    {
        if (!TryValidate(from, to, [location], [particleSize], out var error)) return BadRequest(new { message = error });
        try
        {
            return Ok(await service.GetSpcAsync(from.UtcDateTime, to.UtcDateTime, location.ToUpperInvariant(), particleSize, Normalize(deviceCode), chartType, ct));
        }
        catch (InvalidOperationException ex) when (ex.Message == "TOO_MANY_POINTS")
        {
            return UnprocessableEntity(new { code = ex.Message, message = "資料點超過 10,000，請縮小日期範圍。" });
        }
        catch (ArgumentOutOfRangeException ex) when (ex.Message.Contains("COUNT_PRECISION_EXCEEDED", StringComparison.Ordinal))
        {
            return UnprocessableEntity(new { code = "COUNT_PRECISION_EXCEEDED", message = "Count 超過 C-chart 可精確計算範圍。" });
        }
        catch (ArgumentException ex) when (ex.Message.Contains("SAMPLING_VOLUME_REQUIRED", StringComparison.Ordinal))
        {
            return UnprocessableEntity(new { code = "SAMPLING_VOLUME_REQUIRED", message = "U-chart 要求每筆 SamplingVolume 與單位皆有效。" });
        }
        catch (ArgumentException ex) when (ex.Message.Contains("SAMPLING_VOLUME_UNIT_MISMATCH", StringComparison.Ordinal))
        {
            return UnprocessableEntity(new { code = "SAMPLING_VOLUME_UNIT_MISMATCH", message = "U-chart 查詢範圍內的 SamplingVolume 單位不一致。" });
        }
        catch (ArgumentException ex) when (ex.Message.Contains("CHART_TYPE_UNSUPPORTED", StringComparison.Ordinal))
        {
            return BadRequest(new { code = "CHART_TYPE_UNSUPPORTED", message = "chartType 僅支援 C 或 U。" });
        }
    }

    private static readonly HashSet<decimal> AllowedSizes = [0.5m, 1m, 5m, 10m];
    private static bool TryValidate(DateTimeOffset from, DateTimeOffset to, IReadOnlyCollection<string> locations, IReadOnlyCollection<decimal> sizes, out string? error)
    {
        if (from >= to) { error = "from 必須早於 to。"; return false; }
        if (locations.Any(location => !Enumerable.Range(1, 9).Select(number => $"R{number}").Contains(location.ToUpperInvariant()))) { error = "Location 僅支援 R1 至 R9。"; return false; }
        if (sizes.Any(size => !AllowedSizes.Contains(size))) { error = "ParticleSize 僅支援 0.5、1、5、10。"; return false; }
        error = null; return true;
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
