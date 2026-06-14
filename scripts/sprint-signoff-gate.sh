#!/usr/bin/env bash
# Sprint sign-off gate: local build verification + GitHub CI green on HEAD.
# Usage: scripts/sprint-signoff-gate.sh [--quick] [--wait SECONDS]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

QUICK=false
WAIT=300

while [ $# -gt 0 ]; do
  case "$1" in
    --quick) QUICK=true; shift ;;
    --wait) WAIT="${2:-300}"; shift 2 ;;
    *) echo "Usage: $0 [--quick] [--wait SECONDS]"; exit 1 ;;
  esac
done

echo "=== Sprint sign-off gate ==="

if [ "$QUICK" = true ]; then
  bash scripts/build-verification-gate.sh --quick --skip-pre-commit
else
  bash scripts/build-verification-gate.sh --skip-pre-commit
fi

bash scripts/check-github-ci.sh HEAD --wait "$WAIT"

echo "Sprint sign-off gate passed"
