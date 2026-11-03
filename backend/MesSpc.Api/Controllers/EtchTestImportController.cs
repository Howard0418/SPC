using MesSpc.Api.Services.Localization;
using System.Data.Common;
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
        => IsEnvironmentAllowed(environment,connection,false);

    /// <summary>Keep test access; production writes require an explicit server opt-in and exact database identity.</summary>
    public static bool IsEnvironmentAllowed(string? environment,string? connection,bool allowProduction)
    {
        try{var cs=new SqlConnectionStringBuilder(connection);return cs.DataSource=="172.16.110.16" &&
            (environment=="test"&&cs.InitialCatalog=="PMR_SPC_TEST" || allowProduction&&environment=="production"&&cs.InitialCatalog=="PMR_SPC_2026");}
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
        if(!db.Database.IsSqlServer()||!IsEnvironmentAllowed(config["AppEnvironment"],db.Database.GetConnectionString(),config.GetValue("EtchImport:AllowProduction",false)))return StatusCode(403,new{success=false,data=(object?)null,message="咬蝕嚴格匯入環境不符或正式匯入尚未啟用。", code="ETCH_TEST_DATABASE_ONLY", messageKey="spc.api.etchTestDatabaseOnly", args=ApiUiMessageCatalog.EmptyArguments});
        try{
            EtchSyncResult result;
            if(apply)result=await service.ImportGovernedAsync(report,User.Identity?.Name??"authenticated-user",ct);
            else {
                var check=await service.InspectAsync(report,ct);
                if(check.State=="CONFLICT")throw new InvalidOperationException(check.Message);
                result=new(check.BatchId,check.State=="ALREADY_EXISTS",check.PointCount);
            }
            if(apply)logger.LogInformation("Strict etch import {Date} {Line} by {Actor}, unchanged={Unchanged}",report.ReportDate,report.LineCode,User.Identity?.Name,result.Unchanged);
            return Ok(new{success=true,data=result,message=apply?"匯入完成":"預覽完成"});
        }
        catch(ArgumentException e){return BadRequest(new{success=false,data=(object?)null,message=e.Message});}
        catch(InvalidOperationException e){logger.LogWarning("Strict etch import rejected: {Reason}",e.Message);return Conflict(new{success=false,data=(object?)null,message=e.Message});}
        catch(DbException e)
        {
            var failure=EtchPreviewFailureResponses.Create(e,HttpContext.TraceIdentifier);
            logger.LogError(e,"Strict etch import database failure {ErrorCode} {TraceId} {Action} {Date} {Line}",failure.ErrorCode,HttpContext.TraceIdentifier,apply?"confirm":"preview",report.ReportDate,report.LineCode);
            return StatusCode(failure.StatusCode,new{success=false,data=(object?)null,errorCode=failure.ErrorCode,traceId=HttpContext.TraceIdentifier,message=failure.Message});
        }
        catch(Exception e)
        {
            var failure=EtchPreviewFailureResponses.Create(e,HttpContext.TraceIdentifier);
            logger.LogError(e,"Strict etch import server failure {ErrorCode} {TraceId} {Action} {Date} {Line}",failure.ErrorCode,HttpContext.TraceIdentifier,apply?"confirm":"preview",report.ReportDate,report.LineCode);
            return StatusCode(failure.StatusCode,new{success=false,data=(object?)null,errorCode=failure.ErrorCode,traceId=HttpContext.TraceIdentifier,message=failure.Message});
        }
    }
}
