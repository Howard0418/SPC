Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

function Get-ProductionProfile([ValidateSet('SPC','Portal')][string]$System) {
    if($System -eq 'SPC') {
        return @{HostName='PMR-SPC-SRV.pmr.com.tw'; Machine='PMR-SPC-SRV'; Database='PMR_SPC_2026'; Hosts=@('PMR-SPC-SRV.pmr.com.tw','172.16.119.140'); Targets=@(
            @{Site='SpcApi';Pool='SpcApi';Path='C:\inetpub\wwwroot\production\backend'},
            @{Site='SpcWeb';Pool='SpcWeb';Path='C:\inetpub\wwwroot\production\frontend'})}
    }
    return @{HostName='PMR-WEB.pmr.com.tw'; Machine='PMR-WEB'; Database='PMR_PORTAL_UAT'; Hosts=@('PMR-WEB.pmr.com.tw','172.16.119.130','login.pmr.com.tw'); Targets=@(
        @{Site='PmrPortalApi';Pool='PmrPortalApi';Path='C:\inetpub\wwwroot\publish\portal-api'},
        @{Site='PmrPortalWeb';Pool='PmrPortalWeb';Path='C:\inetpub\wwwroot\publish\portal-web'})}
}

function Assert-ReleaseRelativePath([string]$Path) {
    if([string]::IsNullOrWhiteSpace($Path) -or $Path -match '[:\\]' -or $Path.StartsWith('/') -or $Path -match '[\x00-\x1f]' ) {throw 'Invalid relative path'}
    foreach($part in $Path.Split('/')) {if(!$part -or $part -in @('.','..') -or $part -match '[. ]$' -or $part -match '[<>"|?*]' -or $part -match '^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(\.|$)'){throw 'Unsafe path component'}}
}
function Assert-ReleasePayloadPath([string]$Path) {
    Assert-ReleaseRelativePath $Path
    foreach($part in $Path.Split('/')) {
        if($part -match '^(appsettings.*\.json|web\.config|app_offline\.htm|\.env.*|\.git|\.ssh|\.aws|\.codex|\.agents|uploads|persistent-data|private-data|keys|logs)$' -or $part -match '\.(pfx|p12|pem|key|bak|db|sqlite)$') {throw 'Protected configuration or private data in payload'}
    }
}
function Assert-NoReleaseReparse([string]$Path) {
    $cursor=[IO.Path]::GetFullPath($Path)
    while($cursor){
        if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse point rejected'}}
        $parent=Split-Path $cursor -Parent;if($parent -eq $cursor){break};$cursor=$parent
    }
}
function Assert-ReleaseWorkspacePath([string]$Path) {
    $full=[IO.Path]::GetFullPath($Path).TrimEnd('\','/')
    $blocked=[IO.Path]::GetFullPath('D:\Sites\PmrPortal').TrimEnd('\','/')
    if($full -ieq $blocked -or $full.StartsWith($blocked+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Forbidden Portal directory'}
    Assert-NoReleaseReparse $full
}
function Get-ReleaseHash([string]$Path) {(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Write-ReleaseJson($Value,[string]$Path) {$Value|ConvertTo-Json -Depth 30|Set-Content -LiteralPath $Path -Encoding UTF8}

function Test-ProductionManifest($Manifest,[string]$Bundle,[string]$MachineName) {
    $m=$Manifest;$p=Get-ProductionProfile $m.system
    if($m.system -cnotin @('SPC','Portal')){throw 'Unsupported system'}
    if($m.schemaVersion -ne 1 -or $m.environment -cne 'Production' -or $m.releaseId -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]{2,79}$' -or $m.sourceCommit -notmatch '^[a-fA-F0-9]{40}$'){throw 'Invalid manifest identity'}
    if($m.host -ine $p.HostName -or $MachineName -inotmatch ('^'+[regex]::Escape($p.Machine)+'(\.pmr\.com\.tw)?$') -or $m.database.name -cne $p.Database){throw 'Production host/database mismatch'}
    if(@($m.packages).Count -ne 2 -or @($m.packages.site|Select-Object -Unique).Count -ne 2){throw 'Exactly two distinct site packages required'}
    $bundleRoot=[IO.Path]::GetFullPath($Bundle).TrimEnd('\','/')
    Assert-NoReleaseReparse $bundleRoot
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach($pkg in $m.packages){
        $target=@($p.Targets|Where-Object {$_.Site -ceq $pkg.site})
        if($target.Count -ne 1 -or $pkg.pool -cne $target[0].Pool -or [IO.Path]::GetFullPath($pkg.path).TrimEnd('\') -ine $target[0].Path){throw 'Site/pool/path not in production allowlist'}
        if($bundleRoot -ieq $target[0].Path -or $bundleRoot.StartsWith($target[0].Path+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Bundle must be outside website'}
        if($pkg.version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$' -or $pkg.sha256 -notmatch '^[a-fA-F0-9]{64}$'){throw 'Missing package version/hash'}
        Assert-ReleaseRelativePath $pkg.archive
        $zipPath=Join-Path $bundleRoot $pkg.archive;Assert-NoReleaseReparse $zipPath
        if((Get-ReleaseHash $zipPath) -ine $pkg.sha256){throw 'Archive SHA256 mismatch'}
        $expected=@{};foreach($file in $pkg.files){Assert-ReleasePayloadPath $file.path;if($expected.ContainsKey($file.path) -or $file.sha256 -notmatch '^[a-fA-F0-9]{64}$'){throw 'Duplicate or invalid file hash'};$expected[$file.path]=$file.sha256}
        if(!$expected.Count){throw 'Empty package rejected'}
        $zip=[IO.Compression.ZipFile]::OpenRead($zipPath)
        try{
            $seen=@{};[long]$total=0
            foreach($entry in $zip.Entries){
                if($entry.FullName.EndsWith('/')){Assert-ReleaseRelativePath $entry.FullName.TrimEnd('/');continue}
                Assert-ReleasePayloadPath $entry.FullName
                if($seen.ContainsKey($entry.FullName) -or !$expected.ContainsKey($entry.FullName)){throw 'Unexpected/duplicate archive entry'}
                $total+=$entry.Length;if($total -gt 2GB){throw 'Expanded archive exceeds 2 GB limit'}
                $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
                try{$hash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
                if($hash -ine $expected[$entry.FullName]){throw 'Payload SHA256 mismatch'};$seen[$entry.FullName]=$true
            }
            if($seen.Count -ne $expected.Count){throw 'Missing archive entry'}
        }finally{$zip.Dispose()}
    }
    if(@($m.smoke).Count -lt 2){throw 'At least API and web smoke checks required'}
    if(@($m.rollbackSmoke).Count -lt 2){throw 'Previous API/web version checks required for rollback'}
    foreach($s in @($m.smoke)+@($m.rollbackSmoke)){
        $uri=[uri]$s.url
        if(!$uri.IsAbsoluteUri -or $uri.Scheme -notin @('http','https') -or $uri.Host -notin $p.Hosts -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or $s.method -notin @('GET','POST') -or $s.status -notin @(200,401,403)){throw 'Invalid smoke endpoint/status'}
        if($s.method -eq 'POST' -and $s.status -notin @(401,403)){throw 'Only anonymous authorization probes may POST'}
        if($s.bodyPattern){[void][regex]::new($s.bodyPattern)}
    }
    if(!@($m.database.knownMigrationIds).Count){throw 'Complete source EF migration catalogue required'}
    foreach($id in $m.database.knownMigrationIds){if($id -notmatch '^\d{14}_[A-Za-z0-9_]+$'){throw 'Invalid source migration catalogue'}}
    if(@($m.database.knownMigrationIds|Select-Object -Unique).Count -ne @($m.database.knownMigrationIds).Count){throw 'Duplicate source migration ID'}
    foreach($id in $m.database.migrationIds){if($id -notin $m.database.knownMigrationIds){throw 'Approved migration absent from source catalogue'}}
    if(@($m.database.migrationIds).Count -gt 0){
        Assert-ReleaseRelativePath $m.database.migrationFile
        if($m.database.migrationSha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-ReleaseHash (Join-Path $Bundle $m.database.migrationFile)) -ine $m.database.migrationSha256){throw 'Migration SQL SHA256 mismatch'}
        foreach($id in $m.database.migrationIds){if($id -notmatch '^\d{14}_[A-Za-z0-9_]+$'){throw 'Invalid migration ID'}}
    }elseif($m.database.migrationFile -or $m.database.migrationSha256){throw 'Migration list/file mismatch'}
    return $p
}

function Assert-ProductionPendingMigrations([string[]]$Pending,[string[]]$Approved) {
    if((@($Pending|Sort-Object) -join ',') -cne (@($Approved|Sort-Object) -join ',')){throw 'Actual pending migrations differ from approved SQL list'}
}

function Restore-ProductionFiles([string]$ReleaseRoot,$Plan) {
    $record=Get-Content -LiteralPath (Join-Path $ReleaseRoot 'backup.json') -Raw -Encoding UTF8|ConvertFrom-Json
    if($record.releaseId -cne $Plan.releaseId -or $record.system -cne $Plan.system -or $record.host -ine $Plan.host){throw 'Backup identity mismatch'}
    # Verify the complete backup before the first overwrite.
    foreach($f in $record.files){
        Assert-ReleasePayloadPath $f.relative
        if([int]$f.package -lt 0 -or [int]$f.package -ge @($Plan.packages).Count -or $f.target -ine $Plan.packages[[int]$f.package].path){throw 'Backup target mismatch'}
        $src=Join-Path $ReleaseRoot ('files/'+$f.package+'/'+$f.relative);Assert-NoReleaseReparse $src
        if((Get-ReleaseHash $src) -ine $f.sha256){throw 'Backup hash mismatch'}
        Assert-NoReleaseReparse (Join-Path $f.target $f.relative)
    }
    foreach($f in $record.files){Copy-Item -LiteralPath (Join-Path $ReleaseRoot ('files/'+$f.package+'/'+$f.relative)) -Destination (Join-Path $f.target $f.relative) -Force}
    foreach($f in $record.files){if((Get-ReleaseHash (Join-Path $f.target $f.relative)) -ine $f.sha256){throw 'Restored file hash mismatch'}}
    # New package files are intentionally retained; the old entrypoint/deps manifest selects old application files.
}

function Invoke-ProductionTransaction($Plan,[string]$Bundle,[string]$ReleaseRoot,[hashtable]$Adapter,[switch]$ApplicationAuthorized,[switch]$DatabaseAuthorized) {
    if(!$ApplicationAuthorized -or !$DatabaseAuthorized){throw 'Explicit application AND database/startup authorization required'}
    if(Test-Path -LiteralPath $ReleaseRoot){throw 'Release ID already used'}
    $state=& $Adapter.Inspect $Plan
    [void][IO.Directory]::CreateDirectory($ReleaseRoot)
    & $Adapter.Secure $ReleaseRoot
    $e=@{releaseId=$Plan.releaseId;operator=[Environment]::UserName;applicationAuthorized=[bool]$ApplicationAuthorized;databaseAuthorized=[bool]$DatabaseAuthorized;status='preparing';phase='prepare';startedAt=[DateTime]::UtcNow.ToString('o');newFiles=@();errorType=$null}
    $stopped=$false;$changed=$false;$backupReady=$false
    try{
        # Save original service state before attempting stop, including partially failed stop operations.
        Write-ReleaseJson $state (Join-Path $ReleaseRoot 'pool-state.json')
        Write-ReleaseJson $Plan (Join-Path $ReleaseRoot 'plan.json')
        $e.phase='stop';$stopped=$true;& $Adapter.Stop $Plan
        $e.phase='backup-iis'
        & $Adapter.BackupIis $Plan $ReleaseRoot
        $e.phase='backup-files'
        $records=@();$protected=@()
        for($i=0;$i -lt @($Plan.packages).Count;$i++){
            $pkg=$Plan.packages[$i]
            # Preserve every original payload file including files no longer shipped in the new package.
            foreach($old in Get-ChildItem -LiteralPath $pkg.path -File -Recurse -Force){
                $rel=$old.FullName.Substring($pkg.path.TrimEnd('\','/').Length+1).Replace('\','/')
                try{Assert-ReleasePayloadPath $rel}catch{continue}
                Assert-NoReleaseReparse $old.FullName
                $dest=Join-Path $ReleaseRoot ('files/'+$i+'/'+$rel);[void][IO.Directory]::CreateDirectory((Split-Path $dest -Parent));Copy-Item -LiteralPath $old.FullName -Destination $dest
                $hash=Get-ReleaseHash $old.FullName;if((Get-ReleaseHash $dest) -ine $hash){throw 'Website backup verification failed'}
                $records+=@{package=$i;relative=$rel;target=$pkg.path;sha256=$hash}
            }
            foreach($old in Get-ChildItem -LiteralPath $pkg.path -File -Force|Where-Object {$_.Name -like 'appsettings*.json' -or $_.Name -eq 'web.config'}){
                $dest=Join-Path $ReleaseRoot ('protected/'+$i+'/'+$old.Name);[void][IO.Directory]::CreateDirectory((Split-Path $dest -Parent));Copy-Item -LiteralPath $old.FullName -Destination $dest
                $configHash=Get-ReleaseHash $old.FullName
                if((Get-ReleaseHash $dest) -ine $configHash){throw 'Protected configuration backup failed'}
                $protected+=@{path=$old.FullName;sha256=$configHash}
            }
        }
        Write-ReleaseJson @{releaseId=$Plan.releaseId;system=$Plan.system;host=$Plan.host;files=$records} (Join-Path $ReleaseRoot 'backup.json')
        $backupReady=$true
        $e.phase='backup-database'
        & $Adapter.BackupDatabase $Plan $ReleaseRoot
        $e.phase='migrate'
        & $Adapter.Migrate $Plan $Bundle
        $e.phase='apply-files'
        for($i=0;$i -lt @($Plan.packages).Count;$i++){
            $pkg=$Plan.packages[$i];$stage=Join-Path $ReleaseRoot ('stage/'+$i)
            if($pkg.PSObject.Properties['sha256'] -and (Get-ReleaseHash (Join-Path $Bundle $pkg.archive)) -ine $pkg.sha256){throw 'Archive changed during release'}
            [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $Bundle $pkg.archive),$stage)
            $dll=@{SpcApi='MesSpc.Api.dll';PmrPortalApi='PmrPortal.Api.dll';PmrPortalWeb='PmrPortal.Web.dll'}
            if($dll.ContainsKey($pkg.site)){
                $binary=Join-Path $stage $dll[$pkg.site]
                $actual=[Diagnostics.FileVersionInfo]::GetVersionInfo($binary).FileVersion
                $want=if($pkg.version.Split('.').Count -eq 3){$pkg.version+'.0'}else{$pkg.version}
                if($actual -ne $want){throw 'Assembly version does not match manifest'}
            }
            foreach($file in $pkg.files){
                Assert-ReleasePayloadPath $file.path
                $src=Join-Path $stage $file.path;$dst=Join-Path $pkg.path $file.path
                Assert-NoReleaseReparse $dst
                if((Get-ReleaseHash $src) -ine $file.sha256){throw 'Staged file changed'}
                if(!(Test-Path -LiteralPath $dst)){$e.newFiles+=@{site=$pkg.site;path=$file.path}}
                [void][IO.Directory]::CreateDirectory((Split-Path $dst -Parent));$changed=$true
                Copy-Item -LiteralPath $src -Destination $dst -Force
                if((Get-ReleaseHash $dst) -ine $file.sha256){throw 'Deployed hash mismatch'}
            }
        }
        foreach($f in $protected){if((Get-ReleaseHash $f.path) -ine $f.sha256){throw 'Protected settings changed'}}
        $e.phase='start-and-smoke'
        & $Adapter.Start $Plan
        & $Adapter.Smoke $Plan
        & $Adapter.RestorePools $Plan $state
        $e.status='completed';$e.phase='complete'
    }catch{
        $e.finishedAt=[DateTime]::UtcNow.ToString('o')
        $e.errorType=$_.Exception.GetType().Name
        try{
            if($changed){& $Adapter.Stop $Plan;if(!$backupReady){throw 'Missing backup'};Restore-ProductionFiles $ReleaseRoot $Plan}
            if($changed){& $Adapter.Start $Plan;& $Adapter.RollbackSmoke $Plan}
            if($stopped){& $Adapter.RestorePools $Plan $state}
            $e.status=if($changed){'rolled-back'}else{'failed-before-apply'}
        }catch{try{& $Adapter.Stop $Plan}catch{};$e.status='recovery-failed-maintenance';$e.errorType=$_.Exception.GetType().Name;Write-ReleaseJson $e (Join-Path $ReleaseRoot 'evidence.json');throw 'Recovery failed; inspect private evidence; leave maintenance in place'}
        Write-ReleaseJson $e (Join-Path $ReleaseRoot 'evidence.json')
        throw ('Release failed: '+$e.status+'; database schema retained; see private evidence')
    }
    $e.finishedAt=[DateTime]::UtcNow.ToString('o');Write-ReleaseJson $e (Join-Path $ReleaseRoot 'evidence.json')
    return $e
}
Export-ModuleMember -Function Get-ProductionProfile,Assert-ReleaseWorkspacePath,Assert-ProductionPendingMigrations,Assert-ReleaseRelativePath,Assert-ReleasePayloadPath,Assert-NoReleaseReparse,Get-ReleaseHash,Test-ProductionManifest,Restore-ProductionFiles,Invoke-ProductionTransaction,Write-ReleaseJson
