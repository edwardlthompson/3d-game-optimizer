#!/usr/bin/env bash
# Fail fast when workers/steam-library/wrangler.toml still has a placeholder KV namespace id.
# Usage: scripts/check-wrangler-kv-ready.sh [path-to-wrangler.toml]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

WRANGLER="${1:-workers/steam-library/wrangler.toml}"
PLACEHOLDER="00000000000000000000000000000000"

if [ ! -f "$WRANGLER" ]; then
  echo "FAIL missing $WRANGLER"
  exit 1
fi

if grep -q "id = \"$PLACEHOLDER\"" "$WRANGLER"; then
  echo "FAIL $WRANGLER still uses placeholder KV id"
  echo "      Create namespace: cd workers/steam-library && npx wrangler kv namespace create SYNC_KV"
  echo "      Then update [[kv_namespaces]] id in wrangler.toml (see docs/STEAM_CATALOG_SYNC.md)"
  exit 1
fi

echo "OK   wrangler KV namespace id configured in $WRANGLER"
