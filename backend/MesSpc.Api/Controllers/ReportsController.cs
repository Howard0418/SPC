using MesSpc.Api.Services.Localization;
using ClosedXML.Excel;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Route("api/v2/reports")]
public class ReportsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("cpk-summary")]
    public async Task<IActionResult> ExportCpkSummary([FromQuery] string? month, [FromQuery] string? uiLocale = null)
    {
        if (Request.Path.StartsWithSegments("/api/v2", StringComparison.OrdinalIgnoreCase))
            return LegacyV2Api.Gone("/api/v1/reports/cpk-summary");

        // This optional query is a display preference for the existing popup download, never a data filter.
        var locale = uiLocale is null ? ApiUiMessageCatalog.RequestLocale(Request) : ApiUiMessageCatalog.NormalizeLocale(uiLocale);
        string T(string key) => SpcReportPresentation.Text(key, locale);
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
        var wsSummary = wb.Worksheets.Add(T("reportExecutiveSheet"));
        wsSummary.Cell("B2").Value = T("reportExecutiveTitle");
        wsSummary.Cell("B2").Style.Font.Bold = true;
        wsSummary.Cell("B2").Style.Font.FontSize = 18;
        wsSummary.Cell("B2").Style.Font.FontColor = XLColor.DarkBlue;

        wsSummary.Cell("B4").Value = T("reportMonth");
        wsSummary.Cell("C4").Value = targetMonth;
        wsSummary.Cell("B5").Value = T("reportGeneratedAt");
        wsSummary.Cell("C5").Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        wsSummary.Cell("B7").Value = T("reportMetric");
        wsSummary.Cell("C7").Value = T("reportMetricValue");
        wsSummary.Range("B7:C7").Style.Font.Bold = true;
        wsSummary.Range("B7:C7").Style.Fill.BackgroundColor = XLColor.SteelBlue;
        wsSummary.Range("B7:C7").Style.Font.FontColor = XLColor.White;

        wsSummary.Cell("B8").Value = T("reportMeasurementsTotal");
        wsSummary.Cell("C8").Value = totalMeasurements;
        wsSummary.Cell("B9").Value = T("reportAlertsOpen");
        wsSummary.Cell("C9").Value = alertCount;
        wsSummary.Cell("B10").Value = T("reportViolationsTotal");
        wsSummary.Cell("C10").Value = oocMeasurements;
        wsSummary.Cell("B11").Value = T("reportPassRate");
        wsSummary.Cell("C11").Value = passRate + "%";

        wsSummary.Range("B8:C11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        wsSummary.Range("B8:C11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        wsSummary.Columns().AdjustToContents();

        // Sheet 2: Master CPK Table
        var wsCpk = wb.Worksheets.Add(T("reportCpkSheet"));
        string[] headers = [T("reportIndex"), T("reportPartNo"), T("reportPartName"), T("reportProcessName"), T("reportCharacteristic"), T("reportSubgroupN"), "USL", "LSL", T("reportMean"), T("reportStdDev"), "Cp", "Cpk", T("reportCapabilityStatus")];
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
            string statusKey = "reportUntested";

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

                if (cpk >= 1.33) { status = "優良 (A級)"; statusKey = "reportGradeA"; }
                else if (cpk >= 1.0) { status = "警告 (B級)"; statusKey = "reportGradeB"; }
                else { status = "能力不足 (C級)"; statusKey = "reportGradeC"; }
            }
            else if (measurements.Count == 1)
            {
                mean = measurements[0];
                status = "單筆樣本不足";
                statusKey = "reportSingleSample";
            }

            wsCpk.Cell(rowNo, 1).Value = idx++;
            wsCpk.Cell(rowNo, 2).Value = ppc.Part?.PartNo ?? "N/A";
            wsCpk.Cell(rowNo, 3).Value = ppc.Part?.PartName ?? "N/A";
            wsCpk.Cell(rowNo, 4).Value = ppc.Process?.ProcessName ?? "N/A";
            wsCpk.Cell(rowNo, 5).Value = ppc.Characteristic?.CharacteristicName ?? "N/A";
            wsCpk.Cell(rowNo, 6).Value = ppc.SampleSize;
            wsCpk.Cell(rowNo, 7).Value = ppc.USL.HasValue ? ppc.USL.Value.ToString("F2") : T("reportNone");
            wsCpk.Cell(rowNo, 8).Value = ppc.LSL.HasValue ? ppc.LSL.Value.ToString("F2") : T("reportNone");
            wsCpk.Cell(rowNo, 9).Value = measurements.Count > 0 ? Math.Round(mean, 3) : 0;
            wsCpk.Cell(rowNo, 10).Value = measurements.Count > 1 ? Math.Round(stdev, 4) : 0;
            wsCpk.Cell(rowNo, 11).Value = cp > 0 ? cp : 0;
            
            var cpkCell = wsCpk.Cell(rowNo, 12);
            cpkCell.Value = cpk > 0 ? cpk : 0;

            var statusCell = wsCpk.Cell(rowNo, 13);
            statusCell.Value = T(statusKey);

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
