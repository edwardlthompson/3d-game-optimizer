# Agent Memory

> Centralized index of tech stack, threat models, persistent context, and retrospectives.
> Update only at session startups, milestone boundaries, or major architectural pivots.

## Tech Stack

| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Desktop | WinUI 3 + .NET 8 | 1.4.0 | `src/SpatialLabsOptimizer*` — glasses-free 3D PC gaming hub |
| Catalog site | Vite + TypeScript | 0.1.0 | `site/catalog/` — GitHub Pages at `/catalog/` |
| Steam sync | Cloudflare Worker | 0.1.0 | `workers/steam-library/` — OpenID + owned-games proxy |
| Data | SQLite + JSON seeds | catalog-v2 | `data/compatibility/` merged catalog |
| Template | agent-project-bootstrap | 0.7.1 | `.template-version` — agent rules, CI gates |
| License | MIT | — | Pure FOSS; GitHub Releases (zip + MSI) |

## Active Modules

- [x] WinUI 3 desktop (`modules/winui/MODULE.md` → `src/SpatialLabsOptimizer/`)
- [x] Web catalog (`site/catalog/`, `modules/web/MODULE.md`)
- [x] Node / Cloudflare Worker (`workers/steam-library/`, `modules/node/MODULE.md`)
- [x] Python sync tooling (`scripts/sync-catalog/`, `modules/python/MODULE.md`)
- [ ] Android / F-Droid (inactive stub — `examples/android/`)
- [ ] Lightroom Classic (inactive stub — `examples/lightroom/`)
- [ ] Rust (inactive stub — `examples/rust/`)
- [ ] Go (inactive stub — `examples/go/`)

## Threat Model Checklist

- [x] `docs/THREAT_MODEL.md` drafted (STRIDE, trust boundaries, top abuse cases)
- [x] No proprietary closed-source SDKs in production path
- [x] Opt-in only telemetry (GDPR/CCPA compliant); see `docs/PRIVACY.md`
- [x] Secrets excluded from VCS (Gitleaks pre-commit)
- [x] Dependency vulnerability scanning enabled (CodeQL + Trivy + Dependabot)
- [x] Input validation at all data boundaries (catalog, worker, Steam client)
- [x] `SECURITY.md` and private vulnerability reporting enabled
- [x] Steam sync tokens: fragment delivery, KV TTL, CORS allowlist (see `docs/STEAM_CATALOG_SYNC.md`)

## Persistent Context

### Project Purpose

**3D Game Optimizer** — one-click glasses-free 3D PC gaming: display detection, merged 3D compatibility catalog, silent toolchain setup, **Play in 3D** launch. Public [3D Game Catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) mirrors desktop discovery data.

### Key Constraints

- Max 250 lines per view file, 150 lines per logic file
- MVVM + Clean Architecture (ADR-0001); HTTP via `PrivacyGuard`
- Local-first; no cloud sync of library state
- Trunk-based development with Conventional Commits
- Dual release tracks: template `v*` + product `SpatialLabsOptimizer-v*`

### Golden Path Map

| Concern | Path |
|---------|------|
| Desktop app | `src/SpatialLabsOptimizer/`, `src/SpatialLabsOptimizer.Core/` |
| Tests | `src/SpatialLabsOptimizer.Tests/` (223+ tests) |
| Catalog browser | `site/catalog/` (42+ Vitest tests) |
| Steam worker | `workers/steam-library/` (35+ Vitest tests) |
| Seed data | `data/compatibility/catalog-v2.json` |
| Template demo | `examples/web/` (co-deployed to Pages root) |

## Session Retrospectives

| Date | Milestone | What worked | What to improve |
|------|-----------|-------------|-----------------|
| 2026-06-17 | v1.4.0 + template alignment | Connect Steam batch; product-release CI fixes | Mark inactive `examples/` stubs explicitly in memory |
| 2026-06-13 | v0.6.0 design system | Cross-stack tokens + i18n scaffold | Product uses `site/catalog` not `examples/web` as primary site |

## Template Provenance

- **Source template:** `edwardlthompson/agent-project-bootstrap`
- **Template version:** `0.7.1` (see `.template-version`)
- **Child-repo mode:** Reference + customized `INITIALIZATION_PROMPT.md` (WinUI product)
- **Last update check:** See `.template-update.json`
