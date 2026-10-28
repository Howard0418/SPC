[CmdletBinding()]
param(
    [ValidateSet('Provision', 'Validate', 'Remove')]
    [string]$Action = 'Provision',
    [string]$Path = (Join-Path $env:LOCALAPPDATA 'PMR\TransFiles\portal.credential.json')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$Purpose = 'PMR.TransFiles.Portal'
$Entropy = [Text.Encoding]::UTF8.GetBytes($Purpose)

function Get-PlainTextFromSecureString {
    param([Security.SecureString]$SecureValue)
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

function Set-CredentialFileAcl {
    param([string]$Target)
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    $acl = New-Object Security.AccessControl.FileSecurity
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($principal in @($identity, 'SYSTEM')) {
        $rule = New-Object Security.AccessControl.FileSystemAccessRule(
            $principal,
            [Security.AccessControl.FileSystemRights]::FullControl,
            [Security.AccessControl.AccessControlType]::Allow
        )
        [void]$acl.AddAccessRule($rule)
    }
    Set-Acl -LiteralPath $Target -AclObject $acl
}

function Read-CredentialFile {
    param([string]$Target)
    if (-not (Test-Path -LiteralPath $Target -PathType Leaf)) { throw 'Portal credential file does not exist.' }
    try {
        $data = Get-Content -LiteralPath $Target -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($data.version -ne 1) { throw 'Unsupported credential version.' }
        if ($data.purpose -ne $Purpose) { throw 'Credential purpose mismatch.' }
        if ([string]::IsNullOrWhiteSpace([string]$data.account)) { throw 'Portal account is missing.' }
        $cipher = [Convert]::FromBase64String([string]$data.ciphertext)
        $plain = [Security.Cryptography.ProtectedData]::Unprotect(
            $cipher, $Entropy, [Security.Cryptography.DataProtectionScope]::CurrentUser
        )
        if ($plain.Length -eq 0) { throw 'Decrypted Portal password is empty.' }
        [Array]::Clear($plain, 0, $plain.Length)
        [pscustomobject]@{ Account = [string]$data.account; Decryptable = $true }
    }
    catch { throw 'Credential format is invalid or cannot be decrypted by the current Windows identity.' }
}

switch ($Action) {
    'Provision' {
        $defaultAccount = $env:USERNAME
        $entered = Read-Host "Portal account [$defaultAccount]"
        $account = if ([string]::IsNullOrWhiteSpace($entered)) { $defaultAccount } else { $entered.Trim() }
        if ([string]::IsNullOrWhiteSpace($account)) { throw 'Portal account cannot be empty.' }
        $first = Read-Host 'Portal password' -AsSecureString
        $second = Read-Host 'Enter Portal password again' -AsSecureString
        $firstText = Get-PlainTextFromSecureString $first
        $secondText = Get-PlainTextFromSecureString $second
        try {
            if ([string]::IsNullOrEmpty($firstText)) { throw 'Portal password cannot be empty.' }
            if ($firstText -cne $secondText) { throw 'Passwords do not match. Nothing was saved.' }
            $plain = [Text.Encoding]::UTF8.GetBytes($firstText)
            try {
                $cipher = [Security.Cryptography.ProtectedData]::Protect(
                    $plain, $Entropy, [Security.Cryptography.DataProtectionScope]::CurrentUser
                )
            }
            finally { if ($null -ne $plain) { [Array]::Clear($plain, 0, $plain.Length) } }
            $payload = [ordered]@{
                version = 1
                purpose = $Purpose
                account = $account
                createdAtUtc = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss+00:00')
                ciphertext = [Convert]::ToBase64String($cipher)
            }
            $parent = Split-Path -Parent $Path
            [IO.Directory]::CreateDirectory($parent) | Out-Null
            $temporary = Join-Path $parent ('.' + [IO.Path]::GetFileName($Path) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
            try {
                $payload | ConvertTo-Json | Set-Content -LiteralPath $temporary -Encoding UTF8 -NoNewline
                Set-CredentialFileAcl $temporary
                Move-Item -LiteralPath $temporary -Destination $Path -Force
            }
            catch {
                Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
                throw
            }
            Write-Output "Portal credential saved: account=$account path=$Path"
        }
        finally {
            $firstText = $null
            $secondText = $null
            $first.Dispose()
            $second.Dispose()
        }
    }
    'Validate' {
        $result = Read-CredentialFile $Path
        Write-Output "Portal credential is decryptable: account=$($result.Account) path=$Path"
    }
    'Remove' {
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            Remove-Item -LiteralPath $Path -Force
            Write-Output 'Portal credential removed.'
        }
        else { Write-Output 'Portal credential does not exist.' }
    }
}
