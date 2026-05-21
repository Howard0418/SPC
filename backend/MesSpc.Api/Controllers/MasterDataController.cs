using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/products")]
[Route("api/v1/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Products.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(Product req) { db.Products.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, Product req) { var x = await db.Products.FindAsync(id); if (x is null) return NotFound(); x.ProductCode = req.ProductCode; x.ProductName = req.ProductName; x.IsActive = req.IsActive; await db.SaveChangesAsync(); return Ok(x); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Products.FindAsync(id); if (x is null) return NotFound(); db.Products.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/stations")]
[Route("api/v1/stations")]
public class StationsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Stations.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(Station req) { db.Stations.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, Station req) { var x = await db.Stations.FindAsync(id); if (x is null) return NotFound(); x.StationCode = req.StationCode; x.StationName = req.StationName; x.IsActive = req.IsActive; await db.SaveChangesAsync(); return Ok(x); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Stations.FindAsync(id); if (x is null) return NotFound(); db.Stations.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/inspection-items")]
[Route("api/v1/inspection-items")]
public class InspectionItemsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.InspectionItems.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(InspectionItem req) { db.InspectionItems.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, InspectionItem req)
    {
        var x = await db.InspectionItems.FindAsync(id); if (x is null) return NotFound();
        x.ItemCode = req.ItemCode; x.ItemName = req.ItemName; x.DataType = req.DataType; x.Unit = req.Unit;
        x.Usl = req.Usl; x.Lsl = req.Lsl; x.Ucl = req.Ucl; x.Lcl = req.Lcl; x.TargetValue = req.TargetValue; x.IsSpcEnabled = req.IsSpcEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.InspectionItems.FindAsync(id); if (x is null) return NotFound(); db.InspectionItems.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/product-station-items")]
[Route("api/v1/product-station-items")]
public class ProductStationItemsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? productId, [FromQuery] int? stationId)
    {
        var query = db.ProductStationItems.AsQueryable();
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (stationId.HasValue) query = query.Where(x => x.StationId == stationId.Value);
        return Ok(await query.OrderBy(x => x.Id).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Create(ProductStationItem req) { db.ProductStationItems.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, ProductStationItem req) { var x = await db.ProductStationItems.FindAsync(id); if (x is null) return NotFound(); x.ProductId = req.ProductId; x.StationId = req.StationId; x.InspectionItemId = req.InspectionItemId; x.SampleSize = req.SampleSize; x.IsActive = req.IsActive; await db.SaveChangesAsync(); return Ok(x); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.ProductStationItems.FindAsync(id); if (x is null) return NotFound(); db.ProductStationItems.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}
