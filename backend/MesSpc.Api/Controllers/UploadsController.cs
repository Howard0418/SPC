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
    public async Task<IActionResult> UploadVariable([FromBody] List<Dictionary<string, string?>> rows)
    {
        var batch = await uploadService.CreateVariableBatchAsync(rows, "Api", "api-user", null);
        return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
    }

    [HttpPost("attribute")]
    public async Task<IActionResult> UploadAttribute([FromBody] List<Dictionary<string, string?>> rows)
    {
        var batch = await uploadService.CreateAttributeBatchAsync(rows, "Api", "api-user", null);
        return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
    }

    [HttpPost("variable/excel")]
    public async Task<IActionResult> UploadVariableExcel(IFormFile file) => await UploadCsvLike(file, true);

    [HttpPost("attribute/excel")]
    public async Task<IActionResult> UploadAttributeExcel(IFormFile file) => await UploadCsvLike(file, false);

    [HttpPost("variable/csv")]
    public async Task<IActionResult> UploadVariableCsv(IFormFile file) => await UploadCsvLike(file, true);

    [HttpPost("attribute/csv")]
    public async Task<IActionResult> UploadAttributeCsv(IFormFile file) => await UploadCsvLike(file, false);

    [HttpGet("{uploadBatchId:guid}/preview")]
    public async Task<IActionResult> Preview(Guid uploadBatchId)
    {
        var preview = await uploadService.GetPreviewAsync(uploadBatchId);
        return preview is null ? NotFound() : Ok(preview);
    }

    [HttpPost("{uploadBatchId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid uploadBatchId)
    {
        var result = await uploadService.ConfirmAsync(uploadBatchId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{uploadBatchId:guid}")]
    public async Task<IActionResult> Delete(Guid uploadBatchId)
    {
        var deleted = await uploadService.DeleteBatchAsync(uploadBatchId);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<IActionResult> UploadCsvLike(IFormFile file, bool isVariable)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = ReadDictionaryRows(csv);
        if (rows.Count == 0) return BadRequest("No data rows found.");

        var batch = isVariable
            ? await uploadService.CreateVariableBatchAsync(rows, "File", "file-user", file.FileName)
            : await uploadService.CreateAttributeBatchAsync(rows, "File", "file-user", file.FileName);
        return Ok(new { batch.UploadBatchId, batch.ImportStatus, batch.TotalRows, batch.ValidRows, batch.ErrorRows });
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
        return Ok(await query.OrderByDescending(x => x.SpcResultId).Take(500).ToListAsync());
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

    [HttpGet("chart-types")]
    public async Task<IActionResult> ChartTypes() => Ok(await db.ControlChartTypes.Where(x => x.IsEnabled).OrderBy(x => x.ChartTypeCode).ToListAsync());

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts() => Ok(await db.AlertEvents.OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync());

    public record CalculateReq(Guid? UploadBatchId);
}
