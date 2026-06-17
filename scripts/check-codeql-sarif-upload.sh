#!/usr/bin/env bash
# Warn when CodeQL SARIF upload steps failed (continue-on-error in codeql.yml).
# Usage: scripts/check-codeql-sarif-upload.sh [owner/repo] [ref] [--strict]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

STRICT=false
ARGS=()
for arg in "$@"; do
  case "$arg" in
    --strict) STRICT=true ;;
    *) ARGS+=("$arg") ;;
  esac
done

REPO="${ARGS[0]:-${GITHUB_REPO:-}}"
REF="${ARGS[1]:-HEAD}"

if ! command -v gh >/dev/null 2>&1; then
  if [ "$STRICT" = true ]; then
    echo "FAIL gh not installed"
    exit 1
  fi
  echo "SKIP gh not installed"
  exit 0
fi

if [ -z "$REPO" ]; then
  REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
fi
if [ -z "$REPO" ]; then
  if [ "$STRICT" = true ]; then
    echo "FAIL not a gh-authenticated repo"
    exit 1
  fi
  echo "SKIP not a gh-authenticated repo"
  exit 0
fi

COMMIT="$(git rev-parse "$REF" 2>/dev/null || true)"
if [ -z "$COMMIT" ]; then
  if [ "$STRICT" = true ]; then
    echo "FAIL could not resolve ref $REF"
    exit 1
  fi
  echo "SKIP could not resolve ref $REF"
  exit 0
fi

run_id="$(gh run list --repo "$REPO" --workflow codeql.yml --commit "$COMMIT" \
  --json databaseId,conclusion -q '.[0].databaseId' 2>/dev/null || true)"
if [ -z "$run_id" ] || [ "$run_id" = "null" ]; then
  if [ "$STRICT" = true ]; then
    echo "FAIL no CodeQL workflow run on ${COMMIT:0:7}"
    exit 1
  fi
  echo "SKIP no CodeQL workflow run on ${COMMIT:0:7}"
  exit 0
fi

failed_uploads="$(gh api "repos/${REPO}/actions/runs/${run_id}/jobs" --jq '
  [.jobs[].steps[]
    | select(.name | test("Upload CodeQL SARIF"))
    | select(.conclusion == "failure")
  ] | length
' 2>/dev/null || echo 0)"

if [ "${failed_uploads:-0}" -gt 0 ]; then
  run_url="$(gh run view "$run_id" --repo "$REPO" --json url -q .url 2>/dev/null || true)"
  echo "WARN CodeQL SARIF upload failed on ${run_url:-run $run_id} — enable code scanning or run scripts/setup-release-credentials.sh"
  if [ "$STRICT" = true ]; then
    exit 1
  fi
  exit 0
fi

echo "OK   CodeQL SARIF upload succeeded on commit ${COMMIT:0:7}"
