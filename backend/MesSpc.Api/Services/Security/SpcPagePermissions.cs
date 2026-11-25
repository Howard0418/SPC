using System.Text.Json;

namespace MesSpc.Api.Services.Security;

public static class SpcPagePermissions
{
    public const string RegistryVersionMarker = "system.page-permissions.v2";

    public sealed record Item(
        string Code,
        string Name,
        string NameKey,
        string Route,
        string? MenuGroup = null,
        bool ViewerDefault = false,
        bool ExplicitOnly = false,
        bool ShowInNavigation = true,
        bool ShowInPermissionEditor = true,
        bool EditorOnly = false);

    public static readonly Item[] Catalog =
    [
        Page("analysis.etch", "咬蝕量分析", "etchAnalysis", "/etch-analysis", "processAnalysis", viewer: true),
        Page("analysis.spc", "SPC 管制分析", "spcAnalysis", "/spc", "processAnalysis", viewer: true),
        Page("analysis.particles", "落塵監控分析", "particleMonitoring", "/particle-monitoring", "processAnalysis", viewer: true),
        Page("analysis.capability", "製程能力分析", "processCapability", "/process-analysis/capability", "processAnalysis", viewer: true),
        Page("analysis.trend", "量測趨勢分析", "measurementTrends", "/trend-chart", "processAnalysis", viewer: true),
        Page("analysis.violations", "異常點分析", "violationAnalysis", "/process-analysis/violations", "processAnalysis", viewer: true),
        Page("analysis.reports", "SPC 週月報表", "periodReports", "/monthly-control-chart", "processAnalysis", viewer: true),

        Page("data.measurements", "量測資料輸入", "measurementEntry", "/measurements", "importSampling", editor: true),
        Page("data.particles", "落塵監控資料輸入", "particleEntry", "/particle-monitoring/entry", "importSampling", editor: true),
        Page("data.upload", "匯入批次查詢", "importBatches", "/uploads", "importSampling", editor: true),
        Page("master.spc", "SPC 管制項目設定", "controlItems", "/part-process-characteristics", "qualityMaster", editor: true),
        Page("master.analysis_limits", "分析項目界限總覽", "analysisLimits", "/analysis-limit-overview", "qualityMaster", editor: true),
        Page("master.chemical_overview", "線別分析項目總覽", "lineAnalysis", "/chemical-analysis-overview", "qualityMaster", editor: true),
        Page("master.chemical_f", "藥液 F 表維護", "chemicalTables", "/chemical-f-table", "qualityMaster", editor: true),
        Page("master.chemical_adjustment", "藥液量測調整公式", "chemicalAdjustmentParameters", "/chemical-adjustment-parameters", "qualityMaster", editor: true),
        Page("master.process", "工站製程主檔", "processes", "/processes", "qualityMaster", editor: true),
        Page("master.parts", "產品料號主檔", "parts", "/parts", "qualityMaster", editor: true),
        Page("master.characteristics", "品質特性項目", "characteristics", "/characteristics", "qualityMaster", editor: true),
        Page("master.traceability", "線別槽體設定", "linesTanks", "/traceability-master", "qualityMaster", editor: true),
        Page("master.chart_groups", "管制圖大類別維護", "chartCategories", "/control-chart-groups", "qualityMaster", editor: true),
        Page("master.spc_rules", "SPC 異常規則維護", "alertRules", "/spc-rule-groups", "qualityMaster", editor: true),
        Page("alerts.overview", "異常通報總覽", "alerts", "/alerts", "alertsTraceability", editor: true),
        Page("alerts.workflow", "異常單簽核處置", "alertWorkflow", "/alerts-workflow", "alertsTraceability", editor: true),
        Page("trace.genealogy", "產品系譜圖", "genealogy", "/genealogy", "alertsTraceability", editor: true),
        Page("trace.quality_history", "多維度品質履歷查詢", "qualityHistory", "/spc/query", "alertsTraceability", editor: true),
        Page("admin.guide", "系統操作手冊", "guide", "/guide", "administration", editor: true),
        Page("calibration.manage", "儀器校正管理", "calibration", "/calibration-instruments", "administration", editor: true),
        Page("admin.smtp", "SMTP 郵件與預警設定", "smtp", "/settings/smtp", "administration", editor: true),
        Page("admin.reports", "SPC 週報月報設定", "reportSettings", "/settings/spc-reports", "administration", editor: true),
        Page("admin.operators", "系統使用者管理", "users", "/operators", "administration", editor: true),
        Page(AuditPermissions.Read, "使用者活動紀錄", "auditEvents", "/audit-events", "administration", explicitOnly: true),

        new(AuditPermissions.Export, "稽核紀錄匯出（需查閱權）", "spc.settings.auditExportPermission", "/audit-events", ExplicitOnly: true, ShowInNavigation: false),
        new(AuditPermissions.Evidence, "稽核證據查閱（需查閱權）", "spc.settings.auditEvidencePermission", "/audit-events", ExplicitOnly: true, ShowInNavigation: false),
        new("equipment.status", "設備即時狀態（舊版）", "", "/equipment-status", ShowInNavigation: false, ShowInPermissionEditor: false),
        new("equipment.points", "設備點位總覽（舊版）", "", "/equipment-points", ShowInNavigation: false, ShowInPermissionEditor: false),
        new("equipment.monitor", "重點點位監控（舊版）", "", "/equipment-monitor", ShowInNavigation: false, ShowInPermissionEditor: false),
        new("admin.settings", "系統與通報設定（舊版）", "", "/settings", ShowInNavigation: false, ShowInPermissionEditor: false)
    ];

