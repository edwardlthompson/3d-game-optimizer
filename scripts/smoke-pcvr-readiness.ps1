# Probe PCVR runtime paths and run automated PlayInVR / connector tests.
param(
    [switch]$RequireRuntime
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== smoke-pcvr-readiness ==="

function Test-PathReport {
    param([string]$Label, [string]$Path)
    if (Test-Path $Path) {
        Write-Host "OK   $Label`: $Path"
        return $true
    }
    Write-Host "INFO $Label not found: $Path"
    return $false
}

$steamVr = Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\SteamVR"
$openXr = Join-Path $env:ProgramFiles "OpenXR"
$steamExe = Join-Path ${env:ProgramFiles(x86)} "Steam\steam.exe"

$hasSteamVr = Test-PathReport "SteamVR" $steamVr
$hasOpenXr = Test-PathReport "OpenXR" $openXr
$hasSteam = Test-PathReport "Steam" $steamExe

$tests = @(
    "FullyQualifiedName~PcvrConnector_ReturnsNull_WhenNoRuntime",
    "FullyQualifiedName~PlayInVR_GracefulFail_WhenNoRuntime",
    "FullyQualifiedName~CompatibilityRepository_LoadsSteamVrLaunchOptions"
)

foreach ($filter in $tests) {
    Write-Host "--- dotnet test $filter ---"
    dotnet test "src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj" `
        --filter $filter `
        --nologo -v q
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($RequireRuntime -and -not ($hasSteamVr -or $hasOpenXr)) {
    Write-Host "FAIL: -RequireRuntime set but no SteamVR or OpenXR installation detected"
    exit 1
}

Write-Host ""
Write-Host "AUTOMATED: connector probe, graceful-fail path, VR seed fields — PASS"
if ($hasSteamVr -or $hasOpenXr) {
    Write-Host "RUNTIME DETECTED: manual headset checks still recommended:"
    Write-Host "  [ ] Start SteamVR; launch native VR title via Play in VR"
    Write-Host "  [ ] Launch UEVR-compatible title; confirm no app crash"
} else {
    Write-Host "NO RUNTIME: automated no-runtime graceful-fail verified."
    Write-Host "MANUAL ONLY (when headset available):"
    Write-Host "  [ ] Install/start SteamVR or OpenXR runtime"
    Write-Host "  [ ] Play in VR on native VR + UEVR titles"
}
Write-Host "See docs/HARDWARE_QA_OUT_OF_BAND.md for full GPU/display checklist."
