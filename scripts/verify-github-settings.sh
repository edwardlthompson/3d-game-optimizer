#!/usr/bin/env bash
# Read-only verification of GitHub repo security settings (for CI).
# Usage: scripts/verify-github-settings.sh [owner/repo]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

REPO="${1:-${GITHUB_REPO:-}}"
if [ -z "$REPO" ]; then
  REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
fi
if [ -z "$REPO" ]; then
  echo "ERROR: pass owner/repo or set GITHUB_REPO"
  exit 1
fi

BRANCH="${GITHUB_DEFAULT_BRANCH:-main}"
ERRORS=0

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required"
  exit 1
fi

alerts="$(gh api "repos/${REPO}/vulnerability-alerts" -i 2>&1 | head -1 | awk '{print $2}' || echo "000")"
if [ "$alerts" = "204" ] || [ "$alerts" = "200" ]; then
  echo "OK   Dependabot vulnerability alerts enabled"
else
  echo "FAIL Dependabot alerts (HTTP $alerts)"
  ERRORS=$((ERRORS + 1))
fi

reporting="$(gh api "repos/${REPO}/private-vulnerability-reporting" -i 2>&1 | head -1 | awk '{print $2}' || echo "000")"
if [ "$reporting" = "200" ]; then
  echo "OK   Private vulnerability reporting enabled"
else
  echo "WARN private vulnerability reporting (HTTP $reporting)"
fi

protection="$(gh api "repos/${REPO}/branches/${BRANCH}/protection" 2>/dev/null || echo "{}")"
for check in CI "Security Scan" CodeQL; do
  if echo "$protection" | grep -q "\"$check\""; then
    echo "OK   Branch protection requires: $check"
  else
    echo "WARN branch protection missing required check: $check"
  fi
done

desc="$(gh repo view "$REPO" --json description -q .description 2>/dev/null || true)"
if [ -n "$desc" ]; then
  echo "OK   Repo description set (${#desc} chars)"
else
  echo "WARN repo description empty — run setup-github-repo.sh"
fi

if [ "$ERRORS" -gt 0 ]; then
  exit 1
fi

echo "GitHub settings verification passed"
