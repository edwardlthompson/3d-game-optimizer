# ADR-0002: PCVR Connector Scope

## Status

Proposed

## Context

v1.1 adds **Play in VR** for titles with native VR or UEVR compatibility. The product must delegate to the user's existing PCVR runtime without replacing SteamVR, Meta, or Oculus hubs.

## Decision

- Implement `PcvrRuntimeConnector` as a **read-only probe** for SteamVR/OpenXR installations.
- `PlayInVR` delegates launch to the detected runtime; no headset configuration UI in v1.1.
- Flat-screen `PlayIn3D` remains the primary path; VR is additive.
- Per-game `preferredOutput` override: Monitor | Headset | Auto (stored locally in SQLite).

## Consequences

- No dependency on Meta/Oculus SDKs in v1.1.
- Compatibility seed gains optional `vrCapability` and `steamVrLaunchOptions` fields.
- Manual QA required on SteamVR + one native VR title and one UEVR title before v1.1 tag.

## Boundaries (non-goals)

- Replacing SteamVR or installing PCVR runtimes silently.
- Configuring IPD, guardian, or headset-specific video settings.
- Transmitting user identity or library data to VR vendors.
