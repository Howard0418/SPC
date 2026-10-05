using System.Security.Claims;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

/// <summary>儀器 Excel 批次匯入；僅新增，所有端點要求登入及目前有效的校正管理權限。</summary>
[ApiController]
[Authorize]
[Route("api/v1/instruments/import")]
public sealed class InstrumentImportController(AppDbContext db, CalibrationImportService import) : ControllerBase
{
    /// <summary>取得目前使用者是否能匯入儀器，供畫面顯示入口。</summary>
    [HttpGet("access")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Access(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated!=true) return Unauthorized(new { success=false,message="請先登入。" });
        return Ok(new { success=true,data=new { canManage=await CanManageAsync(ct) },message="" });
    }

    /// <summary>上傳第一工作表範本（.xlsx，10 MB），回傳 SHA256、列號、解析值、原始值與錯誤；不寫入。</summary>
    /// <param name="file">未加密的儀器 Excel 範本。</param>
    /// <param name="ct">請求取消訊號。</param>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CalibrationImportParser.MaxFileBytes + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Preview(IFormFile file, CancellationToken ct)
    {
        var denied=await DeniedAsync(ct); if (denied is not null) return denied;
        try { return Ok(new { success=true,data=await import.PreviewAsync(await ReadAsync(file,ct),ct),message="請核對警示並勾選有效資料。" }); }
        catch (CalibrationException ex) { return StatusCode(ex.Status,new { success=false,message=ex.Message,code=ex.Code }); }
    }

    /// <summary>重傳預覽同檔及選取列號，重新驗證後交易新增；資料庫重複略過，不覆寫、不產生校正結果。</summary>
    /// <param name="file">與預覽 SHA256 相同的 .xlsx 檔。</param>
    /// <param name="options">JSON：hash、selectedRows、department、custodianOperatorId、usageStatus、includeCustodian。</param>
    /// <param name="ct">請求取消訊號。</param>
    [HttpPost("commit")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CalibrationImportParser.MaxFileBytes + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Commit(IFormFile file, [FromForm] string options, CancellationToken ct)
    {
        var denied=await DeniedAsync(ct); if (denied is not null) return denied;
        CalibrationImportOptions? request;
        try { request=JsonSerializer.Deserialize<CalibrationImportOptions>(options,new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return BadRequest(new { success=false,message="匯入選項格式無效。" }); }
        if (request is null) return BadRequest(new { success=false,message="缺少匯入選項。" });
        try
        {
            var result=await import.CommitAsync(await ReadAsync(file,ct),file.FileName,request,User.Identity!.Name!,ct);
            return StatusCode(result.Failed>0 ? 409 : 200,new { success=result.Failed==0,data=result,message=result.Failed>0 ? "本次未新增資料，請查看逐列原因。" : "匯入完成。" });
        }
        catch (CalibrationException ex) { return StatusCode(ex.Status,new { success=false,message=ex.Message,code=ex.Code }); }
    }

    private async Task<bool> CanManageAsync(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated!=true || string.IsNullOrWhiteSpace(User.Identity.Name)) return false;
        var role=User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        if (!string.Equals(role,"Editor",StringComparison.OrdinalIgnoreCase)) return false;
        var name=User.Identity.Name;
        var op=await db.Operators.AsNoTracking().SingleOrDefaultAsync(x=>x.Username==name,ct);
        return op is { IsActive:true } && CalibrationRules.CanManage(op.Role,op.PagePermissionsJson);
    }
    private async Task<IActionResult?> DeniedAsync(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated!=true) return Unauthorized(new { success=false,message="請先登入。" });
        return await CanManageAsync(ct) ? null : StatusCode(403,new { success=false,message="需要啟用的 Editor 與校正管理權限。" });
    }
    private static async Task<byte[]> ReadAsync(IFormFile file,CancellationToken ct)
    {
        if (file is null || !string.Equals(Path.GetExtension(file.FileName),".xlsx",StringComparison.OrdinalIgnoreCase) || file.Length is <=0 or >CalibrationImportParser.MaxFileBytes)
            throw new CalibrationException("請上傳 10 MB 以內的 .xlsx 檔案。");
        using var output = new MemoryStream();
        await file.CopyToAsync(output,ct);
        return output.ToArray();
    }
}
