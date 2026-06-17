#!/usr/bin/env bash
# CI-safe subset of out-of-band QA (SteamDB policy + catalog copy).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "=== run-out-of-band-qa (CI) ==="
bash scripts/check-steamdb-policy.sh
echo "run-out-of-band-qa (CI) passed"
