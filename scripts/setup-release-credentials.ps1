# PowerShell wrapper for setup-release-credentials.sh
param(
    [string]$Repo = "",
    [switch]$PushSideloadSecrets
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

if ($PushSideloadSecrets) {
    $env:AUTO_SETUP_SIDeload_CODESIGN = "1"
}

$Bash = "bash"
if (Test-Path "C:\Program Files\Git\bin\bash.exe") {
    $Bash = "C:\Program Files\Git\bin\bash.exe"
}

$args = @("scripts/setup-release-credentials.sh")
if ($Repo) { $args += $Repo }

& $Bash @args
exit $LASTEXITCODE
