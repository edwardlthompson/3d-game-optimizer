# Publish MSIX product package (Sprint 17) with optional Authenticode signing.
# Requires StoreLogo assets under src/SpatialLabsOptimizer/Assets/ before first MSIX build.
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/product-msix",
    [switch]$Sign,
    [string]$PfxPath = "",
    [string]$PfxPassword = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root
. (Join-Path $Root "scripts/codesign-common.ps1")

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

dotnet publish src/SpatialLabsOptimizer/SpatialLabsOptimizer.csproj `
    -c $Configuration `
    -r win-x64 `
    -p:BuildMsix=true `
    -p:AppxPackageDir=$OutputDir `
    -p:GenerateAppxPackageOnBuild=true

$msix = Get-ChildItem $OutputDir -Filter *.msix -Recurse | Select-Object -First 1
if (-not $msix) {
    throw "MSIX package not found under $OutputDir"
}

Write-Host "MSIX output: $($msix.FullName)"

if ($Sign) {
    $material = Resolve-CodeSignMaterial -Root $Root -PfxPath $PfxPath -PfxPassword $PfxPassword -AllowEphemeralSideload
    Invoke-AuthenticodeSign -FilePath $msix.FullName -Material $material
    Write-Host "Signed MSIX mode=$($material.Mode)"
}
