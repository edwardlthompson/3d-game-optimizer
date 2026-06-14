# Automate optional post-sprint steps: validation, README assets, local build, GitHub release, Winget prep.
param(
    [switch]$SkipRelease,
    [switch]$SkipBuild,
    [switch]$OpenWingetPr,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== automate-optional-next-steps ==="

& (Join-Path $Root "scripts/run-post-sprint-validation.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipRelease) {
    $releaseArgs = @{}
    if ($DryRun) { $releaseArgs.DryRun = $true }
    if ($SkipBuild) { $releaseArgs.SkipLocalBuild = $true }
    & (Join-Path $Root "scripts/publish-product-github-release.ps1") @releaseArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($OpenWingetPr) {
    $wingetArgs = @{ OpenPr = $true }
    if ($DryRun) { $wingetArgs.DryRun = $true }
    & (Join-Path $Root "scripts/prepare-winget-submission.ps1") @wingetArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    & (Join-Path $Root "scripts/prepare-winget-submission.ps1")
}

Write-Host "=== automate-optional-next-steps complete ==="
