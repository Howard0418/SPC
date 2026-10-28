using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/spc/overall")]
public sealed class OverallController(OverallStatisticsService service, ILogger<OverallController> logger) : ControllerBase
{
    /// <summary>Returns all enabled control items, OOC/OOS item and point counts, and deduplicated current handling counts.</summary>
    /// <param name="startDate">Inclusive measurement date; defaults to 29 days before Taiwan today.</param>
    /// <param name="endDate">Inclusive measurement date; defaults to Taiwan today. Maximum date difference is 93 days.</param>
    /// <param name="ct">Request cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        var today=DateTime.UtcNow.AddHours(8).Date;
        var end=endDate?.Date ?? today;
        if (!startDate.HasValue && end < DateTime.MinValue.Date.AddDays(29))
            return BadRequest(new {success=false,data=(object?)null,message="請提供有效的量測起迄日。"});
        var start=startDate?.Date ?? end.AddDays(-29);
        if(start>end || (end-start).TotalDays>93 || end==DateTime.MaxValue.Date)
            return BadRequest(new {success=false,data=(object?)null,message="日期起迄順序不正確，或日期間隔超過93天。"});
        logger.LogDebug("Overall measurement dates {Start} through {End}",start,end);
        var result=await service.GetAsync(start,end,ct);
        return Ok(new {success=true,data=new {startDate=start.ToString("yyyy-MM-dd"),endDate=end.ToString("yyyy-MM-dd"),summary=result},message=""});
    }
}
