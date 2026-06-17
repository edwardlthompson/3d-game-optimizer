#!/usr/bin/env bash
# Set STEAM_SYNC_WORKER_URL repository variable and rebuild GitHub Pages when it changes.
# Usage: scripts/sync-steam-worker-pages.sh <worker-base-url> [owner/repo] [ref]
# Requires: gh CLI; GITHUB_TOKEN or GH_TOKEN with actions:write on the repo.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

WORKER_URL="${1:-}"
REPO="${2:-${GITHUB_REPOSITORY:-}}"
REF="${3:-${GITHUB_REF_NAME:-main}}"

if [ -z "$WORKER_URL" ]; then
  echo "ERROR: worker base URL required"
  echo "Usage: scripts/sync-steam-worker-pages.sh <worker-base-url> [owner/repo] [ref]"
  exit 1
fi

if [ -z "$REPO" ]; then
  if ! command -v gh >/dev/null 2>&1; then
    echo "ERROR: gh CLI required (https://cli.github.com/)"
    exit 1
  fi
  REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"
fi

if [ -z "$REPO" ]; then
  echo "ERROR: pass owner/repo or set GITHUB_REPOSITORY"
  exit 1
fi

# Normalize: no trailing slash (matches site/catalog steamSyncWorkerUrl()).
WORKER_URL="${WORKER_URL%/}"

if ! [[ "$WORKER_URL" =~ ^https:// ]]; then
  echo "ERROR: worker URL must start with https:// (got: $WORKER_URL)"
  exit 1
fi

current=""
if current="$(gh api "repos/${REPO}/actions/variables/STEAM_SYNC_WORKER_URL" --jq '.value' 2>/dev/null)"; then
  :
else
  current=""
fi

if [ "$current" = "$WORKER_URL" ]; then
  echo "OK   STEAM_SYNC_WORKER_URL unchanged ($WORKER_URL) — skip Pages rebuild"
  exit 0
fi

if [ -n "$current" ]; then
  echo "Update STEAM_SYNC_WORKER_URL: $current -> $WORKER_URL"
  gh api --method PATCH "repos/${REPO}/actions/variables/STEAM_SYNC_WORKER_URL" \
    -f value="$WORKER_URL" >/dev/null
else
  echo "Create STEAM_SYNC_WORKER_URL=$WORKER_URL"
  gh api --method POST "repos/${REPO}/actions/variables" \
    -f name=STEAM_SYNC_WORKER_URL \
    -f value="$WORKER_URL" >/dev/null
fi

echo "Dispatch GitHub Pages workflow on ref $REF"
gh workflow run "GitHub Pages" --repo "$REPO" --ref "$REF"
echo "OK   Pages rebuild triggered"
