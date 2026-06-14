# Shared Authenticode credential resolution for product release scripts.
# Supports explicit -PfxPath/-PfxPassword, CI CODESIGN_* env vars, and sideload fallbacks.

function Resolve-CodeSignMaterial {
    param(
        [string]$Root,
        [string]$PfxPath = "",
        [string]$PfxPassword = "",
        [switch]$AllowEphemeralSideload
    )

    $password = $PfxPassword
    if ([string]::IsNullOrWhiteSpace($password)) {
        $password = $env:CODESIGN_PASSWORD
    }

    $pfx = $PfxPath
    $mode = "unsigned"
    $tempPfx = $false

    if (-not [string]::IsNullOrWhiteSpace($pfx)) {
        if (-not (Test-Path $pfx)) {
            throw "PFX not found: $pfx"
        }
        $mode = "local-pfx"
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:CODESIGN_PFX_BASE64)) {
        $baseTemp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } elseif ($env:TEMP) { $env:TEMP } else { "." }
        $pfx = Join-Path $baseTemp "codesign.pfx"
        [IO.File]::WriteAllBytes($pfx, [Convert]::FromBase64String($env:CODESIGN_PFX_BASE64))
        $mode = "ev"
        $tempPfx = $true
    }
    else {
        $localPfx = Join-Path $Root "artifacts/sideload-codesign/sideload-codesign.pfx"
        if (Test-Path $localPfx) {
            $pfx = $localPfx
            if ([string]::IsNullOrWhiteSpace($password)) {
                $password = "sideload-dev"
            }
            $mode = "sideload-local"
        }
        elseif ($AllowEphemeralSideload) {
            Write-Host "CODESIGN_* secrets absent — using ephemeral sideload self-signed cert (AUTO)"
            $password = "sideload-dev"
            $baseTemp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } elseif ($env:TEMP) { $env:TEMP } else { "." }
            $pfx = Join-Path $baseTemp "codesign-ephemeral.pfx"
            $cert = New-SelfSignedCertificate `
                -Subject "CN=SpatialLabsOptimizer CI Sideload" `
                -Type CodeSigningCert `
                -CertStoreLocation "Cert:\CurrentUser\My" `
                -NotAfter (Get-Date).AddDays(90)
            Export-PfxCertificate -Cert $cert -FilePath $pfx -Password (ConvertTo-SecureString $password -AsPlainText -Force) | Out-Null
            $mode = "sideload-auto"
            $tempPfx = $true
        }
        else {
            throw "No signing credentials available. Pass -PfxPath, set CODESIGN_* env vars, or run scripts/generate-sideload-codesign.ps1"
        }
    }

    if ([string]::IsNullOrWhiteSpace($password)) {
        throw "PFX password required (use -PfxPassword or CODESIGN_PASSWORD)"
    }

    return @{
        PfxPath  = $pfx
        Password = $password
        Mode     = $mode
        TempPfx  = $tempPfx
    }
}

function Get-SignToolPath {
    if (Get-Command signtool -ErrorAction SilentlyContinue) {
        return "signtool"
    }
    $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $kitsRoot) {
        $candidate = Get-ChildItem $kitsRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "x64\signtool.exe" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($candidate) { return $candidate }
    }
    return $null
}

function Invoke-AuthenticodeSign {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [hashtable]$Material
    )

    $signtool = Get-SignToolPath
    if ($signtool) {
        $args = @(
            "sign",
            "/f", $Material.PfxPath,
            "/p", $Material.Password,
            "/fd", "SHA256",
            $FilePath
        )

        if ($Material.Mode -eq "ev" -or $Material.Mode -eq "local-pfx") {
            $args = @(
                "sign",
                "/f", $Material.PfxPath,
                "/p", $Material.Password,
                "/fd", "SHA256",
                "/tr", "http://timestamp.digicert.com",
                "/td", "SHA256",
                $FilePath
            )
        }

        & $signtool @args
        if ($LASTEXITCODE -ne 0) {
            throw "signtool failed for $FilePath"
        }
        return
    }

    Write-Host "signtool not found — using Set-AuthenticodeSignature (PowerShell fallback)"
    $securePassword = ConvertTo-SecureString $Material.Password -AsPlainText -Force
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
        $Material.PfxPath, $Material.Password,
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
    $result = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert -HashAlgorithm SHA256
    if ($result.Status -ne "Valid" -and $result.Status -ne "UnknownError") {
        throw "Set-AuthenticodeSignature failed for ${FilePath}: $($result.Status)"
    }
}
