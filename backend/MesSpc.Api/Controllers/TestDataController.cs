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
        if (!env.IsDevelopment()) return Forbid("Test data seeder only allowed in Development.");
        var request = req ?? new GenerateTestDataRequest();
        var result = await seeder.GenerateAsync(request);
        return Ok(result);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear([FromQuery] string? runId)
    {
        if (!env.IsDevelopment()) return Forbid("Test data seeder only allowed in Development.");
        var result = await seeder.ClearAsync(runId);
        return Ok(result);
    }
}

