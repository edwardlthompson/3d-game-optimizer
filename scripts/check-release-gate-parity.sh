#!/usr/bin/env bash
# Verify release-auto-merge.yml and pre-release-gate.sh flag usage stay documented.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

FAIL=0

if ! rg -q '\-\-skip-triage' .github/workflows/release-auto-merge.yml; then
  echo "WARN: release-auto-merge.yml no longer uses --skip-triage"
fi

if ! rg -q 'skip-triage|skip-dotnet' scripts/pre-release-gate.sh; then
  echo "FAIL: pre-release-gate.sh missing documented skip flags"
  FAIL=1
fi

if ! rg -q 'skip-triage|pre-release-gate' docs/RUNBOOK.md; then
  echo "FAIL: RUNBOOK.md missing pre-release gate parity documentation"
  FAIL=1
fi

if [ "$FAIL" -ne 0 ]; then
  exit 1
fi

echo "check-release-gate-parity passed"
