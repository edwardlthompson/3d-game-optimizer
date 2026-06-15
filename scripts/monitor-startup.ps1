# Launch SpatialLabsOptimizer and tail startup trace logs (Windows equivalent of logcat).
param(
    [string]$AppDir = "artifacts/product-win-x64/staging/app",
    [int]$MonitorSeconds = 20,
    [switch]$KeepRunning
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$exe = Join-Path $AppDir "SpatialLabsOptimizer.exe"
if (-not (Test-Path $exe)) {
    throw "Product exe not found: $exe"
}

$logDir = Join-Path $env:LOCALAPPDATA "3d-game-optimizer\logs"
$traceLog = Join-Path $logDir "startup-trace.log"
$failLog = Join-Path $logDir "startup-failures.log"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

foreach ($path in @($traceLog, $failLog)) {
    if (Test-Path $path) { Remove-Item $path -Force }
}

Write-Host "=== monitor-startup ==="
Write-Host "Exe:   $exe"
Write-Host "Trace: $traceLog"
Write-Host "Fail:  $failLog"
Write-Host ""

$proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
Write-Host "Started PID $($proc.Id)"

$seenTrace = 0
$seenFail = 0
$deadline = (Get-Date).AddSeconds($MonitorSeconds)

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 400
    $proc.Refresh()

    if (Test-Path $traceLog) {
        $lines = Get-Content $traceLog
        if ($lines.Count -gt $seenTrace) {
            $lines[$seenTrace..($lines.Count - 1)] | ForEach-Object { Write-Host "[trace] $_" }
            $seenTrace = $lines.Count
        }
    }

    if (Test-Path $failLog) {
        $lines = Get-Content $failLog
        if ($lines.Count -gt $seenFail) {
            $lines[$seenFail..($lines.Count - 1)] | ForEach-Object { Write-Host "[FAIL] $_" -ForegroundColor Red }
            $seenFail = $lines.Count
        }
    }

    if ($proc.HasExited) {
        $code = $proc.ExitCode
        $hex = ('0x{0:X8}' -f ($code -band 0xFFFFFFFF))
        Write-Host ""
        Write-Host "Process exited after $(((Get-Date) - $proc.StartTime).TotalSeconds.ToString('F1'))s code=$code hex=$hex"
        break
    }
}

if (-not $proc.HasExited) {
    Write-Host ""
    Write-Host "Process still running after ${MonitorSeconds}s (MainWindowHandle=$($proc.MainWindowHandle))"
    if (-not $KeepRunning) {
        Stop-Process -Id $proc.Id -Force
        Write-Host "Stopped test process."
    }
}

Write-Host ""
Write-Host "=== Final trace log ==="
if (Test-Path $traceLog) { Get-Content $traceLog } else { Write-Host "(empty)" }

Write-Host ""
Write-Host "=== Final failure log ==="
if (Test-Path $failLog) { Get-Content $failLog } else { Write-Host "(empty)" }

$traceText = if (Test-Path $traceLog) { Get-Content $traceLog -Raw } else { "" }
if ($traceText -notmatch "MainWindow activated") {
    Write-Host ""
    Write-Host "WARNING: startup did not reach MainWindow activation." -ForegroundColor Yellow
    Write-Host "Check that SpatialLabsOptimizer.pri sits beside the exe in the publish folder."
}

$serilog = Get-ChildItem $logDir -Filter "spatiallabs-optimizer-*.log" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($serilog) {
    Write-Host ""
    Write-Host "=== Serilog tail ($($serilog.Name)) ==="
    Get-Content $serilog.FullName -Tail 20
}

exit $(if ($proc.HasExited -and $proc.ExitCode -ne 0) { $proc.ExitCode } elseif ($seenFail -gt 0) { 1 } else { 0 })
