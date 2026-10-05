using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

// Metadata/script generation only: do not open a connection or start API Program.
if(args.Length!=2)throw new ArgumentException("applied-migrations.json output-directory");
using var baseline=JsonDocument.Parse(File.ReadAllText(args[0]));
var applied=baseline.RootElement.GetProperty("Migrations").EnumerateArray().Select(x=>x.GetString()!).ToHashSet();
using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=isolated.invalid;Database=NO_CONNECTION_ALLOWED;Integrated Security=True;TrustServerCertificate=True").Options);
var migrations=db.GetService<IMigrationsAssembly>().Migrations.Keys.ToArray();
var pending=migrations.Where(x=>!applied.Contains(x)).ToArray();
Directory.CreateDirectory(args[1]);
var migrator=db.GetService<IMigrator>();
foreach(var id in pending){var index=Array.IndexOf(migrations,id);var from=index==0?"0":migrations[index-1];
    File.WriteAllText(Path.Combine(args[1],id+".sql"),migrator.GenerateScript(from,id,MigrationsSqlGenerationOptions.Idempotent));}
File.WriteAllText(Path.Combine(args[1],"inventory.json"),JsonSerializer.Serialize(new{pending,unknownHistory=applied.Except(migrations).ToArray()},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Generated {pending.Length} offline migration scripts; no database connection.");
