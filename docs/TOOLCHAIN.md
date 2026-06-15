# Toolchain Install Modes

> See also: [LEGAL.md](LEGAL.md) (third-party EULAs), [data/tools/tool-manifest-v1.json](../data/tools/tool-manifest-v1.json)

## Install modes (`installMode`)

| Mode | Behavior | Example tools |
|------|----------|---------------|
| `bundled` | Elevated helper installs from pinned local zip in `data/tools/` with SHA256 verify | ReShade, UEVR (minimal FOSS fixtures) |
| `download` | Elevated helper downloads from GitHub-allowlisted URL with SHA256 verify | Future community mirrors |
| `manual` | Wizard shows **Manual install required**; no silent download or bundled package | SpatialLabs Runtime, Odyssey Hub |

Entries with empty `downloadUrl` and empty `bundledPackage` are treated as **manual** even without an explicit `installMode` field (backward compatible).

## Vendor tools (manual-only)

Proprietary vendor installers do not ship silent/unattended flags in this FOSS project:

- **SpatialLabs Runtime Platform** — install via Acer/OEM channel; detected by registry key.
- **Odyssey Hub** — install via Samsung support; detected by `OdysseyHub.exe`.

The setup wizard checklist marks these with `!` (**Manual install required**). Silent install orchestrator skips them with an explicit progress message.

## FOSS bundled fixtures

`reshade-minimal.zip` and `uevr-minimal.zip` are minimal placeholder packages for automated install testing. Production users may replace fixtures with vendor-approved builds only when license permits — document any substitution in release notes.

## Security

- Helper accepts GitHub-allowlisted URLs only (Sprint 49).
- SHA256 is mandatory when `downloadUrl` or `bundledPackage` is present.
- Never broaden URL allowlist without ADR.
