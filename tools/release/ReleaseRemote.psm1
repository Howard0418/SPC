Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'ProductionRelease.psm1')

function New-ReleaseDispatchRequest {
    param([string]$ManifestPath,[ValidateSet('Test','Production')][string]$Environment,[ValidateSet('SPC','Portal')][string]$System,[ValidateSet('Plan','Apply','Rollback')][string]$Mode,[string]$ToolRoot,[switch]$ApplicationAuthorized,[switch]$DatabaseAuthorized)
    if($Mode -ne 'Plan' -and (!$ApplicationAuthorized -or !$DatabaseAuthorized)){throw 'Application and database/startup authorization required before transfer'}
    $path=(Resolve-Path -LiteralPath $ManifestPath).Path
    Assert-NoReleaseReparse $path
    $bundle=Split-Path $path -Parent
    $plan=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
    if($plan.system -ine $System -or $plan.environment -ine $Environment){throw 'Selected environment/system differs from bundle; create the correct bundle'}
    $profile=Get-ReleaseProfile $System $Environment
    [void](Test-ReleaseManifest $plan $bundle $profile.HostName $profile.Environment)
    $files=@(@{Source=$path;Destination='manifest.json';Sha256=(Get-ReleaseHash $path)})
    $archives=@($plan.packages.archive|Select-Object -Unique)
    if($plan.database.migrationFile){$archives+=$plan.database.migrationFile}
    foreach($name in $archives){
        Assert-ReleaseRelativePath $name
        if($name -ieq 'manifest.json' -or $name -match '^tools/'){throw 'Reserved transport path'}
        $source=Join-Path $bundle $name
        $files+=@{Source=$source;Destination=$name;Sha256=(Get-ReleaseHash $source)}
    }
    foreach($name in @('Invoke-ReleaseOnServer.ps1','ProductionRelease.psm1','Start-ReleaseWorker.ps1')){
        $source=Join-Path $ToolRoot $name;Assert-NoReleaseReparse $source
        $files+=@{Source=$source;Destination=('tools/'+$name);Sha256=(Get-ReleaseHash $source)}
    }
    if(@($files.Destination|Select-Object -Unique).Count -ne $files.Count){throw 'Duplicate transport path'}
    $sha=[Security.Cryptography.SHA256]::Create()
    try{$fingerprint=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($files.Sha256 -join ','))))).Replace('-','').Substring(0,24)}finally{$sha.Dispose()}
    return [pscustomobject]@{Environment=$profile.Environment;System=$System;HostName=$profile.HostName;Machine=$profile.Machine;Mode=$Mode;ReleaseId=$plan.releaseId;Files=$files;Incoming=('C:\ProgramData\PMR-Release-Incoming\'+$profile.Environment+'\'+$plan.releaseId+'-'+$fingerprint);ApplicationAuthorized=[bool]$ApplicationAuthorized;DatabaseAuthorized=[bool]$DatabaseAuthorized}
}
function Invoke-ReleaseDispatch($Request,[hashtable]$Transport){
    # Check again immediately before any connection or copy; no arbitrary caller package scripts are executed.
    foreach($f in $Request.Files){Assert-ReleaseRelativePath $f.Destination;Assert-NoReleaseReparse $f.Source;if((Get-ReleaseHash $f.Source) -ine $f.Sha256){throw 'Transfer source changed'}}
    $context=$null
    try{
        $context=& $Transport.Open $Request
        $remoteManifest=& $Transport.Stage $context $Request
        return (& $Transport.Run $context $Request $remoteManifest)
    }finally{if($null -ne $context){& $Transport.Close $context}}
}
Export-ModuleMember -Function New-ReleaseDispatchRequest,Invoke-ReleaseDispatch
