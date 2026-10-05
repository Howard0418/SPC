using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/control-limit-segments")]
[Route("api/v1/control-limit-segments")]
public class ControlLimitSegmentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int ppcId)
    {
        var list = await db.ControlLimitSegments
            .Where(x => x.PartProcessCharacteristicId == ppcId)
            .OrderBy(x => x.StartDate)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ControlLimitSegmentRequest req)
    {
        var ppcExists = await db.PartProcessCharacteristics.AnyAsync(x => x.Id == req.PartProcessCharacteristicId && x.IsEnabled);
        if (!ppcExists) return BadRequest("SPC 管制項目設定不存在。");

        if (req.StartDate == default) return BadRequest("起日為必填。");
        if (req.EndDate.HasValue && req.StartDate > req.EndDate.Value) return BadRequest("起日不可大於迄日。");

        var start = req.StartDate;
        var end = req.EndDate ?? DateTime.MaxValue;

        var overlap = await db.ControlLimitSegments
            .AnyAsync(s => s.PartProcessCharacteristicId == req.PartProcessCharacteristicId
                        && s.StartDate <= end
                        && (s.EndDate == null || s.EndDate >= start));
        if (overlap)
        {
            return BadRequest("分段時間區間不可重疊，請調整起迄日期。");
        }

        var segment = new ControlLimitSegment
        {
            PartProcessCharacteristicId = req.PartProcessCharacteristicId,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            UCL = req.UCL,
            CL = req.CL,
            LCL = req.LCL,
            USL = req.USL,
            LSL = req.LSL,
            TargetValue = req.TargetValue,
            Note = req.Note
        };

        db.ControlLimitSegments.Add(segment);
        await db.SaveChangesAsync();
        return Ok(segment);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ControlLimitSegmentRequest req)
    {
        var x = await db.ControlLimitSegments.FindAsync(id);
        if (x is null) return NotFound("此分段設定不存在。");

        if (req.StartDate == default) return BadRequest("起日為必填。");
        if (req.EndDate.HasValue && req.StartDate > req.EndDate.Value) return BadRequest("起日不可大於迄日。");

        var start = req.StartDate;
        var end = req.EndDate ?? DateTime.MaxValue;

        var overlap = await db.ControlLimitSegments
            .AnyAsync(s => s.PartProcessCharacteristicId == x.PartProcessCharacteristicId
                        && s.Id != id
                        && s.StartDate <= end
                        && (s.EndDate == null || s.EndDate >= start));
        if (overlap)
        {
            return BadRequest("分段時間區間不可重疊，請調整起迄日期。");
        }

        x.StartDate = req.StartDate;
        x.EndDate = req.EndDate;
        x.UCL = req.UCL;
        x.CL = req.CL;
        x.LCL = req.LCL;
        x.USL = req.USL;
        x.LSL = req.LSL;
        x.TargetValue = req.TargetValue;
        x.Note = req.Note;

        await db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await db.ControlLimitSegments.FindAsync(id);
        if (x is null) return NotFound("此分段設定不存在。");

        db.ControlLimitSegments.Remove(x);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public sealed class ControlLimitSegmentRequest
{
    public int PartProcessCharacteristicId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    public double? USL { get; set; }
    public double? LSL { get; set; }
    public double? TargetValue { get; set; }
    public string? Note { get; set; }
}
