using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/import-precheck")]
public class ImportPrecheckController(AppDbContext db) : ControllerBase
{
    [HttpGet("master-data")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetMasterData([FromQuery] string scope = "CHEM", CancellationToken ct = default)
    {
        var normalizedScope = scope.Trim().ToUpperInvariant();
        if (normalizedScope != "CHEM")
            return BadRequest(new { message = "目前主檔預檢只支援 CHEM。" });

        var lines = await db.ProductionLines.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.LineCode)
            .Select(x => new { x.Id, x.LineCode, x.LineName })
            .ToListAsync(ct);

        var tanks = await (
            from tank in db.Tanks.AsNoTracking()
            join line in db.ProductionLines.AsNoTracking() on tank.LineId equals line.Id
            where tank.IsActive && line.IsActive
            orderby line.LineCode, tank.SequenceNo, tank.Id
            select new { tank.Id, line.LineCode, tank.TankCode, tank.TankName, tank.TankNameEn, tank.SequenceNo }
        ).ToListAsync(ct);

        var characteristics = await db.QualityCharacteristics.AsNoTracking()
            // 品質特性是全廠共用主檔，不再隸屬管制類型；CHEM 範圍由
            // PartProcessCharacteristic 的 ControlScope 決定。
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.CharacteristicCode)
            .Select(x => new { x.Id, x.CharacteristicCode, x.CharacteristicName, x.CharacteristicNameEn })
            .ToListAsync(ct);

        var mappings = await db.PartProcessCharacteristics.AsNoTracking()
            .Where(x => x.IsEnabled && x.ControlScope == normalizedScope)
            .Include(x => x.Process)
            .Include(x => x.Machine)
            .Include(x => x.Tank)
            .Include(x => x.Characteristic)
            .OrderBy(x => x.SequenceNo).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                ProcessCode = x.Process != null ? x.Process.ProcessCode : null,
                MachineCode = x.Machine != null ? x.Machine.MachineCode : null,
                TankCode = x.Tank != null ? x.Tank.TankCode : null,
                TankName = x.Tank != null ? x.Tank.TankName : null,
                CharacteristicCode = x.Characteristic != null ? x.Characteristic.CharacteristicCode : null,
                CharacteristicName = x.Characteristic != null ? x.Characteristic.CharacteristicName : null,
                CharacteristicNameEn = x.Characteristic != null ? x.Characteristic.CharacteristicNameEn : null,
                x.Unit
            })
            .ToListAsync(ct);

        return Ok(new
        {
            scope = normalizedScope,
            generatedAt = DateTime.UtcNow,
            lines,
            tanks,
            characteristics,
            mappings
        });
    }
}
