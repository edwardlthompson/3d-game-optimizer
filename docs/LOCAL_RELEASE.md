# Local Product Release

Build, sign, and package **SpatialLabsOptimizer** on a Windows machine without waiting for GitHub Actions.

## Quick start

```bash
# Unsigned zip + MSI (no pre-release gate)
bash scripts/build-product-local.sh --skip-gate --skip-msix

# Full gate + signed zip, MSI, MSIX (when assets exist)
bash scripts/build-product-local.sh --sign
```

PowerShell equivalent:

```powershell
pwsh scripts/build-product-local.ps1 -SkipGate -Sign
```

## Prerequisites

Run the checker before your first build:

```powershell
pwsh scripts/check-local-release-prereqs.ps1 -RequireSign -RequireMsi
```

| Tool | Purpose |
|------|---------|
| .NET 8 SDK | Build and publish |
| PowerShell 7+ (`pwsh`) | Release scripts |
| Git Bash (`bash`) | Pre-release gate, winget stub |
| Windows SDK `signtool` | Authenticode signing (`-Sign`) |
| WiX v4 (via NuGet) | MSI packaging |

Validate script inventory:

```bash
bash scripts/check-local-release-scripts.sh
```

Automate optional post-sprint steps (validation, assets, local build, GitHub release):

```powershell
pwsh scripts/automate-optional-next-steps.ps1
pwsh scripts/automate-optional-next-steps.ps1 -DryRun   # preview only
pwsh scripts/publish-product-github-release.ps1          # release only
pwsh scripts/prepare-winget-submission.ps1 -OpenPr       # winget-pkgs PR
```

## Orchestrator flags

| Flag | Effect |
|------|--------|
| `--skip-gate` / `-SkipGate` | Skip `pre-release-gate.sh --product-release` |
| `--sign` / `-Sign` | Authenticode-sign exe, MSI, MSIX |
| `--skip-msix` / `-SkipMsix` | Skip MSIX build |
| `--skip-msi` / `-SkipMsi` | Skip MSI build |
| `--pfx-path` / `-PfxPath` | Path to `.pfx` (overrides env) |
| `--pfx-password` / `-PfxPassword` | PFX password |

## Signing credentials (priority order)

1. `-PfxPath` / `-PfxPassword` parameters
2. CI-compatible env vars: `CODESIGN_PFX_BASE64`, `CODESIGN_PASSWORD`
3. Local sideload cert: `artifacts/sideload-codesign/sideload-codesign.pfx` (from `scripts/generate-sideload-codesign.ps1`)
4. Ephemeral self-signed cert (AUTO — same as CI when secrets are unset)

Generate a reusable sideload cert:

```powershell
pwsh scripts/generate-sideload-codesign.ps1
```

## Individual scripts

| Script | Output |
|--------|--------|
| `publish-product.ps1` | `artifacts/product-win-x64/staging/app/`, zip |
| `sign-product-release.ps1` | Signed exes in staging and/or zip |
| `publish-product-msi.ps1` | `artifacts/product-msi/*.msi` |
| `sign-product-msi.ps1` | Signed MSI |
| `publish-product-msix.ps1` | `artifacts/product-msix/*.msix` |
| `verify-product-signatures.ps1` | Signature report |
| `generate-winget-manifest.sh` | `packaging/winget-product/manifest.stub.yaml` |

### Sign staging + zip (CI-compatible)

```powershell
pwsh scripts/sign-product-release.ps1 `
  -StagingDir artifacts/product-win-x64/staging/app `
  -ZipPath artifacts/product-win-x64/SpatialLabsOptimizer-1.0.1-win-x64.zip
```

Legacy CI path (zip only) still works:

```powershell
pwsh scripts/sign-product-release.ps1 -ZipPath artifacts/product-win-x64/SpatialLabsOptimizer-1.0.1-win-x64.zip
```

## Artifacts

| Path | Description |
|------|-------------|
| `artifacts/product-win-x64/*.zip` | Self-contained portable bundle |
| `artifacts/product-msi/*.msi` | Per-machine WiX installer |
| `artifacts/product-msix/*.msix` | Sideload MSIX (when assets present) |
| `packaging/winget-product/manifest.stub.yaml` | Winget stub (zip or MSI) |

## CI parity

`product-release.yml` uses the same scripts. Local builds should match CI output when using the same version and signing mode.

See also: [ADR-0003](adr/0003-release-and-distribution.md), [packaging/msi/README.md](../packaging/msi/README.md), [RUNBOOK.md](RUNBOOK.md).
