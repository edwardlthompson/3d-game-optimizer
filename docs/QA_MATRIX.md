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

| Scenario | Expected Result | Priority | Test reference |
|---|---|---|---|
| First launch with supported Acer display | recommended profile auto-selected | P0 | `QaMatrixAutomationTests.P0_AcerDisplay_RecommendedProfileFromCatalog` |
| First launch with unknown display | generic safe profile offered | P0 | `QaMatrixAutomationTests.P0_UnknownDisplay_GenericProfileAvailable` |
| Apply preset then rollback | rollback restores prior values | P0 | `QaMatrixAutomationTests.P0_RollbackSnapshot_RestoresPriorState` |
| Steam unavailable | app remains usable via local seed data | P1 | `Sprint43QaGateTests.P1_SteamUnavailable_LaunchFallbackStillSucceeds`; `QaMatrixAutomationTests.P0_OfflineSeed_SteamUnavailableLibraryStillLoads` |
| Tool silent install failure | classified error with next steps shown | P0 | `QaMatrixAutomationTests.P0_SilentInstallFailure_ClassifiedWithRecoverySteps` |
| Offline mode with existing cache | no blocking network dependency | P1 | `Sprint43QaGateTests.P1_OfflineCache_PresetExistsWithoutNetworkFetch` |
| Accessibility keyboard tab flow | setup wizard `CanProceed` gates each step | P0 | `AccessibilitySmokeTests` |
| PCVR launch without runtime | graceful failure, no crash | P1 | `MilestoneFeatureTests.PlayInVR_GracefulFail_WhenNoRuntime` |
| Incremental Steam scan | true delta vs known installs, skip index when none | P1 | `Sprint43QaGateTests.IncrementalSteamScan_CountNewInstalls_IgnoresSeedOnlyTitles` |
| HDR watchdog | registry disable or explicit OS handoff instructions | P1 | `Sprint43QaGateTests.HdrWatchdog_ExposesOsHandoffInstructions` |
| About update status | cached/staged update renders apply affordance | P1 | `Sprint43QaGateTests.AboutUpdate_CachedResultEnablesApply`; `AboutUpdate_RetryPendingFlagRoundTrips` |
| Command palette filter | search narrows quick actions | P1 | `Sprint43QaGateTests.CommandPalette_SearchFiltersByQuery` |
| Library favorites filter | favorites-only subset from SQLite | P1 | `Sprint43QaGateTests.LibraryFilters_FavoritesOnly_ReturnsSubset` |

## v1.2 Scenarios (Sprint 29)

| Scenario | Expected Result | Priority | Test reference |
|---|---|---|---|
| Toggle game favorite | persisted in SQLite, filter shows favorites only | P1 | `LibraryUxTests.SetFavorite_PersistsAcrossUpsert`, `FavoritesFilter_ReturnsOnlyFavorites` |
| Play queue dequeue / play-next | queue count decreases, next title launches | P1 | `LibraryUxTests.PlayQueue_DequeuePlayNext_ReducesCount` |
| Artwork SteamGridDb fallback | cover resolves when Steam CDN fails | P2 | `LibraryUxTests.GameArtworkService_UsesSteamGridDbFallback_WhenCdnEmpty` |
| Save/load session playlist | named playlist round-trips app IDs | P2 | `LibraryUxTests.LocalPlaylistRepository_RoundTripsNamesAndIds` |

## v2 Scenarios (flag: `SPATIALLABS_ENABLE_V2=true`)

| Scenario | Expected Result | Priority | Test reference |
|---|---|---|---|
| Epic launcher absent | empty ID list, no error | P0 | `V2IntegrationTests.EpicGogScanner_ReturnsEmpty_WhenNotInstalled` |
| GOG launcher absent | empty ID list, no error | P0 | same |
| Epic install metadata | install dir + launch exe parsed and persisted | P1 | `V2IntegrationTests.EpicScanner_ParseManifest_ResolvesInstallPath`; `MultiStoreMerge_PersistsInstallMetadata` |
| GOG launch metadata | playTasks/exe resolved to launch path | P1 | `V2IntegrationTests.GogScanner_ParseInfo_ResolvesLaunchExe` |
| Workshop preset import | local cache only, allowlist URLs | P1 | `V2IntegrationTests.WorkshopImporter_ImportsAllowlistedSourceManifest` |
| LAN party export | JSON export, no PII | P1 | `V2IntegrationTests.LanPartyExport_WritesTitlePayloadWithoutPii` |
| Multi-store library merge | read-only scan, no silent install | P0 | `V2IntegrationTests.MultiStoreMerge_UsesParsedExternalTitles`; ADR-0004 |
| Hybrid co-op session | session code persisted, no PII | P2 | `V2IntegrationTests.HybridSession_PersistsSessionCode` |

