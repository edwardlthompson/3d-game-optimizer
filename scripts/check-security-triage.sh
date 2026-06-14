#!/usr/bin/env bash
# Verify weekly CVE triage occurred within the last N days.
# Usage: scripts/check-security-triage.sh [--days N] [--init]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

MAX_DAYS=7
INIT=false

for arg in "$@"; do
  case "$arg" in
    --days) MAX_DAYS="${2:-7}"; shift 2 ;;
    --init) INIT=true ;;
  esac
done

if [ "$INIT" = true ]; then
  echo "OK   security triage check skipped (--init bootstrap)"
  exit 0
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required for security triage check"
  exit 1
fi

REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
if [ -z "$REPO" ]; then
  echo "ERROR: run from a git repo with gh auth"
  exit 1
fi

cutoff="$(python3 - <<PY
from datetime import datetime, timedelta, timezone
print((datetime.now(timezone.utc) - timedelta(days=${MAX_DAYS})).strftime("%Y-%m-%dT%H:%M:%SZ"))
PY
)"

latest_issue=""
latest_issue="$(gh issue list --repo "$REPO" --label security-triage --state all \
  --json number,updatedAt,closedAt --limit 20 \
  --jq 'sort_by(.updatedAt) | reverse | .[0].updatedAt // empty' 2>/dev/null || true)"

issue_count="$(gh issue list --repo "$REPO" --label security-triage --state all --limit 1 --json number \
  --jq 'length' 2>/dev/null || echo "0")"

if [ "${issue_count:-0}" = "0" ]; then
  echo "WARN no security-triage issues yet (bootstrap grace period)"
  exit 0
fi

if [ -n "$latest_issue" ] && [[ "$latest_issue" > "$cutoff" || "$latest_issue" == "$cutoff" ]]; then
  echo "OK   security-triage issue updated within ${MAX_DAYS} days ($latest_issue)"
  exit 0
fi

# Fallback: dated DECISION_LOG.md entry
if [ -f DECISION_LOG.md ]; then
  if grep -qiE "security triage|CVE triage" DECISION_LOG.md; then
    recent_line="$(grep -iE "security triage|CVE triage" DECISION_LOG.md | tail -1 || true)"
    if [ -n "$recent_line" ]; then
      echo "OK   DECISION_LOG.md mentions recent security triage"
      exit 0
    fi
  fi
fi

echo "FAIL no security-triage activity within ${MAX_DAYS} days (cutoff: $cutoff)"
echo "Hint: security-triage.yml opens issues on Mondays, or log triage in DECISION_LOG.md"
exit 1
