using ClosedXML.Excel;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MesSpc.Api.Tests
{
    public class AnalyzeExcelTest
    {
        [Fact]
        public void Analyze()
        {
            var filePath = @"D:\SPC\sample-data\SPC系統建置.xlsx";
            var outputPath = @"C:\Users\ihao_ting.PMR.000\.gemini\antigravity-ide\brain\8f12be77-2494-4512-a774-a316b1044a57\scratch\excel_analysis_result.txt";

            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"Analyzing file: {filePath}");
            sb.AppendLine($"Time: {DateTime.Now}");
            sb.AppendLine("==================================================");

            try
            {
                using var workbook = new XLWorkbook(filePath);
                sb.AppendLine($"Total sheets: {workbook.Worksheets.Count}");
                sb.AppendLine("Sheets: " + string.Join(", ", workbook.Worksheets.Select(w => w.Name)));

                foreach (var ws in workbook.Worksheets)
                {
                    var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                    var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

                    sb.AppendLine();
                    sb.AppendLine($"--------------------------------------------------");
                    sb.AppendLine($"Sheet Name: {ws.Name}");
                    sb.AppendLine($"Used Range: Rows 1 to {lastRow}, Cols 1 to {lastCol}");
                    sb.AppendLine($"--------------------------------------------------");

                    // Print first 20 rows of the sheet
                    var maxRows = Math.Min(lastRow, 30);
                    for (int r = 1; r <= maxRows; r++)
                    {
                        var row = ws.Row(r);
                        var cells = new string[lastCol];
                        for (int c = 1; c <= lastCol; c++)
                        {
                            var cell = row.Cell(c);
                            var val = cell.Value.ToString();
                            // If it's a formula, we might want to see if we can get the value or formula
                            if (cell.HasFormula)
                            {
                                val = $"[Formula: {cell.FormulaA1} = {cell.Value}]";
                            }
                            cells[c - 1] = $"[Col {c}] {val}";
                        }
                        sb.AppendLine($"Row {r:D2}: " + string.Join(" | ", cells.Select(c => c.Length > 60 ? c.Substring(0, 57) + "..." : c)));
                    }

                    if (lastRow > 30)
                    {
                        sb.AppendLine($"... and {lastRow - 30} more rows");
                    }
                }

                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"Analysis written to {outputPath}");
            }
            catch (Exception ex)
            {
                var errStr = $"Error analyzing excel: {ex.Message}\n{ex.StackTrace}";
                File.WriteAllText(outputPath, errStr, Encoding.UTF8);
                Console.WriteLine(errStr);
                throw;
            }
        }
    }
}
