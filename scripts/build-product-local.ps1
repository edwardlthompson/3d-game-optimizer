# Local product release orchestrator: publish, sign, MSIX, MSI, winget stub, verification.
param(
    [switch]$SkipGate,
    [switch]$Sign,
    [switch]$SkipMsix,
    [switch]$SkipMsi,
    [string]$PfxPath = "",
    [string]$PfxPassword = "",
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/product-win-x64"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== build-product-local ($Configuration) ==="

& (Join-Path $Root "scripts/check-local-release-prereqs.ps1") -RequireSign:$Sign -RequireMsi:(-not $SkipMsi) -RequireMsix:(-not $SkipMsix)

if (-not $SkipGate) {
    $bash = "bash"
    if (-not (Get-Command $bash -ErrorAction SilentlyContinue)) {
        if (Test-Path "C:\Program Files\Git\bin\bash.exe") {
            $bash = "C:\Program Files\Git\bin\bash.exe"
        } else {
            throw "bash required for pre-release gate (install Git for Windows)"
        }
    }
    & $bash scripts/pre-release-gate.sh --product-release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& (Join-Path $Root "scripts/publish-product.ps1") -Configuration $Configuration -OutputDir $OutputDir

$version = (Get-Content "src/SpatialLabsOptimizer/product-version.json" | ConvertFrom-Json).version
$zipPath = Join-Path $OutputDir "SpatialLabsOptimizer-$version-win-x64.zip"
$stagingApp = Join-Path $OutputDir "staging/app"

if ($Sign) {
    $signArgs = @{
        StagingDir = $stagingApp
        ZipPath    = $zipPath
    }
    if ($PfxPath) { $signArgs.PfxPath = $PfxPath }
    if ($PfxPassword) { $signArgs.PfxPassword = $PfxPassword }
    & (Join-Path $Root "scripts/sign-product-release.ps1") @signArgs
}

$msiPath = $null
if (-not $SkipMsi) {
    $msiArgs = @{}
    if ($Sign) { $msiArgs.Sign = $true }
    if ($PfxPath) { $msiArgs.PfxPath = $PfxPath }
    if ($PfxPassword) { $msiArgs.PfxPassword = $PfxPassword }
    & (Join-Path $Root "scripts/publish-product-msi.ps1") @msiArgs
    $msiPath = Join-Path "artifacts/product-msi" "SpatialLabsOptimizer-$version-win-x64.msi"
    if (-not (Test-Path $msiPath)) {
        $msiPath = (Get-ChildItem "artifacts/product-msi" -Filter "*.msi" -Recurse | Select-Object -First 1).FullName
    }
}

if (-not $SkipMsix) {
    if (Test-Path "src/SpatialLabsOptimizer/Assets/StoreLogo.png") {
        try {
            $msixArgs = @{}
            if ($Sign) { $msixArgs.Sign = $true }
            if ($PfxPath) { $msixArgs.PfxPath = $PfxPath }
            if ($PfxPassword) { $msixArgs.PfxPassword = $PfxPassword }
            & (Join-Path $Root "scripts/publish-product-msix.ps1") @msixArgs
        } catch {
            Write-Warning "MSIX build skipped: $_"
        }
    } else {
        Write-Host "Skipping MSIX — add src/SpatialLabsOptimizer/Assets/StoreLogo.png"
    }
}

$bash = "bash"
if (-not (Get-Command $bash -ErrorAction SilentlyContinue)) {
    $bash = "C:\Program Files\Git\bin\bash.exe"
}

$wingetInstaller = if ($msiPath -and (Test-Path $msiPath)) { $msiPath } else { $zipPath }
$wingetType = if ($msiPath -and (Test-Path $msiPath)) { "msi" } else { "zip" }
& $bash scripts/generate-winget-manifest.sh `
    "edwardlthompson.SpatialLabsOptimizer" `
    "$version" `
    "packaging/winget-product" `
    "$wingetInstaller" `
    "$wingetType"

if ($Sign) {
    $verifyArgs = @{ ZipPath = $zipPath }
    if ($msiPath -and (Test-Path $msiPath)) { $verifyArgs.MsiPath = $msiPath }
    $msix = Get-ChildItem "artifacts/product-msix" -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($msix) { $verifyArgs.MsixPath = $msix.FullName }
    & (Join-Path $Root "scripts/verify-product-signatures.ps1") @verifyArgs
}

& (Join-Path $Root "scripts/smoke-local-launch.ps1") -AppDir $stagingApp
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "=== build-product-local complete ==="
Write-Host "Zip:  $zipPath"
if ($msiPath -and (Test-Path $msiPath)) { Write-Host "MSI:  $msiPath" }
