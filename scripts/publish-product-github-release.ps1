# Create GitHub product release and trigger CI build/attach workflow.
param(
    [string]$Version = "",
    [switch]$Draft,
    [switch]$SkipValidation,
    [switch]$SkipLocalBuild,
    [switch]$SkipWorkflow,
    [switch]$SkipMsi,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI required (https://cli.github.com/)"
}

if (-not $Version) {
    $Version = (Get-Content "src/SpatialLabsOptimizer/product-version.json" | ConvertFrom-Json).version
}
$Tag = "SpatialLabsOptimizer-v$Version"

Write-Host "=== publish-product-github-release ($Tag) ==="

if (-not $SkipValidation) {
    & (Join-Path $Root "scripts/run-post-sprint-validation.ps1")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$dirty = git status --porcelain
if ($dirty) {
    Write-Warning "Working tree has uncommitted changes. Push to main before CI release workflow can build from remote."
}

if (-not $SkipLocalBuild) {
    $buildArgs = @{ SkipGate = $true; Sign = $true }
    if ($SkipMsi) { $buildArgs.SkipMsi = $true }
    & (Join-Path $Root "scripts/build-product-local.ps1") @buildArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$zip = "artifacts/product-win-x64/SpatialLabsOptimizer-$Version-win-x64.zip"
$msi = "artifacts/product-msi/SpatialLabsOptimizer-$Version-win-x64.msi"
$msix = Get-ChildItem "artifacts/product-msix" -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
$bash = & (Join-Path $Root "scripts/resolve-bash.ps1")

$installer = if (Test-Path $msi) { $msi } else { $zip }
$installerType = if (Test-Path $msi) { "msi" } else { "zip" }
& $bash scripts/generate-winget-manifest.sh `
    "edwardlthompson.SpatialLabsOptimizer" `
    "$Version" `
    "packaging/winget-product" `
    "$installer" `
    "$installerType"
& $bash scripts/generate-winget-multifile.sh "$Version" "$installer" "$installerType"

$notes = @"
## SpatialLabs Optimizer v$Version

- Self-contained win-x64 zip
- WiX MSI installer (when built)
- MSIX sideload package (when StoreLogo assets present)
- Winget multifile manifest stub in \`packaging/winget-product/multifile/$Version/\`

See [docs/LOCAL_RELEASE.md](https://github.com/edwardlthompson/3d-game-optimizer/blob/main/docs/LOCAL_RELEASE.md) for local build instructions.
"@

if ($DryRun) {
    Write-Host "[DryRun] gh release create $Tag ..."
    Write-Host "[DryRun] gh workflow run product-release.yml -f tag=$Tag"
    Write-Host "[DryRun] Assets: $zip"
    if (Test-Path $msi) { Write-Host "[DryRun] MSI: $msi" }
    if ($msix) { Write-Host "[DryRun] MSIX: $($msix.FullName)" }
    exit 0
}

$releaseArgs = @("release", "create", $Tag, "--title", "SpatialLabs Optimizer v$Version", "--notes", $notes)
if ($Draft) { $releaseArgs += "--draft" }
if (Test-Path $zip) { $releaseArgs += $zip }
if (Test-Path $msi) { $releaseArgs += $msi }
if ($msix) { $releaseArgs += $msix.FullName }
$releaseArgs += "packaging/winget-product/manifest.stub.yaml"

& gh @releaseArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Release may already exist — attempting upload only..."
    $uploadArgs = @("release", "upload", $Tag, $zip, "packaging/winget-product/manifest.stub.yaml", "--clobber")
    if (Test-Path $msi) { $uploadArgs += $msi }
    if ($msix) { $uploadArgs += $msix.FullName }
    & gh @uploadArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not $SkipWorkflow) {
    Write-Host "Dispatching product-release workflow for CI-signed rebuild..."
    gh workflow run product-release.yml -f "tag=$Tag"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Workflow dispatch failed — run manually: gh workflow run product-release.yml -f tag=$Tag"
    }
}

Write-Host "=== publish-product-github-release complete ==="
Write-Host "Release: https://github.com/$(gh repo view --json nameWithOwner -q .nameWithOwner)/releases/tag/$Tag"
