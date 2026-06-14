# Run the WinUI app locally with v2 services enabled (Epic/GOG scan, workshop, LAN export).
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$env:SPATIALLABS_ENABLE_V2 = "true"
Write-Host "SPATIALLABS_ENABLE_V2=true — v2 DI services: EpicGogLibraryScanner, WorkshopPresetImporter, LanPartyExportService, SteamGridDbClient, ..."

dotnet run --project src/SpatialLabsOptimizer/SpatialLabsOptimizer.csproj -c $Configuration --launch-profile "SpatialLabsOptimizer (v2)"
