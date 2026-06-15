# Platform Library Connections

> ADR: [0004-epic-gog-connector.md](adr/0004-epic-gog-connector.md)

## What ships today

| Platform | Local install scan | Online owned-games catalog | Credential storage |
|----------|-------------------|---------------------------|-------------------|
| Steam | VDF + optional Web API owned games | Web API when user saves key + Steam ID64 | DPAPI (`DpapiSecretStore`) |
| Epic | `.item` manifest folders | **Not shipped** — FOSS scope | N/A |
| GOG | `goggame-*.info` manifests | **Not shipped** — FOSS scope | N/A |
| Ubisoft | Ubisoft Connect install scan | **Not shipped** — FOSS scope | N/A |

## Why online Epic/GOG/Ubisoft sync is deferred

ADR-0004 limits v2 connectors to **local folder probes** without Epic Online Services, GOG Galaxy API keys, or Ubisoft proprietary SDKs. This preserves FOSS distribution and privacy posture.

Library Settings shows **Online catalog — coming soon** as an honest placeholder. Local install counts and path validation work today.

## Contributing mappings

Community seed PRs can map local scan hashes to compatibility entries. See [STEAM_INTEGRATION.md](STEAM_INTEGRATION.md) for Steam-specific flow.
