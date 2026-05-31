using MesSpc.Api.Domain.DTOs;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenealogyController(GenealogyService genealogyService) : ControllerBase
{
    /// <summary>
    /// 以 LotNo 為起點，查詢單層 (1-Level) 的產品系譜與品質歷程
    /// </summary>
    [HttpGet("lot/{lotNo}")]
    public async Task<ActionResult<LotGenealogyResponse>> GetLotGenealogy(string lotNo)
    {
        var response = await genealogyService.GetLotGenealogyAsync(lotNo);
        if (response == null) return NotFound($"LotNo {lotNo} not found");
        return Ok(response);
    }

    /// <summary>
    /// 以 WorkOrderNo 為起點，展開旗下的所有母批 (Root Lots)
    /// </summary>
    [HttpGet("workorder/{workOrderNo}")]
    public async Task<ActionResult<WorkOrderGenealogyResponse>> GetWorkOrderGenealogy(string workOrderNo)
    {
        var response = await genealogyService.GetWorkOrderGenealogyAsync(workOrderNo);
        if (response.RootLots.Count == 0) return NotFound($"WorkOrderNo {workOrderNo} not found or has no associated lots.");
        return Ok(response);
    }
}
