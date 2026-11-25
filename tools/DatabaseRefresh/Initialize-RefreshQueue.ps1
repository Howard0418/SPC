[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'
if ($env:COMPUTERNAME -cne 'PMR-SPC-SRV') { throw 'Queue initialization requires the SPC production host.' }

Import-Module WebAdministration -ErrorAction Stop
$pool = Get-Item -LiteralPath 'IIS:\AppPools\SpcApi'
if ([string]$pool.processModel.identityType -cne 'ApplicationPoolIdentity') {
    throw 'The SPC API pool identity differs from the approved application pool identity.'
}
$poolSid = ([Security.Principal.NTAccount]::new('IIS APPPOOL\SpcApi')).Translate([Security.Principal.SecurityIdentifier])
$admin = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
$system = [Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$root = 'C:/ProgramData/PMR-SPC-Refresh'
if (Test-Path -LiteralPath $root) { throw 'Queue root already exists; review its ACL rather than overwriting it.' }
if ((Get-Item -LiteralPath 'C:/ProgramData' -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw 'Queue parent cannot be a reparse point.'
}
if (!$PSCmdlet.ShouldProcess($root, 'Create restricted SPC refresh directories')) { return }

function Set-QueueAcl([string]$path, [string]$appRights, [bool]$inheritAppAccess) {
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true,$false)
    $acl.SetOwner($admin)
    foreach ($principal in $admin,$system) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $principal,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
    }
    if ($appRights) {
        $inheritance = if ($inheritAppAccess) { 'ContainerInherit,ObjectInherit' } else { 'None' }
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $poolSid,$appRights,$inheritance,'None','Allow'))
    }
    Set-Acl -LiteralPath $path -AclObject $acl
}

$null = New-Item -ItemType Directory -Path $root
Set-QueueAcl $root 'ReadAndExecute' $false
foreach ($name in 'proof','state','queue') {
    $path = Join-Path $root $name
    $null = New-Item -ItemType Directory -Path $path
    $rights = switch ($name) {
        'proof' { 'ReadAndExecute' }
        'queue' { 'Modify' }
        'state' { $null }
    }
    Set-QueueAcl $path $rights $true
}
'SPC refresh queue directories initialized.'
