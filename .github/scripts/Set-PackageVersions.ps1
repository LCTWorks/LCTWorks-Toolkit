<#
.SYNOPSIS
  Updates <PackageVersion> entries in Directory.Packages.props.

.PARAMETER Versions
  Comma-separated list of Id=Version pairs, e.g. "LCTWorks.Core=1.0.2610.2,LCTWorks.Telemetry=1.0.2610.2".

.PARAMETER IgnoreMissing
  Skip packages that have no <PackageVersion> entry instead of failing
  (used after a release, where e.g. LCTWorks.Web is published but not consumed internally).
#>
param(
    [Parameter(Mandatory)][string]$Versions,
    [string]$Path = 'Directory.Packages.props',
    [switch]$IgnoreMissing
)

$ErrorActionPreference = 'Stop'
$content = Get-Content $Path -Raw

foreach ($entry in $Versions.Split(',', [System.StringSplitOptions]::RemoveEmptyEntries)) {
    $id, $version = $entry.Trim().Split('=', 2)
    if (-not $id -or -not $version) { throw "Invalid entry '$entry' (expected Id=Version)." }

    $pattern = '(<PackageVersion\s+Include="' + [regex]::Escape($id) + '"\s+Version=")[^"]*(")'
    if (-not [regex]::IsMatch($content, $pattern)) {
        if ($IgnoreMissing) { Write-Host "$id has no PackageVersion entry; skipping."; continue }
        throw "No <PackageVersion Include=""$id"" Version=""...""/> entry found in $Path."
    }

    $content = [regex]::Replace($content, $pattern, '${1}' + $version + '${2}')
    Write-Host "$id -> $version"
}

Set-Content $Path $content -NoNewline
