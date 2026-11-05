using System.Data;
using System.Security.Principal;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductionTestBusinessAlign;

if(args.Length!=4||args[3] is not("plan" or "apply" or "verify"))throw new ArgumentException("TEST_CONFIG PROD_CONFIG OUTPUT plan|apply|verify");
if(!OperatingSystem.IsWindows())throw new InvalidOperationException("Windows operator identity is required.");
var json=new JsonSerializerOptions{WriteIndented=true};var ids=new[]{2425,2592,2593};
AppDbContext Open(string file,string env,string name){using var d=JsonDocument.Parse(File.ReadAllText(file));var b=new SqlConnectionStringBuilder(d.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()!.Replace("Trust Server Certificate=","TrustServerCertificate="));if(d.RootElement.GetProperty("AppEnvironment").GetString()!=env||b.InitialCatalog!=name||b.DataSource!="172.16.110.16")throw new InvalidOperationException("Environment guard failed.");return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(b.ConnectionString).Options);}
await using var test=Open(args[0],"test","PMR_SPC_TEST");await using var prod=Open(args[1],"production","PMR_SPC_2026");
await test.Database.OpenConnectionAsync();await prod.Database.OpenConnectionAsync();
async Task<List<BusinessMaster>> Rows(AppDbContext db)=>await db.PartProcessCharacteristics.AsNoTracking().Where(x=>ids.Contains(x.Id)&&x.ControlScope=="CHEM").Select(x=>new BusinessMaster(x.Id,(x.Machine==null?"":x.Machine.MachineCode)+"|"+(x.Tank==null?"":x.Tank.TankCode)+"|"+(x.Characteristic==null?"":x.Characteristic.CharacteristicCode)+"|"+x.Unit,x.IsEnabled,x.LSL,x.USL,x.TargetValue,x.ChemicalAnalysisConfigJson??"{}")).ToListAsync();
async Task<string> Fingerprint(string table){await using var c=prod.Database.GetDbConnection().CreateCommand();c.CommandText=$"SELECT CONCAT(COUNT_BIG(*),':',CHECKSUM_AGG(BINARY_CHECKSUM(*))) FROM dbo.[{table}]";return Convert.ToString(await c.ExecuteScalarAsync())!;}
var protectedTables=new[]{"VariableMeasurements","ChemicalCalculationReceipts","ChemicalCalculationTraces","ChemicalFTableVersions","ChemicalFTableCells","ChemicalFormulaScheduleRevisions","ChemicalFormulaEffectivities"};
var before=new Dictionary<string,string>();foreach(var table in protectedTables)before[table]=await Fingerprint(table);
var source=await Rows(test);var target=await Rows(prod);if(source.Count!=3||target.Count!=3)throw new InvalidOperationException("Reviewed set changed.");
var plan=BusinessAlignPlanner.Build(source,target);Directory.CreateDirectory(args[2]);var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-before.json"),JsonSerializer.Serialize(new{source,target,plan,before},json));
if(args[3]=="apply")
{
 if(plan.Count!=3||!plan.Select(x=>x.Before.Id).Order().SequenceEqual(ids.Order()))throw new InvalidOperationException("Expected exactly three reviewed master changes.");
 using var http=new HttpClient();using var v=JsonDocument.Parse(await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/version"));if(v.RootElement.GetProperty("environment").GetString()!="production")throw new InvalidOperationException("API target mismatch.");await http.GetStringAsync("https://pmr-spc-srv.pmr.com.tw:8443/api/health");
 if(await prod.VariableMeasurements.AnyAsync(x=>x.PartProcessCharacteristicId==2593))throw new InvalidOperationException("Duplicate mapping has measurements; refuse disable.");
 await using(var c=prod.Database.GetDbConnection().CreateCommand())
 {
  c.CommandTimeout=600;c.CommandText="DECLARE @dir nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')); SELECT @dir";var dir=Convert.ToString(await c.ExecuteScalarAsync())??throw new InvalidOperationException("No backup directory.");var backup=dir.TrimEnd('\\')+"\\SPC_align_"+stamp+".bak";c.CommandText="BACKUP DATABASE [PMR_SPC_2026] TO DISK=@p WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=@p WITH CHECKSUM";var p=c.CreateParameter();p.ParameterName="@p";p.Value=backup;c.Parameters.Add(p);await c.ExecuteNonQueryAsync();await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-backup.json"),JsonSerializer.Serialize(new{backup,verified=true},json));
 }
 await using var tx=await prod.Database.BeginTransactionAsync(IsolationLevel.Serializable);var fresh=await Rows(prod);if(JsonSerializer.Serialize(fresh.OrderBy(x=>x.Id))!=JsonSerializer.Serialize(target.OrderBy(x=>x.Id)))throw new InvalidOperationException("Concurrent master change.");
 var actor=WindowsIdentity.GetCurrent().Name;var versions=new ChemicalAnalysisFormulaVersionService(prod);
 foreach(var change in plan){var row=await prod.PartProcessCharacteristics.SingleAsync(x=>x.Id==change.Before.Id);row.IsEnabled=change.After.Enabled;row.LSL=change.After.Lsl;row.USL=change.After.Usl;row.TargetValue=change.After.Target;row.ChemicalAnalysisConfigJson=change.After.Config;row.UpdatedAt=DateTime.UtcNow;row.UpdatedBy=actor;if(change.Before.Config!=change.After.Config)await versions.RecordChangeAsync(row.Id,change.Before.Config,change.After.Config,actor,"2026-11-05 使用者授權正式對齊測試業務主檔及來源定位",default);}
 await prod.SaveChangesAsync();await tx.CommitAsync();
}
prod.ChangeTracker.Clear();var remaining=BusinessAlignPlanner.Build(source,await Rows(prod));var after=new Dictionary<string,string>();foreach(var table in protectedTables)after[table]=await Fingerprint(table);
if(before.Any(x=>after[x.Key]!=x.Value))throw new InvalidOperationException("Protected dataset changed.");if(args[3]!="plan"&&remaining.Count!=0)throw new InvalidOperationException("Remaining differences.");
await File.WriteAllTextAsync(Path.Combine(args[2],stamp+"-"+args[3]+".json"),JsonSerializer.Serialize(new{mode=args[3],changes=plan.Count,remaining=remaining.Count,before,after,protectedUnchanged=true},json));Console.WriteLine($"{args[3]}: changes={plan.Count}; remaining={remaining.Count}; protected=unchanged");
