using ClosedXML.Excel;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Infrastructure.Data;
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

                    var maxRows = Math.Min(lastRow, 40);
                    for (int r = 1; r <= maxRows; r++)
                    {
                        var row = ws.Row(r);
                        var cells = new string[lastCol];
                        for (int c = 1; c <= lastCol; c++)
                        {
                            var cell = row.Cell(c);
                            var val = cell.Value.ToString();
                            if (cell.HasFormula)
                            {
                                val = $"[Formula: {cell.FormulaA1} = {cell.Value}]";
                            }
                            cells[c - 1] = $"[Col {c}] {val}";
                        }
                        sb.AppendLine($"Row {r:D2}: " + string.Join(" | ", cells.Select(c => c.Length > 100 ? c.Substring(0, 97) + "..." : c)));
                    }

                    if (lastRow > 40)
                    {
                        sb.AppendLine($"... and {lastRow - 40} more rows");
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

        [Fact]
        public void DumpDatabaseData()
        {
            var connString = "Server=172.16.110.16;Database=PMR_SPC_2026;User Id=sa;Password=a@t123;Encrypt=True;TrustServerCertificate=True;";
            var outputPath = @"C:\Users\ihao_ting.PMR.000\.gemini\antigravity-ide\brain\8f12be77-2494-4512-a774-a316b1044a57\scratch\database_dump.txt";

            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"Database Dump: PMR_SPC_2026");
            sb.AppendLine($"Time: {DateTime.Now}");
            sb.AppendLine("==================================================");

            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(connString)
                    .Options;

                using var db = new AppDbContext(options);

                sb.AppendLine();
                sb.AppendLine("--- ProductionLines ---");
                var lines = db.ProductionLines.AsNoTracking().ToList();
                foreach (var line in lines)
                {
                    sb.AppendLine($"Line ID: {line.Id} | Code: {line.LineCode} | Name: {line.LineName} | IsActive: {line.IsActive}");
                }

                sb.AppendLine();
                sb.AppendLine("--- Machines ---");
                var machines = db.Machines.AsNoTracking().ToList();
                foreach (var mach in machines)
                {
                    sb.AppendLine($"Machine ID: {mach.Id} | Code: {mach.MachineCode} | Name: {mach.MachineName} | ProcessId: {mach.ProcessId} | IsEnabled: {mach.IsEnabled}");
                }

                sb.AppendLine();
                sb.AppendLine("--- Tanks ---");
                var tanks = db.Tanks.AsNoTracking().ToList();
                foreach (var tank in tanks)
                {
                    sb.AppendLine($"Tank ID: {tank.Id} | LineId: {tank.LineId} | Code: {tank.TankCode} | Name: {tank.TankName} | IsActive: {tank.IsActive}");
                }

                sb.AppendLine();
                sb.AppendLine("--- Slots ---");
                var slots = db.Slots.AsNoTracking().ToList();
                foreach (var slot in slots)
                {
                    sb.AppendLine($"Slot ID: {slot.Id} | TankId: {slot.TankId} | Code: {slot.SlotCode} | Name: {slot.SlotName} | IsActive: {slot.IsActive}");
                }

                sb.AppendLine();
                sb.AppendLine("--- PartProcessCharacteristics (CHEMICAL scope) ---");
                var ppcs = db.PartProcessCharacteristics.AsNoTracking()
                    .Where(x => x.ControlScope == "CHEMICAL")
                    .ToList();
                foreach (var ppc in ppcs)
                {
                    sb.AppendLine($"PPC ID: {ppc.Id} | ProcessId: {ppc.ProcessId} | MachineId: {ppc.MachineId} | TankId: {ppc.TankId} | CharacteristicId: {ppc.CharacteristicId}");
                }

                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"DB dump written to {outputPath}");
            }
            catch (Exception ex)
            {
                var errStr = $"Error dumping DB: {ex.Message}\n{ex.StackTrace}";
                File.WriteAllText(outputPath, errStr, Encoding.UTF8);
                Console.WriteLine(errStr);
                throw;
            }
        }
    }
}
