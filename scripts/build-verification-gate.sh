#!/usr/bin/env bash
# Build Verification Gate — INITIALIZATION_PROMPT Section 7 checklist.
# Usage: scripts/build-verification-gate.sh [--quick] [--skip-pre-commit] [--skip-dotnet]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

QUICK=false
SKIP_PRECOMMIT=false
SKIP_DOTNET=false

for arg in "$@"; do
  case "$arg" in
    --quick) QUICK=true ;;
    --skip-pre-commit) SKIP_PRECOMMIT=true ;;
    --skip-dotnet) SKIP_DOTNET=true ;;
  esac
done

ERRORS=0

run_check() {
  if ! "$@"; then
    ERRORS=$((ERRORS + 1))
  fi
}

echo "=== Build Verification Gate ==="

run_check bash scripts/check-file-encoding.sh

if [ "$QUICK" = false ]; then
  run_check bash scripts/validate-workflow-actions.sh
else
  echo "SKIP validate-workflow-actions (--quick)"
fi

run_check bash scripts/validate-template-index.sh

if [ "$QUICK" = true ]; then
  run_check bash scripts/validate-bootstrap.sh --quick
else
  run_check bash scripts/validate-bootstrap.sh
fi

run_check bash scripts/check-adr-status.sh
run_check bash scripts/check-readme-health.sh --quick
run_check bash scripts/check-legal-consistency.sh

if [ "$SKIP_PRECOMMIT" = false ] && command -v pre-commit >/dev/null 2>&1; then
  run_check pre-commit run --all-files
elif [ "$SKIP_PRECOMMIT" = false ]; then
  echo "SKIP pre-commit (not installed)"
fi

if [ "$SKIP_DOTNET" = false ] && [ -f SpatialLabsOptimizer.sln ] && command -v dotnet >/dev/null 2>&1; then
  run_check dotnet build SpatialLabsOptimizer.sln --configuration Release
  run_check dotnet test src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj \
    --configuration Release --no-build --verbosity minimal
elif [ "$SKIP_DOTNET" = false ] && [ -f SpatialLabsOptimizer.sln ]; then
  echo "SKIP dotnet (not installed)"
fi

if [ "$QUICK" = false ] && command -v npm >/dev/null 2>&1 && [ -f examples/web/package-lock.json ]; then
  if [ -d examples/web/node_modules ]; then
    run_check bash scripts/check-license-compliance.sh web
  else
    echo "SKIP web license check (node_modules missing)"
  fi
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "${ERRORS} build verification check(s) failed"
  exit 1
fi

echo "Build verification gate passed"
