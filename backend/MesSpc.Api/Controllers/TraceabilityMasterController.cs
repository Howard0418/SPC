using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/traceability-master")]
public class TraceabilityMasterController(AppDbContext db) : ControllerBase
{
    // --- Production Lines ---
    [HttpGet("lines")]
    public async Task<IActionResult> GetLines()
    {
        return Ok(await db.Processes.AsNoTracking()
            .OrderBy(x => x.SequenceNo).ThenBy(x => x.ProcessCode).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                LineCode = x.ProcessCode,
                LineName = x.ProcessName,
                LineNameEn = x.ProcessNameEn,
                IsActive = x.IsEnabled,
                x.SequenceNo
            })
            .ToListAsync());
    }

    [HttpPost("lines")]
    public async Task<IActionResult> CreateLine(ProductionLine req)
    {
        // For simplicity, hardcode FactoryId to 1 if it exists or fetch first
        if (req.FactoryId == 0)
        {
            var factory = await db.Factories.FirstOrDefaultAsync();
            if (factory == null)
            {
                var plant = await db.Plants.FirstOrDefaultAsync();
                if (plant == null)
                {
                    plant = new Plant { PlantCode = "PLT-01", PlantName = "Main Plant" };
                    db.Plants.Add(plant);
                    await db.SaveChangesAsync();
                }
                factory = new Factory { FactoryCode = "FAC-01", FactoryName = "Main Factory", PlantId = plant.Id };
                db.Factories.Add(factory);
                await db.SaveChangesAsync();
            }
            req.FactoryId = factory.Id;
        }
        db.ProductionLines.Add(req);
        await db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("lines/{id:int}")]
    public async Task<IActionResult> UpdateLine(int id, ProductionLine req)
    {
        var x = await db.ProductionLines.FindAsync(id);
        if (x is null) return NotFound();
        x.LineCode = req.LineCode;
        x.LineName = req.LineName;
        x.Description = req.Description;
        x.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpDelete("lines/{id:int}")]
    public async Task<IActionResult> DeleteLine(int id)
    {
        var x = await db.ProductionLines.FindAsync(id);
        if (x is null) return NotFound();
        db.ProductionLines.Remove(x);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // --- Tanks ---
    [HttpGet("tanks")]
    public async Task<IActionResult> GetTanks([FromQuery] int? lineId)
    {
        var query = db.Tanks.AsQueryable();
        if (lineId.HasValue)
        {
            var process = await db.Processes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lineId.Value);
            if (process is null) return NotFound("找不到指定的工站製程。");
            var lineCodes = await db.Machines.AsNoTracking()
                .Where(x => x.ProcessId == process.Id)
                .Select(x => x.MachineCode)
                .ToListAsync();
            lineCodes.Add(process.ProcessCode);
            var productionLineIds = await db.ProductionLines.AsNoTracking()
                .Where(x => lineCodes.Contains(x.LineCode))
                .Select(x => x.Id)
                .ToListAsync();
            query = query.Where(t => productionLineIds.Contains(t.LineId));
        }
        return Ok(await query.OrderBy(x => x.SequenceNo).ThenBy(x => x.TankCode).ThenBy(x => x.Id).ToListAsync());
    }

    [HttpPost("tanks")]
    public async Task<IActionResult> CreateTank(Tank req)
    {
        if (string.IsNullOrWhiteSpace(req.TankName)) return BadRequest("槽體中文名稱為必填欄位。");
        var process = await db.Processes.FirstOrDefaultAsync(x => x.Id == req.LineId);
        if (process is null) return BadRequest("找不到指定的工站製程。");
        var line = await GetOrCreateProcessLineAsync(process);
        req.LineId = line.Id;
        req.TankCode = $"TANK-TMP-{Guid.NewGuid():N}";
        req.TankName = req.TankName.Trim();
        req.TankNameEn = CleanOptional(req.TankNameEn);
        db.Tanks.Add(req);
        await db.SaveChangesAsync();
        req.TankCode = $"TANK-{req.Id:D6}";
        await db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("tanks/{id:int}")]
    public async Task<IActionResult> UpdateTank(int id, Tank req)
    {
        var x = await db.Tanks.FindAsync(id);
        if (x is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.TankName)) return BadRequest("槽體中文名稱為必填欄位。");
        x.TankName = req.TankName.Trim();
        x.TankNameEn = CleanOptional(req.TankNameEn);
        x.SequenceNo = req.SequenceNo;
        x.Description = req.Description;
        x.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpDelete("tanks/{id:int}")]
    public async Task<IActionResult> DeleteTank(int id)
    {
        var x = await db.Tanks.FindAsync(id);
        if (x is null) return NotFound();
        db.Tanks.Remove(x);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ProductionLine> GetOrCreateProcessLineAsync(Process process)
    {
        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == process.ProcessCode);
        if (line is not null) return line;

        var factory = await db.Factories.FirstOrDefaultAsync();
        if (factory is null)
        {
            var plant = await db.Plants.FirstOrDefaultAsync();
            if (plant is null)
            {
                plant = new Plant { PlantCode = "PLT-01", PlantName = "Main Plant" };
                db.Plants.Add(plant);
                await db.SaveChangesAsync();
            }
            factory = new Factory { FactoryCode = "FAC-01", FactoryName = "Main Factory", PlantId = plant.Id };
            db.Factories.Add(factory);
            await db.SaveChangesAsync();
        }

        line = new ProductionLine { FactoryId = factory.Id, LineCode = process.ProcessCode, LineName = process.ProcessName, IsActive = process.IsEnabled };
        db.ProductionLines.Add(line);
        await db.SaveChangesAsync();
        return line;
    }

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // --- Slots ---
    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots([FromQuery] int? tankId)
    {
        var query = db.Slots.AsQueryable();
        if (tankId.HasValue) query = query.Where(s => s.TankId == tankId.Value);
        return Ok(await query.OrderBy(x => x.SlotCode).ToListAsync());
    }

    [HttpPost("slots")]
    public async Task<IActionResult> CreateSlot(Slot req)
    {
        db.Slots.Add(req);
        await db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("slots/{id:int}")]
    public async Task<IActionResult> UpdateSlot(int id, Slot req)
    {
        var x = await db.Slots.FindAsync(id);
        if (x is null) return NotFound();
        x.SlotCode = req.SlotCode;
        x.SlotName = req.SlotName;
        x.SequenceNo = req.SequenceNo;
        x.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpDelete("slots/{id:int}")]
    public async Task<IActionResult> DeleteSlot(int id)
    {
        var x = await db.Slots.FindAsync(id);
        if (x is null) return NotFound();
        db.Slots.Remove(x);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
