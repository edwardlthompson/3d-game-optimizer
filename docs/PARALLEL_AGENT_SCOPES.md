# Parallel Agent Scopes

> Isolated file scopes for BUILD_PLAN Parallel lane. No two agents may touch the same path prefix.

## Rules

1. One branch per agent: `feature/agent-<task-slug>`
2. Run `scripts/check-parallel-scope.sh` before dispatch
3. Shared types/schemas (`data/compatibility/`, catalog JSON schema): **Sequential agent only**
4. Never edit `BUILD_PLAN.md` from parallel agents (sequential owner)

## Product scopes (3D Game Optimizer)

| Task area | Owner | Isolated scope |
|-----------|-------|----------------|
| WinUI desktop | AGENT | `src/SpatialLabsOptimizer/**`, `src/SpatialLabsOptimizer.Core/**` |
| WinUI tests | AGENT | `src/SpatialLabsOptimizer.Tests/**` |
| Catalog browser | AGENT | `site/catalog/**` |
| Steam worker | AGENT | `workers/steam-library/**` |
| Catalog seed merge | AGENT | `scripts/sync-catalog/**`, `data/compatibility/**` |
| Elevated helper | AGENT | `src/SpatialLabsOptimizer.ElevatedHelper/**` |
| Packaging / MSI | AGENT | `packaging/msi/**`, `scripts/publish-product*.ps1` |
| Design tokens | AGENT | `design-tokens/**`, `scripts/sync-design-tokens.py` |
| Inactive stubs | AGENT | `examples/{android,python,lightroom,rust,go}/**` (do not overlap product paths) |

## Collision boundaries

| Shared resource | Rule |
|-----------------|------|
| `data/compatibility/catalog-v2.json` | Sequential only — one agent per merge |
| `site/catalog/public/data/catalog-v2.json` | Copied at build; edit source in `data/` |
| `workers/steam-library/wrangler.toml` | Sequential + `[HUMAN]` for KV id |
| `.github/workflows/*.yml` | Sequential unless single-workflow edit agreed |

## Inactive template examples

`examples/web/` is a **demo stub** co-deployed to Pages root. Product catalog work belongs in `site/catalog/`. Do not assign both to parallel agents in the same sprint.

## Collision response

If `check-parallel-scope.sh` fails, split the task or move one item back to Sequential lane.
