#requires -Version 5.1
[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('SPC','Portal')][string]$System,
 [Parameter(Mandatory=$true)][string]$ApiProject,
 [Parameter(Mandatory=$true)][string]$OutputDirectory,
 [string]$FromMigration
)
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'ProductionRelease.psm1') -Force
$project=(Resolve-Path -LiteralPath $ApiProject).Path
[xml]$xml=Get-Content -LiteralPath $project -Raw
$framework=[string]($xml.Project.PropertyGroup|Where-Object {$_.TargetFramework}|Select-Object -First 1).TargetFramework
if($framework -notmatch '^net(8|9|10)\.0$'){throw 'Unsupported target framework'}
$out=[IO.Path]::GetFullPath($OutputDirectory)
Assert-ReleaseWorkspacePath $out
$sourceDir=Split-Path $project -Parent
if($out -ieq $sourceDir -or $out.StartsWith($sourceDir+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inspector output must be outside the API source directory'}
foreach($systemName in @('SPC','Portal')){foreach($target in (Get-ProductionProfile $systemName).Targets){if($out -ieq $target.Path -or $out.StartsWith($target.Path+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inspector must not write into production sites'}}}
if(Test-Path -LiteralPath $out){throw 'Use a new output directory'}
if($FromMigration -and $FromMigration -notmatch '^\d{14}_[A-Za-z0-9_]+$'){throw 'Invalid migration ID'}
[void][IO.Directory]::CreateDirectory($out)
$scratch=Join-Path $out 'offline-inspector';[void][IO.Directory]::CreateDirectory($scratch)
$escaped=[Security.SecurityElement]::Escape($project)
@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>$framework</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><ProjectReference Include="$escaped"/></ItemGroup></Project>
"@|Set-Content -LiteralPath (Join-Path $scratch 'Inspector.csproj') -Encoding UTF8
$ns=if($System -eq 'SPC'){'MesSpc.Api.Infrastructure.Data'}else{'PmrPortal.Api.Data'}
@"
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using $ns;
// Construct the model only. Do not invoke Program, host startup, Migrate, or open any connection.
using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=OfflineSchemaOnly;Integrated Security=True").Options);
var ids=db.Database.GetMigrations().ToArray();
File.WriteAllText(Path.Combine(args[0],"migration-catalog.json"), System.Text.Json.JsonSerializer.Serialize(ids));
if(args.Length>1){
 if(!ids.Contains(args[1]))throw new InvalidOperationException("FromMigration is not in the actual EF catalogue");
 var sql=db.GetService<IMigrator>().GenerateScript(args[1],ids.Last(),MigrationsSqlGenerationOptions.Idempotent);
 File.WriteAllText(Path.Combine(args[0],"reviewed-ef-migration.sql"),sql);
}
Console.WriteLine("Offline EF catalogue exported; no database connection opened.");
"@|Set-Content -LiteralPath (Join-Path $scratch 'Program.cs') -Encoding UTF8
& dotnet restore (Join-Path $scratch 'Inspector.csproj')
if($LASTEXITCODE -ne 0){throw 'Offline inspector restore failed'}
$runArgs=@('run','--no-restore','--project',(Join-Path $scratch 'Inspector.csproj'),'--',$out)
if($FromMigration){$runArgs+=$FromMigration}
& dotnet @runArgs
if($LASTEXITCODE -ne 0){throw 'Offline EF review export failed'}
