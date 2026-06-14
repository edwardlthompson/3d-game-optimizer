# QA Matrix

## Test Dimensions

| Dimension | Variants |
|---|---|
| OS | Windows 11 (23H2+), Windows 10 (22H2) |
| GPU Vendor | NVIDIA, AMD, Intel |
| Display Vendor | Acer, Samsung, Generic |
| Runtime Mode | Offline, Online metadata refresh |
| Game Source | Steam-installed, seed-only manual selection |

## Core Scenarios

| Scenario | Expected Result | Priority |
|---|---|---|
| First launch with supported Acer display | recommended profile auto-selected | P0 |
| First launch with unknown display | generic safe profile offered | P0 |
| Apply preset then rollback | rollback restores prior values | P0 |
| Steam unavailable | app remains usable via local seed data | P1 |
| Tool silent install failure | classified error with next steps shown | P0 |
| Offline mode with existing cache | no blocking network dependency | P1 |

## Exit Criteria for v1

- All P0 scenarios pass on at least one system per GPU vendor.
- No critical crashes in setup flow.
- Accessibility smoke checks pass for keyboard-only navigation.
