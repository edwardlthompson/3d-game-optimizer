# Sign an MSI package with Authenticode (EV cert or sideload fallback).
param(
    [Parameter(Mandatory = $true)]
    [string]$MsiPath,
    [string]$PfxPath = "",
    [string]$PfxPassword = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root
. (Join-Path $Root "scripts/codesign-common.ps1")

if (-not (Test-Path $MsiPath)) {
    throw "MSI not found: $MsiPath"
}

$material = Resolve-CodeSignMaterial -Root $Root -PfxPath $PfxPath -PfxPassword $PfxPassword -AllowEphemeralSideload
Invoke-AuthenticodeSign -FilePath (Resolve-Path $MsiPath).Path -Material $material
Write-Host "Signed MSI mode=$($material.Mode): $MsiPath"
