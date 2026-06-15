# Prepare or open a Winget manifest PR on microsoft/winget-pkgs.
param(
    [string]$Version = "",
    [switch]$OpenPr,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

if (-not $Version) {
    $Version = (Get-Content "src/SpatialLabsOptimizer/product-version.json" | ConvertFrom-Json).version
}

$bash = & (Join-Path $Root "scripts/resolve-bash.ps1")
$zip = "artifacts/product-win-x64/SpatialLabsOptimizer-$Version-win-x64.zip"
$msi = "artifacts/product-msi/SpatialLabsOptimizer-$Version-win-x64.msi"
$installer = if (Test-Path $msi) { $msi } elseif (Test-Path $zip) { $zip } else { "" }
$installerType = if (Test-Path $msi) { "msi" } else { "zip" }

if (-not $installer) {
    throw "Build artifacts missing. Run scripts/build-product-local.ps1 or publish-product-github-release.ps1 first."
}

& $bash scripts/generate-winget-multifile.sh "$Version" "$installer" "$installerType"
$src = Join-Path $Root "packaging/winget-product/multifile/$Version"
if (-not (Test-Path $src)) { throw "Multifile manifest not generated at $src" }

Write-Host "=== prepare-winget-submission (v$Version) ==="
Write-Host "Manifest triplet: $src"

if (-not $OpenPr) {
    Write-Host "Run with -OpenPr to fork winget-pkgs and open a PR (requires gh auth)."
    exit 0
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh CLI required for -OpenPr"
}

$wingetDir = Join-Path $env:TEMP "winget-pkgs-$Version"
$branch = "SpatialLabsOptimizer-v$Version"
$destRel = "manifests/e/edwardlthompson/SpatialLabsOptimizer/$Version"

if ($DryRun) {
    Write-Host "[DryRun] Clone microsoft/winget-pkgs -> $wingetDir"
    Write-Host "[DryRun] Copy $src -> $destRel"
    Write-Host "[DryRun] gh pr create --repo microsoft/winget-pkgs"
    exit 0
}

if (Test-Path $wingetDir) { Remove-Item -Recurse -Force $wingetDir }

$forkOwner = (gh api user --jq .login).Trim()
if (-not $forkOwner) { throw "gh auth required to resolve GitHub user for winget fork" }
gh repo fork microsoft/winget-pkgs --clone=false 2>$null | Out-Null

gh repo clone "$forkOwner/winget-pkgs" $wingetDir -- --depth 1
Push-Location $wingetDir
git remote add upstream https://github.com/microsoft/winget-pkgs.git 2>$null
git fetch upstream master --depth 1 2>$null
git checkout -b $branch
New-Item -ItemType Directory -Force -Path $destRel | Out-Null
Copy-Item -Path (Join-Path $src "*") -Destination $destRel -Force
git add $destRel
git commit -m "New version: edwardlthompson.SpatialLabsOptimizer $Version"
git push -u origin HEAD
gh pr create --repo microsoft/winget-pkgs `
    --head "${forkOwner}:${branch}" `
    --title "New version: edwardlthompson.SpatialLabsOptimizer $Version" `
    --body "Automated submission from 3d-game-optimizer v$Version.`n`nRelease: https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v$Version"
Pop-Location

Write-Host "prepare-winget-submission complete"
