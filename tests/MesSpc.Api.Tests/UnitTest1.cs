using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace MesSpc.Api.Tests;

public class UnitTest1
{
    [Fact]
    public async Task TestImportCustomSpc_SucceedsAndSeedsDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = new MigrationController(context, new TestWebHostEnvironment())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var path = @"C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\SPC管制項目.xlsx";
        using var stream = File.OpenRead(path);
        var file = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(path));

        var result = await controller.ImportCustomSpc(file);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value;
        data.Should().NotBeNull();

        var groups = await context.ControlChartGroups.ToListAsync();
        groups.Should().HaveCount(3);
        groups.Select(g => g.GroupCode).Should().Contain(new[] { "PROC", "CHEM", "PROD" });

        var categories = await context.ControlChartCategories.ToListAsync();
        categories.Should().HaveCount(3);

        var ppcs = await context.PartProcessCharacteristics.ToListAsync();
        ppcs.Count.Should().Be(186); // 12 in PROC, 102 in CHEM, 72 in PROD
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MesSpc.Api.Tests";
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