## v1.3 Scenarios (Sprint 33 — trainer coexistence)

| Scenario | Expected Result | Priority | Test reference |
|---|---|---|---|
| Trainer conflict blocks launch | `3DGO-0004` when coexistence off and WeMod/Vortex/MO2 running | P0 | `CoexistenceLaunchTests` |
| Game-first skips UEVR wrapper | UEVR injector skipped when game-first policy active | P1 | `CoexistenceLaunchTests.UevrLauncher_SkipsInjector` |

## v1.4 Scenarios (Sprint 35 — library intelligence)

| Scenario | Expected Result | Priority | Test reference |
|---|---|---|---|
| Why not ready filter | filter hides titles missing prerequisites | P1 | `LibraryIntelligenceTests.WhyNotReadyFilter` |
| Smart collection local only | local-only smart collection excludes online-only | P2 | `LibraryIntelligenceTests.SmartCollection_LocalOnly` |
| Compatibility badge on tiles | badge shows local/verified state | P1 | `LibraryIntelligenceTests.CompatibilityBadge_LocalAndVerified` |
| Recent launches SQLite | recent launches persist across sessions | P1 | `LibraryIntelligenceTests.RecentLaunches_PersistInSqlite` |
| Compatibility notes local | per-title notes round-trip | P2 | `LibraryIntelligenceTests.CompatibilityNotes_RoundTrip` |
| Preset freshness indicator | stale preset flagged when cache old | P2 | `LibraryIntelligenceTests.PresetFreshnessIndicator_ReportsStaleWhenOld` |

## v1.5 Scenarios (Sprint 35 — local game folders)

| Scenario | Expected Result | Priority | Test reference |
|---|---|---|---|
| Add folder games appear after index | scanned exe titles appear in library | P0 | `LocalFolderGameTests.AddLocalFolder_GamesAppearAfterIndex` |
| Removed folder marks stale | removed watch path marks installs stale | P1 | `LocalFolderGameTests.RemovedFolder_MarksInstallStale` |
| Launch local game without Steam | direct exe path used when no Steam ID | P0 | `LocalFolderGameTests.LocalGameInstallResolver_UsesDirectExePath` |
| Local scanner exclude heuristics | setup/uninstall exes excluded | P1 | `LocalFolderGameTests.LocalFolderGameScanner_ExcludesSetupAndUninstallExes` |

## Exit Criteria for v1

- All P0 scenarios covered by CI (`scripts/check-qa-matrix-coverage.sh`).
- No critical crashes in setup flow (integration tests).
- Accessibility smoke checks pass (`AccessibilitySmokeTests`).

## Exit Criteria for v1.2 (Sprint 29)

- `V12_MAP` in `check-qa-matrix-coverage.sh` covers favorites and queue dequeue scenarios.
- v1.2 P1 scenarios have automated test references (no `TBD` in table above).

## Exit Criteria for v1.3 (Sprint 33)

- `V13_MAP` in `check-qa-matrix-coverage.sh` covers trainer coexistence scenarios.

## Exit Criteria for v1.4 / v1.5 (Sprint 35)

- `V14_MAP` and `V15_MAP` in `check-qa-matrix-coverage.sh` cover library intelligence and local folder scenarios.

## Exit Criteria for v2

- All v2 P0 scenarios have automated or documented coverage (table above).
- `FeatureFlags.V2Enabled` requires explicit `SPATIALLABS_ENABLE_V2=true`; default off in production builds.
- Epic/GOG read-only scan documented in [ADR-0004](adr/0004-epic-gog-connector.md); no elevated silent install for third-party stores.
- Product tag `SpatialLabsOptimizer-v*` runs full pre-release gate including QA matrix coverage.
- Optional Authenticode signing when `CODESIGN_*` secrets are configured.

## Exit Criteria for Sprint 43 (P1 gate)

- `P1_MAP` in `check-qa-matrix-coverage.sh` covers offline/Steam, incremental scan delta, HDR handoff, About update, palette search, and library filters.
- P1 scenarios in the core table above reference automated tests (no `TBD`).
- README UI previews regenerated via `scripts/generate-brand-assets.py`.
