using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

var root = @"D:\SPC";
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root,"release/test/backend/appsettings.json")), new JsonDocumentOptions { CommentHandling=JsonCommentHandling.Skip, AllowTrailingCommas=true });
var settings=config.RootElement;
var connection=settings.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!;
if (settings.GetProperty("AppEnvironment").GetString()!="test" || new SqlConnectionStringBuilder(connection).InitialCatalog!="PMR_SPC_TEST")
    throw new InvalidOperationException("Only SPC test database is permitted.");
var file=Directory.GetFiles(Path.Combine(root,"docs"),"QA-2-003*.xlsx").Single();
var source=CalibrationImportParser.Parse(await File.ReadAllBytesAsync(file),TimeProvider.System);
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
var apply=args.Contains("--apply");
var count=await new CalibrationDetailsBackfill(db,new InstrumentCalibrationService(db,TimeProvider.System)).ApplyAsync(source,apply,default);
Console.WriteLine(JsonSerializer.Serialize(new { Database="PMR_SPC_TEST",Apply=apply,Rows=count,SourceHash=source.Hash }));
