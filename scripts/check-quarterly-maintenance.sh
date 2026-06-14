#!/usr/bin/env bash
# Quarterly maintenance: ADR status, DECISION_LOG recency, Dependabot exception doc.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

FAIL=0
MAX_DAYS="${QUARTERLY_MAX_DAYS:-120}"

echo "=== check-quarterly-maintenance ==="

if ! bash scripts/check-adr-status.sh; then
  echo "FAIL: ADR status check"
  FAIL=1
fi

if [ -f DECISION_LOG.md ]; then
  if git log -1 --format=%ct -- DECISION_LOG.md 2>/dev/null | grep -q .; then
    last_ts="$(git log -1 --format=%ct -- DECISION_LOG.md)"
    now_ts="$(date +%s)"
    age_days=$(( (now_ts - last_ts) / 86400 ))
    if [ "$age_days" -gt "$MAX_DAYS" ]; then
      echo "WARN DECISION_LOG.md last commit ${age_days}d ago (threshold ${MAX_DAYS}d)"
    else
      echo "OK   DECISION_LOG.md updated within ${age_days}d"
    fi
  else
    echo "WARN DECISION_LOG.md not in git history"
  fi
else
  echo "WARN DECISION_LOG.md missing"
fi

if [ -f docs/DEPENDABOT_EXCEPTIONS.md ]; then
  echo "OK   Dependabot exceptions documented"
else
  echo "OK   No Dependabot exceptions file (optional)"
fi

if command -v gh >/dev/null 2>&1; then
  bash scripts/check-release-credentials.sh 2>/dev/null || echo "WARN check-release-credentials failed"
fi

if [ "$FAIL" -ne 0 ]; then exit 1; fi
echo "check-quarterly-maintenance passed"
