using ClosedXML.Excel;

namespace MES.SPC.E2ETests.Utilities;

/// <summary>
/// 產生 E2E 用 Excel 夾具（.xlsx），供計量型 / 計數型上傳 UI 測試使用。
/// </summary>
public static class E2EExcelFixtures
{
    private static readonly object Gate = new();
    private static bool _initialized;

    public static string VariableMixedPath => Path.Combine(FixturesDir, "variable_upload_mixed.xlsx");
    public static string VariableValidPath => Path.Combine(FixturesDir, "variable_upload_valid.xlsx");
    public static string AttributeValidPath => Path.Combine(FixturesDir, "attribute_upload_valid.xlsx");

    public static string AttributeValidPathFor(E2EAttributeMasterDataContext ctx) =>
        Path.Combine(FixturesDir, $"attribute_upload_valid_{ctx.Suffix}.xlsx");

    private static string FixturesDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static void EnsureAll()
    {
        lock (Gate)
        {
            if (_initialized) return;
            Directory.CreateDirectory(FixturesDir);

            WriteVariableMixed(VariableMixedPath);
            WriteVariableValid(VariableValidPath, rowCount: 10);
            WriteAttributeValid(AttributeValidPath, "P-1002", "ST-02", "M-03", "DEF-001");

            _initialized = true;
        }
    }

    public static void WriteTemplatesTo(string variablePath, string attributePath)
    {
        WriteVariableValid(variablePath, rowCount: 100);
        WriteAttributeValid(attributePath, "P-1001", "ST-01", "M-01", "DEF-001", rowCount: 30);
    }

    /// <summary>計量型：含空料號錯誤列 + 計數型項目誤用 + 一筆有效列。</summary>
    private static void WriteVariableMixed(string path)
    {
        var headers = new[] { "料號", "製程", "機台", "檢驗項目", "測量值", "日期", "作業員", "lot", "樣本編號" };
        var rows = new object?[][]
        {
            new object?[] { "", "ST-01", "M-01", "LEN-001", 10.12, "2026-05-21 08:00:00", "OP-01", "L-E2E-ERR-001", 1 },
            new object?[] { "P-1001", "ST-01", "M-01", "DEF-001", 0.05, "2026-05-21 08:01:00", "OP-01", "L-E2E-ERR-002", 1 },
            new object?[] { "P-1001", "ST-01", "M-01", "LEN-001", 10.05, "2026-05-21 08:02:00", "OP-01", "L-E2E-OK-001", 1 }
        };
        WriteSheet(path, "計量型資料匯入", headers, rows);
    }

    private static void WriteVariableValid(string path, int rowCount)
    {
        var headers = new[] { "料號", "製程", "機台", "檢驗項目", "測量值", "日期", "作業員", "lot", "樣本編號" };
        var rows = new List<object?[]>();
        var lot = $"L-E2E-OK-{DateTime.UtcNow:yyyyMMddHHmmss}";
        for (var i = 0; i < rowCount; i++)
        {
            rows.Add(new object?[]
            {
                "P-1001", "ST-01", "M-01", "LEN-001",
                10.0 + i * 0.01, DateTime.UtcNow.AddMinutes(-rowCount + i).ToString("yyyy-MM-dd HH:mm:ss"),
                "E2E-OP", lot, (i % 5) + 1
            });
        }
        WriteSheet(path, "計量型資料匯入", headers, rows.ToArray());
    }

    public static void WriteAttributeValidForContext(E2EAttributeMasterDataContext ctx)
    {
        Directory.CreateDirectory(FixturesDir);
        WriteAttributeValid(
            AttributeValidPathFor(ctx),
            ctx.PartNo,
            ctx.ProcessCode,
            ctx.MachineCode,
            ctx.CharacteristicCode);
    }

    private static void WriteAttributeValid(string path, string partNo, string processCode, string machineCode, string charCode, int rowCount = 2)
    {
        var headers = new[] { "料號", "製程", "機台", "檢驗項目", "總數", "不良數", "缺點數", "單位數", "日期", "作業員", "lot", "樣本編號" };
        var rows = new List<object?[]>();
        for (var i = 0; i < rowCount; i++)
        {
            var lot = $"L-E2E-ATTR-{DateTime.UtcNow.AddMinutes(-rowCount + i):yyyyMMddHHmmss}";
            var inspected = 500 + (i % 3) * 50;
            var defectQty = 5 + (i % 7);
            var defectCount = defectQty + 3;
            rows.Add(new object?[]
            {
                partNo, processCode, machineCode, charCode,
                inspected, defectQty, defectCount, inspected,
                DateTime.UtcNow.AddMinutes(-rowCount + i).ToString("yyyy-MM-dd HH:mm:ss"),
                $"E2E-OP-{(char)('A' + (i % 3))}", lot, 1
            });
        }
        WriteSheet(path, "計數型資料匯入", headers, rows.ToArray());
    }

    private static void WriteSheet(string path, string sheetName, string[] headers, object?[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < headers.Length; c++)
            {
                var val = rows[r][c];
                if (val is null) continue;
                ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(val);
            }
        }
        wb.SaveAs(path);
    }
}
