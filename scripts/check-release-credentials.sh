#!/usr/bin/env bash
# Verify release credential posture: code scanning, signing secrets, auto-merge token need.
# Exits 0 when unsigned/sideload path is acceptable; fails only on gh/auth errors when required.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

REPO="${1:-${GITHUB_REPO:-}}"
FAIL=0

echo "=== check-release-credentials ==="

if ! command -v gh >/dev/null 2>&1; then
  echo "SKIP gh not installed — run locally with gh auth for full credential audit"
  exit 0
fi

if [ -z "$REPO" ]; then
  REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
fi
if [ -z "$REPO" ]; then
  echo "SKIP not a gh-authenticated repo"
  exit 0
fi

# Code scanning / SARIF upload readiness
scanning="$(gh api "repos/${REPO}" --jq '.security_and_analysis.advanced_security.status // "disabled"' 2>/dev/null || echo "unknown")"
secret_scan="$(gh api "repos/${REPO}" --jq '.security_and_analysis.secret_scanning.status // "disabled"' 2>/dev/null || echo "unknown")"
cs_setup="$(gh api "repos/${REPO}/code-scanning/default-setup" --jq '.state // "unknown"' 2>/dev/null || echo "unknown")"

if [ "$scanning" = "enabled" ] || [ "$secret_scan" = "enabled" ]; then
  echo "OK   Code security analysis enabled (advanced_security=$scanning secret_scanning=$secret_scan)"
elif [ "$cs_setup" = "configured" ]; then
  echo "OK   Code scanning default setup configured"
else
  echo "WARN code scanning not enabled — run: bash scripts/setup-github-repo.sh $REPO"
fi

# Authenticode secrets (optional — product-release uses sideload-auto when absent)
if gh secret list --repo "$REPO" 2>/dev/null | grep -q '^CODESIGN_PFX_BASE64'; then
  if gh secret list --repo "$REPO" 2>/dev/null | grep -q '^CODESIGN_PASSWORD'; then
    echo "OK   CODESIGN_* secrets configured (EV signing path)"
  else
    echo "WARN CODESIGN_PFX_BASE64 set but CODESIGN_PASSWORD missing"
    FAIL=1
  fi
else
  echo "OK   CODESIGN_* absent — product-release uses AUTO sideload self-signed signing"
fi

# RELEASE_BOT_TOKEN — only needed when workflow cannot approve PRs
workflow_perms="$(gh api "repos/${REPO}/actions/permissions/workflow" --jq '.can_approve_pull_request_reviews // false' 2>/dev/null || echo "false")"
protection="$(gh api "repos/${REPO}/branches/${GITHUB_DEFAULT_BRANCH:-main}/protection" 2>/dev/null || echo "{}")"
review_count="$(echo "$protection" | jq '.required_pull_request_reviews.required_approving_review_count // 0' 2>/dev/null || echo 0)"

if [ "$workflow_perms" = "true" ] && [ "${review_count:-0}" = "0" ]; then
  echo "OK   RELEASE_BOT_TOKEN not required (GITHUB_TOKEN can auto-merge Release Please PRs)"
elif gh secret list --repo "$REPO" 2>/dev/null | grep -q '^RELEASE_BOT_TOKEN'; then
  echo "OK   RELEASE_BOT_TOKEN configured"
else
  echo "WARN set RELEASE_BOT_TOKEN or run: bash scripts/setup-github-repo.sh $REPO"
fi

if [ "$FAIL" -ne 0 ]; then
  exit 1
fi
echo "check-release-credentials passed"
