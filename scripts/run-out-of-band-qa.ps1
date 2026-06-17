# Run all automatable out-of-band QA checks (cover art, PCVR, SteamDB policy).
param(
    [switch]$UserCache,
    [switch]$SkipUi,
    [string]$AppDir = "artifacts/product-win-x64/staging/app"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

Write-Host "=== run-out-of-band-qa ==="
Write-Host "Root: $Root"
Write-Host ""

function Invoke-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Host ""
    Write-Host ">>> $Name"
    & $Action
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL at: $Name"
        exit $LASTEXITCODE
    }
}

Invoke-Step "SteamDB policy (ADR-0005)" {
    $bash = $null
    if (Get-Command bash -ErrorAction SilentlyContinue) {
        $bash = "bash"
    } elseif (Test-Path "C:\Program Files\Git\bin\bash.exe") {
        $bash = "C:\Program Files\Git\bin\bash.exe"
    }
    if ($bash) {
        & $bash scripts/check-steamdb-policy.sh
    } else {
        Write-Error "bash not found — install Git for Windows to run ADR-0005 SteamDB policy check"
    }
}

Invoke-Step "Cover art smoke" {
    $args = @()
    if ($UserCache) { $args += "-UserCache" }
    & pwsh -NoProfile -File scripts/smoke-cover-art.ps1 @args
}

Invoke-Step "PCVR readiness" {
    & pwsh -NoProfile -File scripts/smoke-pcvr-readiness.ps1
}

$exe = Join-Path $AppDir "SpatialLabsOptimizer.exe"
if (-not $SkipUi -and (Test-Path $exe)) {
    Invoke-Step "UI startup smoke" {
        & pwsh -NoProfile -File scripts/smoke-ui-flows.ps1 -AppDir $AppDir
    }
} elseif (-not $SkipUi) {
    Write-Host ""
    Write-Host "SKIP UI startup smoke (no staging build at $exe)"
    Write-Host "  Build: pwsh scripts/build-product-local.ps1 -SkipGate"
}

Write-Host ""
Write-Host "=== run-out-of-band-qa PASS (automated) ==="
Write-Host ""
Write-Host "MANUAL ONLY — not automatable in CI:"
Write-Host "  [ ] Physical GPU vendors (NVIDIA / AMD / Intel) — docs/HARDWARE_QA_OUT_OF_BAND.md"
Write-Host "  [ ] Acer / Samsung display profile auto-select"
Write-Host "  [ ] Play in 3D on installed Steam title"
Write-Host "  [ ] Play in VR with headset (when runtime installed)"
Write-Host "  [ ] Keyboard-only setup wizard"
