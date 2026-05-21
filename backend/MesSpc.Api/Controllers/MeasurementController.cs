using CsvHelper;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Domain.Enums;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MesSpc.Api.Controllers;

public record MeasurementValueDto(int InspectionItemId, int SampleNo, double? ValueNumeric, string? ValueText, bool? ValueBool);
public record CreateBatchDto(string BatchNo, int ProductId, int StationId, DateTime MeasuredAt, string? OperatorName, List<MeasurementValueDto> Values);

[ApiController]
[Route("api/measurement-batches")]
[Route("api/v1/measurement-batches")]
public class MeasurementBatchesController(AppDbContext db, SpcService spcService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await db.MeasurementBatches.Include(x => x.Values).OrderByDescending(x => x.MeasuredAt).Take(100).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var batch = await db.MeasurementBatches.Include(x => x.Values).FirstOrDefaultAsync(x => x.Id == id);
        return batch is null ? NotFound() : Ok(batch);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBatchDto req)
    {
        var batch = new MeasurementBatch
        {
            BatchNo = req.BatchNo,
            ProductId = req.ProductId,
            StationId = req.StationId,
            MeasuredAt = req.MeasuredAt,
            OperatorName = req.OperatorName,
            SourceType = SourceType.Manual,
            Values = req.Values.Select(v => new MeasurementValue
            {
                InspectionItemId = v.InspectionItemId,
                SampleNo = v.SampleNo,
                ValueNumeric = v.ValueNumeric,
                ValueText = v.ValueText,
                ValueBool = v.ValueBool
            }).ToList()
        };

        db.MeasurementBatches.Add(batch);
        await db.SaveChangesAsync();
        var alerts = await spcService.EvaluateBatchAsync(batch);
        return Ok(new { batch, alerts });
    }
}

[ApiController]
[Route("api/measurements")]
[Route("api/v1/measurements")]
public class MeasurementImportController(AppDbContext db, SpcService spcService) : ControllerBase
{
    [HttpPost("import-csv")]
    public async Task<IActionResult> ImportCsv(IFormFile file, [FromQuery] int productId, [FromQuery] int stationId, [FromQuery] DateTime? measuredAt)
    {
        if (file.Length == 0) return BadRequest("CSV is empty.");
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<CsvRow>().ToList();

        var batch = new MeasurementBatch
        {
            BatchNo = $"CSV-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ProductId = productId,
            StationId = stationId,
            MeasuredAt = measuredAt ?? DateTime.UtcNow,
            SourceType = SourceType.Csv,
            Values = rows.Select(r => new MeasurementValue
            {
                InspectionItemId = r.InspectionItemId,
                SampleNo = r.SampleNo,
                ValueNumeric = r.ValueNumeric
            }).ToList()
        };

        db.MeasurementBatches.Add(batch);
        await db.SaveChangesAsync();
        var alerts = await spcService.EvaluateBatchAsync(batch);
        return Ok(new { batchId = batch.Id, importedCount = batch.Values.Count, alertsCount = alerts.Count });
    }

    public class CsvRow
    {
        public int InspectionItemId { get; set; }
        public int SampleNo { get; set; } = 1;
        public double ValueNumeric { get; set; }
    }
}
