using ClosedXML.Excel;
using Xunit.Abstractions;

namespace MesSpc.Api.Tests;

public class UnitTest1(ITestOutputHelper output)
{
    [Fact(Skip = "Developer-only local workbook inspection utility; not an automated test.")]
    public void InspectExcelFile()
    {
        var filePath = @"C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\SPC開發\管制項目\藥液分析總表new.xlsx";
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        using var wb = new XLWorkbook(filePath);
        output.WriteLine($"Total Sheets: {wb.Worksheets.Count}");
        foreach (var s in wb.Worksheets)
        {
            output.WriteLine($"Sheet Name: {s.Name}");
        }

        var ws = wb.Worksheets.First();
        var maxCol = ws.LastColumnUsed().ColumnNumber();
        var maxRow = ws.LastRowUsed().RowNumber();

        var colMeta = new Dictionary<int, (string PartNo, string ProcessCode, string CharCode, string CharName, string USL, string LSL)>();
        string currLine = "CN_LINE";
        string currProc = "PROC_01";

        for (int c = 4; c <= maxCol; c++)
        {
            var l = ws.Cell(3, c).GetString().Trim();
            if (!string.IsNullOrEmpty(l)) currLine = l;

            var p = ws.Cell(4, c).GetString().Trim();
            if (!string.IsNullOrEmpty(p)) currProc = p;

            var code = ws.Cell(6, c).GetString().Trim();
            var name = ws.Cell(9, c).GetString().Trim().Replace("\r", "").Replace("\n", " ");
            if (string.IsNullOrEmpty(name)) continue;

            if (string.IsNullOrEmpty(code))
            {
                var clean = new string(name.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-').ToArray()).Trim('_', '-').ToUpperInvariant();
                code = clean.Length > 30 ? clean[..30] : clean;
                if (string.IsNullOrEmpty(code)) code = $"C{c:D2}";
            }

            var usl = ws.Cell(7, c).GetString().Trim();
            var lsl = ws.Cell(8, c).GetString().Trim();

            colMeta[c] = (currLine, currProc, code, name, usl, lsl);
        }

        var rows = new List<Dictionary<string, string?>>();
        for (int r = 10; r <= maxRow; r++)
        {
            var dateCell = ws.Cell(r, 1);
            var dateStr = dateCell.GetString().Trim();
            if (string.IsNullOrEmpty(dateStr)) continue;

            DateTime measuredAt = DateTime.UtcNow;
            if (dateCell.DataType == XLDataType.DateTime) measuredAt = dateCell.GetDateTime();
            else if (DateTime.TryParse(dateStr, out var dt)) measuredAt = dt;

            var shiftStr = ws.Cell(r, 2).GetString().Trim();
            var timeStr = ws.Cell(r, 3).GetString().Trim();
            var lotNo = !string.IsNullOrEmpty(timeStr) ? $"{shiftStr}-{timeStr}" : shiftStr;

            foreach (var kvp in colMeta)
            {
                int c = kvp.Key;
                var meta = kvp.Value;
                var valStr = ws.Cell(r, c).GetString().Trim();

                if (string.IsNullOrEmpty(valStr) || valStr == "-" || valStr == "NA" || valStr == "N/A") continue;
                if (!double.TryParse(valStr, out var valNum)) continue;

                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PartNo"] = meta.PartNo,
                    ["ProcessCode"] = meta.ProcessCode,
                    ["MachineCode"] = $"{meta.ProcessCode}-M01",
                    ["CharacteristicCode"] = meta.CharCode,
                    ["CharacteristicName"] = meta.CharName,
                    ["USL"] = meta.USL,
                    ["LSL"] = meta.LSL,
                    ["MeasuredValue"] = valNum.ToString(),
                    ["MeasuredAt"] = measuredAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["Operator"] = shiftStr,
                    ["LotNo"] = lotNo,
                    ["SampleNo"] = "1"
                };
                rows.Add(dict);
            }
        }

        output.WriteLine($"Total parsed measurement rows: {rows.Count}");
        foreach (var r in rows.Take(10))
        {
            output.WriteLine($"Part={r["PartNo"]} | Proc={r["ProcessCode"]} | Char={r["CharacteristicCode"]} ({r["CharacteristicName"]}) | Val={r["MeasuredValue"]} | Time={r["MeasuredAt"]} | USL={r["USL"]} | LSL={r["LSL"]}");
        }
    }
}
