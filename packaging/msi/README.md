# MSI packaging (WiX v4)

Per-machine MSI for the self-contained `win-x64` product bundle.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- WiX Toolset v4 (restored automatically via `WixToolset.Sdk` in `Product.wixproj`)

Optional for signing:

- Windows SDK `signtool.exe`
- EV Authenticode PFX, or sideload cert from `scripts/generate-sideload-codesign.ps1`

## Layout

| File | Purpose |
|------|---------|
| `Product.wxs` | Package metadata, `UpgradeCode`, install directory |
| `Product.wixproj` | WiX v4 project; harvests `artifacts/product-win-x64/staging/app/` |
| `obj/` | Generated harvest fragments (gitignored) |

## UpgradeCode

`A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D` is fixed for the product lifetime. **Never change it** — doing so breaks major upgrades and uninstall tracking.

## Build

Publish the app first, then build the MSI:

```powershell
pwsh scripts/publish-product.ps1
pwsh scripts/publish-product-msi.ps1
```

Signed MSI:

```powershell
pwsh scripts/publish-product-msi.ps1 -Sign -PfxPath artifacts/sideload-codesign/sideload-codesign.pfx -PfxPassword sideload-dev
```

Or use the full local orchestrator:

```bash
bash scripts/build-product-local.sh --sign
```

Output: `artifacts/product-msi/SpatialLabsOptimizer-{version}-win-x64.msi`

## Harvest source

Files are harvested from `artifacts/product-win-x64/staging/app/` (same tree as the release zip). Run `publish-product.ps1` before MSI build so staging is current.
