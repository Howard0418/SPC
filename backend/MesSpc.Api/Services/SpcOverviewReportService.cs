using ClosedXML.Excel;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class SpcOverviewReportService(AppDbContext db, SpcService spcService)
{
    public async Task<byte[]> BuildExcelAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
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

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("項目管制總覽");
        string[] headers = ["ID", "管制類別", "圖表類型", "製程線別", "槽位", "管制圖名稱", "管制圖種類",
            "USL", "LSL", "UCL", "LCL", "管制界線計算方式", "本期量測數", "本期OOS", "上月OOS",
            "本期%OOS", "上月%OOS", "本期OOC", "本期%OOC", "Ca", "Pp", "本期Ppk", "上月Ppk",
            "工程負責人", "備註"];
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(3, i + 1).Value = headers[i];
            ws.Cell(3, i + 1).Style.Font.Bold = true;
            ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.Navy;
            ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
        }

        ws.Cell(1, 1).Value = "SPC 管制項目總覽報表";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "報表期間";
        ws.Cell(2, 2).Value = $"{startUtc:yyyy-MM-dd HH:mm} ~ {endUtc:yyyy-MM-dd HH:mm}";
        ws.Range(1, 1, 2, headers.Length).Style.Fill.BackgroundColor = XLColor.LightYellow;

        var row = 4;
        foreach (var item in summaryRows)
        {
            object?[] values = [item.PartProcessCharacteristicId, item.ControlCategory, item.ChartKind,
                item.LineOrProcessName, item.SlotName, item.ChartName, item.ChartType,
                item.Usl, item.Lsl, item.Ucl, item.Lcl, item.LimitCalculationMethod,
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
