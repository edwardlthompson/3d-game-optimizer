# Register or unregister the 3dgo://play/{appid} custom URL protocol for SpatialLabs Optimizer.
param(
    [string]$ExePath = "",
    [switch]$Unregister
)

$ErrorActionPreference = "Stop"

if (-not $ExePath) {
    $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
    $ReleaseExe = Join-Path $Root "artifacts\product-win-x64\inspect\SpatialLabsOptimizer.exe"
    $DebugExe = Join-Path $Root "src\SpatialLabsOptimizer\bin\Debug\net8.0-windows10.0.19041.0\win-x64\SpatialLabsOptimizer.exe"
    if (Test-Path $ReleaseExe) {
        $ExePath = (Resolve-Path $ReleaseExe).Path
    } elseif (Test-Path $DebugExe) {
        $ExePath = (Resolve-Path $DebugExe).Path
    } else {
        Write-Error "SpatialLabsOptimizer.exe not found. Pass -ExePath explicitly."
    }
}

$ProtocolRoot = "HKCU:\Software\Classes\3dgo"

if ($Unregister) {
    if (Test-Path $ProtocolRoot) {
        Remove-Item -Path $ProtocolRoot -Recurse -Force
        Write-Host "Unregistered 3dgo:// protocol."
    } else {
        Write-Host "3dgo:// protocol was not registered."
    }
    exit 0
}

New-Item -Path $ProtocolRoot -Force | Out-Null
Set-ItemProperty -Path $ProtocolRoot -Name "(Default)" -Value "URL:3dgo Protocol"
Set-ItemProperty -Path $ProtocolRoot -Name "URL Protocol" -Value ""

$iconKey = Join-Path $ProtocolRoot "DefaultIcon"
New-Item -Path $iconKey -Force | Out-Null
Set-ItemProperty -Path $iconKey -Name "(Default)" -Value "`"$ExePath`",1"

$commandKey = Join-Path $ProtocolRoot "shell\open\command"
New-Item -Path $commandKey -Force | Out-Null
Set-ItemProperty -Path $commandKey -Name "(Default)" -Value "`"$ExePath`" `"%1`""

Write-Host "Registered 3dgo://play/{appid} -> $ExePath"
Write-Host "Example: Start-Process '3dgo://play/570'"
