[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Import-Module (Join-Path $repoRoot 'tools/release/ReleaseTargetGuard.psm1') -Force

function Assert-ThrowsLike {
    param(
        [scriptblock]$Script,
        [string]$ExpectedMessage
    )

    try {
        & $Script
    }
    catch {
        if ($_.Exception.Message -notlike $ExpectedMessage) {
            throw "Expected error like '$ExpectedMessage' but got '$($_.Exception.Message)'."
        }
        return
    }

    throw "Expected script to throw '$ExpectedMessage'."
}

Assert-ThrowsLike -ExpectedMessage '*Production targets*' -Script {
    Assert-SpcReleaseTarget -EnvironmentName 'Production' -SitePath 'D:\SPC\release\test\backend'
}

Assert-ThrowsLike -ExpectedMessage '*Production targets*' -Script {
    Assert-SpcReleaseTarget -EnvironmentName '正式' -SitePath 'D:\SPC\release\test\backend'
}

Assert-ThrowsLike -ExpectedMessage '*D:\Sites\PmrPortal*' -Script {
    Assert-SpcReleaseTarget -EnvironmentName 'Sandbox' -SitePath 'D:\Sites\PmrPortal\wwwroot'
}

Assert-SpcReleaseTarget -EnvironmentName 'Sandbox' -SitePath 'D:\Sites\PmrPortal2'

Assert-ThrowsLike -ExpectedMessage '*EnvironmentName is required*' -Script {
    Assert-SpcReleaseTarget -EnvironmentName ' ' -SitePath 'D:\SPC\release\test\backend'
}

Assert-ThrowsLike -ExpectedMessage '*SitePath is required*' -Script {
    Assert-SpcReleaseTarget -EnvironmentName 'Sandbox' -SitePath ' '
}

Assert-SpcReleaseTarget -EnvironmentName 'Sandbox' -SitePath 'D:\SPC\release\test\backend'

Write-Output 'ReleaseTargetGuard.Tests passed.'
