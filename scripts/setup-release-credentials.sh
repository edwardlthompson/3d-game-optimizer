#!/usr/bin/env bash
# Automate former HUMAN release credentials: code scanning, optional sideload signing secrets.
# Usage: scripts/setup-release-credentials.sh [owner/repo]
# Optional: AUTO_SETUP_SIDeload_CODESIGN=1 to push generated sideload cert to GitHub secrets.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

REPO="${1:-${GITHUB_REPO:-}}"
if [ -z "$REPO" ]; then
  REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
fi
if [ -z "$REPO" ]; then
  echo "ERROR: pass owner/repo or run from authenticated gh context"
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required"
  exit 1
fi

echo "=== setup-release-credentials ($REPO) ==="

# Re-use base repo security (branch protection, workflow PR approve, dependabot)
bash scripts/setup-github-repo.sh "$REPO"

# Enable code scanning features for SARIF upload (public repos: free)
security_json='{
  "security_and_analysis": {
    "secret_scanning": { "status": "enabled" },
    "secret_scanning_push_protection": { "status": "disabled" },
    "advanced_security": { "status": "enabled" }
  }
}'
if gh api --method PATCH "repos/${REPO}" --input - <<<"$security_json" >/dev/null 2>&1; then
  echo "OK   security_and_analysis enabled (secret scanning + advanced security)"
else
  echo "WARN could not PATCH security_and_analysis — may need org admin or public repo"
fi

# Prefer custom codeql.yml — do not enable default-setup (would duplicate workflows)
cs_state="$(gh api "repos/${REPO}/code-scanning/default-setup" --jq '.state // "not-configured"' 2>/dev/null || echo "unknown")"
if [ "$cs_state" = "configured" ]; then
  echo "WARN code-scanning default-setup is configured — may duplicate .github/workflows/codeql.yml"
else
  echo "OK   code-scanning default-setup not configured (custom codeql.yml retained)"
fi

# Optional: push sideload cert secrets for consistent CI signing
if [ "${AUTO_SETUP_SIDeload_CODESIGN:-}" = "1" ]; then
  pwsh -NoProfile -File scripts/generate-sideload-codesign.ps1
  b64_path="artifacts/sideload-codesign/sideload-codesign.b64.txt"
  if [ ! -f "$b64_path" ]; then
    echo "FAIL sideload cert generation failed"
    exit 1
  fi
  gh secret set CODESIGN_PFX_BASE64 --repo "$REPO" < "$b64_path"
  gh secret set CODESIGN_PASSWORD --repo "$REPO" -b "sideload-dev"
  echo "OK   CODESIGN_* secrets set from sideload cert (not publicly trusted)"
else
  echo "OK   sideload cert available via: pwsh scripts/generate-sideload-codesign.ps1"
  echo "     Push secrets with: AUTO_SETUP_SIDeload_CODESIGN=1 bash scripts/setup-release-credentials.sh $REPO"
fi

bash scripts/check-release-credentials.sh "$REPO"
echo "setup-release-credentials complete"
