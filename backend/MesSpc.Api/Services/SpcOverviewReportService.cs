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
        if (endUtc <= startUtc) throw new ArgumentException("報表結束時間必須晚於開始時間。");
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
                new { GroupCode = "CHEM", GroupType = "CONTROL_CHART" },
                new { GroupCode = "CHEM", GroupType = "TREND_CHART" }
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
                ct: ct,
                exactUtcRange: true);
            summaryRows.AddRange(rows);
        }

        summaryRows = summaryRows
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
        var rows = summaryRows.ToList();
        var conflicting = rows.GroupBy(x => x.PartProcessCharacteristicId)
            .Any(g => g.Select(CategorySheet).Distinct().Count() > 1);
        if (conflicting) throw new InvalidOperationException("同一管制項目具有衝突的月報分類。");
        rows = rows.DistinctBy(x => x.PartProcessCharacteristicId).ToList();
        var categories = new[] { "製程管制", "藥液管制", "藥液趨勢" }
            .Concat(rows.Select(CategorySheet).Distinct().Where(x => x is not ("製程管制" or "藥液管制" or "藥液趨勢")));
        foreach (var category in categories)
        {
            var ws = wb.Worksheets.Add(category);
            string[] headers = ["ID", T("reportControlCategory"), T("reportChartKindHeader"), T("reportProcessLine"), T("reportSlot"), T("reportChartName"), T("reportChartTypeHeader"),
                "USL", "LSL", "UCL", "LCL", T("reportLimitMethod"), T("reportMeasurementsCurrent"), T("reportOosCurrent"), T("reportOosPrevious"),
                T("reportOosPercentCurrent"), T("reportOosPercentPrevious"), T("reportOocCurrent"), T("reportOocPercentCurrent"), "Ca", "Pp", T("reportPpkCurrent"), T("reportPpkPrevious"),
                T("reportOwner"), T("reportRemarks"), "異常件數", "已處置件數", "未處置件數"];
            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(3, i + 1).Value = headers[i];
                ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.Navy;
                ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }

            ws.Cell(1, 1).Value = T("reportOverviewTitle");
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Range("D1:Y1").Merge().Value = "依查詢當下處置狀態統計，非月底快照；未處置含處置中及無處置紀錄。";
            ws.Cell(2, 1).Value = T("reportPeriod");
            ws.Cell(2, 2).Value = $"{startUtc:yyyy-MM-dd HH:mm} ~ {endUtc:yyyy-MM-dd HH:mm}";
            ws.Range(1, 1, 2, headers.Length).Style.Fill.BackgroundColor = XLColor.LightYellow;

            var row = 4;
            foreach (var item in rows.Where(x => CategorySheet(x) == category))
            {
                object?[] values = [item.PartProcessCharacteristicId, item.ControlCategory, SpcReportPresentation.Display(item.ChartKindMessage, item.ChartKind, uiLocale),
                    item.LineOrProcessName, item.SlotName, CharacteristicNamePresentation.Summary(item, uiLocale), item.ChartType,
                    item.Usl, item.Lsl, item.Ucl, item.Lcl, SpcReportPresentation.Display(item.LimitCalculationMethodMessage, item.LimitCalculationMethod, uiLocale),
                    item.TotalCount, item.OosCount, item.PreviousMonthOosCount,
                    item.OosPercentage, item.PreviousMonthOosPercentage, item.OocCount,
                    item.OocPercentage, item.Ca, item.Pp, item.Ppk, item.PreviousMonthPpk,
                    item.ResponsibleUser, item.Remarks, item.AbnormalCount, item.HandledCount, item.UnhandledCount];
                for (var i = 0; i < values.Length; i++) ws.Cell(row, i + 1).Value = XLCellValue.FromObject(values[i] ?? "");
                row++;
            }
            if (row == 4) ws.Cell(4, 1).Value = "無資料";
            ws.Columns().AdjustToContents();
            // Server font measurement can underestimate Chinese glyph widths.
            for (var col = 1; col <= headers.Length; col++)
            {
                var textWidth = ws.Range(3, col, Math.Max(4, row - 1), col).Cells()
                    .Max(cell => cell.GetFormattedString().Sum(c => c > 127 ? 2d : 1d));
                ws.Column(col).Width = Math.Min(255, Math.Max(ws.Column(col).Width, textWidth + 3));
                if (textWidth + 3 > 255)
                    ws.Range(3, col, Math.Max(4, row - 1), col).Style.Alignment.WrapText = true;
            }
            ws.RowsUsed().AdjustToContents();
            ws.SheetView.FreezeRows(3);
        }
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Uses stable source codes rather than translated or editable display labels.</summary>
    private static string CategorySheet(ChartSummaryDto row) => (row.GroupCode.Trim().ToUpperInvariant(), row.GroupType.Trim().ToUpperInvariant()) switch
    {
        ("PROCESS", "CONTROL_CHART") => "製程管制",
        ("CHEM", "CONTROL_CHART") => "藥液管制",
        ("CHEM", "TREND_CHART") => "藥液趨勢",
        ("PRODUCT", _) => "產品管制",
        ("DUST", _) => "落塵管制",
        _ => "未分類"
    };
}
