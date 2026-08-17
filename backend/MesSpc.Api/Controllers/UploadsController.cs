using ClosedXML.Excel;
using CsvHelper;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Route("api/v1/uploads")]
public class UploadsController(UploadService uploadService)
    : ControllerBase
{
    [HttpPost("variable")]
    public async Task<IActionResult> UploadVariable([FromBody] List<Dictionary<string, string?>> rows, [FromQuery] Guid? clientBatchId = null)
    {
        var jsonStr = System.Text.Json.JsonSerializer.Serialize(rows);
        var hashBytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(jsonStr));
        var hashStr = Convert.ToHexString(hashBytes);

        try
        {
            var batch = await uploadService.CreateVariableBatchAsync(rows, "Api", "api-user", null, hashStr, uploadBatchId: clientBatchId);
            return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_FILE")
        {
            return Conflict(new { message = "檔案已重複上傳" });
        }
    }

    [HttpPost("attribute")]
    public async Task<IActionResult> UploadAttribute([FromBody] List<Dictionary<string, string?>> rows, [FromQuery] Guid? clientBatchId = null)
    {
        var jsonStr = System.Text.Json.JsonSerializer.Serialize(rows);
        var hashBytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(jsonStr));
        var hashStr = Convert.ToHexString(hashBytes);

        try
        {
            var batch = await uploadService.CreateAttributeBatchAsync(rows, "Api", "api-user", null, hashStr, uploadBatchId: clientBatchId);
            return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_FILE")
        {
            return Conflict(new { message = "檔案已重複上傳" });
        }
    }

    [HttpPost("variable/excel")]
    public async Task<IActionResult> UploadVariableExcel(IFormFile file) => await UploadExcelFileAsync(file, true);

    [HttpPost("attribute/excel")]
    public async Task<IActionResult> UploadAttributeExcel(IFormFile file) => await UploadExcelFileAsync(file, false);

    [HttpPost("variable/csv")]
    public async Task<IActionResult> UploadVariableCsv(IFormFile file) => await UploadCsvLike(file, true);

    [HttpPost("attribute/csv")]
    public async Task<IActionResult> UploadAttributeCsv(IFormFile file) => await UploadCsvLike(file, false);

    [HttpGet("{uploadBatchId:guid}/preview")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Preview(Guid uploadBatchId)
    {
        var preview = await uploadService.GetPreviewAsync(uploadBatchId);
        return preview is null ? NotFound() : Ok(preview);
    }

    [HttpGet("{uploadBatchId:guid}/progress")]
    public async Task<IActionResult> Progress(Guid uploadBatchId)
    {
        var progress = await uploadService.GetProgressAsync(uploadBatchId);
        return progress is null ? NotFound() : Ok(progress);
    }

    [HttpGet("{uploadBatchId:guid}/chart-targets")]
    public async Task<IActionResult> ChartTargets(Guid uploadBatchId)
    {
        var result = await uploadService.GetChartTargetsAsync(uploadBatchId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{uploadBatchId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid uploadBatchId, [FromQuery] string mode = "upsert")
    {
        if (mode is not ("upsert" or "insertOnly"))
            return BadRequest(new { message = "不支援的匯入模式。" });
        var result = await uploadService.ConfirmAsync(uploadBatchId, mode);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{uploadBatchId:guid}/create-missing-mappings")]
    public async Task<IActionResult> CreateMissingMappings(Guid uploadBatchId)
    {
        var result = await uploadService.CreateMissingMappingsAndRevalidateAsync(uploadBatchId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{uploadBatchId:guid}")]
    public async Task<IActionResult> Delete(Guid uploadBatchId)
    {
        var deleted = await uploadService.DeleteBatchAsync(uploadBatchId);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<IActionResult> UploadExcelFileAsync(IFormFile file, bool isVariable)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        
        string fileHash;
        using (var hashStream = file.OpenReadStream())
        {
            var hashBytes = await System.Security.Cryptography.MD5.HashDataAsync(hashStream);
            fileHash = Convert.ToHexString(hashBytes);
        }

        using var stream = file.OpenReadStream();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        var rows = new List<Dictionary<string, string?>>();
        var firstCellStr = ws.Cell(1, 1).GetString();
        var c3Str = ws.Cell(3, 3).GetString();
        var c7Str = ws.Cell(7, 3).GetString();
        var isChemicalMatrix = firstCellStr.Contains("藥液分析") || c3Str.Contains("線別") || c7Str.Contains("USL") || ws.Cell(8, 3).GetString().Contains("LSL");

        if (isChemicalMatrix)
        {
            var maxCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var maxRow = ws.LastRowUsed()?.RowNumber() ?? 0;

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
        }
        else
        {
            var headerRow = ws.Row(1);
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var headers = new List<string>();
            for (int c = 1; c <= lastCol; c++)
            {
                headers.Add(headerRow.Cell(c).GetString().Trim());
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 2; r <= lastRow; r++)
            {
                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                bool hasData = false;
                for (int c = 1; c <= lastCol; c++)
                {
                    var val = ws.Cell(r, c).GetString().Trim();
                    if (!string.IsNullOrEmpty(val)) hasData = true;
                    if (c - 1 < headers.Count)
                    {
                        dict[headers[c - 1]] = val;
                    }
                }
                if (hasData) rows.Add(dict);
            }
        }

        if (rows.Count == 0) return BadRequest("No data rows found in Excel.");

        try
        {
            var batch = isVariable
                ? await uploadService.CreateVariableBatchAsync(rows, "File", "excel-user", file.FileName, fileHash)
                : await uploadService.CreateAttributeBatchAsync(rows, "File", "excel-user", file.FileName, fileHash);
            return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_FILE")
        {
            return Conflict(new { message = "檔案已重複上傳" });
        }
    }

    private async Task<IActionResult> UploadCsvLike(IFormFile file, bool isVariable)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        
        string fileHash;
        using (var hashStream = file.OpenReadStream())
        {
            var hashBytes = await System.Security.Cryptography.MD5.HashDataAsync(hashStream);
            fileHash = Convert.ToHexString(hashBytes);
        }

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = ReadDictionaryRows(csv);
        if (rows.Count == 0) return BadRequest("No data rows found.");

        try
        {
            var batch = isVariable
                ? await uploadService.CreateVariableBatchAsync(rows, "File", "file-user", file.FileName, fileHash)
                : await uploadService.CreateAttributeBatchAsync(rows, "File", "file-user", file.FileName, fileHash);
            return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE_FILE")
        {
            return Conflict(new { message = "檔案已重複上傳" });
        }
    }

    private static List<Dictionary<string, string?>> ReadDictionaryRows(CsvReader csv)
    {
        var rows = new List<Dictionary<string, string?>>();
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        while (csv.Read())
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in headers)
            {
                row[h] = csv.GetField(h);
            }
            rows.Add(row);
        }
        return rows;
    }

    [HttpGet("template/variable")]
    public IActionResult GetVariableTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("計量型資料匯入範本");

        var headers = new string[]
        {
            "管制類型", "料號", "製程", "機台", "槽位", "檢驗項目", "測量值", "複驗", "調整", "調整量", "日期", "作業員", "批號", "樣本編號", "序號"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1E, 0x3A, 0x8A); // Deep Blue
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Add 3 sample rows
        var samples = new object[][]
        {
            new object[] { "PROCESS", "PART-A001", "ST-01", "ST-01-M01", "", "LENGTH", 100.12, "", "", "", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "OP-01", "L20260518-1", 1, "WO-101" },
            new object[] { "CHEM", "", "CHEM-PROC", "N2", "C1", "H2SO4", 3.25, 3.10, "添加", 0.5, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "OP-01", "", 1, "" },
            new object[] { "PROCESS", "PART-A001", "ST-01", "ST-01-M01", "", "WIDTH", 50.05, "", "", "", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "OP-02", "L20260518-2", 1, "WO-102" }
        };

        for (int r = 0; r < samples.Length; r++)
        {
            for (int c = 0; c < samples[r].Length; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                cell.Value = samples[r][c].ToString();
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Variable_Import_Template.xlsx");
    }

    [HttpGet("template/attribute")]
    public IActionResult GetAttributeTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("計數型資料匯入範本");

        var headers = new string[]
        {
            "料號", "製程", "機台", "檢驗項目", "總數", "不良數", "缺點數", "單位數", "日期", "作業員", "批號", "樣本編號"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x0D, 0x94, 0x88); // Teal
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Add 2 sample rows
        ws.Cell(2, 1).Value = "PART-B001";
        ws.Cell(2, 2).Value = "ST-02";
        ws.Cell(2, 3).Value = "ST-02-M01";
        ws.Cell(2, 4).Value = "DEFECT_RATE";
        ws.Cell(2, 5).Value = 500;
        ws.Cell(2, 6).Value = 12;
        ws.Cell(2, 7).Value = 15;
        ws.Cell(2, 8).Value = 500;
        ws.Cell(2, 9).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ws.Cell(2, 10).Value = "OP-02";
        ws.Cell(2, 11).Value = "L20260518-A";
        ws.Cell(2, 12).Value = 1;

        ws.Cell(3, 1).Value = "PART-B001";
        ws.Cell(3, 2).Value = "ST-02";
        ws.Cell(3, 3).Value = "ST-02-M01";
        ws.Cell(3, 4).Value = "DEFECT_RATE";
        ws.Cell(3, 5).Value = 500;
        ws.Cell(3, 6).Value = 8;
        ws.Cell(3, 7).Value = 9;
        ws.Cell(3, 8).Value = 500;
        ws.Cell(3, 9).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ws.Cell(3, 10).Value = "OP-03";
        ws.Cell(3, 11).Value = "L20260518-B";
        ws.Cell(3, 12).Value = 1;

        for (int r = 2; r <= 3; r++)
        {
            for (int c = 1; c <= 12; c++)
            {
                ws.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Attribute_Import_Template.xlsx");
    }
}

[ApiController]
[Route("api/v2/spc")]
public class SpcV2Controller(AppDbContext db) : ControllerBase
{
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculateReq req)
    {
        var query = db.SpcCalculationResults.AsQueryable();
        if (req.UploadBatchId.HasValue) query = query.Where(x => x.UploadBatchId == req.UploadBatchId.Value);
        var count = await query.CountAsync();
        return Ok(new { calculated = count });
    }

    [HttpGet("results")]
    public async Task<IActionResult> Results([FromQuery] Guid? uploadBatchId)
    {
        var query = db.SpcCalculationResults.AsQueryable();
        if (uploadBatchId.HasValue) query = query.Where(x => x.UploadBatchId == uploadBatchId.Value);
        return Ok(await query.OrderByDescending(x => x.Id).Take(500).ToListAsync());
    }

    [HttpGet("chart")]
    public async Task<IActionResult> Chart([FromQuery] int partProcessCharacteristicId)
    {
        var data = await db.SpcCalculationResults
            .Where(x => x.PartProcessCharacteristicId == partProcessCharacteristicId)
            .OrderBy(x => x.CalculatedAt)
            .Take(500)
            .Select(x => new { x.CalculatedAt, x.StatisticValue, x.UCL, x.CL, x.LCL, x.IsOutOfControl, x.IsOutOfSpec })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("interactive-chart")]
    public async Task<IActionResult> InteractiveChart(
        [FromQuery] int productId, 
        [FromQuery] int stationId, 
        [FromQuery] int inspectionItemId, 
        [FromQuery] string? batchNo = null,
        CancellationToken ct = default)
    {
        return LegacyV2Api.Gone("/api/v1/spc/chart?ppcId={ppcId}");
    }

    [HttpPost("exclude-batch/{uploadBatchId:guid}")]
    public IActionResult ToggleExcludeUploadBatch(Guid uploadBatchId, CancellationToken ct = default) =>
        LegacyV2Api.Gone("/api/v1/spc/exclude-batch/{uploadBatchId}");

    [HttpPost("exclude-batch/{batchId:int}")]
    public IActionResult ToggleExcludeBatch(int batchId, CancellationToken ct = default) =>
        LegacyV2Api.Gone("/api/v1/spc/exclude-batch/{uploadBatchId}");

    [HttpGet("chart-types")]
    public async Task<IActionResult> ChartTypes() => Ok(await db.ControlChartTypes.Where(x => x.IsEnabled).OrderBy(x => x.ChartTypeCode).ToListAsync());

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts() => Ok(await db.AlertEvents.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync());

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> DashboardStats()
    {
        var today = DateTime.UtcNow.Date;
        var trendData = await db.VariableMeasurements
            .Where(x => x.MeasuredAt >= today)
            .GroupBy(x => x.MeasuredAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync();

        var characteristics = await db.PartProcessCharacteristics
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .Where(x => x.IsEnabled)
            .ToListAsync();

        var cpkList = new List<object>();
        foreach (var ppc in characteristics)
        {
            var measurements = await db.VariableMeasurements
                .Where(x => x.PartProcessCharacteristicId == ppc.Id)
                .Select(x => x.MeasuredValue)
                .ToListAsync();

            if (measurements.Count > 1)
            {
                double mean = measurements.Average();
                double sumOfSquares = measurements.Sum(val => Math.Pow(val - mean, 2));
                double stdev = Math.Sqrt(sumOfSquares / (measurements.Count - 1));

                double usl = ppc.USL ?? (mean + 3 * stdev);
                double lsl = ppc.LSL ?? (mean - 3 * stdev);

                if (stdev > 0)
                {
                    double cpu = (usl - mean) / (3 * stdev);
                    double cpl = (mean - lsl) / (3 * stdev);
                    double cpk = Math.Round(Math.Min(cpu, cpl), 2);
                    
                    cpkList.Add(new {
                        Name = $"{ppc.Process?.ProcessName} {ppc.Characteristic?.CharacteristicName}",
                        Cpk = cpk
                    });
                }
            }
        }
        
        var bottom5Cpk = cpkList.OrderBy(x => ((dynamic)x).Cpk).Take(5).ToList();
        
        // Pareto Chart Data (Top Alerts by Process/Characteristic)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var allAlertGroups = await db.AlertEvents
            .Where(x => x.OccurredAt >= thirtyDaysAgo && x.ProcessId > 0 && x.CharacteristicId > 0)
            .GroupBy(x => new { x.ProcessId, x.CharacteristicId })
            .Select(g => new { g.Key.ProcessId, g.Key.CharacteristicId, Count = g.Count() })
            .ToListAsync();
            
        var totalAlerts = allAlertGroups.Sum(x => x.Count);
        var topAlertGroups = allAlertGroups.OrderByDescending(x => x.Count).Take(10).ToList();

        var paretoData = new List<object>();
        double cumPct = 0;
        
        foreach (var item in topAlertGroups)
        {
            var proc = await db.Processes.FindAsync(item.ProcessId);
            var ch = await db.QualityCharacteristics.FindAsync(item.CharacteristicId);
            var name = $"{proc?.ProcessName ?? $"P-{item.ProcessId}"} - {ch?.CharacteristicName ?? $"C-{item.CharacteristicId}"}";
            
            var pct = totalAlerts > 0 ? (double)item.Count / totalAlerts * 100 : 0;
            cumPct += pct;
            paretoData.Add(new { Name = name, Count = item.Count, CumulativePercentage = Math.Round(cumPct, 1) });
        }
        
        return Ok(new { Trend = trendData, BottomCpk = bottom5Cpk, Pareto = paretoData });
    }

    public record CalculateReq(Guid? UploadBatchId);
}
