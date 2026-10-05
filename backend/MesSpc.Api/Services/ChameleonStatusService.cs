using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

/// <summary>
/// Reads the latest equipment snapshots from Chameleon. This service is intentionally
/// read-only: it never writes to PLCs or calls Chameleon mutation endpoints.
/// </summary>
public class ChameleonStatusService(
    HttpClient httpClient,
    IConfiguration configuration,
    AppDbContext db,
    IMemoryCache cache,
    ILogger<ChameleonStatusService> logger)
{
    private const int DefaultFreshnessThresholdSeconds = 90;
    private const string StatusCacheKey = "chameleon-equipment-status";
    private static readonly string[] ConfigurationProtocols =
        ["fins", "melsec", "mewtocol", "modbus", "step7", "toyopuc", "virtual"];

    public async Task<ChameleonStatusResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(StatusCacheKey, out ChameleonStatusResult? cached) && cached is not null)
            return cached;

        var enabledSources = await GetConfiguredSourcesAsync(cancellationToken);
        if (enabledSources.Count == 0)
            return ChameleonStatusResult.NotConfigured();

        var threshold = configuration.GetValue("Chameleon:FreshnessThresholdSeconds", DefaultFreshnessThresholdSeconds);
        threshold = Math.Clamp(threshold, 15, 600);
        var sourceStatuses = await Task.WhenAll(enabledSources.Select(source => GetSourceStatusAsync(source, threshold, cancellationToken)));
        var result = new ChameleonStatusResult(true, threshold, DateTimeOffset.UtcNow, sourceStatuses);
        cache.Set(StatusCacheKey, result, TimeSpan.FromSeconds(10));
        return result;
    }

    /// <summary>Gets the latest raw point values for one configured equipment.</summary>
    public async Task<ChameleonPointReadResult> GetLatestPointsAsync(string sourceId, string equipmentId, CancellationToken cancellationToken)
    {
        var source = (await GetConfiguredSourcesAsync(cancellationToken)).FirstOrDefault(x => string.Equals(x.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));
        if (source is null) throw new KeyNotFoundException("找不到或未啟用指定的 Chameleon 資料來源。");
        using var response = await httpClient.GetAsync($"{source.BaseUrl}/reading/equipments/{Uri.EscapeDataString(equipmentId)}/channels/latest-data", cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var at = DateTimeOffset.FromUnixTimeMilliseconds(root.GetProperty("at").GetInt64());
        var definitions = await GetPointDefinitionsAsync(source, equipmentId, cancellationToken);
        var points = root.GetProperty("reading").EnumerateObject().Select(x => new ChameleonPointValue(
            x.Name,
            x.Value.ValueKind switch { JsonValueKind.Number => "number", JsonValueKind.String => "string", JsonValueKind.True or JsonValueKind.False => "boolean", _ => "unknown" },
            x.Value.ToString(), definitions.GetValueOrDefault(x.Name)?.Name, definitions.GetValueOrDefault(x.Name)?.Category)).OrderBy(x => x.ChannelId).ToList();
        return new ChameleonPointReadResult(source.SourceId, source.DisplayName, equipmentId, at, points);
    }

    public async Task<Dictionary<string, string>> GetLatestValuesAsync(string sourceId, string equipmentId, CancellationToken cancellationToken)
    {
        var source = (await GetConfiguredSourcesAsync(cancellationToken)).FirstOrDefault(x => string.Equals(x.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));
        if (source is null) return [];
        using var response = await httpClient.GetAsync($"{source.BaseUrl}/reading/equipments/{Uri.EscapeDataString(equipmentId)}/channels/latest-data", cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.TryGetProperty("reading", out var reading) && reading.ValueKind == JsonValueKind.Object
            ? reading.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString())
            : [];
    }

    private async Task<Dictionary<string, ChameleonPointDefinition>> GetPointDefinitionsAsync(ChameleonSourceOptions source, string equipmentId, CancellationToken cancellationToken)
    {
        foreach (var protocol in ConfigurationProtocols)
        {
            using var equipmentResponse = await httpClient.GetAsync($"{source.BaseUrl}/config/{protocol}/equipments", cancellationToken);
            if (!equipmentResponse.IsSuccessStatusCode) continue;
            await using var equipmentStream = await equipmentResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var equipmentDocument = await JsonDocument.ParseAsync(equipmentStream, cancellationToken: cancellationToken);
            var templateId = FindTemplateId(equipmentDocument.RootElement, equipmentId);
            if (string.IsNullOrWhiteSpace(templateId)) continue;

            using var templateResponse = await httpClient.GetAsync($"{source.BaseUrl}/config/{protocol}/templates", cancellationToken);
            if (!templateResponse.IsSuccessStatusCode) return [];
            await using var templateStream = await templateResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var templateDocument = await JsonDocument.ParseAsync(templateStream, cancellationToken: cancellationToken);
            var template = templateDocument.RootElement.EnumerateArray().FirstOrDefault(x =>
                x.TryGetProperty("templateId", out var id) &&
                string.Equals(id.GetString(), templateId, StringComparison.OrdinalIgnoreCase));
            if (template.ValueKind != JsonValueKind.Object || !template.TryGetProperty("channels", out var channels)) return [];
            return channels.EnumerateArray()
                .Where(x => x.TryGetProperty("channelId", out var id) && !string.IsNullOrWhiteSpace(id.GetString()))
                .ToDictionary(
                    x => x.GetProperty("channelId").GetString()!,
                    x => new ChameleonPointDefinition(
                        x.TryGetProperty("channelName", out var name) ? name.GetString() : null,
                        x.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array && categories.GetArrayLength() > 0 ? categories[0].GetString() : null));
        }

        return [];
    }

    private static string? FindTemplateId(JsonElement configurations, string equipmentId)
    {
        if (configurations.ValueKind != JsonValueKind.Array) return null;
        foreach (var configuration in configurations.EnumerateArray())
        {
            if (configuration.TryGetProperty("equipment", out var equipment) && EquipmentMatches(equipment, equipmentId))
                return equipment.TryGetProperty("templateId", out var templateId) ? templateId.GetString() : null;

            if (!configuration.TryGetProperty("equipmentList", out var equipmentList) || equipmentList.ValueKind != JsonValueKind.Array) continue;
            foreach (var candidate in equipmentList.EnumerateArray())
                if (EquipmentMatches(candidate, equipmentId))
                    return candidate.TryGetProperty("templateId", out var templateId) ? templateId.GetString() : null;
        }

        return null;
    }

    private static bool EquipmentMatches(JsonElement equipment, string equipmentId) =>
        equipment.ValueKind == JsonValueKind.Object &&
        equipment.TryGetProperty("equipmentId", out var id) &&
        string.Equals(id.GetString(), equipmentId, StringComparison.OrdinalIgnoreCase);

    public void InvalidateCache() => cache.Remove(StatusCacheKey);

    private async Task<List<ChameleonSourceOptions>> GetConfiguredSourcesAsync(CancellationToken cancellationToken)
    {
        var databaseSources = await db.ChameleonSourceSettings.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new ChameleonSourceOptions { SourceId = x.SourceId, DisplayName = x.DisplayName, BaseUrl = x.BaseUrl, Enabled = x.IsEnabled })
            .ToListAsync(cancellationToken);
        if (databaseSources.Count > 0) return NormalizeSources(databaseSources);

        var sources = configuration.GetSection("Chameleon:Sources").Get<List<ChameleonSourceOptions>>() ?? [];
        if (sources.Count == 0 && !string.IsNullOrWhiteSpace(configuration["Chameleon:BaseUrl"])) sources.Add(new ChameleonSourceOptions { SourceId = "CHA-111", DisplayName = "Chameleon 111", BaseUrl = configuration["Chameleon:BaseUrl"]!, Enabled = true });
        return NormalizeSources(sources);
    }

    private static List<ChameleonSourceOptions> NormalizeSources(IEnumerable<ChameleonSourceOptions> sources) => sources
        .Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.SourceId) && !string.IsNullOrWhiteSpace(x.BaseUrl))
        .Select(x => x with { SourceId = x.SourceId.Trim(), DisplayName = string.IsNullOrWhiteSpace(x.DisplayName) ? x.SourceId.Trim() : x.DisplayName.Trim(), BaseUrl = x.BaseUrl.Trim().TrimEnd('/') })
        .ToList();

    private async Task<ChameleonSourceStatus> GetSourceStatusAsync(ChameleonSourceOptions source, int thresholdSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var equipmentIds = await GetEquipmentIdsAsync(source.BaseUrl, cancellationToken);
            var devices = await Task.WhenAll(equipmentIds.Take(20)
                .Select(id => GetDeviceStatusAsync(source, id, thresholdSeconds, cancellationToken)));
            return new ChameleonSourceStatus(source.SourceId, source.DisplayName, "online", devices, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Unable to obtain Chameleon source status from {BaseUrl}", source.BaseUrl);
            return new ChameleonSourceStatus(source.SourceId, source.DisplayName, "unavailable", [], "無法連線至此 Chameleon 資料來源。");
        }
    }

    private async Task<List<string>> GetEquipmentIdsAsync(string baseUrl, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"{baseUrl}/reading/equipments", cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<string>>(stream, cancellationToken: cancellationToken) ?? [];
    }

    private async Task<ChameleonDeviceStatus> GetDeviceStatusAsync(
        ChameleonSourceOptions source, string equipmentId, int thresholdSeconds, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"{source.BaseUrl}/reading/equipments/{Uri.EscapeDataString(equipmentId)}/channels/latest-data", cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = document.RootElement;
            if (!root.TryGetProperty("at", out var atElement) || !atElement.TryGetInt64(out var atMilliseconds))
                return ChameleonDeviceStatus.Unavailable(source.SourceId, source.DisplayName, equipmentId, "Chameleon 未回傳資料時間。");

            var lastUpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(atMilliseconds);
            var ageSeconds = Math.Max(0, (DateTimeOffset.UtcNow - lastUpdatedAt).TotalSeconds);
            var channelCount = root.TryGetProperty("reading", out var reading) && reading.ValueKind == JsonValueKind.Object
                ? reading.EnumerateObject().Count()
                : 0;

            return new ChameleonDeviceStatus(
                source.SourceId,
                source.DisplayName,
                equipmentId,
                ageSeconds <= thresholdSeconds ? "online" : "stale",
                lastUpdatedAt,
                Math.Round(ageSeconds, 0),
                channelCount,
                null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or ArgumentOutOfRangeException)
        {
            logger.LogWarning(ex, "Unable to obtain Chameleon latest data for equipment {EquipmentId}", equipmentId);
            return ChameleonDeviceStatus.Unavailable(source.SourceId, source.DisplayName, equipmentId, "無法取得最新資料。");
        }
    }
}

