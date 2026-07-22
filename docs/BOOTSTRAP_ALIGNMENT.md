# Bootstrap Alignment — Gap Analysis

> **Phase 0 deliverable** (2026-07-21). Migration / alignment of this live product repo with upstream [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) **v0.15.0**.
>
> This is **not** a fresh bootstrap. Preserve application code, history, and product-specific decisions.

## Snapshot

| Item | This repo | Upstream |
|------|-----------|----------|
| Template version | **0.15.0** (aligned 2026-07-21) | **0.15.0** (2026-07-22) |
| Product | **3D Game Optimizer v1.5.0** | N/A (template) |
| Mode | Reference + customized product | Template / bootstrap |
| License | MIT | MIT |

## Recommended stack selection (keep)

Already recorded in `.cursor/stack-selection.json`:

| Module | Path | Status |
|--------|------|--------|
| **winui** | `src/SpatialLabsOptimizer*` | Active (primary desktop) |
| **web** | `site/catalog/` | Active (GitHub Pages catalog) |
| **node** | `workers/steam-library/` | Active (Cloudflare Worker) |
| **python** | `scripts/sync-catalog/` | Active (catalog tooling) |
| android / go / rust / lightroom | stubs / inactive | Do **not** copy examples wholesale |

---

## What already matches

Core agent surface is largely present from the 0.7.1-era adoption:

- `AGENTS.md` router (`.cursorrules` already absent — good)
- `docs/START_HERE.md`, `CURSOR_MODES.md`, `FOR_AGENTS.md`, `INITIALIZATION_PROMPT.md`, `UPGRADING_FROM_TEMPLATE.md`
- Memory: `AGENT_MEMORY.md`, `DECISION_LOG.md`, `KNOWLEDGE_BASE.md`, `COMPLETED_TASKS.md`, `PROMPT_LIBRARY.md`
- Living `BUILD_PLAN.md` with AGENT/HUMAN/ADB/AUTO labels + Sequential / Parallel lanes
- Security: `SECURITY.md`, `docs/SECURITY_TRIAGE.md`, `docs/THREAT_MODEL.md`, `docs/PRIVACY.md`
- Template tracking: `.template-version`, `.template-update.json`, update-checker scripts
- Supporting: `.editorconfig`, `.gitattributes`, `.env.example`, `.pre-commit-config.yaml`, session-state example
- Product module: `modules/winui/MODULE.md` (+ web/node/python modules)
- Slash commands under `.cursor/commands/` (most of the set)
- Strong product CI: WinUI, catalog, Steam worker, CodeQL, Scorecard, Dependabot, Pages, product-release
- Many hygiene/gate scripts already customized for this product

---

## What is missing (vs v0.15.0)

### A. Cursor rules (6 missing of ~15 upstream)

| Missing rule | Why it matters |
|--------------|----------------|
| `cursor-modes.mdc` | Always-apply mode router (Ask/Plan/Agent/Debug) |
| `batch-commands.mdc` | Slash-command / batch registry enforcement |
| `local-compute.mdc` | Local-first compute (3.9–3.11 surfaces, v0.15.0) |
| `repo-hygiene.mdc` | Hygiene automation contract |
| `security-triage.mdc` | CVE triage agent rule |
| `commercial-compliance.mdc` | Optional; FOSS-only product may adopt as example/off |

### B. Batch command docs

- `docs/BATCH_COMMANDS.md` (agent registry)
- `docs/help/BATCH_COMMANDS.md` (human cheat sheet)
- `docs/help/` directory entirely absent
- `scripts/check-batch-commands.sh`
- `/cleanup` slash command (added upstream in 0.12.x)

### C. Cursor FOSS integrations (0.12+ / 0.14+ / 0.15)

- `.cursor/hooks.json` + `.cursor/hooks/*` (quiet shell, denylist, encoding, MCP audit)
- `.cursor/skills/*` (validate-bootstrap, watch-gates, hygiene, vertical-slice, …)
- `.cursor/agents/*` (explorer, gate-fixer, verifier)
- `.cursor/permissions.json`, feature radar docs/registry
- `docs/CURSOR_FEATURE_RADAR.md`, `docs/CURSOR_FEATURE_REGISTRY.json`
- Related scripts: `check-cursor-hooks.sh`, `check-cursor-integrations.sh`, `cursor-feature-radar.sh`, `sync-cursor-features.py`, `agent-run.py`

### D. Process / hygiene docs and files

