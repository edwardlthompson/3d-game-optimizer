# Build MSI installer from published win-x64 staging using WiX v4.
param(
    [string]$StagingDir = "artifacts/product-win-x64/staging/app",
    [string]$OutputDir = "artifacts/product-msi",
    [switch]$Sign,
    [string]$PfxPath = "",
    [string]$PfxPassword = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

if (-not (Test-Path $StagingDir)) {
    throw "StagingDir not found: $StagingDir — run scripts/publish-product.ps1 first"
}

$version = (Get-Content "src/SpatialLabsOptimizer/product-version.json" | ConvertFrom-Json).version
$wixVersion = if ($version -match '^\d+\.\d+\.\d+$') { "$version.0" } else { $version }
$stagingFull = (Resolve-Path $StagingDir).Path
$outputFull = Join-Path $Root $OutputDir
New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

Write-Host "=== publish-product-msi ($version) ==="
Write-Host "Harvesting from $stagingFull"

dotnet build packaging/msi/Product.wixproj `
  -c Release `
  -p:AppVersion=$wixVersion `
  -p:DisplayVersion=$version `
  -p:StagingDir=$stagingFull `
  -p:OutputPath=$outputFull

$msiName = "SpatialLabsOptimizer-$version-win-x64.msi"
$msiPath = Join-Path $outputFull $msiName
if (-not (Test-Path $msiPath)) {
    $built = Get-ChildItem $outputFull -Filter "*.msi" -Recurse | Select-Object -First 1
    if (-not $built) {
        throw "MSI not found under $outputFull"
    }
    $msiPath = $built.FullName
}

if ($Sign) {
    & (Join-Path $Root "scripts/sign-product-msi.ps1") -MsiPath $msiPath -PfxPath $PfxPath -PfxPassword $PfxPassword
}

Write-Host "Wrote $msiPath"
