#!/usr/bin/env bash
# Pre-release gate: CI green, security triage, Dependabot, CHANGELOG, licenses, dotnet tests.
# Usage: scripts/pre-release-gate.sh [--allow-exception ISSUE_URL] [--skip-triage] [--skip-dotnet]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ERRORS=0
VERSION=""
ALLOW_EXCEPTION=""
SKIP_TRIAGE=false
SKIP_DOTNET=false
SKIP_CI_CHECK=false
PRODUCT_RELEASE=false

while [ $# -gt 0 ]; do
  case "$1" in
    --allow-exception) ALLOW_EXCEPTION="${2:-}"; shift 2 ;;
    --skip-triage) SKIP_TRIAGE=true; shift ;;
    --skip-dotnet) SKIP_DOTNET=true; shift ;;
    --skip-ci-check) SKIP_CI_CHECK=true; shift ;;
    --product-release) PRODUCT_RELEASE=true; shift ;;
    *) echo "Usage: $0 [--allow-exception ISSUE_URL] [--skip-triage] [--skip-dotnet] [--skip-ci-check] [--product-release]"; exit 1 ;;
  esac
done

echo "=== Pre-release gate ==="

if [ "$SKIP_CI_CHECK" = false ]; then
  if ! bash scripts/check-github-ci.sh HEAD --wait 300; then
    echo "FAIL: required GitHub workflows not green"
    ERRORS=$((ERRORS + 1))
  fi
else
  echo "SKIP GitHub CI check"
fi

if [ "$SKIP_TRIAGE" = false ]; then
  if ! bash scripts/check-security-triage.sh; then
    echo "FAIL: weekly CVE triage not current"
    ERRORS=$((ERRORS + 1))
  fi
else
  echo "SKIP security triage check"
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required for Dependabot alert count"
  exit 1
fi

REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
if [ -z "$REPO" ]; then
  echo "ERROR: run from a git repo with gh auth, or export GITHUB_REPO=owner/name"
  exit 1
fi

if [ "$PRODUCT_RELEASE" = true ]; then
  if ! bash scripts/check-codeql-sarif-upload.sh "$REPO" HEAD --strict; then
    echo "FAIL: CodeQL SARIF upload failed"
    ERRORS=$((ERRORS + 1))
  fi
else
  bash scripts/check-codeql-sarif-upload.sh "$REPO" HEAD || true
fi

if ALERT_COUNT="$(bash scripts/dependabot-critical-high-count.sh "$REPO" 2>/dev/null)"; then
  :
else
  ALERT_COUNT=""
fi

if [ -z "$ALERT_COUNT" ]; then
  if [ "$PRODUCT_RELEASE" = true ]; then
    echo "WARN: could not fetch Dependabot alerts — skipped for product-release (CI Security Scan covers dependency posture)"
  else
    echo "WARN: could not fetch Dependabot alerts (check gh auth, Dependabot alerts, and security-events: read)"
    ERRORS=$((ERRORS + 1))
  fi
elif [ "${ALERT_COUNT:-0}" -gt 0 ]; then
  if [ -n "$ALLOW_EXCEPTION" ]; then
    issue_state="$(gh issue view "$ALLOW_EXCEPTION" --json state -q .state 2>/dev/null || echo "")"
    if [ "$issue_state" = "OPEN" ] || [ "$issue_state" = "CLOSED" ]; then
      echo "WARN ${ALERT_COUNT} Critical/High alert(s) — documented exception: $ALLOW_EXCEPTION"
    else
      echo "FAIL: ${ALERT_COUNT} alert(s) and exception issue not found: $ALLOW_EXCEPTION"
      ERRORS=$((ERRORS + 1))
    fi
  else
    echo "FAIL: ${ALERT_COUNT} open Critical/High Dependabot alert(s)"
    ERRORS=$((ERRORS + 1))
  fi
else
  echo "OK   Zero open Critical/High Dependabot alerts"
fi

if [ ! -f .template-version ]; then
  echo "MISSING: .template-version"
  ERRORS=$((ERRORS + 1))
else
  VERSION="$(tr -d '[:space:]' < .template-version)"
  echo "OK   .template-version = ${VERSION}"
fi

if [ -n "$VERSION" ] && [ -f CHANGELOG.md ]; then
  if grep -qE "^## \[${VERSION}\]|^## ${VERSION}" CHANGELOG.md; then
    echo "OK   CHANGELOG.md section for ${VERSION}"
  else
    echo "FAIL: CHANGELOG.md missing section for version ${VERSION}"
    ERRORS=$((ERRORS + 1))
  fi
fi

