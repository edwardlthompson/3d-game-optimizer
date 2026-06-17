# Changelog

All notable changes to **3D Game Optimizer** (SpatialLabs Optimizer) are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Product releases use tags `SpatialLabsOptimizer-v*`. Template bootstrap history is retained below.

## [0.8.0](https://github.com/edwardlthompson/3d-game-optimizer/compare/v0.7.1...v0.8.0) (2026-06-17)


### Added

* 3D Rank column, filter popover polish, drop 3D level ([2cd7a25](https://github.com/edwardlthompson/3d-game-optimizer/commit/2cd7a257f001ffddc395cb00d756515e27c040de))
* automate BUILD_PLAN gates and close maintenance backlog ([c6b3b41](https://github.com/edwardlthompson/3d-game-optimizer/commit/c6b3b41bbc8180e79a2f42f080692210e8c79747))
* bootstrap 3D Game Optimizer WinUI foundation through v1.0 scaffold ([7dbb819](https://github.com/edwardlthompson/3d-game-optimizer/commit/7dbb819ea98d8bb5a84344ed7d55ce0ee1dd1dcb))
* catalog UX v2 with spreadsheet filters, wishlist PWA, and price history ([865d739](https://github.com/edwardlthompson/3d-game-optimizer/commit/865d739ee6ee103131a6caf3dafbbcc15cf364e7))
* complete living 3D catalog Phases 3-6 and settings toolchain UX ([d1a96a8](https://github.com/edwardlthompson/3d-game-optimizer/commit/d1a96a8cdf86340f71bead602bcdb7d628546715))
* Game Rank blends Steam popularity with 3D Rank ([f392914](https://github.com/edwardlthompson/3d-game-optimizer/commit/f3929148b845bbfaca685fbddee90d37194fe12e))
* library checklist, list filters, and weighted review rank ([2242d20](https://github.com/edwardlthompson/3d-game-optimizer/commit/2242d2006ecf574421f38377b8083c75f757f710))
* publish 3D Game Catalog on GitHub Pages with catalog-v2 data ([ba21787](https://github.com/edwardlthompson/3d-game-optimizer/commit/ba21787e24e31b9b8ec82b484666383d53ae8ef6))
* ship 686-title lenticular catalog with TanStack grid and Steam links ([a3691c2](https://github.com/edwardlthompson/3d-game-optimizer/commit/a3691c22f0599d49d022d3f84d23e52fb2f72fc9))
* ship Sprints 32-52 and 40-44 for v1.1.0 ([#2](https://github.com/edwardlthompson/3d-game-optimizer/issues/2)) ([1cd32fd](https://github.com/edwardlthompson/3d-game-optimizer/commit/1cd32fdcd749928c99653527d38050f5c9e4e364))
* ship v1.1.0 with local release pipeline and Sprint 39 polish ([a4131c8](https://github.com/edwardlthompson/3d-game-optimizer/commit/a4131c8da8d559d7612b28063093e70464e7bfa1))
* Steam library sync and title links to Steam store ([fa4eec0](https://github.com/edwardlthompson/3d-game-optimizer/commit/fa4eec0440293ef90c65a1ebbfb4adc397d1bdce))
* Steam Rank filter, 3D score filters, collapsible footer ([159543c](https://github.com/edwardlthompson/3d-game-optimizer/commit/159543cbac0402001ce73d45c4214e3af3968217))


### Fixed

* auto-size catalog columns per page and use Steam app link only ([e64ab16](https://github.com/edwardlthompson/3d-game-optimizer/commit/e64ab165c2a6a1f9ffef418b671352d2835232f2))
* catalog table wrap, drop redundant columns, buy links in new tab ([f8d93ed](https://github.com/edwardlthompson/3d-game-optimizer/commit/f8d93edf3f8256c0066411ca78812633ac5aff17))
* **ci:** allow ExternalDataGateway in privacy-audit HttpClient check ([f944a56](https://github.com/edwardlthompson/3d-game-optimizer/commit/f944a56528438bcb90b23cff7d862d56d941e7b7))
* **ci:** bootstrap owner-label grep and preset cache test race ([8df87ed](https://github.com/edwardlthompson/3d-game-optimizer/commit/8df87ed13b693d579999b57c7d9c16f008353b0c))
* **ci:** cap WinUI test hang and prefer Git Bash in setup script ([7cb7882](https://github.com/edwardlthompson/3d-game-optimizer/commit/7cb7882e387981f51c4979355dad522daea008f2))
* **ci:** extract Core library so unit tests skip WinUI bootstrap ([425dc3b](https://github.com/edwardlthompson/3d-game-optimizer/commit/425dc3bc069e302abd96cbe4c1df23475f4a569c))
* **ci:** grant Scorecard workflow read permissions ([b3aae1c](https://github.com/edwardlthompson/3d-game-optimizer/commit/b3aae1cb5a1900644a16c2ce2ee76841b80ea89d))
* **ci:** product-release gate build step and dependabot skip ([7ba5fa3](https://github.com/edwardlthompson/3d-game-optimizer/commit/7ba5fa38f60e4ef5b15357c31ff74508883a3658))
* **ci:** product-release wait-for-ci, pwsh publish, and zip path ([d0c1583](https://github.com/edwardlthompson/3d-game-optimizer/commit/d0c1583c047885310845457fd4a3aa5adb8eeca2))
* **ci:** remove invalid secrets permission from release-credentials workflow ([e1e819b](https://github.com/edwardlthompson/3d-game-optimizer/commit/e1e819bbe0c4451742b68cc9dc31508d56df55a9))
* **ci:** repair workflows, sync npm lockfiles, file limit exemptions ([7ee6707](https://github.com/edwardlthompson/3d-game-optimizer/commit/7ee6707187d25b21c6a95e8cce6066a4f3cf2821))
* **ci:** resolve workflow failures on main after v1.1.0 ship ([fb9988c](https://github.com/edwardlthompson/3d-game-optimizer/commit/fb9988cc5ef54fb91c81564b8e117285146fb717))
* **ci:** skip worker deploy without secrets; resolve npm audit alerts ([672eb8f](https://github.com/edwardlthompson/3d-game-optimizer/commit/672eb8fa3e2e27e4f4b64ac7d540a01a2f61a28b))
* **ci:** use quick build-verification gate with GH_TOKEN ([3028a95](https://github.com/edwardlthompson/3d-game-optimizer/commit/3028a9593505187b1c8cf2610a47fbaa5ae57ad7))
* default catalog sort to Game Rank descending ([dd3d872](https://github.com/edwardlthompson/3d-game-optimizer/commit/dd3d87218d5605c121595baf17c8e513998c3279))
* initialize TanStack columnPinning so catalog grid loads ([6e0c08f](https://github.com/edwardlthompson/3d-game-optimizer/commit/6e0c08f88d1f06d99ea7338b0cade1fa46ba7753))
* narrow catalog app root element type for strict TypeScript ([4c630e8](https://github.com/edwardlthompson/3d-game-optimizer/commit/4c630e8e8222ee31b096be44fd05848da07054f4))
* resolve catalog site TypeScript build errors for GitHub Pages ([5e6c77a](https://github.com/edwardlthompson/3d-game-optimizer/commit/5e6c77ab09cd5c6870f038a6f63917f73fec1541))
* unique snapshot filenames to prevent CI test collision ([f633eb1](https://github.com/edwardlthompson/3d-game-optimizer/commit/f633eb17ab45ce8c1c9efc7585ab0a08d9d30c35))
* wrap long game titles with overflow-wrap and column width ([058059c](https://github.com/edwardlthompson/3d-game-optimizer/commit/058059cc0b3ce042a1106579f4cdb7309f7bc3f3))


### Changed

* **assets:** regenerate README UI previews ([6ab5bc9](https://github.com/edwardlthompson/3d-game-optimizer/commit/6ab5bc958a2ad9ec881d74c4a3d86971d81237f5))
* **assets:** regenerate README UI previews ([71d77b8](https://github.com/edwardlthompson/3d-game-optimizer/commit/71d77b8f90722307a0c1e860914a3f73bfcd8d9f))
* **assets:** regenerate README UI previews ([02d5c64](https://github.com/edwardlthompson/3d-game-optimizer/commit/02d5c6485f6856884441f6f33c4eab5d89b6edba))
* **assets:** regenerate README UI previews ([8445331](https://github.com/edwardlthompson/3d-game-optimizer/commit/8445331efedc216c602c1bbd6e419feb14f5b581))


### Documentation

* link WinGet v1.1.0 PR [#388074](https://github.com/edwardlthompson/3d-game-optimizer/issues/388074) ([2c03a7a](https://github.com/edwardlthompson/3d-game-optimizer/commit/2c03a7a05a84e0c2ce64ab4fe8e7a249de89e4df))
* mark catalog layout polish shipped in BUILD_PLAN ([93c3ffe](https://github.com/edwardlthompson/3d-game-optimizer/commit/93c3ffe267169e8a7496d375d3befff561faa585))
* mark catalog UX v2 ship complete in BUILD_PLAN ([c8dbcba](https://github.com/edwardlthompson/3d-game-optimizer/commit/c8dbcba4a1e91c6bcc17c9e28ac58a1e0c236429))
* mark lenticular catalog deploy complete in BUILD_PLAN ([631d3af](https://github.com/edwardlthompson/3d-game-optimizer/commit/631d3af489a834ded65dfb48f19f33133ede3d22))
* mark Sprint 39 release gate complete in BUILD_PLAN ([720b6dd](https://github.com/edwardlthompson/3d-game-optimizer/commit/720b6ddd7a070ac421cbf5ee6c647c22e91664ec))
* trim BUILD_PLAN to active follow-ups after v1.1.0 ship ([5c5df3b](https://github.com/edwardlthompson/3d-game-optimizer/commit/5c5df3ba03d4198083b11eb1128a4b484c097880))
* update BUILD_PLAN after Sprint 39 ship gate merge ([8d61bd8](https://github.com/edwardlthompson/3d-game-optimizer/commit/8d61bd800428ed56b46d0b30e1ab6451782bb257))

## [1.3.0] - 2026-06-17

### Removed

- MSIX sideload packaging and Windows Store manifest (`Package.appxmanifest`, `publish-product-msix.ps1`)
- In-app MSIX update applier and install-type override
- WinGet manifest pipeline (`packaging/winget*`, `generate-winget-*`, `prepare-winget-submission.ps1`)

### Changed

- Distribution policy: GitHub Releases only (portable zip + WiX MSI); in-app updates unchanged
- Splash branding uses `SplashLogo.png` instead of Store tile assets

### Added

- Golden rank fixtures (`data/rank-golden/fixtures.json`) with C# and catalog Vitest parity tests
- Catalog import validation and integrity fail-closed behavior
- Game Rank sort metadata deduplication; Game Rank tooltip on library tiles
- External download cap (20 MB); parallel cover art hydration

### Fixed

- Cover refresh toolbar action routes through prefetch pipeline
- `SteamCoverArtPolicy.IsEligible(null)` returns false
- Epic/GOG/Ubisoft cover URLs no longer hit Steam CDN

## [1.2.0] - 2026-06-16

### Added

- **Game Rank** library sort — 72% weighted Steam popularity + 28% best 3D path (matches catalog site)
- **Min 3D quality** filter (Any / Experimental 26+ through Ultra 88+)
- `CatalogGameRankScorer`, `CatalogSteamRanking`, `CatalogRank3DScorer` — C# ports of catalog ranking logic
- Game detail popup on library thumbnail click (`GameDetailDialog`)
- Resizable recent launches panel with drag grip
- Cover art on-demand resolve and disk cache sync (`CoverArtCacheSync`, `SteamCoverArtPolicy`)
- Auto preset prefetch during library indexing (`LibraryPrefetchService.Presets`)
- Last navigation screen persistence across sessions
- Cover art and game rank unit/smoke tests

### Changed

- Condensed library toolbar (removed inline selected-game panel, playlist/LAN/hybrid clutter from main view)
- Default library sort is **Game Rank** (descending)
- Cover thumbnails use `Uniform` stretch (letterbox, no crop)
- README updated for v1.2.0 features and roadmap

### Fixed

- Cover art prefetch HTTP handler missing `InnerHandler` (`PrivacyGuardHttpHandler`)
- Library re-sort falling back to compatibility tier instead of rank scores
- Recent launches panel cropping multi-line entries (taller default + resize)

## [1.1.0] - 2026-06-15

### Added

- Local signed release pipeline (zip, MSI, MSIX)
- Local game folder watch list
- In-app updates (About → Check now / Update and restart)
- PCVR launch path and command palette
- Diagnostic bundle export and `3dgo://` protocol handler

---

## Template bootstrap history

## [0.7.1](https://github.com/edwardlthompson/agent-project-bootstrap/compare/v0.7.0...v0.7.1) (2026-06-13)


### Fixed

* **android:** bump compileSdk to 37 for androidx 1.19 dependencies ([3a74f0c](https://github.com/edwardlthompson/agent-project-bootstrap/commit/3a74f0c08d32aff7a14d6fb7eff829578cbe73da))
* **android:** migrate Golden Path example to AGP 9 built-in Kotlin ([a84a16c](https://github.com/edwardlthompson/agent-project-bootstrap/commit/a84a16c8fa7e67d3179b820962aead03add7be34))
* **android:** rewrite Gradle Kotlin DSL files as UTF-8 ([11bc782](https://github.com/edwardlthompson/agent-project-bootstrap/commit/11bc78233b804ccaaca318f630f9eb1501cf10b8))
* **ci:** gofmt Go example and harden PR coverage comment parsing ([0423990](https://github.com/edwardlthompson/agent-project-bootstrap/commit/0423990b4ece9559a42db6c6bc66cec0eab518c8))
* **ci:** remove job-level hashFiles guards from ci.yml ([36fdbc1](https://github.com/edwardlthompson/agent-project-bootstrap/commit/36fdbc118f25e5865fb18aee653728ee87613f09))
* **ci:** repair corrupted template literals in coverage comment job ([a64ad04](https://github.com/edwardlthompson/agent-project-bootstrap/commit/a64ad04b1fd5ec301110925d8783ed8a1653a6ab))


### Changed

* **deps:** Bump the github-actions group across 1 directory with 12 updates ([c70ce00](https://github.com/edwardlthompson/agent-project-bootstrap/commit/c70ce00d5d2bd89525aa928ef279ffe66d82e720))
* **deps:** Bump the github-actions group across 1 directory with 12 updates ([c242785](https://github.com/edwardlthompson/agent-project-bootstrap/commit/c2427853e0aec0c0680b68beb0497cbed4da7e8c))
* **deps:** Bump the node-dependencies group in /examples/node with 2 updates ([42b87eb](https://github.com/edwardlthompson/agent-project-bootstrap/commit/42b87eb548942b4197fe67194698d1eaf49028e4))
* sync v0.7.0 and complete BUILD_PLAN automation pass ([369e17e](https://github.com/edwardlthompson/agent-project-bootstrap/commit/369e17ea198e671a4e0cec5943282e6f5e1f2786))

## [0.7.0](https://github.com/edwardlthompson/agent-project-bootstrap/compare/v0.6.0...v0.7.0) (2026-06-13)


### Added

* design system, web layout docs, and Golden Path UI refresh ([912ebbe](https://github.com/edwardlthompson/agent-project-bootstrap/commit/912ebbe2c57fb2d47223c49b3332f3037cc9c80f))
* initial agent-project-bootstrap template v0.1.0 ([d71c23c](https://github.com/edwardlthompson/agent-project-bootstrap/commit/d71c23c22dd97b96f3ef91435319d5df04bb28b6))
* template v0.2.0 with UTF-8 gates, lockfiles, and build verification ([2317440](https://github.com/edwardlthompson/agent-project-bootstrap/commit/2317440eeecec0ef961bb9cf54ea3830c183d8cf))


### Fixed

* add gradle.properties with android.useAndroidX for assembleDebug ([ea87f7d](https://github.com/edwardlthompson/agent-project-bootstrap/commit/ea87f7dfd40500a7550759fb4466418de5ed2aae))
* Android CI ΓÇö google maven for AGP and FOSS grep comment false positive ([b907c46](https://github.com/edwardlthompson/agent-project-bootstrap/commit/b907c46628f05275ecefb149bc230ea08ac47845))
* bind vite preview to 127.0.0.1 for Playwright CI and extend pre-commit encoding ([9a3f935](https://github.com/edwardlthompson/agent-project-bootstrap/commit/9a3f9357ab4ca2d2afcdf8250ce955a6f46e9c60))
* **ci:** repair workflow action SHAs, e2e selectors, and shell line endings ([38ce003](https://github.com/edwardlthompson/agent-project-bootstrap/commit/38ce003771c04cdc043ffc79bf8ee2296fac19aa))
* **ci:** use full git history for Gitleaks on Release Please PRs ([01d585b](https://github.com/edwardlthompson/agent-project-bootstrap/commit/01d585bcc1183633139cf46d4ca4932c3dbd8ee3))
* correct gh api calls in validate-workflow-actions.sh ([100df56](https://github.com/edwardlthompson/agent-project-bootstrap/commit/100df5649aee5724b22a70c84742453a106f1f1a))
* **deps:** override transitive tmp and uuid for @lhci/cli alerts ([222fb59](https://github.com/edwardlthompson/agent-project-bootstrap/commit/222fb59f5aa2d11b66c79496c23ad0980108b620))
* extend encoding scan to Android kts/kt/xml/properties ([8f52c55](https://github.com/edwardlthompson/agent-project-bootstrap/commit/8f52c550f52231520c76dd0a12326c0838db07e4))
* improve CI configs for python packaging and vitest ([36fbb39](https://github.com/edwardlthompson/agent-project-bootstrap/commit/36fbb394c5c1aa414c812334aa8628abfc47f178))
* normalize Android example UTF-16 files for Gradle CI ([39a78d3](https://github.com/edwardlthompson/agent-project-bootstrap/commit/39a78d36844739a9b5b77cb1f9876feb229accca))
* normalize UTF-16 index.html and extend encoding scan to html/css ([e16272e](https://github.com/edwardlthompson/agent-project-bootstrap/commit/e16272eded9901380c086dad4da6189e548209f4))
* playwright preview server host and timeout for CI e2e ([99188fa](https://github.com/edwardlthompson/agent-project-bootstrap/commit/99188faff52638196366aa8f967cfa832900e37e))
* repair Security Scan workflow and add GH CI automation ([80f9fc0](https://github.com/edwardlthompson/agent-project-bootstrap/commit/80f9fc03a5ada9ef795b39111d2de6b508a982c6))
* resolve CI failures in web lint, python format, and android grep ([444ad7b](https://github.com/edwardlthompson/agent-project-bootstrap/commit/444ad7b4c713257bb749371c7c9882b0e883bd19))
* stabilize Lighthouse CI with 3-run median ([f41c48d](https://github.com/edwardlthompson/agent-project-bootstrap/commit/f41c48daff6d8a148d86ba5cfabf800d015c806d))


### Changed

* **deps:** Bump the github-actions group across 1 directory with 10 updates ([#3](https://github.com/edwardlthompson/agent-project-bootstrap/issues/3)) ([648f5d2](https://github.com/edwardlthompson/agent-project-bootstrap/commit/648f5d202b1b2820e10b59c680b6980109290f77))
* release v0.2.1 full bootstrap hardening ([a2749a3](https://github.com/edwardlthompson/agent-project-bootstrap/commit/a2749a30cfac1b3bf3f2d450666592453ca3aca2))


### Documentation

* harden initialization prompt against CI failure patterns ([ec5ee91](https://github.com/edwardlthompson/agent-project-bootstrap/commit/ec5ee914332e7697e6168676c88eaca341cff643))
* mark v0.2.1 About and release human tasks complete ([33dc47f](https://github.com/edwardlthompson/agent-project-bootstrap/commit/33dc47fa934728a4b1b0247e14af95e95d031db0))

## [0.6.0] - 2026-06-12

### Added

- Cross-stack design system: `design-tokens/design-tokens.json` + `scripts/sync-design-tokens.py`
- Android Jetpack Compose Material 3 Golden Path with system/light/dark theme toggle (DataStore)
- Web CSS design tokens, theme toggle (`theme.ts`), and i18n scaffold (`locales/`, `t()`)
- `docs/DESIGN_GUIDE.md` and `.cursor/rules/design-system.mdc`
- `scripts/check-design-cohesion.sh` / `.ps1` wired into `validate-bootstrap.sh`
- Sprint M4 in BUILD_PLAN.md (template maintainer v0.6.0)

### Changed

- `examples/android/` migrated from XML Views to Compose M3
- `examples/web/` uses CSS variables, logical properties, `prefers-reduced-motion`
- `modules/android/MODULE.md` and `modules/web/MODULE.md` design system sections
- `docs/START_HERE.md`, `docs/FOR_AGENTS.md` read order includes DESIGN_GUIDE
- `.template-version` bumped to `0.6.0`

## [0.5.0] - 2026-06-13





### Added





- `examples/lightroom/` stub (`Info.lua`, README with SDK version table) per `modules/lightroom/MODULE.md`


- Optional `modules/rust/MODULE.md` + `examples/rust/` hello stub (Cargo.toml, clippy/fmt/test CI)


- Optional `modules/go/MODULE.md` + `examples/go/` hello stub (vet/fmt/test CI)


- `docs/OPTIONAL_STACKS.md` ΓÇö Rust/Go/Lightroom/Node opt-in outside default init stack picker


- CI `stack-filters` job; `lightroom`, `node`, `rust`, `go` jobs gated on directory existence and path changes


- F-Droid submission dry-run checklist in `modules/android/MODULE.md` (`[ADB]`)





### Changed





- `TEMPLATE_INDEX.json` ΓÇö `modules.lightroom.example` ΓåÆ `examples/lightroom/`; added `rust` and `go` modules


- `.template-version` ΓåÆ `0.5.0`


- README Supported Stacks table includes Lightroom example path and optional stacks note





## [0.2.2] - 2026-06-13





### Added





- `scripts/setup-github-repo.sh` and `scripts/setup-github-repo.ps1` - idempotent gh api setup for Dependabot alerts, private vulnerability reporting, branch protection


- `scripts/pre-release-gate.sh` and `scripts/pre-release-gate.ps1` - CI poll, Dependabot Critical/High count, template version check, release workflow reminder


- `scripts/check-file-encoding.py` - cross-platform UTF-8/UTF-16 BOM check; `check-file-encoding.sh` delegates to Python


- `.cursor/rules/windows-encoding.mdc` - Python UTF-8 write guidance for Windows


- Gitleaks CI job in `.github/workflows/security.yml` (SHA-pinned `gitleaks/gitleaks-action@v3.0.0`)


- Pre-commit hooks: `file-limits`, `validate-bootstrap --quick`


- KNOWLEDGE_BASE KB-007 npm/pip overrides policy; DECISION_LOG entry for `@lhci/cli` overrides


- PROMPT_LIBRARY entries 10 (pre-release gate) and 11 (GitHub repo setup)





### Changed





- `scripts/validate-bootstrap.sh` - `--quick` flag skips `validate-workflow-actions`


- `.github/workflows/health-check.yml` - `npm audit` for `examples/web`, `uv pip audit` for `examples/python`


- `docs/SECURITY_TRIAGE.md` - documents `setup-github-repo.sh` in Setup section


- `init-project` scripts remind to run `setup-github-repo` after init


- `AGENT_MEMORY.md` template version synced to `0.2.2`; em-dash corruption fixed to ASCII hyphen


- README CI gate section mentions `setup-github-repo` and `pre-release-gate`





## [Unreleased]

### Changed

- README (M5.1): hero badges, table of contents, GitHub alert callouts, collapsible detail sections, audience dividers
- README (M5): shields.io badges + HTML definition lists/tables for What's Included, BUILD_PLAN Labels, Template Update Checker, and Supported Stacks
- `scripts/normalize-markdown-whitespace.py`: table-aware blank-line collapse
- `scripts/check-markdown-tables.sh`: fail on broken GFM table rows; wired into `validate-bootstrap.sh`
- `docs/MAINTAINING_THE_TEMPLATE.md`: README badge conventions and hero/TOC sync notes

## [0.3.0] - 2026-06-13





### Added





- Stack picker `web/python/android/multi/none` in `init-project` scripts; `none` keeps all examples


- `scripts/init-stack-sync.py` - sync `AGENT_MEMORY.md` checkboxes and `.cursor/stack-selection.json`


- `.cursor-session-state.example.json` - session restore schema


- `docs/adr/0001-core-architecture.md` - MVVM / Clean / Hexagonal choice template for child repos


- `android-release` CI job with `SOURCE_DATE_EPOCH=1700000000` and APK hash flake check


- Semantic PR title job (`amannn/action-semantic-pull-request`, SHA-pinned)


- `scripts/check-bundle-size.sh` - Vite dist JS gzip budget (200 KB)


- Playwright visual snapshot and service-worker offline e2e tests


- Optional `pyright` CI job for Python example


- `.cursor/rules/testing.mdc` and `.cursor/rules/ci-gates.mdc`


- `docs/PARALLEL_AGENT_SCOPES.md` and `scripts/check-parallel-scope.sh`


- PROMPT_LIBRARY entries 12-14


- `.github/workflows/scorecard.yml` - weekly OpenSSF Scorecard (SHA-pinned)


- Android Fastlane `short_description.txt` stub and emulator checklist in README





### Changed





- `docs/FOR_AGENTS.md` - failure playbook (CI poll, GH_TOKEN, Dependabot, 3-strike, parallel scope)


- Python CI enforces `pytest --cov-fail-under=90` explicitly


- `.template-version` bumped to `0.3.0`; TEMPLATE_INDEX and README updated





## [0.2.1] - 2026-06-13





### Added





- `scripts/check-workflow-action-ref-format.sh` ΓÇö local pre-commit guard against bare-semver action refs


- `.github/workflows/health-check.yml` ΓÇö weekly Monday 07:00 UTC poll of CI + Security Scan + CodeQL on main


- CI `android-build` job ΓÇö `./gradlew assembleDebug` smoke for `examples/android/`


- Gradle wrapper binaries (`gradlew`, `gradlew.bat`, `gradle-wrapper.jar`) in `examples/android/`


- `KNOWLEDGE_BASE.md` ΓÇö six structured entries from v0.2.0 CI/security fix round


- `PROMPT_LIBRARY.md` entries 8ΓÇô9 ΓÇö workflow action validation and post-push GitHub gate


- Devcontainer `github-cli` feature; postStart runs encoding check + CI gate reminder


- README GitHub CI Gate section; init scripts run `validate-workflow-actions` and remind `check-github-ci`





### Changed





- Normalized root `.gitignore` from UTF-16 to UTF-8; added to encoding scan and pre-commit hook


- SHA-pinned `release.yml` actions: `anchore/sbom-action`, `softprops/action-gh-release`, `actions/attest-build-provenance`


- `docs/SECURITY_TRIAGE.md` ΓÇö GitHub Actions pin policy, health-check in weekly triage table


- `modules/web/MODULE.md` ΓÇö Lighthouse 3-run median policy documented


- `modules/android/MODULE.md` ΓÇö CI assembleDebug documented; fixed corrupted path characters


- `docs/INITIALIZATION_PROMPT.md` ΓÇö root `.gitignore` in encoding extension list


- `PROMPT_LIBRARY.md` entries 4 and 6 ΓÇö validate-workflow-actions, three-workflow sign-off





### Fixed





- CI: Lighthouse CI uses 3 runs with median assertion to reduce shared-runner flake while keeping 0.9 performance budget


- Security Scan: pin `aquasecurity/trivy-action` to SHA `a9c7b0f` (v0.36.0); invalid `@0.28.0` ref caused workflow setup failure


- Automation: `scripts/validate-workflow-actions.sh` and `scripts/check-github-ci.sh` (+ `.ps1`) to catch bad action refs and poll required GH workflows before sign-off


- CI: Web TS null narrowing in main.ts, MIT license on web package, scoped Android FOSS grep to Gradle files


- Python: ruff format on greet.py


- Index/pre-commit: CONTRIBUTING.md in TEMPLATE_INDEX; encoding hook covers .ts/.tsx/.toml


- License script: --excludePrivatePackages for private stub packages


- Encoding: normalize UTF-16 index.html and style.css; extend encoding scan to .html/.css





## [0.2.0] - 2026-06-12





### Added





- `scripts/check-file-encoding.sh` ΓÇö UTF-8 enforcement in CI and pre-commit


- `.env.example` ΓÇö documented environment variable stub


- `examples/web/package-lock.json` and `examples/python/uv.lock` ΓÇö reproducible locked installs


- Build Verification Gate in `INITIALIZATION_PROMPT.md` Section 7 (Sprint 0 + release)


- `PROMPT_LIBRARY.md` entries: bootstrap verification, security triage, SBOM audit, build verification


- Secret rotation procedure in `docs/RUNBOOK.md`


- Android operations checklist in `modules/android/MODULE.md`


- Release workflow `workflow_dispatch` for maintainer dry-run


- Web Vitest coverage budget (90%) matching Python example





### Changed





- Normalized ~46 UTF-16 corrupted files to UTF-8


- `scripts/validate-bootstrap.sh` ΓÇö encoding, index, lockfile, and LICENSE checks


- `scripts/check-license-compliance.sh` ΓÇö strict fail on disallowed licenses; stack-scoped CI steps


- `TEMPLATE_INDEX.json` ΓÇö added LICENSE, scripts, dependency-review, destructive-ops, `.env.example`; version 0.2.0


- `.github/CODEOWNERS` ΓÇö `@[PROJECT_OWNER]` placeholder; init scripts replace during Sprint 0


- `docs/SECURITY_TRIAGE.md` ΓÇö private vulnerability reporting in setup


- `docs/UPGRADING_FROM_TEMPLATE.md` ΓÇö cherry-pick rows for new scripts/workflows


- `BUILD_PLAN.md` ΓÇö encoding, lockfiles, Build Verification Gate in Sprint 0 and Milestone Gates


- `README.md` ΓÇö links THREAT_MODEL, PRIVACY, RUNBOOK, THIRD_PARTY_LICENSES, LICENSE


- CI: license check after locked installs; `uv sync --locked`; encoding-check job first


- `docs/MAINTAINING_THE_TEMPLATE.md` ΓÇö release dry-run steps


- Init scripts ΓÇö CODEOWNERS replacement, GITHUB_ABOUT.md draft, update checker config





### Human-only (not automated)





- Enable Dependabot alerts + private vulnerability reporting on GitHub


- Branch protection on `main` with required CI checks (`encoding-check`, `validate-bootstrap`)


- Replace `@[PROJECT_OWNER]` in CODEOWNERS with real GitHub username


- Paste GitHub About description from `docs/GITHUB_ABOUT.md`





## [0.1.0] - 2026-06-12





### Added





- Verbatim Project Initialization Prompt (`docs/INITIALIZATION_PROMPT.md`)


- Agent routing: `docs/START_HERE.md`, `docs/FOR_AGENTS.md`, `TEMPLATE_INDEX.json`


- Workspace memory files: `AGENT_MEMORY.md`, `DECISION_LOG.md`, `KNOWLEDGE_BASE.md`, `BUILD_PLAN.md`


- Multi-stack Golden Path stubs: Web (Vite PWA), Python (uv CLI), Android (FOSS Gradle skeleton)


- Ecosystem module guides: Android, Web, Python, Lightroom


- CI/CD guardrails: matrix CI, CodeQL, Trivy, Dependabot, release workflow


- Template update checker with configurable intervals (`off`, `daily`, `weekly`, `monthly`, `on_session`)


- Maintainer and consumer docs: `MAINTAINING_THE_TEMPLATE.md`, `UPGRADING_FROM_TEMPLATE.md`


- Devcontainer, pre-commit hooks, init scripts (bash + PowerShell)


- `SECURITY.md`, `CODE_OF_CONDUCT.md`, `.github/CODEOWNERS` ΓÇö community health and responsible disclosure


- `docs/THREAT_MODEL.md`, `docs/PRIVACY.md`, `docs/RUNBOOK.md` ΓÇö threat model, privacy-by-design, operations


- `THIRD_PARTY_LICENSES.md` + `scripts/check-license-compliance.sh` ΓÇö license compliance


- `scripts/validate-bootstrap.sh` ΓÇö Sprint 0 artifact verification in CI


- `.github/workflows/dependency-review.yml` ΓÇö PR dependency review (fail on High/Critical)


- Release workflow: SBOM (CycloneDX) + SLSA build provenance attestation


- `.cursor/rules/destructive-ops.mdc` ΓÇö human-in-the-loop gates for destructive agent operations





[0.2.0]: https://github.com/edwardlthompson/agent-project-bootstrap/releases/tag/v0.2.0


[0.2.1]: https://github.com/edwardlthompson/agent-project-bootstrap/releases/tag/v0.2.1


[0.1.0]: https://github.com/edwardlthompson/agent-project-bootstrap/releases/tag/v0.1.0
