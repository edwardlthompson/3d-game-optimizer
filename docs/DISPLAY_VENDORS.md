# Display Vendors and Profiles

## Supported Seeded Vendors (v1)

| Vendor | Primary Devices | Detection Strategy | Notes |
|---|---|---|---|
| Acer | SpatialLabs View / laptops | EDID signature + model pattern | preferred for glasses-free profile presets |
| Samsung | Odyssey 3D line | EDID signature + optional utility presence | may require vendor hub utility for full features |
| NVIDIA legacy | 3D Vision capable setups | GPU vendor + manual confirmation | legacy ecosystem, user validation required |
| Generic | unknown displays | manual profile creation | fallback path with safety defaults |

## Detection and Confidence

- **High confidence:** EDID signature match and known model alias.
- **Medium confidence:** model alias only, unresolved revision.
- **Low confidence:** unknown device, user-selected generic profile.

## Profile Safety Baseline

- Start with conservative depth/convergence defaults.
- Warn before applying high-intensity presets.
- Provide quick rollback to previous profile.
