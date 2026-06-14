#!/usr/bin/env bash
# Block third-party vendor logos in shipped app assets (legal policy).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ASSETS="$ROOT/src/SpatialLabsOptimizer/Assets"

if [ ! -d "$ASSETS" ]; then
  echo "No Assets folder — skip logo audit."
  exit 0
fi

BLOCKED=("acer" "samsung" "nvidia" "steam" "valve" "wemod" "reshade")
FOUND=0

while IFS= read -r -d '' file; do
  base=$(basename "$file" | tr '[:upper:]' '[:lower:]')
  for term in "${BLOCKED[@]}"; do
    if [[ "$base" == *"$term"* ]]; then
      echo "BLOCKED asset name: $file"
      FOUND=1
    fi
  done
done < <(find "$ASSETS" -type f \( -iname '*.png' -o -iname '*.jpg' -o -iname '*.svg' -o -iname '*.webp' \) -print0 2>/dev/null || true)

if [ "$FOUND" -eq 1 ]; then
  echo "Logo audit failed — rename or remove vendor-branded assets."
  exit 1
fi

echo "Logo audit passed."
