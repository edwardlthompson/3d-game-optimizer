# ADR-0004: Epic and GOG Connector Scope

## Status

Accepted

## Context

v2.0 adds read-only library discovery from non-Steam stores without silent installers or proprietary SDKs.

## Decision

- `EpicGogLibraryScanner` probes local manifest/install folders only — no Epic Online Services or GOG Galaxy API keys.
- Scanned titles merge into compatibility seed by hash-derived placeholder IDs until manual mapping exists.
- No silent install or elevated helper paths for third-party stores.
- Workshop/preset import uses existing `PrivacyGuard` allowlist URLs only.

## Consequences

- Epic/GOG titles appear as experimental entries until community seed PRs map real app IDs.
- FOSS and privacy constraints preserved.
