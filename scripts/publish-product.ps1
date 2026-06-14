# Publish self-contained win-x64 product bundle (app + ElevatedHelper + data).
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/product-win-x64"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$AppProj = "src/SpatialLabsOptimizer/SpatialLabsOptimizer.csproj"
$HelperProj = "src/SpatialLabsOptimizer.ElevatedHelper/SpatialLabsOptimizer.ElevatedHelper.csproj"
$Staging = Join-Path $OutputDir "staging"
$PublishApp = Join-Path $Staging "app"
$PublishHelper = Join-Path $Staging "helper"

if (Test-Path $Staging) { Remove-Item -Recurse -Force $Staging }
New-Item -ItemType Directory -Force -Path $PublishApp, $PublishHelper | Out-Null

Write-Host "=== publish-product ($Configuration) ==="

dotnet publish $AppProj -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=false -o $PublishApp
dotnet publish $HelperProj -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -o $PublishHelper

$HelperExe = Join-Path $PublishHelper "SpatialLabsOptimizer.ElevatedHelper.exe"
if (-not (Test-Path $HelperExe)) {
    throw "ElevatedHelper not found at $HelperExe"
}

Copy-Item $HelperExe (Join-Path $PublishApp "SpatialLabsOptimizer.ElevatedHelper.exe") -Force

$Version = (Get-Content "src/SpatialLabsOptimizer/product-version.json" | ConvertFrom-Json).version
$ZipName = "SpatialLabsOptimizer-$Version-win-x64.zip"
$ZipPath = Join-Path $OutputDir $ZipName
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path (Join-Path $PublishApp "*") -DestinationPath $ZipPath

Write-Host "Wrote $ZipPath"
Write-Host "Helper present: $(Test-Path (Join-Path $PublishApp 'SpatialLabsOptimizer.ElevatedHelper.exe'))"
