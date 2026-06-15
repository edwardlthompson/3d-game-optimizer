# Smoke-test a published local product build: exe must stay alive with a visible window.
param(
    [string]$AppDir = "artifacts/product-win-x64/staging/app",
    [int]$WaitSeconds = 15,
    [switch]$KeepRunning
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$exe = Join-Path $AppDir "SpatialLabsOptimizer.exe"
if (-not (Test-Path $exe)) {
    throw "Product exe not found: $exe (run scripts/publish-product.ps1 or scripts/build-product-local.ps1 first)"
}

$traceLog = Join-Path $env:LOCALAPPDATA "3d-game-optimizer\logs\startup-trace.log"
$failLog = Join-Path $env:LOCALAPPDATA "3d-game-optimizer\logs\startup-failures.log"
$successMarker = "Main shell shown; startup complete"

Write-Host "=== smoke-local-launch ==="
Write-Host "Exe: $exe"

Remove-Item $traceLog, $failLog -ErrorAction SilentlyContinue

$proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$passed = $false

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    $proc.Refresh()

    if ($proc.HasExited) {
        Write-Host "FAIL: process exited with code $($proc.ExitCode)"
        break
    }

    if ((Test-Path $traceLog) -and ((Get-Content $traceLog -Raw) -match [regex]::Escape($successMarker))) {
        $passed = $true
        break
    }
}

if (-not $proc.HasExited) {
    $proc.Refresh()
    if ($proc.MainWindowHandle -ne 0) {
        Write-Host "Window handle: $($proc.MainWindowHandle) title='$($proc.MainWindowTitle)'"
    }
}

if ($passed) {
    Start-Sleep -Seconds 3
    $proc.Refresh()
    if ($proc.HasExited) {
        Write-Host "FAIL: process exited after startup (code $($proc.ExitCode))"
        if (Test-Path $failLog) {
            Write-Host "--- startup-failures.log ---"
            Get-Content $failLog
        }
        exit 1
    }

    if (Test-Path $failLog) {
        $failText = Get-Content $failLog -Raw
        if ($failText.Trim().Length -gt 0) {
            Write-Host "FAIL: startup-failures.log has entries"
            Get-Content $failLog
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            exit 1
        }
    }

    Write-Host "PASS: startup completed and process still running (PID $($proc.Id))"
} elseif (-not $proc.HasExited -and $proc.MainWindowHandle -ne 0) {
    Write-Host "PARTIAL PASS: window visible but shell not fully loaded (PID $($proc.Id))"
    if (Test-Path $traceLog) { Get-Content $traceLog -Tail 10 }
    if (Test-Path $failLog) {
        Write-Host "--- startup-failures.log ---"
        Get-Content $failLog -Tail 10
    }
    $passed = $true
} else {
    Write-Host "FAIL: no window or startup trace after ${WaitSeconds}s"
    if (Test-Path $traceLog) {
        Write-Host "--- startup-trace.log ---"
        Get-Content $traceLog -Tail 20
    }
    if (Test-Path $failLog) {
        Write-Host "--- startup-failures.log ---"
        Get-Content $failLog -Tail 20
    }
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    exit 1
}

if (-not $KeepRunning -and -not $proc.HasExited) {
    Stop-Process -Id $proc.Id -Force
    Write-Host "Stopped test process."
}

exit 0
