using ClosedXML.Excel;

namespace MesSpc.Api.Services.Parsers;

/// <summary>
/// 解析「藥液分析日報表.xlsx」格式的 Excel 檔
/// 每張工作表結構：
///   Row 2 = 報表名稱
///   Row 3 = 線別名稱
///   Row 4 = 日期 (C2)
///   Row 5 = 分析者 (C2)
///   Row 6 = 欄位標頭
///   Row 7+ = 資料列
/// </summary>
public class ChemicalDailyReportParser
{
    private static readonly HashSet<string> _skipSheets = new(StringComparer.OrdinalIgnoreCase)
    {
        "F值", "f值"
    };

    public ChemicalDailyReportParseResult Parse(Stream excelStream)
    {
        var result = new ChemicalDailyReportParseResult();
        using var wb = new XLWorkbook(excelStream);

        foreach (var ws in wb.Worksheets)
        {
            if (_skipSheets.Contains(ws.Name)) continue;

            // 讀取表頭
            var sheetLine = ws.Cell(3, 2).GetString();
            var dateCell = ws.Cell(4, 2);
            var analyst = ws.Cell(5, 2).GetString();

            DateTime measuredDate = DateTime.Today;
            if (dateCell.DataType == XLDataType.DateTime)
                measuredDate = dateCell.GetDateTime().Date;
            else if (DateTime.TryParse(dateCell.GetString(), out var parsed))
                measuredDate = parsed.Date;

            // 資料從 Row 7 開始
            int rowNum = 7;
            while (true)
            {
                var row = ws.Row(rowNum);

                // 若 C2 (槽位) 和 C3 (分析項目) 都空白，代表已到表尾
                var tankCell = row.Cell(2).GetString().Trim();
                var itemCell = row.Cell(3).GetString().Trim();
                if (string.IsNullOrEmpty(tankCell) && string.IsNullOrEmpty(itemCell)) break;
                if (tankCell.StartsWith("備註")) break;

                result.TotalRows++;
                rowNum++;

                // 若分析項目為空，跳過（可能是備註行）
                if (string.IsNullOrEmpty(itemCell))
                {
                    result.SkippedRows++;
                    continue;
                }

                // C8 滴定數 & C13 複驗滴定數 — 都空白代表當日未填，跳過
                var titrationRaw = row.Cell(8).GetString().Trim();
                var recheckRaw = row.Cell(13).GetString().Trim();
                bool hasTitration = TryParseDouble(titrationRaw, out var titrationVal);
                bool hasRecheck = TryParseDouble(recheckRaw, out var recheckVal);

                // 如果都沒有量測值，仍保留這行（如 LSL/USL 管制項建立用），但標記為未量測
                var dataRow = new ChemicalAnalysisRow
                {
                    SheetName = ws.Name,
                    Line = string.IsNullOrEmpty(sheetLine) ? ws.Name : sheetLine,
                    Tank = tankCell,
                    AnalysisItem = itemCell,
                    SpecText = row.Cell(4).GetString().Trim(),
                    LSL = TryParseDouble(row.Cell(5).GetString(), out var lsl) ? lsl : null,
                    USL = TryParseDouble(row.Cell(7).GetString(), out var usl) ? usl : null,

                    // 主測
                    TitrationMl = hasTitration ? titrationVal : null,
                    Concentration = TryParseDoubleFormula(row.Cell(9), out var conc) ? conc : null,
                    Judgment = row.Cell(10).GetString().Trim().NullIfEmpty(),

                    // 複驗
                    RecheckTitrationMl = hasRecheck ? recheckVal : null,
                    RecheckConcentration = TryParseDoubleFormula(row.Cell(14), out var rConc) ? rConc : null,
                    RecheckJudgment = row.Cell(15).GetString().Trim().NullIfEmpty(),

                    // 藥液管理
                    Chemical = row.Cell(16).GetString().Trim().NullIfEmpty(),
                    TankCapacityL = TryParseDouble(row.Cell(17).GetString(), out var cap) ? cap : null,
                    AddCtrlLower = TryParseDouble(row.Cell(18).GetString(), out var addLow) ? addLow : null,
                    DilCtrlUpper = TryParseDouble(row.Cell(19).GetString(), out var dilUp) ? dilUp : null,

                    // ECR
                    EcrNo = row.Cell(20).GetString().Trim().NullIfEmpty(),
                    EcrStartDate = TryParseDate(row.Cell(21), out var ecrStart) ? ecrStart : null,
                    EcrEndDate = TryParseDate(row.Cell(22), out var ecrEnd) ? ecrEnd : null,

                    // 表頭
                    MeasuredDate = measuredDate,
                    Analyst = analyst.NullIfEmpty()
                };

                result.Rows.Add(dataRow);
                result.ParsedRows++;
            }
        }

        return result;
    }

    private static bool TryParseDouble(string? raw, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim().ToUpperInvariant();
        if (raw == "NA" || raw == "-" || raw == "N/A") return false;
        return double.TryParse(raw, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseDoubleFormula(IXLCell cell, out double value)
    {
        value = 0;
        try
        {
            if (cell.DataType == XLDataType.Number)
            {
                value = cell.GetDouble();
                return true;
            }
            return TryParseDouble(cell.GetString(), out value);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseDate(IXLCell cell, out DateTime value)
    {
        value = default;
        try
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                value = cell.GetDateTime().Date;
                return true;
            }
            return DateTime.TryParse(cell.GetString(), out value);
        }
        catch { return false; }
    }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
