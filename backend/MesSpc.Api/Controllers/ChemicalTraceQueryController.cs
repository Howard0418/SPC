using MesSpc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MesSpc.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/v1/part-process-characteristics/{ppcId:int}/chemical-calculation-traces")]
public sealed class ChemicalTraceQueryController(ChemicalTraceQueryService service):ControllerBase
{
 /// <summary>Read immutable chemical formula evidence by date segment or exact measurement. Never recalculates or writes.</summary>
 [HttpGet]
 public async Task<IActionResult> Get(int ppcId,CancellationToken ct,[FromQuery]int? segmentId=null,[FromQuery]long? measurementId=null,[FromQuery]int skip=0,[FromQuery]int traceSkip=0)
 {
  try
  {
   var data=await service.GetAsync(ppcId,segmentId,measurementId,skip,traceSkip,ct);
   if(data is null)return NotFound(new{success=false,data=(object?)null,message="找不到所屬管制項目、分段或量測。"});
   return Ok(new{success=true,data,message="只顯示已保存證據；未知不推定、不重算。"});
  }
  catch(ArgumentException ex){return BadRequest(new{success=false,data=(object?)null,message=ex.Message});}
 }
}
