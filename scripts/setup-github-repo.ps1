# Idempotent GitHub repo security setup (PowerShell wrapper).
# Usage: scripts/setup-github-repo.ps1 [-Repo owner/name]
param(
    [string]$Repo = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$GitBash = "C:\Program Files\Git\bin\bash.exe"
if (Test-Path $GitBash) {
    $Bash = $GitBash
} elseif (Get-Command bash -ErrorAction SilentlyContinue) {
    $Bash = "bash"
} else {
    Write-Host "ERROR: Git for Windows bash required for setup-github-repo.sh"
    exit 1
}

$bashArgs = @("scripts/setup-github-repo.sh")
if ($Repo) { $bashArgs += $Repo }

& $Bash @bashArgs
exit $LASTEXITCODE
