using System.Net.Http.Json;

namespace MES.SPC.E2ETests.Utilities;

public sealed class E2EVariableMasterDataContext
{
    public string Suffix { get; } = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    public string PartNo => $"E2E-VAR-P-{Suffix}";
    public string ProcessCode => $"E2E-VAR-ST-{Suffix}";
    public string MachineCode => $"E2E-VAR-M-{Suffix}";
    public string CharacteristicCode => $"E2E-VAR-C-{Suffix}";

    public int? PartId { get; set; }
    public int? ProcessId { get; set; }
    public int? MachineId { get; set; }
    public int? CharacteristicId { get; set; }
    public int? PpcId { get; set; }
}

public static class E2EVariableMasterDataHelper
{
    public static async Task<E2EVariableMasterDataContext> EnsureAsync(string apiBaseUrl)
    {
        var ctx = new E2EVariableMasterDataContext();
        using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

        var chartTypes = await http.GetFromJsonAsync<List<ChartTypeDto>>("/api/control-chart-types");
        var chartTypeId = chartTypes?.FirstOrDefault(x => string.Equals(x.DataCategory, "Variable", StringComparison.OrdinalIgnoreCase))?.Id
            ?? chartTypes?.FirstOrDefault()?.Id
            ?? 1;

        var partRes = await http.PostAsJsonAsync("/api/parts", new
        {
            partNo = ctx.PartNo,
            partName = $"E2E 計量測試料號 {ctx.Suffix}",
            isEnabled = true
        });
        partRes.EnsureSuccessStatusCode();
        ctx.PartId = (await partRes.Content.ReadFromJsonAsync<PartDto>())!.Id;

        var processRes = await http.PostAsJsonAsync("/api/processes", new
        {
            processCode = ctx.ProcessCode,
            processName = $"E2E 計量測試製程 {ctx.Suffix}",
            isEnabled = true
        });
        processRes.EnsureSuccessStatusCode();
        ctx.ProcessId = (await processRes.Content.ReadFromJsonAsync<ProcessDto>())!.Id;

        var machineRes = await http.PostAsJsonAsync("/api/machines", new
        {
            machineCode = ctx.MachineCode,
            machineName = $"E2E 計量測試機台 {ctx.Suffix}",
            processId = ctx.ProcessId,
            isEnabled = true
        });
        machineRes.EnsureSuccessStatusCode();
        ctx.MachineId = (await machineRes.Content.ReadFromJsonAsync<MachineDto>())!.Id;

        var charRes = await http.PostAsJsonAsync("/api/characteristics", new
        {
            characteristicCode = ctx.CharacteristicCode,
            characteristicName = $"E2E 計量測試特性 {ctx.Suffix}",
            dataCategory = "Variable",
            unit = "mm",
            defaultChartTypeId = chartTypeId,
            isSpcEnabled = true,
            isEnabled = true
        });
        charRes.EnsureSuccessStatusCode();
        ctx.CharacteristicId = (await charRes.Content.ReadFromJsonAsync<CharacteristicDto>())!.Id;

        var ppcRes = await http.PostAsJsonAsync("/api/part-process-characteristics", new
        {
            partId = ctx.PartId,
            processId = ctx.ProcessId,
            characteristicId = ctx.CharacteristicId,
            usl = 10.2,
            lsl = 9.8,
            ucl = 10.15,
            cl = 10.0,
            lcl = 9.85,
            targetValue = 10.0,
            sampleSize = 5,
            chartTypeId = chartTypeId,
            ruleGroupId = 1,
            isRequired = true,
            isEnabled = true
        });
        ppcRes.EnsureSuccessStatusCode();
        ctx.PpcId = (await ppcRes.Content.ReadFromJsonAsync<PpcDto>())!.Id;

        return ctx;
    }

    public static async Task CleanupAsync(string apiBaseUrl, E2EVariableMasterDataContext ctx)
    {
        using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        if (ctx.PpcId.HasValue)
        {
            try { await http.DeleteAsync($"/api/part-process-characteristics/{ctx.PpcId}"); } catch { }
        }
        if (ctx.CharacteristicId.HasValue)
        {
            try { await http.DeleteAsync($"/api/characteristics/{ctx.CharacteristicId}"); } catch { }
        }
        if (ctx.MachineId.HasValue)
        {
            try { await http.DeleteAsync($"/api/machines/{ctx.MachineId}"); } catch { }
        }
        if (ctx.ProcessId.HasValue)
        {
            try { await http.DeleteAsync($"/api/processes/{ctx.ProcessId}"); } catch { }
        }
        if (ctx.PartId.HasValue)
        {
            try { await http.DeleteAsync($"/api/parts/{ctx.PartId}"); } catch { }
        }
    }

    private sealed record ChartTypeDto(int Id, string? DataCategory);
    private sealed record PartDto(int Id);
    private sealed record ProcessDto(int Id);
    private sealed record MachineDto(int Id);
    private sealed record CharacteristicDto(int Id);
    private sealed record PpcDto(int Id);
}
