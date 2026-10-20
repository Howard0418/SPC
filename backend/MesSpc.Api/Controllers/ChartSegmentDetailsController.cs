using MesSpc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/part-process-characteristics/{ppcId:int}/chart-segment-details")]
public sealed class ChartSegmentDetailsController(ChartSegmentDetailsService service,
    ILogger<ChartSegmentDetailsController> logger) : ControllerBase
{
    /// <summary>Read current segment limits and paginated modification history; no historical formula inference or writes.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(int ppcId, CancellationToken ct, [FromQuery] int? segmentId = null,
        [FromQuery] int formulaSkip = 0, [FromQuery] int specificationSkip = 0)
    {
        if (formulaSkip < 0 || specificationSkip < 0 || segmentId is <= 0)
            return BadRequest(new { success = false, message = "無效的分段或分頁範圍。" });
        var data = await service.GetAsync(ppcId, segmentId, formulaSkip, specificationSkip, ct);
        if (data is null)
            return NotFound(new { success = false, message = "找不到啟用的管制項目或所屬日期分段，請重新查詢。" });
        logger.LogDebug("Read chart segment details for PPC {PpcId}, segment {SegmentId}", ppcId, segmentId);
        return Ok(new { success = true, data, message = "目前設定與修改歷史；未推定量測實際使用版本。" });
    }
}