public record ChameleonSourceOptions
{
    public string SourceId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string BaseUrl { get; init; } = "";
    public bool Enabled { get; init; } = true;
}

public record ChameleonStatusResult(
    bool IsConfigured,
    int FreshnessThresholdSeconds,
    DateTimeOffset RefreshedAt,
    IReadOnlyList<ChameleonSourceStatus> Sources,
    string? Message = null)
{
    public static ChameleonStatusResult NotConfigured() => new(
        false, 90, DateTimeOffset.UtcNow, [],
        "尚未設定 Chameleon API 位址。");
}

public record ChameleonSourceStatus(
    string SourceId,
    string DisplayName,
    string Status,
    IReadOnlyList<ChameleonDeviceStatus> Devices,
    string? Message);

public record ChameleonDeviceStatus(
    string SourceId,
    string SourceName,
    string EquipmentId,
    string Status,
    DateTimeOffset? LastUpdatedAt,
    double? AgeSeconds,
    int ChannelCount,
    string? Message)
{
    public static ChameleonDeviceStatus Unavailable(string sourceId, string sourceName, string equipmentId, string message) =>
        new(sourceId, sourceName, equipmentId, "unavailable", null, null, 0, message);
}
public record ChameleonPointDefinition(string? Name, string? Category);
public record ChameleonPointValue(string ChannelId, string ValueType, string Value, string? ChannelName, string? Category);
public record ChameleonPointReadResult(string SourceId, string SourceName, string EquipmentId, DateTimeOffset LastUpdatedAt, IReadOnlyList<ChameleonPointValue> Points);
