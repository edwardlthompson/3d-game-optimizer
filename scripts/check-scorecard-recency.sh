#!/usr/bin/env bash
# Verify OpenSSF Scorecard workflow succeeded within the last N days.
# Usage: scripts/check-scorecard-recency.sh [--days N]
set -euo pipefail

MAX_DAYS=30
while [ $# -gt 0 ]; do
  case "$1" in
    --days) MAX_DAYS="${2:-30}"; shift 2 ;;
    *) shift ;;
  esac
done

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI required"
  exit 1
fi

REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
if [ -z "$REPO" ]; then
  echo "ERROR: run from a git repo with gh auth"
  exit 1
fi

latest="$(gh run list --repo "$REPO" --workflow "OpenSSF Scorecard" --limit 1 \
  --json conclusion,createdAt --jq '.[0]' 2>/dev/null || echo "{}")"

conclusion="$(printf '%s' "$latest" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('conclusion',''))" 2>/dev/null || true)"
created="$(printf '%s' "$latest" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('createdAt',''))" 2>/dev/null || true)"

if [ -z "$created" ] || [ "$created" = "null" ]; then
  echo "WARN no Scorecard workflow runs yet — dispatch .github/workflows/scorecard.yml"
  exit 0
fi

cutoff="$(python3 - <<PY
from datetime import datetime, timedelta, timezone
print((datetime.now(timezone.utc) - timedelta(days=${MAX_DAYS})).strftime("%Y-%m-%dT%H:%M:%SZ"))
PY
)"

if [ "$conclusion" != "success" ]; then
  echo "FAIL latest Scorecard run conclusion: ${conclusion:-unknown}"
  exit 1
fi

if [[ "$created" < "$cutoff" ]]; then
  echo "FAIL Scorecard last success older than ${MAX_DAYS} days ($created)"
  exit 1
fi

echo "OK   Scorecard succeeded within ${MAX_DAYS} days ($created)"
