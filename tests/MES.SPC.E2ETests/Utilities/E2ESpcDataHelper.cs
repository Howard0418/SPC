using System.Net;
using System.Net.Http.Json;

namespace MES.SPC.E2ETests.Utilities;

public sealed record E2EPpcDto(
    int Id,
    int? PartId,
    int ProcessId,
    int CharacteristicId,
    E2EPartRef? Part,
    E2EProcessRef? Process,
    E2ECharacteristicRef? Characteristic);

public sealed record E2EPartRef(string? PartNo, string? PartName);
public sealed record E2EProcessRef(string? ProcessCode, string? ProcessName);
public sealed record E2ECharacteristicRef(string? CharacteristicCode, string? CharacteristicName);

public sealed record E2ESeedResponse(bool Success, int PpcId, string? BatchId, int SeededCount, string? Message);

public static class E2ESpcDataHelper
{
    public static async Task<E2EPpcDto> GetUsablePpcAsync(HttpClient http)
    {
        var ppcs = await http.GetFromJsonAsync<List<E2EPpcDto>>("/api/part-process-characteristics");
        if (ppcs is null || ppcs.Count == 0)
            throw new InvalidOperationException("資料庫應至少有一筆 PartProcessCharacteristic。");

        var candidates = ppcs
            .Where(p => p.ProcessId > 0 && p.CharacteristicId > 0)
            .OrderByDescending(p => p.PartId.HasValue && p.PartId.Value > 0)
            .ThenBy(p => p.Id)
            .ToList();

        foreach (var ppc in candidates)
        {
            using var res = await http.GetAsync($"/api/v1/spc/chart?ppcId={ppc.Id}");
            if (res.IsSuccessStatusCode) return ppc;
        }

        return candidates.FirstOrDefault()
            ?? throw new InvalidOperationException("找不到具備 ProcessId / CharacteristicId 的 PPC。");
    }

    public static async Task<E2EPpcDto> GetUsablePpcWithLimitsAsync(HttpClient http)
    {
        var ppcs = await http.GetFromJsonAsync<List<E2EPpcDto>>("/api/part-process-characteristics");
        if (ppcs is null || ppcs.Count == 0)
            throw new InvalidOperationException("資料庫應至少有一筆 PartProcessCharacteristic。");

        foreach (var ppc in ppcs.Where(p => p.ProcessId > 0 && p.CharacteristicId > 0).OrderBy(p => p.Id))
        {
            using var res = await http.GetAsync($"/api/v1/spc/chart?ppcId={ppc.Id}");
            if (!res.IsSuccessStatusCode) continue;

            var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            if (!json.TryGetProperty("limits", out var limits)) continue;
            if (limits.EnumerateObject().Any(x => x.Value.ValueKind != System.Text.Json.JsonValueKind.Null))
                return ppc;
        }

        throw new InvalidOperationException("找不到含規格或管制界限的可用 SPC 圖表資料。");
    }

    public static async Task<E2ESeedResponse?> TrySeedMeasurementsAsync(HttpClient http, int ppcId, int count = 25)
    {
        var res = await http.PostAsync($"/api/v1/migration/seed-sample-measurements?ppcId={ppcId}&count={count}", null);
        if (res.StatusCode == HttpStatusCode.Forbidden)
            return null;

        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<E2ESeedResponse>();
    }

    public static async Task TryClearSeedMeasurementsAsync(HttpClient http, string? batchId)
    {
        if (string.IsNullOrWhiteSpace(batchId)) return;
        try
        {
            await http.DeleteAsync($"/api/v1/migration/seed-sample-measurements?batchId={Uri.EscapeDataString(batchId)}");
        }
        catch
        {
            // Cleanup should not mask the test result.
        }
    }
}