- `docs/REPO_HYGIENE.md`, `docs/FILE_SIZE_GUIDE.md`
- `HUMAN_BACKLOG.md` (+ `.example`)
- `RELEASE_NOTES.md.example`
- `.cursorignore`
- Template version sync scripts (`check-template-version-sync.sh`, `sync-template-version.sh`)
- Parallel dispatch helpers (`plan-parallel-dispatch.sh`, `attempt-build-plan-row.sh`, …)
- Maintainer gates / purge / PS feature-gate variants (product already has `.sh` variants for several)

### E. Stale / conflict content (must migrate carefully)

| Conflict | Detail |
|----------|--------|
| `docs/START_HERE.md` | Still describes **the template repo**, not this product ("agent-project-bootstrap is a GitHub Template Repository") |
| `BUILD_PLAN` status glyph | Uses `⬜` open; upstream/official marker is `🔲` (also `✅` / `❌`) |
| `TEMPLATE_INDEX.json` | Locked at **0.7.1**; missing newer index entries |
| Template version | **0.7.1 → 0.15.0** spans 8 minors; cherry-pick required, not blind sync |
| File-limit taxonomy | Upstream 0.12+ added static-data 300L / pure-logic 150L; product has exemptions — merge carefully |
| Parallel-first tooling | Upstream 0.12+ autonomous `/build` + multi-agent dispatch; product AGENTS.md still Sequential-first (correct for this repo) |
| CI workflows | Product CI is WinUI/Steam/catalog-specific; do **not** replace with template `ci.yml` |
| `examples/` | Keep as reference stubs; product golden paths are `src/`, `site/catalog/`, `workers/` |
| Commercial hooks/MCP examples | Upstream has `.commercial.example` files — FOSS product should skip or keep as non-active examples only |

---

## Risk areas

| Risk | Level | Notes |
|------|-------|-------|
| Cursor hooks / shell denylist | **High** | Can block legitimate agent shell; needs local trial before always-on |
| CI workflow replacement | **High** | Would break WinUI/product-release/Steam/catalog gates |
| `validate-bootstrap.sh` hard upgrade | **Medium** | New checks may fail until docs/rules/scripts land |
| File-limit taxonomy change | **Medium** | May fail CI until exemptions/docs updated |
| Secrets / Steam Connect | **High (product)** | Already `[HUMAN]` — out of scope for template alignment |
| Bumping `.template-version` without content | **Medium** | Lie about alignment; only bump after adopted surfaces land |
| commercial-compliance alwaysApply | **Low–Med** | Prefer FOSS-only; skip or make non-alwaysApply |

---

### Critique

- **Null/empty:** Several "missing" scripts may be superseded by product-specific gates (`feature-gate.sh`, `watch-agent-gates.sh`); copy only if validate/check scripts expect them.
- **Timeouts / CI:** Do not enable new required checks on `main` without a green dry-run on a branch.
- **Race:** Hooks + parallel dispatch interact with existing `watch-agent-gates` — introduce hooks behind docs first, enable gradually.
- **Exceptions:** Preserve product ADRs, Steam worker secrets flow, and dual release tracks (`v*` template vs `SpatialLabsOptimizer-v*`).

---

## Prioritized alignment plan (Sequential)

> Execute only after human confirmation on **High-risk** items below.

### Sequential — AGENT (template alignment)

1. 🔲 [AGENT] Write this gap analysis (`docs/BOOTSTRAP_ALIGNMENT.md`) + append DECISION_LOG stub *(this step)*
2. 🔲 [AGENT] Phase 1a — Missing `.cursor/rules/*.mdc` (cursor-modes, batch-commands, local-compute, repo-hygiene, security-triage); skip or soft-add commercial
3. 🔲 [AGENT] Phase 1b — Batch command surface: `docs/BATCH_COMMANDS.md`, `docs/help/*`, `check-batch-commands.sh`, `/cleanup` command
4. 🔲 [AGENT] Phase 1c — Adapt `docs/START_HERE.md` for this product (Reference-first); keep router tables
5. 🔲 [AGENT] Phase 1d — `HUMAN_BACKLOG.md`, `.cursorignore`, `RELEASE_NOTES.md.example`; normalize BUILD_PLAN open glyph `⬜` → `🔲`
6. 🔲 [AGENT] Phase 1e — Refresh `TEMPLATE_INDEX.json` entries for new files; do **not** bump version yet
7. 🔲 [AGENT] Phase 2a — Cherry-pick safe scripts (hygiene/docs/radar/version-sync/dispatch) adapted for winui+web+node+python
8. 🔲 [AGENT] Phase 2b — Optionally stage Cursor hooks/skills/agents as **opt-in** (docs + examples); enable hooks only after local smoke
9. 🔲 [AGENT] Phase 2c — Align security/docs surfaces (`REPO_HYGIENE`, `FILE_SIZE_GUIDE`); Dependabot already present — review deltas only
10. 🔲 [AGENT] Phase 2d — Run `validate-bootstrap` / encoding / hygiene; fix failures in feature scope (3-strike)
11. 🔲 [AGENT] Phase 3 — Confirm modules mapping in AGENT_MEMORY; document golden-path map (no inactive example copy)
12. 🔲 [AGENT] Phase 4 — README "How agents should work" + Migration-notes section in this file; bump `.template-version` to **0.15.0** only when adopted set is coherent
13. 🔲 [AUTO] CI green on alignment branch before merge

