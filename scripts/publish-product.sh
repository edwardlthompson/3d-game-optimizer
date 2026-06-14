#!/usr/bin/env bash
# Publish self-contained win-x64 product bundle (app + ElevatedHelper + data).
# Usage: scripts/publish-product.sh [Configuration] [OutputDir]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

CONFIG="${1:-Release}"
OUT_DIR="${2:-artifacts/product-win-x64}"

APP_PROJ="src/SpatialLabsOptimizer/SpatialLabsOptimizer.csproj"
HELPER_PROJ="src/SpatialLabsOptimizer.ElevatedHelper/SpatialLabsOptimizer.ElevatedHelper.csproj"
STAGING="$OUT_DIR/staging"
PUBLISH_APP="$STAGING/app"
PUBLISH_HELPER="$STAGING/helper"

rm -rf "$STAGING"
mkdir -p "$PUBLISH_APP" "$PUBLISH_HELPER"

echo "=== publish-product ($CONFIG) ==="

dotnet publish "$APP_PROJ" \
  -c "$CONFIG" \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o "$PUBLISH_APP"

dotnet publish "$HELPER_PROJ" \
  -c "$CONFIG" \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o "$PUBLISH_HELPER"

HELPER_EXE="$PUBLISH_HELPER/SpatialLabsOptimizer.ElevatedHelper.exe"
if [ ! -f "$HELPER_EXE" ]; then
  echo "FAIL: ElevatedHelper not found at $HELPER_EXE"
  exit 1
fi

cp "$HELPER_EXE" "$PUBLISH_APP/SpatialLabsOptimizer.ElevatedHelper.exe"

VERSION="$(python3 -c "import json; print(json.load(open('src/SpatialLabsOptimizer/product-version.json'))['version'])")"
ZIP_NAME="SpatialLabsOptimizer-${VERSION}-win-x64.zip"
mkdir -p "$OUT_DIR"
( cd "$PUBLISH_APP" && zip -r "../../$ZIP_NAME" . )

echo "Wrote $OUT_DIR/$ZIP_NAME"
echo "Helper present: $(test -f "$PUBLISH_APP/SpatialLabsOptimizer.ElevatedHelper.exe" && echo yes || echo no)"
