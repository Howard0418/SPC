using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/chemicals")]
[Route("api/v1/chemicals")]
public class ChemicalsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = db.Chemicals.AsNoTracking();
        if (!includeDeleted) query = query.Where(x => !x.IsDeleted);

        var rows = await query
            .OrderBy(x => x.ChemicalCode)
            .Select(x => new
            {
                x.Id,
                x.ChemicalCode,
                x.ChemicalName,
                x.ChemicalType,
                x.IsActive,
                x.CreatedAt,
                x.CreatedBy,
                x.UpdatedAt,
                x.UpdatedBy
            })
            .ToListAsync(ct);

        return Ok(rows);
    }
}
