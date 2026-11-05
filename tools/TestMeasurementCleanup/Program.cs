using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TestMeasurementCleanup;
if(args.Length is 4 or 5){await CleanupRunner.RunAsync(args);return;}
if(args.Length!=3)throw new ArgumentException("TEST_CONFIG PROD_CONFIG OUTPUT");
var options=new JsonSerializerOptions{WriteIndented=true};
AppDbContext Open(string p,string env,string name){using var d=JsonDocument.Parse(File.ReadAllText(p));var b=new SqlConnectionStringBuilder(d.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!.Replace("Trust Server Certificate=","TrustServerCertificate="));if(d.RootElement.GetProperty("AppEnvironment").GetString()!=env||b.DataSource!="172.16.110.16"||b.InitialCatalog!=name)throw new InvalidOperationException("Environment mismatch");return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(b.ConnectionString).Options);}
await using var test=Open(args[0],"test","PMR_SPC_TEST");await using var prod=Open(args[1],"production","PMR_SPC_2026");
var inventory=new{test=await DatabaseInventory.ReadAsync(test),production=await DatabaseInventory.ReadAsync(prod)};Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);await File.WriteAllTextAsync(args[2],JsonSerializer.Serialize(inventory,options));Console.WriteLine("Read-only table inventory saved.");
