using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

/// <summary>Read-only etch report reconciliation. Does not create batches, measurements or master data.</summary>
[ApiController, Route("api/v1/etch-import"), Authorize(Roles = "Admin,Editor")]
public sealed class EtchImportInspectionController(EtchReportSyncService service, AppDbContext db, IConfiguration config,
    ILogger<EtchImportInspectionController> logger) : ControllerBase
{
    /// <summary>Compare a full source report with stored date/line, 25/50 samples, SampleNo and rate; classify new/existing/conflict.</summary>
    [HttpPost("inspect"), RequestSizeLimit(256 * 1024)]
    public async Task<IActionResult> Inspect(EtchReportRequest report, CancellationToken ct)
    {
        // Production inspection is read-only. The separate confirmation endpoint retains its explicit write opt-in.
        if (!db.Database.IsSqlServer() || !EtchTestImportController.IsEnvironmentAllowed(config["AppEnvironment"], db.Database.GetConnectionString(), true))
            return StatusCode(403, new { success = false, data = (object?)null, message = "咬蝕核對的環境或資料庫不符。" });
        try { return Ok(new { success = true, environment = config["AppEnvironment"], contractVersion = 1, data = await service.InspectAsync(report, ct), message = "咬蝕完整性核對完成" }); }
        catch (ArgumentException e) { return BadRequest(new { success = false, data = (object?)null, message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { success = false, data = (object?)null, message = e.Message }); }
        catch (Exception e)
        {
            var failure = EtchPreviewFailureResponses.Create(e, HttpContext.TraceIdentifier);
            logger.LogError(e, "Etch inspection failed {TraceId} {Date} {Line}", HttpContext.TraceIdentifier, report.ReportDate, report.LineCode);
            return StatusCode(failure.StatusCode, new { success = false, data = (object?)null, message = failure.Message, errorCode = failure.ErrorCode, traceId = HttpContext.TraceIdentifier });
        }
    }
}
