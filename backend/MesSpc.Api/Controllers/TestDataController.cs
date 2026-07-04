using MesSpc.Api.Services.TestData;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/testdata")]
public class TestDataController(IWebHostEnvironment env, TestDataSeeder seeder) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateTestDataRequest? req)
    {
        if (!env.IsDevelopment()) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Test data seeder only allowed in Development." });
        var request = req ?? new GenerateTestDataRequest();
        var result = await seeder.GenerateAsync(request);
        return Ok(result);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear([FromQuery] string? runId)
    {
        if (!env.IsDevelopment()) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Test data seeder only allowed in Development." });
        var result = await seeder.ClearAsync(runId);
        return Ok(result);
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAll()
    {
        if (!env.IsDevelopment()) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Clear all database data only allowed in Development." });
        var result = await seeder.ClearAllDatabaseDataAsync();
        return Ok(result);
    }

    [HttpDelete("clear-transactions")]
    public async Task<IActionResult> ClearTransactions()
    {
        if (!env.IsDevelopment()) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Clear transactions only allowed in Development." });
        var result = await seeder.ClearTransactionalDataAsync();
        return Ok(result);
    }

    [HttpDelete("clear-tagged")]
    public async Task<IActionResult> ClearTagged()
    {
        if (!env.IsDevelopment()) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tagged test data cleanup only allowed in Development." });
        var result = await seeder.ClearTaggedTestDataAsync();
        return Ok(result);
    }
}

