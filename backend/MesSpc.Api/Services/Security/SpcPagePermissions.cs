using System.Text.Json;

namespace MesSpc.Api.Services.Security;

public static class SpcPagePermissions
{
    public sealed record Item(string Code, string Name, string Route, bool ViewerDefault = false);
    public static readonly Item[] Catalog =
    [
        new("analysis.etch", "咬蝕量分析", "/etch-analysis", true),
        new("analysis.spc", "SPC 管制分析", "/spc", true),
        new("analysis.capability", "製程能力分析", "/process-analysis/capability", true),
        new("analysis.trend", "量測趨勢分析", "/trend-chart", true),
        new("analysis.violations", "異常點分析", "/process-analysis/violations", true),
        new("analysis.reports", "SPC 週月報表", "/monthly-control-chart", true),
        new("equipment.status", "設備即時狀態", "/equipment-status", true),
        new("equipment.points", "設備點位總覽", "/equipment-points", true),
        new("equipment.monitor", "重點點位監控", "/equipment-monitor", true),
        new("data.measurements", "量測資料輸入", "/measurements"), new("data.upload", "SPC 資料匯入", "/uploads"),
        new("master.spc", "SPC 管制項目設定", "/part-process-characteristics"), new("master.process", "工站製程主檔", "/processes"),
        new("master.parts", "產品料號主檔", "/parts"), new("master.characteristics", "品質特性項目", "/characteristics"),
        new("admin.operators", "系統使用者管理", "/operators"), new("admin.settings", "系統與通報設定", "/settings"),
        new("calibration.manage", "儀器校正管理", "/calibration-instruments")
    ];

    public static string[] Resolve(string? role, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch { }
        }
        return UserRoles.Normalize(role) == UserRoles.Editor ? Catalog.Select(x => x.Code).ToArray()
            : Catalog.Where(x => x.ViewerDefault).Select(x => x.Code).ToArray();
    }
}
