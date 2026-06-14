# Sprint sign-off gate (PowerShell wrapper).
param(
    [switch]$Quick,
    [int]$WaitSeconds = 300
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$Bash = "bash"
if (-not (Get-Command $Bash -ErrorAction SilentlyContinue)) {
    if (Test-Path "C:\Program Files\Git\bin\bash.exe") {
        $Bash = "C:\Program Files\Git\bin\bash.exe"
    } else {
        Write-Host "ERROR: bash or Git for Windows required"
        exit 1
    }
}

$Args = @("scripts/sprint-signoff-gate.sh", "--wait", "$WaitSeconds")
if ($Quick) { $Args = @("scripts/sprint-signoff-gate.sh", "--quick", "--wait", "$WaitSeconds") }

& $Bash @Args
exit $LASTEXITCODE
