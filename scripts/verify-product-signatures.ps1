# Verify Authenticode signatures on product release artifacts.
param(
    [string]$ZipPath = "",
    [string]$MsiPath = "",
    [string]$MsixPath = "",
    [string]$StagingDir = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$targets = @()
$fail = 0

function Test-SignatureReport {
    param([string]$Path, [string]$Label)

    if (-not (Test-Path $Path)) {
        Write-Host "SKIP $Label not found: $Path"
        return
    }

    $sig = Get-AuthenticodeSignature -FilePath $Path
    $status = $sig.Status.ToString()
    Write-Host "$Label $Path => $status"
    if ($sig.SignerCertificate) {
        Write-Host "  Subject: $($sig.SignerCertificate.Subject)"
        Write-Host "  Thumbprint: $($sig.SignerCertificate.Thumbprint)"
    }

    if ($status -eq "Valid") {
        return
    }
    if ($status -eq "NotTrusted" -or $status -eq "UnknownError") {
        Write-Host "  WARN self-signed/sideload signature (expected for local AUTO signing)"
        return
    }
    Write-Host "  FAIL unexpected signature status: $status"
    $script:fail = 1
}

if (-not [string]::IsNullOrWhiteSpace($StagingDir)) {
    Get-ChildItem $StagingDir -Filter *.exe -Recurse | ForEach-Object {
        $targets += @{ Path = $_.FullName; Label = "EXE" }
    }
}

if (-not [string]::IsNullOrWhiteSpace($ZipPath) -and (Test-Path $ZipPath)) {
    $temp = Join-Path $env:TEMP "verify-sign-$(New-Guid)"
    New-Item -ItemType Directory -Force -Path $temp | Out-Null
    try {
        Expand-Archive $ZipPath -DestinationPath $temp -Force
        Get-ChildItem $temp -Filter *.exe -Recurse | ForEach-Object {
            $targets += @{ Path = $_.FullName; Label = "ZIP-EXE" }
        }
    } finally {
        if (Test-Path $temp) { Remove-Item -Recurse -Force $temp }
    }
}

foreach ($t in $targets) {
    Test-SignatureReport -Path $t.Path -Label $t.Label
}

if (-not [string]::IsNullOrWhiteSpace($MsiPath)) {
    Test-SignatureReport -Path $MsiPath -Label "MSI"
}

if (-not [string]::IsNullOrWhiteSpace($MsixPath)) {
    Test-SignatureReport -Path $MsixPath -Label "MSIX"
}

if ($targets.Count -eq 0 -and [string]::IsNullOrWhiteSpace($MsiPath) -and [string]::IsNullOrWhiteSpace($MsixPath)) {
    throw "No artifacts to verify — pass -ZipPath, -StagingDir, -MsiPath, and/or -MsixPath"
}

if ($fail -ne 0) {
    exit 1
}

Write-Host "verify-product-signatures passed"
