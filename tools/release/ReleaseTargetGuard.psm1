Set-StrictMode -Version Latest

function Assert-SpcReleaseTarget {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$EnvironmentName,

        [Parameter(Mandatory = $true)]
        [string]$SitePath
    )

    if ([string]::IsNullOrWhiteSpace($EnvironmentName)) {
        throw 'EnvironmentName is required.'
    }
    if ([string]::IsNullOrWhiteSpace($SitePath)) {
        throw 'SitePath is required.'
    }

    if ($EnvironmentName -match '^(prod|production|formal|正式)$') {
        throw 'This prototype refuses Production targets. Formal release requires a separate explicit authorization.'
    }

    $fullPath = [System.IO.Path]::GetFullPath($SitePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $blocked = [System.IO.Path]::GetFullPath('D:\Sites\PmrPortal').TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $blockedChildPrefix = $blocked + [System.IO.Path]::DirectorySeparatorChar
    if (
        [string]::Equals($fullPath, $blocked, [System.StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($blockedChildPrefix, [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        throw 'Blocked target path: D:\Sites\PmrPortal must not be used by this SPC release tool.'
    }
}

Export-ModuleMember -Function Assert-SpcReleaseTarget
