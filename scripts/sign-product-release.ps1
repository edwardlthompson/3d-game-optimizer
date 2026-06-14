# Sign product release exes inside a staging directory and/or published zip.
# Supports local builds (staging dir, explicit PFX) and CI (CODESIGN_* env vars).
param(
    [string]$ZipPath = "",
    [string]$StagingDir = "",
    [string]$PfxPath = "",
    [string]$PfxPassword = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root
. (Join-Path $Root "scripts/codesign-common.ps1")

if ([string]::IsNullOrWhiteSpace($ZipPath) -and [string]::IsNullOrWhiteSpace($StagingDir)) {
    throw "Specify -ZipPath and/or -StagingDir"
}

$workDir = $null
$zipProvided = -not [string]::IsNullOrWhiteSpace($ZipPath)
$stagingProvided = -not [string]::IsNullOrWhiteSpace($StagingDir)

if ($stagingProvided) {
    if (-not (Test-Path $StagingDir)) {
        throw "StagingDir not found: $StagingDir"
    }
    $workDir = (Resolve-Path $StagingDir).Path
}
elseif ($zipProvided) {
    if (-not (Test-Path $ZipPath)) {
        throw "Zip not found: $ZipPath"
    }
    $baseTemp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } elseif ($env:TEMP) { $env:TEMP } else { (Join-Path $Root "artifacts/sign-staging") }
    $workDir = Join-Path $baseTemp "sign-staging"
    if (Test-Path $workDir) { Remove-Item -Recurse -Force $workDir }
    New-Item -ItemType Directory -Force -Path $workDir | Out-Null
    Expand-Archive $ZipPath -DestinationPath $workDir -Force
}

$exes = Get-ChildItem $workDir -Filter *.exe -Recurse
if ($exes.Count -eq 0) {
    Write-Host "No executables to sign"
    exit 0
}

$material = Resolve-CodeSignMaterial -Root $Root -PfxPath $PfxPath -PfxPassword $PfxPassword -AllowEphemeralSideload

foreach ($exe in $exes) {
    Invoke-AuthenticodeSign -FilePath $exe.FullName -Material $material
}

if ($zipProvided) {
    $zipTarget = if ($stagingProvided) {
        if (-not (Test-Path (Split-Path $ZipPath -Parent))) {
            New-Item -ItemType Directory -Force -Path (Split-Path $ZipPath -Parent) | Out-Null
        }
        $ZipPath
    } else {
        $ZipPath
    }
    if (Test-Path $zipTarget) { Remove-Item $zipTarget -Force }
    Compress-Archive -Path (Join-Path $workDir "*") -DestinationPath $zipTarget
    Write-Host "Updated $zipTarget"
}

Write-Host "Signed $($exes.Count) executable(s) mode=$($material.Mode)"
