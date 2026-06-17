# ADR-0003: Release and Distribution Policy

## Status

Accepted

## Context

The repository ships two release tracks: template bootstrap (`agent-project-bootstrap` lineage) and the WinUI product (`SpatialLabsOptimizer`).

## Decision

### Tag naming

| Track | Tag format | Version source | Workflow |
|-------|------------|----------------|----------|
| Template | `v{semver}` | `.template-version` | `release.yml` |
| Product | `SpatialLabsOptimizer-v{semver}` | `src/SpatialLabsOptimizer/product-version.json` | `product-release.yml` |

Release Please manages both packages via `release-please-config.json`.

### Distribution

- **Primary:** Self-contained `win-x64` zip attached to product GitHub Releases.
- **MSI:** Per-machine WiX v4 installer (`packaging/msi/Product.wxs`) harvested from published staging; attached to releases when build succeeds. Fixed `UpgradeCode` enables major upgrades.
- **Updates:** In-app checker pulls from GitHub Releases API (`UpdateService`); zip or MSI apply via `ZipUpdateApplier` / `MsiUpdateApplier`.
- **Local builds:** `scripts/build-product-local.ps1` / `docs/LOCAL_RELEASE.md` mirror CI publish, sign, and MSI steps.

### Signing

- Unsigned builds acceptable for internal beta.
- Public releases SHOULD use Authenticode when `CODESIGN_PFX_BASE64` and `CODESIGN_PASSWORD` secrets are configured in CI.

### QA

- P0 QA matrix scenarios enforced in CI via `scripts/check-qa-matrix-coverage.sh`.
- Physical GPU/display validation is optional and documented in `docs/HARDWARE_QA_OUT_OF_BAND.md` (non-blocking).

## Consequences

- Product and template releases never share the same tag.
- `pre-release-gate.sh` runs without `--skip-dotnet` before product publish.