### Human & device (after automation)

1. 🔲 [HUMAN] Approve high-risk items (hooks on/off; CI non-replacement; commercial rule skip)
2. 🔲 [HUMAN] Steam Connect (existing): KV namespace + Cloudflare/Steam secrets + post-deploy smoke
3. 🔲 [HUMAN] Hardware QA / screenshots (existing Parallel HUMAN backlog)
4. 🔲 [HUMAN] Review Scorecard pin + CodeQL SARIF strict gate (existing deferred)

### Parallel — Deferred (AGENT) — isolated scopes only

| Task | Scope |
|------|--------|
| Scorecard pin + permissions | `.github/workflows/**` |
| WinUI file-budget sweep | `src/SpatialLabsOptimizer/**` |
| Optional hooks enablement | `.cursor/hooks*` after smoke |

---

## High-risk confirmations needed before Phase 1+

Reply with approve/deny (or modify) for each:

1. **Cursor hooks** — Adopt FOSS hooks from upstream as opt-in first (`hooks.json` present but documented "enable after smoke"), **or** install enabled immediately?
2. **CI** — Confirm **no replacement** of product `ci.yml` / product-release / Steam / catalog workflows; only additive checks if needed?
3. **commercial-compliance.mdc** — Skip for this FOSS product?
4. **Template version bump** — Bump to 0.15.0 only at end of alignment, after gates pass?
5. **BUILD_PLAN glyph** — Normalize `⬜` → `🔲` for open items?

---

## Migration notes (executed 2026-07-21)

### Done (AGENT)

- Added Cursor rules: `cursor-modes`, `batch-commands`, `local-compute`, `repo-hygiene`, `security-triage` (skipped `commercial-compliance`)
- Batch surface: `docs/BATCH_COMMANDS.md`, `docs/help/*`, `check-batch-commands.sh`, `/cleanup`
- Product-aware `docs/START_HERE.md` (Reference-first for this repo)
- `HUMAN_BACKLOG.md`, `.cursorignore` (product paths), BUILD_PLAN glyphs 🔲/✅/❌
- Skills, agents, permissions, worktrees, feature radar docs; hooks **installed but disabled** via `<!-- cursor-hooks: off -->`
- Scripts: agent-run, parallel dispatch, template version sync, cursor integrations, maintainer gates, etc.
- `TEMPLATE_INDEX.json` refreshed (paths that exist only); `.template-version` → **0.15.0**
- README “How agents should work”; DECISION_LOG + AGENT_MEMORY updated
- Product CI workflows **not** replaced

### Still needs human attention

1. Steam Connect KV + secrets + post-deploy smoke (unchanged product blocker)
2. Optional: enable Cursor hooks — remove `<!-- cursor-hooks: off -->` from BUILD_PLAN after local smoke
3. Hardware QA / screenshots / CodeQL SARIF strict (existing Parallel HUMAN backlog)
4. Push / CI green on this alignment commit when ready (`[HUMAN]` / `[AUTO]`)

### Critique (post-exec)

- Null: commercial rule and commercial hook examples intentionally omitted
- Race: hooks present on disk but off-marker prevents enforcement until human enables
- CI: no required-check changes; additive scripts only
- File-limit taxonomy not force-migrated; existing product exemptions retained

## Upstream delta summary (0.7.1 → 0.15.0)

| Band | Themes to cherry-pick |
|------|------------------------|
| 0.8–0.9 | Repo hygiene, feature gates, human-gate automation |
| 0.10–0.11 | Batch commands, Cursor modes, template version sync, SBOM |
| 0.12 | Hooks/skills/subagents/radar, file-limit taxonomy, autonomous `/build`, `/cleanup` |
| 0.13–0.14 | Release-please automerge, quiet agent shell / agent-run |
| 0.15 | Local-first compute + Cursor 3.9–3.11 surfaces |

Reference: `docs/UPGRADING_FROM_TEMPLATE.md` cherry-pick table.
