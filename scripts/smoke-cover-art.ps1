# Smoke test: download Steam cover art into the local cache using production handler wiring.
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== smoke-cover-art ==="

dotnet test "src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj" `
    --filter "FullyQualifiedName~CoverArtSmoke" `
    --nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$cacheDir = Join-Path $env:LOCALAPPDATA "3d-game-optimizer/cache/covers"
New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null

$sampleIds = @(570, 1091500, 1086940)
$found = @()
foreach ($id in $sampleIds) {
    $path = Join-Path $cacheDir "$id.jpg"
    if (Test-Path $path) { $found += $path }
}

if ($found.Count -eq 0) {
    Write-Host "NOTE: Temp test cache passed; user cache still empty until app Refresh cover art runs."
    Write-Host "Cache dir: $cacheDir"
    Write-Host "Run the staging build, open Library, click Refresh cover art."
} else {
    Write-Host "Cached covers:"
    $found | ForEach-Object { Write-Host "  $_ ($((Get-Item $_).Length) bytes)" }
}

Write-Host "PASS: cover art smoke tests"
