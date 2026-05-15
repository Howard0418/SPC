using MesSpc.Api.Services;
using MesSpc.Api.Services.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v1/uploads/chemical-daily-report")]
public class ChemicalDailyReportController : ControllerBase
{
    private readonly ChemicalDailyReportParser _parser;
    private readonly ChemicalImportService _importService;

    public ChemicalDailyReportController(
        ChemicalDailyReportParser parser,
        ChemicalImportService importService)
    {
        _parser = parser;
        _importService = importService;
    }

    /// <summary>
    /// Step 1: 上傳並解析 Excel，回傳預覽資料（不寫入資料庫）
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請上傳 .xlsx 檔案" });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "只接受 .xlsx 格式" });

        await using var stream = file.OpenReadStream();
        var result = _parser.Parse(stream);

        return Ok(new
        {
            totalRows = result.TotalRows,
            parsedRows = result.ParsedRows,
            skippedRows = result.SkippedRows,
            warnings = result.Warnings,
            preview = result.Rows.Take(50).Select(r => new
            {
                sheetName = r.SheetName,
                line = r.Line,
                tank = r.Tank,
                analysisItem = r.AnalysisItem,
                spec = r.SpecText,
                lsl = r.LSL,
                usl = r.USL,
                measuredDate = r.MeasuredDate,
                analyst = r.Analyst,
                // 主測
                titrationMl = r.TitrationMl,
                concentration = r.Concentration,
                judgment = r.Judgment,
                // 複驗
                recheckTitrationMl = r.RecheckTitrationMl,
                recheckConcentration = r.RecheckConcentration,
                recheckJudgment = r.RecheckJudgment,
                // ECR
                ecrNo = r.EcrNo,
                ecrStartDate = r.EcrStartDate,
                ecrEndDate = r.EcrEndDate
            })
        });
    }

    /// <summary>
    /// Step 2: 上傳並直接寫入資料庫（確認後執行）
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請上傳 .xlsx 檔案" });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "只接受 .xlsx 格式" });

        await using var stream = file.OpenReadStream();
        var parseResult = _parser.Parse(stream);

        if (parseResult.ParsedRows == 0)
            return BadRequest(new { error = "檔案中沒有有效的資料列可匯入" });

        var importedBy = User.Identity?.Name ?? "system";
        var summary = await _importService.ImportAsync(parseResult, importedBy, ct);

        return Ok(new
        {
            message = summary.FailedRows == 0 ? "匯入成功" : "匯入完成（含部分失敗）",
            importedRows = summary.ImportedRows,
            failedRows = summary.FailedRows,
            errors = summary.Errors
        });
    }
}
