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
        return Ok(await db.ProductionLines.OrderBy(x => x.LineCode).ToListAsync());
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
        if (lineId.HasValue) query = query.Where(t => t.LineId == lineId.Value);
        return Ok(await query.OrderBy(x => x.TankCode).ToListAsync());
    }

    [HttpPost("tanks")]
    public async Task<IActionResult> CreateTank(Tank req)
    {
        db.Tanks.Add(req);
        await db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("tanks/{id:int}")]
    public async Task<IActionResult> UpdateTank(int id, Tank req)
    {
        var x = await db.Tanks.FindAsync(id);
        if (x is null) return NotFound();
        x.TankCode = req.TankCode;
        x.TankName = req.TankName;
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