if ! bash scripts/check-license-compliance.sh all 2>/dev/null; then
  if [ -f THIRD_PARTY_LICENSES.md ] && [ -s THIRD_PARTY_LICENSES.md ]; then
    echo "WARN license-compliance.sh skipped or partial — THIRD_PARTY_LICENSES.md present"
  else
    echo "FAIL: THIRD_PARTY_LICENSES.md missing or empty"
    ERRORS=$((ERRORS + 1))
  fi
else
  echo "OK   License compliance check passed"
fi

if [ "$SKIP_DOTNET" = false ] && [ -f SpatialLabsOptimizer.sln ] && command -v dotnet >/dev/null 2>&1; then
  if ! dotnet test src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj \
    --configuration Release \
    --verbosity minimal; then
    echo "FAIL: product test suite failed"
    ERRORS=$((ERRORS + 1))
  else
    echo "OK   Full test suite passed"
  fi
elif [ "$SKIP_DOTNET" = false ] && [ -f SpatialLabsOptimizer.sln ]; then
  echo "SKIP dotnet tests (dotnet not installed)"
fi

if command -v npm >/dev/null 2>&1; then
  if [ -f site/catalog/package.json ]; then
    if ! (cd site/catalog && npm ci && npm test); then
      echo "FAIL: catalog npm ci/test failed"
      ERRORS=$((ERRORS + 1))
    else
      echo "OK   catalog npm ci/test passed"
    fi
  fi
  if [ -f workers/steam-library/package.json ]; then
    if ! (cd workers/steam-library && npm ci && npm test); then
      echo "FAIL: steam-library worker npm ci/test failed"
      ERRORS=$((ERRORS + 1))
    else
      echo "OK   steam-library worker npm ci/test passed"
    fi
  fi
else
  echo "SKIP npm tests (npm not installed)"
fi

if [ "${PRODUCT_RELEASE:-false}" = true ]; then
  if [ -f src/SpatialLabsOptimizer/product-version.json ]; then
    PRODUCT_VERSION="$(python3 -c "import json; print(json.load(open('src/SpatialLabsOptimizer/product-version.json'))['version'])" 2>/dev/null || true)"
    if [ -n "$PRODUCT_VERSION" ]; then
      echo "OK   product-version.json = ${PRODUCT_VERSION}"
    else
      echo "FAIL: could not read product-version.json"
      ERRORS=$((ERRORS + 1))
    fi
  fi
  if command -v rg >/dev/null 2>&1; then
    if ! bash scripts/check-qa-matrix-coverage.sh; then
      echo "FAIL: QA matrix P0 coverage incomplete"
      ERRORS=$((ERRORS + 1))
    fi
    if ! bash scripts/check-compatibility-seed.sh; then
      echo "FAIL: compatibility seed validation failed"
      ERRORS=$((ERRORS + 1))
    fi
  fi
fi

# Legacy persistence spot-check (non-product-release quick path)
if [ "${PRODUCT_RELEASE:-false}" != true ] && [ "$SKIP_DOTNET" = false ] && [ -f SpatialLabsOptimizer.sln ] && command -v dotnet >/dev/null 2>&1; then
  if ! dotnet test src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj \
    --configuration Release \
    --filter "FullyQualifiedName~SqliteSettingsStore_SurvivesSchemaMigration|FullyQualifiedName~SettingsStore" \
    --verbosity minimal 2>/dev/null; then
    if ! dotnet test src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj \
      --configuration Release \
      --filter "FullyQualifiedName~SqliteSettingsStore" \
      --verbosity minimal; then
      echo "FAIL: state persistence tests failed"
      ERRORS=$((ERRORS + 1))
    else
      echo "OK   State persistence tests passed"
    fi
  else
    echo "OK   State persistence / settings tests passed"
  fi
fi

# Conventional commits on last 20 commits on main (when available)
if git rev-parse --verify main >/dev/null 2>&1; then
  bad=0
  while IFS= read -r subject; do
    [ -z "$subject" ] && continue
    if ! echo "$subject" | grep -qE '^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\(.+\))?!?: .+|^Merge |^Release |^chore\(main\): release'; then
      bad=$((bad + 1))
    fi
  done < <(git log main -20 --pretty=format:'%s' 2>/dev/null || true)
  if [ "$bad" -gt 5 ]; then
    echo "WARN ${bad}/20 recent main commits may not follow Conventional Commits"
  else
    echo "OK   Conventional Commits check on recent main commits"
  fi
fi

echo ""
echo "REMINDER: Release Please merges trigger tagging; or run Release workflow manually."
if [ -n "$VERSION" ]; then
  echo "  Confirm CHANGELOG.md [${VERSION}] and tag match .template-version"
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "${ERRORS} pre-release gate check(s) failed"
  exit 1
fi

echo "Pre-release gate passed"
