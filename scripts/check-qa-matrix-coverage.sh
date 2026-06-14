#!/usr/bin/env bash
# Verify every QA matrix P0 scenario has a corresponding automated test reference.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

FAIL=0
TESTS_DIR="src/SpatialLabsOptimizer.Tests"

pattern_in_tests() {
  local pattern="$1"
  if command -v rg >/dev/null 2>&1; then
    rg -q "$pattern" "$TESTS_DIR" --glob '*.cs'
  else
    grep -rqE "$pattern" "$TESTS_DIR" --include '*.cs'
  fi
}

declare -A P0_MAP=(
  ["First launch with supported Acer display"]="DisplayAutoDetector|SetupWizard|Acer"
  ["First launch with unknown display"]="DisplayAutoDetector|generic|ManualDisplay"
  ["Apply preset then rollback"]="RollbackSnapshot|Rollback"
  ["Tool silent install failure"]="InstallErrorCatalog|SilentInstall|LaunchIntegrationTests|3DGO-010"
  ["Accessibility keyboard"]="AccessibilitySmoke|SetupWizardFlow|CanProceed"
)

declare -A V11_MAP=(
  ["PCVR launch without runtime"]="PlayInVR|PcvrConnector|graceful"
  ["Incremental Steam scan"]="IncrementalSteamScan"
  ["Command palette search"]="CommandPalette_Search|CommandPaletteService"
)

declare -A V12_MAP=(
  ["Toggle game favorite"]="SetFavorite|FavoritesFilter|IsFavorite"
  ["Play queue dequeue / play-next"]="PlayQueue_DequeuePlayNext|TryDequeue"
)

declare -A V2_MAP=(
  ["Epic launcher absent"]="EpicGogScanner_ReturnsEmpty|ScanEpicInstalledGames"
  ["GOG launcher absent"]="EpicGogScanner_ReturnsEmpty|ScanGogInstalledGames"
  ["Workshop preset import"]="WorkshopImporter_ImportsAllowlisted|ImportAllowlistedSources"
  ["LAN party export"]="LanPartyExport_WritesTitlePayload|ExportSessionAsync"
  ["Multi-store library merge"]="MultiStoreMerge_UsesParsedExternal|EpicScanner_ParseManifest"
  ["Hybrid co-op session"]="HybridSession_PersistsSessionCode"
)

declare -A V13_MAP=(
  ["Trainer conflict blocks launch"]="CoexistenceLaunch|3DGO-0004|ExternalToolCoexistenceService"
  ["Game-first skips UEVR wrapper"]="UevrLauncher_SkipsInjector|GameFirstLaunchOrchestrator"
)

declare -A V14_MAP=(
  ["Why not ready filter"]="WhyNotReadyFilter|ApplyWhyNotReadyFilter"
  ["Smart collection local only"]="SmartCollection_LocalOnly"
  ["Compatibility badge on tiles"]="CompatibilityBadge_LocalAndVerified"
  ["Recent launches SQLite"]="RecentLaunches_PersistInSqlite"
  ["Compatibility notes local"]="CompatibilityNotes_RoundTrip"
  ["Preset freshness indicator"]="PresetFreshnessIndicator_ReportsStaleWhenOld"
)

declare -A V15_MAP=(
  ["Add folder games appear after index"]="AddLocalFolder_GamesAppearAfterIndex"
  ["Removed folder marks stale"]="RemovedFolder_MarksInstallStale"
  ["Launch local game without Steam"]="LocalGameInstallResolver_UsesDirectExePath"
  ["Local scanner exclude heuristics"]="LocalFolderGameScanner_ExcludesSetupAndUninstallExes"
)

check_map() {
  local label="$1"
  local -n map_ref="$2"
  for scenario in "${!map_ref[@]}"; do
    local pattern="${map_ref[$scenario]}"
    if pattern_in_tests "$pattern"; then
      echo "OK   $label: $scenario"
    else
      echo "FAIL $label: $scenario (expected test matching: $pattern)"
      FAIL=1
    fi
  done
}

echo "=== check-qa-matrix-coverage ==="
check_map "P0" P0_MAP
check_map "v1.1" V11_MAP
check_map "v1.2" V12_MAP
check_map "v2" V2_MAP
check_map "v1.3" V13_MAP
check_map "v1.4" V14_MAP
check_map "v1.5" V15_MAP

if [ ! -f docs/QA_MATRIX.md ]; then
  echo "FAIL: docs/QA_MATRIX.md missing"
  FAIL=1
fi

if [ "$FAIL" -ne 0 ]; then exit 1; fi
echo "check-qa-matrix-coverage passed"
