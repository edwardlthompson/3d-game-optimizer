#!/usr/bin/env bash
# Idempotent GitHub repo security setup via gh api.
# Enables Dependabot alerts, private vulnerability reporting, and branch protection on main.
# Usage: scripts/setup-github-repo.sh [owner/repo]
# Requires: gh CLI authenticated with admin access to the repo.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

REPO="${1:-${GITHUB_REPO:-}}"
if [ -z "$REPO" ]; then
  if ! command -v gh >/dev/null 2>&1; then
    echo "ERROR: gh CLI required (https://cli.github.com/)"
    exit 1
  fi
  REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
fi
if [ -z "$REPO" ]; then
  echo "ERROR: pass owner/repo or set GITHUB_REPO, or run from a git repo with gh auth"
  exit 1
fi

BRANCH="${GITHUB_DEFAULT_BRANCH:-main}"
REQUIRED_CHECKS=("CI" "Security Scan" "CodeQL")
TRANSIENT=0
FAILED=0

print_manual_checklist() {
  cat <<'EOF'
MANUAL SETUP CHECKLIST (GitHub UI - API returned 422 or insufficient permissions):
  1. Settings -> Code security and analysis -> Dependabot alerts: ON
  2. Settings -> Code security and analysis -> Dependabot security updates: ON
  3. Settings -> Code security and analysis -> Private vulnerability reporting: ON
  4. Settings -> Branches -> Branch protection rules -> main:
     - Require status checks: CI, Security Scan, CodeQL
     - Require branches to be up to date before merging (recommended)
  5. Re-run: bash scripts/setup-github-repo.sh
EOF
}

gh_api_retry() {
  local method="$1"
  local endpoint="$2"
  local data="${3:-}"
  local attempt=1
  local out http_code body rc

  while [ "$attempt" -le 3 ]; do
    if [ -n "$data" ]; then
      out="$(gh api --method "$method" "$endpoint" --input - <<<"$data" -i 2>&1)" || true
    else
      out="$(gh api --method "$method" "$endpoint" -i 2>&1)" || true
    fi

    http_code="$(printf '%s' "$out" | head -1 | awk '{print $2}')"
    body="$(printf '%s' "$out" | tail -n +2)"

    case "$http_code" in
      200|201|204)
        return 0
      ;;
      422)
        echo "SKIP $endpoint (422): $body"
        print_manual_checklist
        return 2
      ;;
      401|403)
        echo "FAIL $endpoint ($http_code): insufficient permissions"
        echo "$body"
        FAILED=$((FAILED + 1))
        return 1
      ;;
      404)
        echo "FAIL $endpoint (404): not found"
        echo "$body"
        FAILED=$((FAILED + 1))
        return 1
      ;;
      000|"")
        echo "RETRY $endpoint (attempt $attempt): network or gh error"
        echo "$out"
        TRANSIENT=$((TRANSIENT + 1))
      ;;
      *)
        if [ "${http_code:-0}" -ge 500 ]; then
          echo "RETRY $endpoint ($http_code) attempt $attempt"
          TRANSIENT=$((TRANSIENT + 1))
        else
          echo "FAIL $endpoint ($http_code)"
          echo "$body"
          FAILED=$((FAILED + 1))
          return 1
        fi
      ;;
    esac
    attempt=$((attempt + 1))
    sleep $((attempt * 2))
  done
  return 3
}

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required (https://cli.github.com/)"
  exit 1
fi

echo "Setting up GitHub repo security for ${REPO} (branch: ${BRANCH})"

rc=0
if ! gh_api_retry PUT "repos/${REPO}/vulnerability-alerts"; then
  rc=$?
  if [ "$rc" -eq 3 ]; then TRANSIENT=$((TRANSIENT + 1)); elif [ "$rc" -eq 1 ]; then FAILED=$((FAILED + 1)); fi
else
  echo "OK   Dependabot vulnerability alerts enabled"
fi

if ! gh_api_retry PUT "repos/${REPO}/automated-security-fixes"; then
  rc=$?
  if [ "$rc" -eq 3 ]; then TRANSIENT=$((TRANSIENT + 1)); elif [ "$rc" -eq 1 ]; then FAILED=$((FAILED + 1)); fi
else
  echo "OK   Dependabot security updates enabled"
fi

if ! gh_api_retry PUT "repos/${REPO}/private-vulnerability-reporting"; then
  rc=$?
  if [ "$rc" -eq 3 ]; then TRANSIENT=$((TRANSIENT + 1)); elif [ "$rc" -eq 1 ]; then FAILED=$((FAILED + 1)); fi
else
  echo "OK   Private vulnerability reporting enabled"
fi

workflow_json='{"default_workflow_permissions":"write","can_approve_pull_request_reviews":true}'
if ! gh_api_retry PUT "repos/${REPO}/actions/permissions/workflow" "$workflow_json"; then
  rc=$?
  if [ "$rc" -eq 3 ]; then TRANSIENT=$((TRANSIENT + 1)); elif [ "$rc" -eq 1 ]; then FAILED=$((FAILED + 1)); fi
else
  echo "OK   Actions workflow permissions: write + PR create/approve (Release Please)"
fi

protection_json="$(python3 - <<'PY'
import json
print(json.dumps({
    "required_status_checks": {"strict": True, "contexts": ["CI", "Security Scan", "CodeQL"]},
    "enforce_admins": False,
    "required_pull_request_reviews": {
        "dismiss_stale_reviews": True,
        "require_code_owner_reviews": False,
        "required_approving_review_count": 0,
    },
    "restrictions": None,
    "required_linear_history": False,
    "allow_force_pushes": False,
    "allow_deletions": False,
    "block_creations": False,
}))
PY
)"

