using ClosedXML.Excel;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v2/reports")]
public class ReportsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("cpk-summary")]
    public async Task<IActionResult> ExportCpkSummary([FromQuery] string? month)
    {
        var targetMonth = string.IsNullOrEmpty(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
        
        var characteristics = await dbContext.PartProcessCharacteristics
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Where(x => x.IsEnabled)
            .ToListAsync();

        var alertCount = await dbContext.AlertEvents.CountAsync(x => x.Status != "Closed");
        var totalMeasurements = await dbContext.VariableMeasurements.CountAsync();
        var oocMeasurements = await dbContext.SpcCalculationResults.CountAsync(x => x.IsOutOfControl || x.IsOutOfSpec);
        double passRate = totalMeasurements > 0 ? Math.Round((1.0 - ((double)oocMeasurements / totalMeasurements)) * 100.0, 2) : 100.0;

        using var wb = new XLWorkbook();
        
        // Sheet 1: Executive Summary
        var wsSummary = wb.Worksheets.Add("廠級品質執行摘要");
        wsSummary.Cell("B2").Value = "PMR 全廠品管指標與 SPC 執行摘要";
        wsSummary.Cell("B2").Style.Font.Bold = true;
        wsSummary.Cell("B2").Style.Font.FontSize = 18;
        wsSummary.Cell("B2").Style.Font.FontColor = XLColor.DarkBlue;

        wsSummary.Cell("B4").Value = "統計月份:";
        wsSummary.Cell("C4").Value = targetMonth;
        wsSummary.Cell("B5").Value = "報表產出時間:";
        wsSummary.Cell("C5").Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        wsSummary.Cell("B7").Value = "指標名稱";
        wsSummary.Cell("C7").Value = "統計數據";
        wsSummary.Range("B7:C7").Style.Font.Bold = true;
        wsSummary.Range("B7:C7").Style.Fill.BackgroundColor = XLColor.SteelBlue;
        wsSummary.Range("B7:C7").Style.Font.FontColor = XLColor.White;

        wsSummary.Cell("B8").Value = "總量測數據筆數";
        wsSummary.Cell("C8").Value = totalMeasurements;
        wsSummary.Cell("B9").Value = "異常通報件數 (未結案)";
        wsSummary.Cell("C9").Value = alertCount;
        wsSummary.Cell("B10").Value = "SPC 觸發失控/判定不良次數";
        wsSummary.Cell("C10").Value = oocMeasurements;
        wsSummary.Cell("B11").Value = "全廠製程合格率 (%)";
        wsSummary.Cell("C11").Value = passRate + "%";

        wsSummary.Range("B8:C11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        wsSummary.Range("B8:C11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        wsSummary.Columns().AdjustToContents();

        // Sheet 2: Master CPK Table
        var wsCpk = wb.Worksheets.Add("各項料號 CPK 總表");
        string[] headers = ["項次", "產品料號", "產品名稱", "工站名稱", "檢驗特性", "子組大小 N", "USL", "LSL", "量測平均數", "標準差 (Sigma)", "Cp", "Cpk", "能力判定狀態"];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = wsCpk.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.Navy;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int rowNo = 2;
        int idx = 1;
        foreach (var ppc in characteristics)
        {
            var measurements = await dbContext.VariableMeasurements
                .Where(x => x.PartProcessCharacteristicId == ppc.Id)
                .Select(x => x.MeasuredValue)
                .ToListAsync();

            double mean = 0;
            double stdev = 0;
            double cp = 0;
            double cpk = 0;
            string status = "未檢測";

            if (measurements.Count > 1)
            {
                mean = measurements.Average();
                double sumOfSquares = measurements.Sum(val => Math.Pow(val - mean, 2));
                stdev = Math.Sqrt(sumOfSquares / (measurements.Count - 1));

                double usl = ppc.USL ?? (mean + 3 * stdev);
                double lsl = ppc.LSL ?? (mean - 3 * stdev);

                if (stdev > 0)
                {
                    cp = Math.Round((usl - lsl) / (6 * stdev), 2);
                    double cpu = (usl - mean) / (3 * stdev);
                    double cpl = (mean - lsl) / (3 * stdev);
                    cpk = Math.Round(Math.Min(cpu, cpl), 2);
                }

                if (cpk >= 1.33) status = "優良 (A級)";
                else if (cpk >= 1.0) status = "警告 (B級)";
                else status = "能力不足 (C級)";
            }
            else if (measurements.Count == 1)
            {
                mean = measurements[0];
                status = "單筆樣本不足";
            }

            wsCpk.Cell(rowNo, 1).Value = idx++;
            wsCpk.Cell(rowNo, 2).Value = ppc.Part?.PartNo ?? "N/A";
            wsCpk.Cell(rowNo, 3).Value = ppc.Part?.PartName ?? "N/A";
            wsCpk.Cell(rowNo, 4).Value = ppc.Process?.ProcessName ?? "N/A";
            wsCpk.Cell(rowNo, 5).Value = ppc.Characteristic?.CharacteristicName ?? "N/A";
            wsCpk.Cell(rowNo, 6).Value = ppc.SampleSize;
            wsCpk.Cell(rowNo, 7).Value = ppc.USL.HasValue ? ppc.USL.Value.ToString("F2") : "無";
            wsCpk.Cell(rowNo, 8).Value = ppc.LSL.HasValue ? ppc.LSL.Value.ToString("F2") : "無";
            wsCpk.Cell(rowNo, 9).Value = measurements.Count > 0 ? Math.Round(mean, 3) : 0;
            wsCpk.Cell(rowNo, 10).Value = measurements.Count > 1 ? Math.Round(stdev, 4) : 0;
            wsCpk.Cell(rowNo, 11).Value = cp > 0 ? cp : 0;
            
            var cpkCell = wsCpk.Cell(rowNo, 12);
            cpkCell.Value = cpk > 0 ? cpk : 0;

            var statusCell = wsCpk.Cell(rowNo, 13);
            statusCell.Value = status;

            if (status.Contains("優良"))
            {
                cpkCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4EDDA");
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4EDDA");
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#155724");
            }
            else if (status.Contains("警告"))
            {
                cpkCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#856404");
            }
            else if (status.Contains("不足"))
            {
                cpkCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA");
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA");
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#721C24");
            }

            wsCpk.Range(rowNo, 1, rowNo, 13).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsCpk.Range(rowNo, 1, rowNo, 13).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            rowNo++;
        }

        wsCpk.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PMR_Quality_CPK_Summary_{targetMonth.Replace("-", "")}.xlsx");
    }
}
