# Rebuild, publish, launch, and poll startup-trace.log until success or max attempts.
param(
    [string]$AppDir = "artifacts/product-win-x64/staging/app",
    [int]$MaxAttempts = 5,
    [int]$WaitSeconds = 20
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$logDir = Join-Path $env:LOCALAPPDATA "3d-game-optimizer\logs"
$traceLog = Join-Path $logDir "startup-trace.log"
$failLog = Join-Path $logDir "startup-failures.log"
$successMarker = "Main shell shown; startup complete"
$minMarker = "MainWindow activated"

function Test-LaunchSuccess {
    param([string]$TracePath)
    if (-not (Test-Path $TracePath)) { return $false }
    $text = Get-Content $TracePath -Raw -ErrorAction SilentlyContinue
    return ($text -match [regex]::Escape($successMarker))
}

function Test-PartialLaunch {
    param([string]$TracePath)
    if (-not (Test-Path $TracePath)) { return $false }
    $text = Get-Content $TracePath -Raw -ErrorAction SilentlyContinue
    return ($text -match [regex]::Escape($minMarker))
}

Write-Host "=== troubleshoot-launch-loop ==="

for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    Write-Host ""
    Write-Host "--- Attempt $attempt/$MaxAttempts ---"

    Get-Process -Name "SpatialLabsOptimizer" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500

    Write-Host "Building and publishing..."
    dotnet build src/SpatialLabsOptimizer/SpatialLabsOptimizer.csproj -c Release -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    pwsh -NoProfile -File scripts/publish-product.ps1 -Configuration Release -OutputDir artifacts/product-win-x64 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

    $exe = Join-Path $AppDir "SpatialLabsOptimizer.exe"
    $pri = Join-Path $AppDir "SpatialLabsOptimizer.pri"
    if (-not (Test-Path $exe)) { throw "Missing exe: $exe" }
    if (-not (Test-Path $pri)) { throw "Missing pri: $pri" }

    Remove-Item $traceLog, $failLog -ErrorAction SilentlyContinue

    Write-Host "Launching $exe"
    $proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru

    $deadline = (Get-Date).AddSeconds($WaitSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        if (Test-LaunchSuccess $traceLog) {
            Write-Host "SUCCESS: Full startup completed (PID $($proc.Id))"
            if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
            exit 0
        }
        if ($proc.HasExited) {
            $hex = ('0x{0:X8}' -f ($proc.ExitCode -band 0xFFFFFFFF))
            Write-Host "Process exited: code=$($proc.ExitCode) hex=$hex"
            break
        }
    }

    if (Test-PartialLaunch $traceLog) {
        Write-Host "PARTIAL: Window activated but shell not loaded yet"
    }

    Write-Host "Trace tail:"
    if (Test-Path $traceLog) { Get-Content $traceLog -Tail 15 } else { Write-Host "(no trace)" }
    if (Test-Path $failLog) {
        Write-Host "Failures:"
        Get-Content $failLog -Tail 10
    }

    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
}

Write-Host ""
Write-Host "FAILED after $MaxAttempts attempts"
exit 1
