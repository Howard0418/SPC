using System.Net.Http.Json;

namespace MES.SPC.E2ETests.Utilities;

/// <summary>
/// 確保計數型上傳 E2E 有可用的 Attribute 檢驗項目與 PPC 對照（遷移後 DB 可能無 Attribute 種子）。
/// </summary>
public sealed class E2EAttributeMasterDataContext
{
    public string Suffix { get; } = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    public string CharacteristicCode => $"E2E-ATTR-{Suffix}";
    public string PartNo { get; set; } = "P-1001";
    public string ProcessCode { get; set; } = "ST-01";
    public string MachineCode { get; set; } = "M-01";

    public int? CharacteristicId { get; set; }
    public int? PpcId { get; set; }
}

public static class E2EAttributeMasterDataHelper
{
    public static async Task<E2EAttributeMasterDataContext> EnsureAsync(string apiBaseUrl)
    {
        var ctx = new E2EAttributeMasterDataContext();
        using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

        var chartTypes = await http.GetFromJsonAsync<List<ChartTypeDto>>("/api/control-chart-types");
        var chartTypeId = chartTypes?.FirstOrDefault()?.Id ?? 12;

        var charPayload = new
        {
            characteristicCode = ctx.CharacteristicCode,
            characteristicName = $"E2E計數特性-{ctx.Suffix}",
            dataCategory = "Attribute",
            unit = "%",
            defaultChartTypeId = chartTypeId,
            isSpcEnabled = true,
            isEnabled = true
        };
        var charRes = await http.PostAsJsonAsync("/api/characteristics", charPayload);
        charRes.EnsureSuccessStatusCode();
        var createdChar = await charRes.Content.ReadFromJsonAsync<CharDto>();
        ctx.CharacteristicId = createdChar!.Id;

        var parts = await http.GetFromJsonAsync<List<PartDto>>("/api/parts");
        var part = parts?.FirstOrDefault(p => p.PartNo == ctx.PartNo)
            ?? throw new InvalidOperationException($"找不到料號 {ctx.PartNo}");
        var processes = await http.GetFromJsonAsync<List<ProcessDto>>("/api/processes");
        var process = processes?.FirstOrDefault(p => p.ProcessCode == ctx.ProcessCode)
            ?? throw new InvalidOperationException($"找不到製程 {ctx.ProcessCode}");
        var machines = await http.GetFromJsonAsync<List<MachineDto>>("/api/machines");
        var machine = machines?.FirstOrDefault(m => m.MachineCode == ctx.MachineCode)
            ?? machines?.FirstOrDefault(m => m.ProcessId == process.Id)
            ?? throw new InvalidOperationException($"找不到機台 {ctx.MachineCode}");
        ctx.MachineCode = machine.MachineCode;

        var ppcPayload = new
        {
            partId = part.Id,
            processId = process.Id,
            characteristicId = ctx.CharacteristicId,
            usl = 0.05,
            lsl = 0.0,
            ucl = 0.03,
            cl = 0.01,
            lcl = 0.0,
            targetValue = 0.01,
            sampleSize = 1,
            chartTypeId = chartTypeId,
            ruleGroupId = 1,
            isRequired = true,
            isEnabled = true
        };
        var ppcRes = await http.PostAsJsonAsync("/api/part-process-characteristics", ppcPayload);
        ppcRes.EnsureSuccessStatusCode();
        var createdPpc = await ppcRes.Content.ReadFromJsonAsync<PpcDto>();
        ctx.PpcId = createdPpc!.Id;

        return ctx;
    }

    public static async Task CleanupAsync(string apiBaseUrl, E2EAttributeMasterDataContext ctx)
    {
        using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        if (ctx.PpcId.HasValue)
        {
            try { await http.DeleteAsync($"/api/part-process-characteristics/{ctx.PpcId}"); } catch { /* ignore */ }
        }
        if (ctx.CharacteristicId.HasValue)
        {
            try { await http.DeleteAsync($"/api/characteristics/{ctx.CharacteristicId}"); } catch { /* ignore */ }
        }
    }

    private sealed record ChartTypeDto(int Id, string ChartTypeCode);
    private sealed record CharDto(int Id, string CharacteristicCode);
    private sealed record PartDto(int Id, string PartNo);
    private sealed record ProcessDto(int Id, string ProcessCode);
    private sealed record MachineDto(int Id, string MachineCode, int ProcessId);
    private sealed record PpcDto(int Id);
}