if ! gh_api_retry PUT "repos/${REPO}/branches/${BRANCH}/protection" "$protection_json"; then
  rc=$?
  if [ "$rc" -eq 3 ]; then TRANSIENT=$((TRANSIENT + 1)); elif [ "$rc" -eq 1 ]; then FAILED=$((FAILED + 1)); fi
else
  echo "OK   Branch protection on ${BRANCH} (required checks: ${REQUIRED_CHECKS[*]})"
fi

if [ "$TRANSIENT" -gt 0 ]; then
  echo "Transient errors after retries ($TRANSIENT); re-run later"
  exit 2
fi
if [ "$FAILED" -gt 0 ]; then
  echo "$FAILED setup step(s) failed"
  exit 1
fi

# Repo About block from docs/GITHUB_ABOUT.md
if [ -f docs/GITHUB_ABOUT.md ]; then
  ABOUT_DESC="$(python3 - <<'PY'
import re
from pathlib import Path
text = Path("docs/GITHUB_ABOUT.md").read_text(encoding="utf-8")
# Prefer Child Project Draft when placeholders are replaced; else template description.
child = re.search(r"## Child Project Draft\s*\n\s*(.+)", text)
if child and "[PROJECT_NAME]" not in child.group(1):
    print(child.group(1).strip()[:350])
else:
    m = re.search(r"## Template Repo Description[^\n]*\n\s*(.+)", text)
    print((m.group(1).strip() if m else "")[:350])
PY
)"
  ABOUT_TOPICS="$(python3 - <<'PY'
import re
from pathlib import Path
text = Path("docs/GITHUB_ABOUT.md").read_text(encoding="utf-8")
m = re.search(r"## Topics\s*\n\s*(.+)", text)
if not m:
    print("")
else:
    topics = [t.strip() for t in m.group(1).split(",") if t.strip()]
    print(",".join(topics[:10]))
PY
)"
  if [ -n "$ABOUT_DESC" ]; then
    if gh repo edit "$REPO" --description "$ABOUT_DESC" 2>/dev/null; then
      echo "OK   GitHub About description updated"
    else
      echo "WARN could not update GitHub About description"
    fi
    if [ -n "$ABOUT_TOPICS" ]; then
      IFS=',' read -ra TOPIC_ARR <<< "$ABOUT_TOPICS"
      for topic in "${TOPIC_ARR[@]}"; do
        topic="$(echo "$topic" | xargs)"
        [ -z "$topic" ] && continue
        gh repo edit "$REPO" --add-topic "$topic" 2>/dev/null || true
      done
      echo "OK   GitHub topics updated"
    fi
  fi
fi

echo "GitHub repo security setup complete for ${REPO}"
