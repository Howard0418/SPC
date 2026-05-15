namespace MesSpc.Api.Services.Parsers;

/// <summary>
/// 代表藥液分析日報表中，一行的完整資料（主測 + 複驗）
/// </summary>
public class ChemicalAnalysisRow
{
    // --- 來源識別 ---
    public string SheetName { get; set; } = string.Empty;     // 工作表名 (C1, 新線, DP...)
    public string Line { get; set; } = string.Empty;           // 線別
    public string Tank { get; set; } = string.Empty;           // 槽位
    public string AnalysisItem { get; set; } = string.Empty;   // 分析項目

    // --- 規格 ---
    public string SpecText { get; set; } = string.Empty;       // 規格文字 (如 15±2)
    public double? LSL { get; set; }                           // C5 下限
    public double? USL { get; set; }                           // C7 上限

    // --- 主測 ---
    public double? TitrationMl { get; set; }                   // C8 滴定數(ml)
    public double? Concentration { get; set; }                 // C9 濃度
    public string? Judgment { get; set; }                      // C10 判定

    // --- 複驗 ---
    public double? RecheckTitrationMl { get; set; }            // C13 複驗滴定數
    public double? RecheckConcentration { get; set; }          // C14 複驗濃度
    public string? RecheckJudgment { get; set; }               // C15 複驗判定

    // --- 藥液管理 ---
    public string? Chemical { get; set; }                      // C16 藥液名稱
    public double? TankCapacityL { get; set; }                 // C17 槽容(L)
    public double? AddCtrlLower { get; set; }                  // C18 添加管制值
    public double? DilCtrlUpper { get; set; }                  // C19 稀釋管制值

    // --- 工程變更 ---
    public string? EcrNo { get; set; }                         // C20 ECR編號
    public DateTime? EcrStartDate { get; set; }                // C21 申請日期
    public DateTime? EcrEndDate { get; set; }                  // C22 截止日期

    // --- 表頭資訊 ---
    public DateTime MeasuredDate { get; set; }                 // Row4 日期
    public string? Analyst { get; set; }                       // Row5 分析者
}

public class ChemicalDailyReportParseResult
{
    public int TotalRows { get; set; }
    public int ParsedRows { get; set; }
    public int SkippedRows { get; set; }
    public List<ChemicalAnalysisRow> Rows { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
