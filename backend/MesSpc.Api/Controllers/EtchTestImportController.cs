using MesSpc.Api.Services;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Controllers;

/// <summary>Test-only strict etch import. Existing different content is never overwritten.</summary>
[ApiController,Route("api/v1/etch-import-test"),Authorize(Roles="Admin,Editor")]
public sealed class EtchTestImportController(EtchReportSyncService service,AppDbContext db,IConfiguration config,ILogger<EtchTestImportController> logger):ControllerBase
{
    /// <summary>Check actual database and application environment before any preview or import.</summary>
    public static bool IsTest(string? environment,string? connection)
    {
        try{var cs=new SqlConnectionStringBuilder(connection);return environment=="test"&&cs.InitialCatalog=="PMR_SPC_TEST"&&cs.DataSource=="172.16.110.16";}
        catch{return false;}
    }
    /// <summary>Read-only validation and unchanged/new classification.</summary>
    [HttpPost("preview"),RequestSizeLimit(256*1024)]
    public Task<IActionResult> Preview(EtchReportRequest report,CancellationToken ct)=>Run(report,false,ct);
    /// <summary>Confirm strict insert or unchanged retry; reject any existing different report.</summary>
    [HttpPost("confirm"),RequestSizeLimit(256*1024)]
    public Task<IActionResult> Confirm(EtchReportRequest report,CancellationToken ct)=>Run(report,true,ct);
    private async Task<IActionResult> Run(EtchReportRequest report,bool apply,CancellationToken ct)
    {
        if(!db.Database.IsSqlServer()||!IsTest(config["AppEnvironment"],db.Database.GetConnectionString()))return StatusCode(403,new{success=false,data=(object?)null,message="只允許SPC測試庫。"});
        try{
            var result=apply?await service.SyncAsync(report,User.Identity?.Name??"authenticated-user",false,ct):await service.PreviewAsync(report,ct);
            if(apply)logger.LogInformation("Strict etch import {Date} {Line} by {Actor}, unchanged={Unchanged}",report.ReportDate,report.LineCode,User.Identity?.Name,result.Unchanged);
            return Ok(new{success=true,data=result,message=apply?"匯入完成":"預覽完成"});
        }
        catch(ArgumentException e){return BadRequest(new{success=false,data=(object?)null,message=e.Message});}
        catch(InvalidOperationException e){logger.LogWarning("Strict etch import rejected: {Reason}",e.Message);return Conflict(new{success=false,data=(object?)null,message=e.Message});}
    }
}
