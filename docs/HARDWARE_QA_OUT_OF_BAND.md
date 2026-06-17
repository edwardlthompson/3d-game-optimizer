# Hardware QA — Out of Band (Optional)

> CI automation covers logical P0 scenarios. Physical validation is optional — **not required** to ship.

## Automated checks (run locally or in CI)

| Check | Command | What it covers |
|-------|---------|----------------|
| **All automatable QA** | `pwsh scripts/run-out-of-band-qa.ps1 -UserCache` | SteamDB policy, cover art CDN download, PCVR tests, optional UI startup |
| Cover art only | `pwsh scripts/smoke-cover-art.ps1 -UserCache` | Live Steam CDN + user cache writes |
| PCVR only | `pwsh scripts/smoke-pcvr-readiness.ps1` | Runtime probe + graceful-fail tests |
| SteamDB policy (CI) | `bash scripts/run-out-of-band-qa.sh` | ADR-0005 + no scraping in sync scripts |

Build staging first for UI startup smoke:

```powershell
pwsh scripts/build-product-local.ps1 -SkipGate
pwsh scripts/run-out-of-band-qa.ps1 -UserCache
```

Optional cover-art debug while testing the installed app:

```powershell
$env:SLO_COVER_ART_DEBUG = "1"
# launch SpatialLabsOptimizer.exe — log: %LOCALAPPDATA%\3d-game-optimizer\logs\debug-2ca1ae.log
```

## Manual only (cannot automate)

- GPU vendor matrix (NVIDIA / AMD / Intel)
- Physical Acer / Samsung display profile selection
- Play in 3D on a real installed Steam title
- Play in VR with headset + SteamVR/OpenXR running
- Keyboard-only setup wizard on real hardware

## Sign-off

| Field | Value |
|-------|-------|
| Version | |
| Date | |
| Tester | |
| `run-out-of-band-qa.ps1` | PASS / FAIL / skipped |

## GPU vendors tested

| Vendor | System | Pass |
|--------|--------|------|
| NVIDIA | | [ ] |
| AMD | | [ ] |
| Intel | | [ ] |

## P0 spot checks

- [ ] Supported Acer display — profile auto-selected
- [ ] Unknown display — generic profile offered
- [ ] Silent install on real toolchain
- [ ] Play in 3D launch on installed Steam title
- [ ] Keyboard-only setup wizard navigation

## Notes

_Add observations, driver versions, display models._
