using MesSpc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MesSpc.Api.Controllers;

/// <summary>Synchronize one complete etch report, retaining 25/50 samples per side and provenance.</summary>
[ApiController,Route("api/v1/etch-reports"),Authorize(Roles="Admin,Editor")]
public sealed class EtchReportsController(EtchReportSyncService sync):ControllerBase
{
    /// <summary>Explicit report save: atomic upsert of the two subgroups, overall rate and actual line speed.</summary>
    [HttpPut]
    public async Task<IActionResult> Put(EtchReportRequest report,CancellationToken ct)
    {
        try{return Ok(await sync.SyncAsync(report,User.Identity?.Name??"authenticated-user",true,ct));}
        catch(ArgumentException e){return BadRequest(new{message=e.Message});}
        catch(InvalidOperationException e){return Conflict(new{message=e.Message});}
    }
}
