using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Controllers;
[ApiController]
[Route("api/v1/part-process-characteristics/{ppcId:int}/specification-limit-versions")]
[Route("api/part-process-characteristics/{ppcId:int}/specification-limit-versions")]
public sealed class SpecificationLimitVersionsController(AppDbContext db) : ControllerBase
{
    /// <summary>Read engineering specification changes by inclusive Taipei modification dates.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(int ppcId,CancellationToken ct,[FromQuery]string? fromDate=null,[FromQuery]string? toDate=null,[FromQuery]int skip=0,[FromQuery]int take=21)
    {
        VersionHistoryDateRange range;
        try {range=VersionHistoryDateRange.Parse(fromDate,toDate);}
        catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
        if(skip<0 || take is <1 or >100)return BadRequest(new{message="無效的分頁範圍。"});
        if(!await db.PartProcessCharacteristics.IgnoreQueryFilters().AnyAsync(x=>x.Id==ppcId,ct))return NotFound(new{message="找不到指定的管制項目。"});
        var query=db.SpecificationLimitVersions.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==ppcId);
        if(range.FromUtc.HasValue)query=query.Where(x=>x.ChangedAt>=range.FromUtc.Value);
        if(range.UntilUtc.HasValue)query=query.Where(x=>x.ChangedAt<range.UntilUtc.Value);
        var data=await query.OrderByDescending(x=>x.VersionNo).ThenByDescending(x=>x.Id).Skip(skip).Take(take).ToListAsync(ct);
        foreach(var row in data)row.ChangedAt=DateTime.SpecifyKind(row.ChangedAt,DateTimeKind.Utc);
        return Ok(new{data});
    }
}
