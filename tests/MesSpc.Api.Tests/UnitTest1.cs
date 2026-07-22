using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using ClosedXML.Excel;

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

        using var stream = BuildPortableWorkbook();
        var file = new FormFile(stream, 0, stream.Length, "file", "spc-test.xlsx");

        var result = await controller.ImportCustomSpc(file);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value;
        data.Should().NotBeNull();

        var groups = await context.ControlChartGroups.ToListAsync();
        groups.Should().HaveCount(3);
        groups.Select(g => g.GroupCode).Should().Contain(new[] { "PROC", "CHEM", "PROD" });

        var ppcs = await context.PartProcessCharacteristics.ToListAsync();
        ppcs.Should().HaveCount(3);
        ppcs.Select(x => x.ControlScope).Should().BeEquivalentTo("PROCESS", "CHEMICAL", "PRODUCT");
    }

    private static MemoryStream BuildPortableWorkbook()
    {
        using var workbook = new XLWorkbook();
        AddProcessStyleSheet(workbook, "製程管制項目", "PROC-001", "LINE-P", "製程厚度");
        AddProcessStyleSheet(workbook, "藥液管制項目", "CHEM-001", "LINE-C", "銅離子濃度");

        var product = workbook.AddWorksheet("產品管制項目");
        product.Cell(1, 1).Value = "管制圖號";
        product.Cell(1, 2).Value = "檢驗製程";
        product.Cell(1, 3).Value = "管制圖標題";
        product.Cell(1, 4).Value = "管制圖種類";
        product.Cell(1, 5).Value = "料號";
        product.Cell(2, 1).Value = "PROD-001";
        product.Cell(2, 2).Value = "LINE-O";
        product.Cell(2, 3).Value = "產品厚度";
        product.Cell(2, 4).Value = "I_MR";
        product.Cell(2, 5).Value = "PART-001";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static void AddProcessStyleSheet(XLWorkbook workbook, string sheetName, string chartNo, string lineCode, string chartName)
    {
        var sheet = workbook.AddWorksheet(sheetName);
        sheet.Cell(1, 1).Value = "Chart No";
        sheet.Cell(1, 2).Value = "製程線別";
        sheet.Cell(1, 3).Value = "Chart Name";
        sheet.Cell(1, 4).Value = "管制圖種類";
        sheet.Cell(1, 5).Value = "USL";
        sheet.Cell(1, 6).Value = "LSL";
        sheet.Cell(2, 1).Value = chartNo;
        sheet.Cell(2, 2).Value = lineCode;
        sheet.Cell(2, 3).Value = chartName;
        sheet.Cell(2, 4).Value = "I_MR";
        sheet.Cell(2, 5).Value = 11;
        sheet.Cell(2, 6).Value = 9;
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
