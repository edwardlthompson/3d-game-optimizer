#!/usr/bin/env bash
# Warn when stale Release Please PRs remain open beyond SLA.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

MAX_DAYS="${RELEASE_PLEASE_PR_MAX_DAYS:-14}"
FAIL=0

echo "=== check-release-please-pr ==="

if ! command -v gh >/dev/null 2>&1; then
  echo "SKIP gh not installed"
  exit 0
fi

REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
if [ -z "$REPO" ]; then
  echo "SKIP not a gh-authenticated repo"
  exit 0
fi

open_prs="$(gh pr list --state open --json title,createdAt,number \
  --jq '[.[] | select(.title | test("chore\\(main\\): release"; "i"))]' 2>/dev/null || echo "[]")"

count="$(echo "$open_prs" | jq 'length')"
if [ "$count" -eq 0 ]; then
  echo "OK   No open Release Please PRs"
  exit 0
fi

echo "$open_prs" | jq -r '.[] | "\(.number)\t\(.title)\t\(.createdAt)"' | while IFS=$'\t' read -r num title created; do
  created_ts="$(date -d "$created" +%s 2>/dev/null || date -j -f "%Y-%m-%dT%H:%M:%SZ" "$created" +%s 2>/dev/null || echo 0)"
    if [ "$created_ts" -gt 0 ]; then
    age_days=$(( ($(date +%s) - created_ts) / 86400 ))
    if [ "$age_days" -gt "$MAX_DAYS" ]; then
      echo "FAIL PR #$num stale (${age_days}d): $title — release-auto-merge.yml should merge when green"
      FAIL=1
    else
      echo "OK   PR #$num open ${age_days}d: $title"
    fi
  else
    echo "OK   PR #$num: $title"
  fi
done

echo "check-release-please-pr passed"
if [ "$FAIL" -ne 0 ]; then exit 1; fi
