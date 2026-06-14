#!/usr/bin/env bash
# Verify UI consent strings align with docs/LEGAL.md required themes.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ERRORS=0

LEGAL="docs/LEGAL.md"
UI_SOURCES=(src/SpatialLabsOptimizer/Views/SetupWizardView.xaml)

if [ ! -d src/SpatialLabsOptimizer/ViewModels ]; then
  echo "FAIL missing ViewModels directory"
  exit 1
fi

while IFS= read -r -d '' vm; do
  UI_SOURCES+=("$vm")
done < <(find src/SpatialLabsOptimizer/ViewModels -maxdepth 1 -name '*.cs' -print0 2>/dev/null)

if [ ! -f "$LEGAL" ]; then
  echo "MISSING: $LEGAL"
  exit 1
fi

check_phrase_in_ui() {
  local phrase="$1"
  local label="$2"
  local found=false
  for src in "${UI_SOURCES[@]}"; do
    if [ -f "$src" ] && grep -qi "$phrase" "$src"; then
      found=true
      break
    fi
  done
  if [ "$found" = false ]; then
    echo "FAIL UI missing ${label}: expected phrase matching '$phrase'"
    ERRORS=$((ERRORS + 1))
  else
    echo "OK   UI contains ${label}"
  fi
}

check_phrase_in_legal() {
  local phrase="$1"
  local label="$2"
  if ! grep -qi "$phrase" "$LEGAL"; then
    echo "FAIL LEGAL.md missing ${label}"
    ERRORS=$((ERRORS + 1))
  else
    echo "OK   LEGAL.md contains ${label}"
  fi
}

check_phrase_in_legal "without warranty" "warranty disclaimer"
check_phrase_in_legal "third-party" "third-party notice"
check_phrase_in_legal "local-first" "privacy position"
check_phrase_in_ui "EULA" "toolchain EULA consent"
check_phrase_in_ui "legal" "legal disclaimers consent"

if [ "$ERRORS" -gt 0 ]; then
  echo "${ERRORS} legal consistency check(s) failed"
  exit 1
fi

echo "Legal consistency check passed"
