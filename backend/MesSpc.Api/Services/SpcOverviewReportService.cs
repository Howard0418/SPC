using MesSpc.Api.Services.Localization;
using MesSpc.Api.Controllers;
using ClosedXML.Excel;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class SpcOverviewReportService(AppDbContext db, SpcService spcService) : ISpcOverviewReportBuilder
{
    public async Task<byte[]> BuildExcelAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default, string? uiLocale = null)
    {
        var dimensions = await db.ControlChartGroups.AsNoTracking()
            .Where(x => x.IsEnabled)
            .Select(x => new { x.GroupCode, x.GroupType })
            .Distinct()
            .ToListAsync(ct);

        if (dimensions.Count == 0)
        {
            dimensions =
            [
                new { GroupCode = "PRODUCT", GroupType = "CONTROL_CHART" },
                new { GroupCode = "PROCESS", GroupType = "CONTROL_CHART" },
                new { GroupCode = "CHEM", GroupType = "CONTROL_CHART" }
            ];
        }

        var summaryRows = new List<MesSpc.Api.Controllers.ChartSummaryDto>();
        foreach (var dimension in dimensions)
        {
            var rows = await spcService.GetChartSummaryListAsync(
                dimension.GroupCode,
                uploadBatchId: null,
                startDate: startUtc,
                endDate: endUtc,
                batchNo: null,
                partId: null,
                groupType: dimension.GroupType,
                ct: ct);
            summaryRows.AddRange(rows);
        }

        summaryRows = summaryRows
            .GroupBy(x => x.PartProcessCharacteristicId)
            .Select(x => x.First())
            .OrderBy(x => x.LineOrProcessName)
            .ThenBy(x => x.SlotName)
            .ThenBy(x => x.ChartName)
            .ToList();

        return BuildWorkbook(summaryRows, startUtc, endUtc, uiLocale);
    }

    /// <summary>Formats human-readable labels only; source values and cell types remain unchanged.</summary>
    public static byte[] BuildWorkbook(IEnumerable<ChartSummaryDto> summaryRows, DateTime startUtc, DateTime endUtc, string? uiLocale = null)
    {
        string T(string key) => SpcReportPresentation.Text(key, uiLocale);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(T("reportOverviewSheet"));
        string[] headers = ["ID", T("reportControlCategory"), T("reportChartKindHeader"), T("reportProcessLine"), T("reportSlot"), T("reportChartName"), T("reportChartTypeHeader"),
            "USL", "LSL", "UCL", "LCL", T("reportLimitMethod"), T("reportMeasurementsCurrent"), T("reportOosCurrent"), T("reportOosPrevious"),
            T("reportOosPercentCurrent"), T("reportOosPercentPrevious"), T("reportOocCurrent"), T("reportOocPercentCurrent"), "Ca", "Pp", T("reportPpkCurrent"), T("reportPpkPrevious"),
            T("reportOwner"), T("reportRemarks")];
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(3, i + 1).Value = headers[i];
            ws.Cell(3, i + 1).Style.Font.Bold = true;
            ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.Navy;
            ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
        }

        ws.Cell(1, 1).Value = T("reportOverviewTitle");
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = T("reportPeriod");
        ws.Cell(2, 2).Value = $"{startUtc:yyyy-MM-dd HH:mm} ~ {endUtc:yyyy-MM-dd HH:mm}";
        ws.Range(1, 1, 2, headers.Length).Style.Fill.BackgroundColor = XLColor.LightYellow;

        var row = 4;
        foreach (var item in summaryRows)
        {
            object?[] values = [item.PartProcessCharacteristicId, item.ControlCategory, SpcReportPresentation.Display(item.ChartKindMessage, item.ChartKind, uiLocale),
                item.LineOrProcessName, item.SlotName, CharacteristicNamePresentation.Summary(item, uiLocale), item.ChartType,
                item.Usl, item.Lsl, item.Ucl, item.Lcl, SpcReportPresentation.Display(item.LimitCalculationMethodMessage, item.LimitCalculationMethod, uiLocale),
                item.TotalCount, item.OosCount, item.PreviousMonthOosCount,
                item.OosPercentage, item.PreviousMonthOosPercentage, item.OocCount,
                item.OocPercentage, item.Ca, item.Pp, item.Ppk, item.PreviousMonthPpk,
                item.ResponsibleUser, item.Remarks];
            for (var i = 0; i < values.Length; i++) ws.Cell(row, i + 1).Value = XLCellValue.FromObject(values[i] ?? "");
            row++;
        }
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(3);
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
