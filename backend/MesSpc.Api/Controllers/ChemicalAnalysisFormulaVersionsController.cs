using MesSpc.Api.Domain;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/part-process-characteristics/{partProcessCharacteristicId:int}/chemical-analysis-formula-versions")]
[Route("api/v1/part-process-characteristics/{partProcessCharacteristicId:int}/chemical-analysis-formula-versions")]
public sealed class ChemicalAnalysisFormulaVersionsController(
    AppDbContext db,
    ChemicalAnalysisFormulaVersionService versions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetVersions(int partProcessCharacteristicId, CancellationToken ct)
    {
        var mapping = await db.PartProcessCharacteristics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == partProcessCharacteristicId, ct);
        if (mapping is null) return NotFound(new { message = "找不到指定的 SPC 管制項目。" });
        if (!string.Equals(ControlScopeCodes.Normalize(mapping.ControlScope), ControlScopeCodes.Chemical, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "只有藥液管制項目支援公式版本紀錄。" });

        var data = await db.ChemicalAnalysisFormulaVersions.AsNoTracking()
            .Where(x => x.PartProcessCharacteristicId == partProcessCharacteristicId)
            .OrderByDescending(x => x.VersionNo)
            .ThenByDescending(x => x.Id)
            .Select(x => new ChemicalAnalysisFormulaVersionResponse(
                x.Id,
                x.PartProcessCharacteristicId,
                x.VersionNo,
                x.ConfigJson,
                x.PreviousConfigJson,
                x.ChangeType,
                x.RestoredFromVersionId,
                x.Reason,
                x.ChangedBy,
                x.ChangedAt))
            .ToListAsync(ct);

        return Ok(new { data });
    }

    [Authorize(Roles = "Admin," + UserRoles.Editor)]
    [HttpPost("{versionId:long}/restore")]
    public async Task<IActionResult> RestoreVersion(
        int partProcessCharacteristicId,
        long versionId,
        RestoreChemicalAnalysisFormulaVersionRequest req,
        CancellationToken ct)
    {
        try
        {
            var result = await versions.RestoreAsync(
                partProcessCharacteristicId,
                versionId,
                User?.Identity?.Name,
                req.Reason,
                ct);

            return Ok(new
            {
                restored = result.Restored,
                version = result.Version is null ? null : ToResponse(result.Version)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static ChemicalAnalysisFormulaVersionResponse ToResponse(Domain.Entities.ChemicalAnalysisFormulaVersion x) => new(
        x.Id,
        x.PartProcessCharacteristicId,
        x.VersionNo,
        x.ConfigJson,
        x.PreviousConfigJson,
        x.ChangeType,
        x.RestoredFromVersionId,
        x.Reason,
        x.ChangedBy,
        x.ChangedAt);
}

public sealed record RestoreChemicalAnalysisFormulaVersionRequest(string? Reason);

public sealed record ChemicalAnalysisFormulaVersionResponse(
    long Id,
    int PartProcessCharacteristicId,
    int VersionNo,
    string ConfigJson,
    string? PreviousConfigJson,
    string ChangeType,
    long? RestoredFromVersionId,
    string? Reason,
    string? ChangedBy,
    DateTime ChangedAt);
