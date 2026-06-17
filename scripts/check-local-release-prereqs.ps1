# Verify local product release toolchain prerequisites.
param(
    [switch]$RequireSign,
    [switch]$RequireMsi
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$fail = 0

function Test-CommandPresent {
    param([string]$Name, [string]$Hint)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Host "FAIL missing $Name - $Hint"
        $script:fail = 1
        return $false
    }
    Write-Host "OK   $Name"
    return $true
}

Write-Host "=== check-local-release-prereqs ==="

Test-CommandPresent "dotnet" "Install .NET 8 SDK" | Out-Null
Test-CommandPresent "pwsh" "Install PowerShell 7+" | Out-Null

$bash = Get-Command bash -ErrorAction SilentlyContinue
if (-not $bash -and -not (Test-Path "C:\Program Files\Git\bin\bash.exe")) {
    Write-Host "FAIL missing bash - install Git for Windows for pre-release gate scripts"
    $fail = 1
} else {
    Write-Host "OK   bash"
}

if ($RequireSign) {
    $signtool = Get-Command signtool -ErrorAction SilentlyContinue
    if (-not $signtool) {
        $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
        if (Test-Path $kitsRoot) {
            $signtool = Get-ChildItem $kitsRoot -Directory -ErrorAction SilentlyContinue |
                Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName "x64\signtool.exe" } |
                Where-Object { Test-Path $_ } |
                Select-Object -First 1
        }
    }
    if ($signtool) {
        Write-Host "OK   signtool"
    } elseif ($IsWindows -or ($env:OS -match "Windows")) {
        Write-Host "OK   signing (PowerShell Set-AuthenticodeSignature fallback; signtool optional)"
    } else {
        Write-Host "FAIL missing signtool - Install Windows SDK Signing Tools"
        $fail = 1
    }
}

if ($RequireMsi) {
    if (-not (Test-Path "packaging/msi/Product.wixproj")) {
        Write-Host "FAIL packaging/msi/Product.wixproj missing"
        $fail = 1
    } else {
        Write-Host "OK   packaging/msi/Product.wixproj"
    }
}

if ($fail -ne 0) {
    exit 1
}

Write-Host "check-local-release-prereqs passed"
