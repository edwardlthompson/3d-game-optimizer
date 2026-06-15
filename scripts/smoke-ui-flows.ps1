# Launch app and poll startup trace until success or timeout.
param(
    [string]$AppDir = "artifacts/product-win-x64/staging/app",
    [int]$WaitSeconds = 30
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$exe = Join-Path $AppDir "SpatialLabsOptimizer.exe"
if (-not (Test-Path $exe)) {
    throw "Product exe not found: $exe"
}

$traceLog = Join-Path $env:LOCALAPPDATA "3d-game-optimizer\logs\startup-trace.log"
$failLog = Join-Path $env:LOCALAPPDATA "3d-game-optimizer\logs\startup-failures.log"
$successMarker = "Main shell shown; startup complete"
$iconMarker = "Window icon applied"

Get-Process -Name "SpatialLabsOptimizer" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Remove-Item $traceLog, $failLog -ErrorAction SilentlyContinue

Write-Host "=== smoke-ui-flows ==="
$proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$passed = $false

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if ($proc.HasExited) {
        throw "Process exited early with code $($proc.ExitCode)"
    }

    if ((Test-Path $traceLog) -and ((Get-Content $traceLog -Raw) -match [regex]::Escape($successMarker))) {
        $passed = $true
        break
    }
}

if (Test-Path $failLog) {
    $failText = Get-Content $failLog -Raw
    if ($failText.Trim().Length -gt 0) {
        Write-Host "FAIL: startup-failures.log has entries"
        Get-Content $failLog
        if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
        exit 1
    }
}

if (-not $passed) {
    Write-Host "FAIL: startup did not complete within ${WaitSeconds}s"
    if (Test-Path $traceLog) { Get-Content $traceLog -Tail 20 }
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    exit 1
}

Start-Sleep -Seconds 3
$proc.Refresh()
if ($proc.HasExited) {
    Write-Host "FAIL: process exited after startup (code $($proc.ExitCode))"
    if (Test-Path $failLog) { Get-Content $failLog }
    exit 1
}

if (Test-Path $failLog) {
    $failText = Get-Content $failLog -Raw
    if ($failText.Trim().Length -gt 0) {
        Write-Host "FAIL: startup-failures.log has entries"
        Get-Content $failLog
        Stop-Process -Id $proc.Id -Force
        exit 1
    }
}

Write-Host "PASS: UI startup flow completed and process still running"
if (Test-Path $traceLog) {
    $traceText = Get-Content $traceLog -Raw
    if ($traceText -notmatch [regex]::Escape($iconMarker)) {
        Write-Host "WARN: Window icon trace marker not found (non-fatal)"
    }
}
if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
exit 0
