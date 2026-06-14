# Generate an ephemeral self-signed Authenticode cert for sideload/dev builds.
# Not publicly trusted — use for MSIX sideload and CI when CODESIGN_* secrets are unset.
param(
    [string]$OutputDir = "artifacts/sideload-codesign",
    [string]$Password = "sideload-dev",
    [int]$ValidDays = 365
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$cert = New-SelfSignedCertificate `
    -Subject "CN=SpatialLabsOptimizer Sideload" `
    -Type CodeSigningCert `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddDays($ValidDays)

$pfxPath = Join-Path $OutputDir "sideload-codesign.pfx"
$b64Path = Join-Path $OutputDir "sideload-codesign.b64.txt"

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password (ConvertTo-SecureString $Password -AsPlainText -Force) | Out-Null
[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath)) | Set-Content -Path $b64Path -Encoding utf8NoBOM

Write-Host "Wrote $pfxPath"
Write-Host "Wrote $b64Path (base64 for CODESIGN_PFX_BASE64 secret)"
Write-Host "Suggested password for CODESIGN_PASSWORD secret: $Password"
Write-Host "Thumbprint: $($cert.Thumbprint)"
