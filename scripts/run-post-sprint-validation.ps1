# Run post-sprint validation: encoding, dotnet tests, bash gates, README assets.
param(
    [switch]$SkipDotnet,
    [switch]$SkipBash,
    [switch]$SkipAssets
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== run-post-sprint-validation ==="

python (Join-Path $Root "scripts/check-file-encoding.py")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipDotnet) {
    dotnet build (Join-Path $Root "SpatialLabsOptimizer.sln") -c Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet test (Join-Path $Root "SpatialLabsOptimizer.sln") -c Release --no-build --verbosity minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not $SkipBash) {
    $bash = & (Join-Path $Root "scripts/resolve-bash.ps1")
    $scripts = @(
        "scripts/check-local-release-scripts.sh",
        "scripts/check-qa-matrix-coverage.sh",
        "scripts/check-compatibility-seed.sh"
    )
    foreach ($script in $scripts) {
        & $bash $script
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}

if (-not $SkipAssets) {
    python (Join-Path $Root "scripts/generate-brand-assets.py")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "run-post-sprint-validation passed"
