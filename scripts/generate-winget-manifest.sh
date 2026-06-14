#!/usr/bin/env bash
# Generate a Winget manifest stub for desktop binaries.
# Usage: generate-winget-manifest.sh <PackageIdentifier> <Version> [OutputDir] [InstallerPath] [InstallerType]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

PKG_ID="${1:-Example.Project.GoldenPath}"
VERSION="${2:-0.0.0}"
OUT_DIR="${3:-packaging/winget}"
INSTALLER_PATH="${4:-}"
INSTALLER_TYPE="${5:-}"

mkdir -p "$OUT_DIR"

INSTALLER_URL="https://example.com/releases/$VERSION/setup.exe"
INSTALLER_SHA256="REPLACE_WITH_SHA256"

if [ -z "$INSTALLER_TYPE" ]; then
  case "$INSTALLER_PATH" in
    *.msi) INSTALLER_TYPE="msi" ;;
    *.zip) INSTALLER_TYPE="zip" ;;
    *.exe) INSTALLER_TYPE="exe" ;;
    *) INSTALLER_TYPE="exe" ;;
  esac
fi

if [ -n "$INSTALLER_PATH" ] && [ -f "$INSTALLER_PATH" ]; then
  case "$INSTALLER_TYPE" in
    msi)
      INSTALLER_URL="https://github.com/edwardlthompson/3d-game-optimizer/releases/download/SpatialLabsOptimizer-v${VERSION}/SpatialLabsOptimizer-${VERSION}-win-x64.msi"
      ;;
    zip)
      INSTALLER_URL="https://github.com/edwardlthompson/3d-game-optimizer/releases/download/SpatialLabsOptimizer-v${VERSION}/SpatialLabsOptimizer-${VERSION}-win-x64.zip"
      ;;
    *)
      INSTALLER_URL="https://github.com/edwardlthompson/3d-game-optimizer/releases/download/SpatialLabsOptimizer-v${VERSION}/$(basename "$INSTALLER_PATH")"
      ;;
  esac
  if command -v sha256sum >/dev/null 2>&1; then
    INSTALLER_SHA256="$(sha256sum "$INSTALLER_PATH" | awk '{print $1}')"
  elif command -v shasum >/dev/null 2>&1; then
    INSTALLER_SHA256="$(shasum -a 256 "$INSTALLER_PATH" | awk '{print $1}')"
  fi
fi

cat > "$OUT_DIR/manifest.stub.yaml" <<EOF
# Winget manifest stub — customize before submitting to microsoft/winget-pkgs
PackageIdentifier: $PKG_ID
PackageVersion: $VERSION
PackageLocale: en-US
Publisher: Edward Thompson
PackageName: 3D Game Optimizer
License: MIT
ShortDescription: One-click glasses-free 3D PC gaming hub for SpatialLabs and Odyssey 3D displays
Installers:
  - Architecture: x64
    InstallerType: $INSTALLER_TYPE
    InstallerUrl: $INSTALLER_URL
    InstallerSha256: $INSTALLER_SHA256
ManifestType: singleton
ManifestVersion: 1.6.0
EOF

echo "Wrote $OUT_DIR/manifest.stub.yaml"
echo "See https://github.com/microsoft/winget-pkgs for submission guidelines."
