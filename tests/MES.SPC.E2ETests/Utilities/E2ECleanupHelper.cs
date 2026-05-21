using System.Net.Http.Json;

namespace MES.SPC.E2ETests.Utilities;

public static class E2ECleanupHelper
{
    public static async Task CleanupAsync(string apiBaseUrl, E2ETestRunContext ctx)
    {
        using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

        if (ctx.UploadBatchId.HasValue)
        {
            try { await http.DeleteAsync($"/api/uploads/{ctx.UploadBatchId}"); } catch { /* ignore */ }
        }

        if (ctx.TypeId.HasValue)
        {
            try { await http.DeleteAsync($"/api/control-chart-types/{ctx.TypeId}"); } catch { /* ignore */ }
        }

        if (ctx.CategoryId.HasValue)
        {
            try { await http.DeleteAsync($"/api/control-chart-categories/{ctx.CategoryId}"); } catch { /* ignore */ }
        }

        if (ctx.GroupId.HasValue)
        {
            try { await http.DeleteAsync($"/api/control-chart-groups/{ctx.GroupId}"); } catch { /* ignore */ }
        }

        if (ctx.OperatorId.HasValue)
        {
            try { await http.DeleteAsync($"/api/operators/{ctx.OperatorId}"); } catch { /* ignore */ }
        }
    }

    public static async Task ResolveCreatedIdsAsync(string apiBaseUrl, E2ETestRunContext ctx)
    {
        using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

        var groups = await http.GetFromJsonAsync<List<IdCodeDto>>("/api/control-chart-groups");
        ctx.GroupId = groups?.FirstOrDefault(g => g.GroupCode == ctx.GroupCode)?.Id;

        var categories = await http.GetFromJsonAsync<List<CatDto>>("/api/control-chart-categories");
        ctx.CategoryId = categories?.FirstOrDefault(c => c.CategoryCode == ctx.CategoryCode)?.Id;

        var types = await http.GetFromJsonAsync<List<TypeDto>>("/api/control-chart-types");
        ctx.TypeId = types?.FirstOrDefault(t => t.ChartTypeCode == ctx.TypeCode)?.Id;

        var operators = await http.GetFromJsonAsync<List<OpDto>>("/api/operators");
        ctx.OperatorId = operators?.FirstOrDefault(o => o.OperatorCode == ctx.OperatorCode)?.Id;
    }

    private sealed record IdCodeDto(int Id, string GroupCode, string GroupName);
    private sealed record CatDto(int Id, string CategoryCode, string CategoryName, int ChartGroupId);
    private sealed record TypeDto(int Id, string ChartTypeCode, string ChartTypeName, int ChartCategoryId);
    private sealed record OpDto(int Id, string OperatorCode, string OperatorName);
}
