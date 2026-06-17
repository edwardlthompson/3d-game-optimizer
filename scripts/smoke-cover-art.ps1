# Smoke test: download Steam cover art using production handler wiring.
param(
    [switch]$UserCache
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== smoke-cover-art ==="

$filter = "FullyQualifiedName~CoverArtSmoke"
if ($UserCache) {
    $env:SLO_COVER_SMOKE_USER_CACHE = "1"
    $filter = "FullyQualifiedName~CoverArtSmoke|FullyQualifiedName~ResolveCoverPathAsync_WritesToUserCache"
    Write-Host "UserCache: will write to %LOCALAPPDATA%\3d-game-optimizer\cache\covers"
}

dotnet test "src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj" `
    --filter $filter `
    --nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$cacheDir = Join-Path $env:LOCALAPPDATA "3d-game-optimizer/cache/covers"
New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null

$sampleIds = @(570, 1091500, 1086940)
$found = @()
foreach ($id in $sampleIds) {
    $path = Join-Path $cacheDir "$id.jpg"
    if (Test-Path $path) {
        $len = (Get-Item $path).Length
        if ($len -gt 1000) {
            $found += $path
            Write-Host "OK   cover $id ($len bytes)"
        } else {
            Write-Host "WARN cover $id too small ($len bytes): $path"
        }
    }
}

if ($UserCache) {
    if ($found.Count -lt $sampleIds.Count) {
        Write-Host "FAIL: -UserCache expected $($sampleIds.Count) covers in $cacheDir"
        exit 1
    }
    Write-Host "PASS: user cache populated ($($found.Count) covers)"
} elseif ($found.Count -eq 0) {
    Write-Host "NOTE: Temp test cache passed; user cache empty until -UserCache or app Refresh cover art."
    Write-Host "Cache dir: $cacheDir"
} else {
    Write-Host "Cached covers in user profile: $($found.Count)"
}

Write-Host "PASS: cover art smoke tests"