    private static readonly string[] LegacyEditorImplicitCodes =
    [
        "master.spc", "master.analysis_limits", "master.chemical_overview", "master.chemical_f",
        "master.chemical_adjustment", "master.process", "master.parts", "master.characteristics",
        "master.traceability", "master.chart_groups", "master.spc_rules", "alerts.overview",
        "alerts.workflow", "trace.genealogy", "trace.quality_history", "admin.guide", "admin.smtp", "admin.reports"
    ];

    private static Item Page(string code, string name, string nameKey, string route, string group,
        bool viewer = false, bool editor = false, bool explicitOnly = false) =>
        new(code, name, $"spc.navigation.{nameKey}", route, $"spc.navigation.{group}", viewer, explicitOnly,
            ShowInNavigation: true, ShowInPermissionEditor: true, EditorOnly: editor);

    public static string[] Resolve(string? role, string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var values = JsonSerializer.Deserialize<string[]>(json) ?? [];
                if (values.Contains(RegistryVersionMarker, StringComparer.Ordinal))
                    return values.Where(x => x != RegistryVersionMarker).Distinct(StringComparer.Ordinal).ToArray();

                if (values.Length == 0) return Defaults(role);
                var compatible = values.ToHashSet(StringComparer.Ordinal);
                if (compatible.Contains("analysis.spc")) compatible.Add("analysis.particles");
                if (UserRoles.Normalize(role) == UserRoles.Editor)
                    compatible.UnionWith(LegacyEditorImplicitCodes);
                return compatible.ToArray();
            }
            catch { }
        }
        return Defaults(role);
    }

    public static string NormalizeForStorage(IEnumerable<string> values)
    {
        var normalized = values.Where(x => x != RegistryVersionMarker).ToHashSet(StringComparer.Ordinal);
        if (normalized.Contains("admin.smtp") || normalized.Contains("admin.reports"))
            normalized.Add("admin.settings");
        normalized.Add(RegistryVersionMarker);
        return JsonSerializer.Serialize(normalized);
    }

    private static string[] Defaults(string? role) => UserRoles.Normalize(role) == UserRoles.Editor
        ? Catalog.Where(x => !x.ExplicitOnly).Select(x => x.Code).ToArray()
        : Catalog.Where(x => x.ViewerDefault).Select(x => x.Code).ToArray();
}
