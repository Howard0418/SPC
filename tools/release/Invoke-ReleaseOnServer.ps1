#requires -Version 5.1
[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Plan','Apply','Rollback')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$ManifestPath,
    [Parameter(Mandatory=$true)][ValidateSet('Test','Production')][string]$Environment,
    [switch]$ApplicationAuthorized,
    [switch]$DatabaseAuthorized
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'ProductionRelease.psm1') -Force

# Run on the actual destination host as administrator. No caller supplied target/root overrides.
$phase='manifest'
try{
    $manifestFile=(Resolve-Path -LiteralPath $ManifestPath).Path
    Assert-NoReleaseReparse $manifestFile
    $bundle=Split-Path $manifestFile -Parent
    $plan=Get-Content -LiteralPath $manifestFile -Raw -Encoding UTF8|ConvertFrom-Json
    $profile=Test-ReleaseManifest $plan $bundle $env:COMPUTERNAME $Environment
    $phase='iis-module'
    if(![Environment]::Is64BitProcess){throw 'Use 64-bit PowerShell for IIS'}
    Import-Module WebAdministration -ErrorAction Stop

    function Inspect-ReleaseSites($p) {
        $state=@{}
        foreach($pkg in $p.packages){
            Assert-NoReleaseReparse $pkg.path
            if(!(Test-Path -LiteralPath $pkg.path -PathType Container)){throw 'Website directory missing'}
            if(Test-Path -LiteralPath (Join-Path $pkg.path 'app_offline.htm')){throw 'Existing maintenance file; resolve before release'}
            $definition=@($profile.Targets|Where-Object {$_.Path -ieq $pkg.path})[0]
            $site=Get-Website -Name $pkg.site
            if($site.State -ne 'Started' -or $site.ApplicationPool -cne $pkg.pool -or [IO.Path]::GetFullPath($site.physicalPath).TrimEnd('\') -ine $definition.IisPath){throw 'Live IIS site/pool/path mismatch'}
            # An app pool shared by another site or application must not be stopped by this tool.
            foreach($other in Get-Website){if($other.Name -ne $pkg.site -and $other.ApplicationPool -eq $pkg.pool){throw 'Shared IIS pool rejected'}}
            foreach($app in Get-WebApplication){if($app.applicationPool -eq $pkg.pool){throw 'Pool with child applications rejected'}}
            $poolItem=Get-Item ('IIS:\AppPools\'+$pkg.pool)
            foreach($variable in $poolItem.environmentVariables.Collection){if($variable.name -match '^(ConnectionStrings|EnvironmentSwitch|AppEnvironment|ASPNETCORE_ENVIRONMENT|DOTNET_ENVIRONMENT)'){throw 'Pool environment override requires review'}}
            $state[$pkg.pool]=(Get-WebAppPoolState $pkg.pool).Value
            if($state[$pkg.pool] -notin @('Started','Stopped')){throw 'Pool is transitioning'}
            # Refuse reparse directories, including private directories, without traversing their targets.
            if(@(Get-ChildItem -LiteralPath $pkg.path -Recurse -Force|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Website contains reparse point'}
        }
        return $state
    }
    function Set-ReleasePool($name,$desired){
        if((Get-WebAppPoolState $name).Value -ne $desired){if($desired -eq 'Started'){Start-WebAppPool $name}else{Stop-WebAppPool $name}}
        for($attempt=0;$attempt -lt 30;$attempt++){if((Get-WebAppPoolState $name).Value -eq $desired){return};Start-Sleep -Milliseconds 500}
        throw 'IIS pool state timeout'
    }
    function Open-ReleaseDatabase($p){
        $apiPath=@($profile.Targets|Where-Object {$_.Role -eq 'Api'})[0].Path
        $api=@($p.packages|Where-Object {$_.path -ieq $apiPath})[0]
        # Merge exactly the normal Production JSON layers; secrets remain in process only.
        $settings=@{}
        foreach($name in @('appsettings.json','appsettings.Production.json')){
            $path=Join-Path $api.path $name
            if(Test-Path -LiteralPath $path){
                $j=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
                foreach($prop in $j.PSObject.Properties){
                    if($prop.Name -eq 'ConnectionStrings'){
                        if(!$settings.ContainsKey('ConnectionStrings')){$settings.ConnectionStrings=@{}}
                        foreach($c in $prop.Value.PSObject.Properties){$settings.ConnectionStrings[$c.Name]=$c.Value}
                    }else{$settings[$prop.Name]=$prop.Value}
                }
            }
        }
        # Environment overrides can change the effective database; stop instead of guessing.
        foreach($pkg in $p.packages){
            $config=Join-Path $pkg.path 'web.config'
            if(Test-Path -LiteralPath $config){
                [xml]$xml=Get-Content -LiteralPath $config -Raw -Encoding UTF8
                foreach($node in $xml.SelectNodes('//environmentVariable')){
                    if($node.name -match '^(ConnectionStrings|EnvironmentSwitch|AppEnvironment)' -or ($node.name -in @('ASPNETCORE_ENVIRONMENT','DOTNET_ENVIRONMENT') -and $node.value -ne 'Production')){throw 'IIS environment override needs explicit review'}
                }
            }
        }
        foreach($scope in @('Machine','Process')){foreach($name in [Environment]::GetEnvironmentVariables($scope).Keys){if($name -match '^(ConnectionStrings__|EnvironmentSwitch__|AppEnvironment$)'){throw 'Database environment override needs review'}}}
        if($p.system -eq 'SPC'){
            if($settings.AppEnvironment -ine $profile.Environment){throw 'SPC environment mismatch'}
            $cs=$settings.ConnectionStrings.SqlServer
        }else{
            if($settings.EnvironmentSwitch.Target -ine $profile.Environment){throw 'Portal environment mismatch'}
            $cs=$settings.ConnectionStrings[$profile.Environment]
        }
        Add-Type -AssemblyName System.Data
        $builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cs)
        if($builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$' -or $builder.InitialCatalog -cne $p.database.name){throw 'SQL server/database identity mismatch'}
        $connection=New-Object System.Data.SqlClient.SqlConnection($cs)
        try{$connection.Open();$q=$connection.CreateCommand();$q.CommandText='SELECT DB_NAME()';if($q.ExecuteScalar() -cne $p.database.name){throw 'Live database name mismatch'}}catch{$connection.Dispose();throw 'Database preflight failed; connection details withheld'}
        return $connection
    }
    function Get-ReleasePending($p){
        $c=Open-ReleaseDatabase $p
        try{$cmd=$c.CreateCommand();$cmd.CommandText='SELECT MigrationId FROM __EFMigrationsHistory';$reader=$cmd.ExecuteReader();$ids=@();while($reader.Read()){$ids+=$reader.GetString(0)};$reader.Close();return @($p.database.knownMigrationIds|Where-Object {$_ -notin $ids})}finally{$c.Dispose()}
    }
    function Test-ReleaseDatabase($p){if(@(Get-ReleasePending $p).Count){throw 'Source migrations remain pending; do not start new application'}}
    function Test-ReleasePendingAuthorization($p){
        $pending=@(Get-ReleasePending $p|Sort-Object);$approved=@($p.database.migrationIds|Sort-Object)
        Assert-ProductionPendingMigrations $pending $approved
    }
    function Invoke-ReleaseSmoke($p,$checks=$p.smoke){
        foreach($s in $checks){
            $ok=$false
            for($attempt=0;$attempt -lt 6;$attempt++){
                try{
                    $args=@{Uri=$s.url;Method=$s.method;UseBasicParsing=$true;TimeoutSec=15;MaximumRedirection=0}
                    if($s.method -eq 'POST'){$args.ContentType='application/json';$args.Body='{}'}
                    $w=Invoke-WebRequest @args
                    $ok=([int]$w.StatusCode -eq [int]$s.status -and (!$s.bodyPattern -or $w.Content -match $s.bodyPattern))
                    if($ok -and $s.PSObject.Properties['contentType'] -and $s.contentType){$ok=([string]$w.Headers['Content-Type']).Split(';')[0].Trim() -eq $s.contentType}
                }catch{
                    # Accept only deliberately expected anonymous authorization statuses, never arbitrary 4xx.
                    $ok=($s.status -in @(401,403) -and $_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq [int]$s.status -and !$s.bodyPattern)
                }
                if($ok){break};Start-Sleep -Seconds 2
            }
            if(!$ok){throw 'Smoke endpoint failed; response body withheld'}
        }
    }
    $adapter=@{
        Secure={param($r)
            & icacls.exe $r '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
            if($LASTEXITCODE -ne 0){throw 'Backup ACL failed'}
        }
        Inspect={param($p)$state=Inspect-ReleaseSites $p;Test-ReleasePendingAuthorization $p;return $state}
        Stop={param($p)foreach($pkg in $p.packages){Set-ReleasePool $pkg.pool 'Stopped'}}
        Start={param($p)foreach($pkg in $p.packages){Set-ReleasePool $pkg.pool 'Started'}}
        RestorePools={param($p,$state)foreach($pkg in $p.packages){$v=if($state -is [hashtable]){$state[$pkg.pool]}else{$state.PSObject.Properties[$pkg.pool].Value};Set-ReleasePool $pkg.pool $v}}
        BackupIis={param($p,$r)
            & "$env:windir\system32\inetsrv\appcmd.exe" add backup $p.releaseId | Out-Null
            if($LASTEXITCODE -ne 0){throw 'IIS backup failed'}
            Write-ReleaseJson @{backupName=$p.releaseId;system=$p.system} (Join-Path $r 'iis.json')
        }
        BackupDatabase={param($p,$r)
            $c=Open-ReleaseDatabase $p
            try{
                $cmd=$c.CreateCommand();$cmd.CommandTimeout=600
                $cmd.CommandText="SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(512))";$dir=[string]$cmd.ExecuteScalar();if(!$dir){throw 'No SQL backup directory'}
                $path=Join-Path $dir ($p.database.name+'-'+$p.releaseId+'-'+[guid]::NewGuid().ToString('N')+'.bak')
                $cmd.CommandText='BACKUP DATABASE ['+$p.database.name+'] TO DISK=@file WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=@file WITH CHECKSUM;'
                [void]$cmd.Parameters.AddWithValue('@file',$path);[void]$cmd.ExecuteNonQuery()
                Write-ReleaseJson @{database=$p.database.name;path=$path;verified=$true} (Join-Path $r 'database.json')
            }finally{$c.Dispose()}
        }
        Migrate={param($p,$bundle)
            if(!@($p.database.migrationIds).Count){Test-ReleaseDatabase $p;return}
            $file=Join-Path $bundle $p.database.migrationFile
            if((Get-ReleaseHash $file) -ine $p.database.migrationSha256){throw 'Migration SQL changed after plan'}
            $sql=Get-Content -LiteralPath $file -Raw -Encoding UTF8
            $c=Open-ReleaseDatabase $p
            try{$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*\r?$')){if($batch.Trim()){$cmd.CommandText=$batch;[void]$cmd.ExecuteNonQuery()}}}finally{$c.Dispose()}
            Test-ReleaseDatabase $p
        }
        Smoke={param($p)Invoke-ReleaseSmoke $p;Test-ReleaseDatabase $p}
        RollbackSmoke={param($p)Invoke-ReleaseSmoke $p $p.rollbackSmoke}
    }
    $phase='iis-preflight'
    [void](Inspect-ReleaseSites $plan)
    $phase='database-preflight'
    if($Mode -ne 'Rollback'){Test-ReleasePendingAuthorization $plan}else{$db=Open-ReleaseDatabase $plan;$db.Dispose()}
    $releaseRoot=Join-Path (Join-Path 'C:\ProgramData\PMR-Releases' $profile.Environment) $plan.releaseId
    Assert-NoReleaseReparse $releaseRoot
    if($Mode -eq 'Plan'){
        if(Test-Path -LiteralPath $releaseRoot){throw 'Release ID already used'}
        [pscustomobject]@{mode='Plan';environment=$profile.Environment;system=$plan.system;host=$plan.host;releaseId=$plan.releaseId;sourceCommit=$plan.sourceCommit;targets=@($plan.packages.site);database=$plan.database.name;message='Read-only preflight passed; no service or database changes'}
        return
    }
    $phase='authorization'
    if(!$ApplicationAuthorized -or !$DatabaseAuthorized){throw 'Apply/Rollback requires application and database/startup authorization'}
    if(!$PSCmdlet.ShouldProcess($plan.host,"$Mode $($plan.system) release $($plan.releaseId)")){return}
    # An exclusive file lock serializes both system tools on this server. Its file is retained for audit.
    [void][IO.Directory]::CreateDirectory('C:\ProgramData\PMR-Releases')
    $phase='exclusive-lock'
    $lock=[IO.File]::Open('C:\ProgramData\PMR-Releases\production-release.lock',[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try{
        $phase=$Mode
        if($Mode -eq 'Apply'){
            # Validate again inside lock to close concurrent modification windows.
            [void](Test-ReleaseManifest $plan $bundle $env:COMPUTERNAME $Environment)
            $result=Invoke-ProductionTransaction $plan $bundle $releaseRoot $adapter -ApplicationAuthorized -DatabaseAuthorized
            Write-Output ($result|Select-Object releaseId,status)
        }else{
            $saved=Get-Content -LiteralPath (Join-Path $releaseRoot 'plan.json') -Raw -Encoding UTF8
            if($saved.TrimEnd() -cne ($plan|ConvertTo-Json -Depth 30).TrimEnd()){throw 'Rollback manifest differs from applied manifest'}
            $state=Get-Content -LiteralPath (Join-Path $releaseRoot 'pool-state.json') -Raw -Encoding UTF8|ConvertFrom-Json
            foreach($pkg in $plan.packages){foreach($f in $pkg.files){if((Get-ReleaseHash (Join-Path $pkg.path $f.path)) -ine $f.sha256){throw 'Current files differ; rollback would overwrite a later change'}}}
            try{& $adapter.Stop $plan;Restore-ProductionFiles $releaseRoot $plan;& $adapter.Start $plan;& $adapter.RollbackSmoke $plan;& $adapter.RestorePools $plan $state;Write-ReleaseJson @{status='manual-rollback-completed';time=[DateTime]::UtcNow.ToString('o');database='schema retained'} (Join-Path $releaseRoot 'rollback.json')}
            catch{try{& $adapter.Stop $plan}catch{};Write-ReleaseJson @{status='manual-rollback-failed-maintenance';time=[DateTime]::UtcNow.ToString('o');errorType=$_.Exception.GetType().Name} (Join-Path $releaseRoot 'rollback.json');throw 'Rollback failed; keep maintenance; inspect private backup'}
            Write-Output 'Application rollback completed; schema retained; verify restored version using previous release checks'
        }
    }finally{$lock.Dispose()}
}catch{
    # Do not let PowerShell display connection strings, SQL errors or configuration source lines.
    Write-Error ('Production release stopped at '+$phase+' ('+$_.Exception.GetType().Name+'). '+$(if($_.Exception.Message -match '^(Release failed:|Recovery failed|Rollback failed)'){$_.Exception.Message}else{'Review manifest, authorization and private evidence; no response/configuration body is printed.'})) -ErrorAction Continue
    exit 1
}
