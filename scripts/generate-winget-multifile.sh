#!/usr/bin/env bash
# Generate Winget 1.6.0 multi-file manifest triplet for microsoft/winget-pkgs PRs.
# Usage: generate-winget-multifile.sh <Version> [InstallerPath] [InstallerType]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

VERSION="${1:?Version required}"
INSTALLER_PATH="${2:-}"
INSTALLER_TYPE="${3:-}"

PKG_ID="edwardlthompson.SpatialLabsOptimizer"
OUT="packaging/winget-product/multifile/${VERSION}"
mkdir -p "$OUT"

INSTALLER_URL="https://github.com/edwardlthompson/3d-game-optimizer/releases/download/SpatialLabsOptimizer-v${VERSION}/SpatialLabsOptimizer-${VERSION}-win-x64.zip"
INSTALLER_SHA256="REPLACE_WITH_SHA256"

if [ -z "$INSTALLER_TYPE" ]; then
  case "$INSTALLER_PATH" in
    *.msi) INSTALLER_TYPE="msi" ;;
    *.zip) INSTALLER_TYPE="zip" ;;
    *) INSTALLER_TYPE="zip" ;;
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

cat > "$OUT/${PKG_ID}.yaml" <<EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json
PackageIdentifier: ${PKG_ID}
PackageVersion: ${VERSION}
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
EOF

cat > "$OUT/${PKG_ID}.locale.en-US.yaml" <<EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.defaultLocale.1.6.0.schema.json
PackageIdentifier: ${PKG_ID}
PackageVersion: ${VERSION}
PackageLocale: en-US
Publisher: Edward Thompson
PublisherUrl: https://github.com/edwardlthompson
PublisherSupportUrl: https://github.com/edwardlthompson/3d-game-optimizer/issues
PackageName: 3D Game Optimizer
PackageUrl: https://github.com/edwardlthompson/3d-game-optimizer
License: MIT
LicenseUrl: https://github.com/edwardlthompson/3d-game-optimizer/blob/main/LICENSE
ShortDescription: One-click glasses-free 3D PC gaming hub for SpatialLabs and Odyssey 3D displays
Description: |
  3D Game Optimizer (SpatialLabs Optimizer) is a glasses-free 3D PC gaming hub.
  It automates display detection, silent toolchain setup, and one-click Play in 3D launches.
Moniker: 3d-game-optimizer
Tags:
  - gaming
  - 3d
  - spatial
ManifestType: defaultLocale
ManifestVersion: 1.6.0
EOF

if [ "$INSTALLER_TYPE" = "zip" ]; then
  cat > "$OUT/${PKG_ID}.installer.yaml" <<EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: ${PKG_ID}
PackageVersion: ${VERSION}
InstallerLocale: en-US
Platform:
  - Windows.Desktop
InstallerType: zip
NestedInstallerType: portable
NestedInstallerFiles:
  - RelativeFilePath: SpatialLabsOptimizer.exe
    PortableCommandAlias: SpatialLabsOptimizer
Installers:
  - Architecture: x64
    InstallerUrl: ${INSTALLER_URL}
    InstallerSha256: ${INSTALLER_SHA256}
ManifestType: installer
ManifestVersion: 1.6.0
EOF
else
  cat > "$OUT/${PKG_ID}.installer.yaml" <<EOF
# yaml-language-server: \$schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: ${PKG_ID}
PackageVersion: ${VERSION}
InstallerLocale: en-US
Platform:
  - Windows.Desktop
Installers:
  - Architecture: x64
    InstallerType: ${INSTALLER_TYPE}
    InstallerUrl: ${INSTALLER_URL}
    InstallerSha256: ${INSTALLER_SHA256}
ManifestType: installer
ManifestVersion: 1.6.0
EOF
fi

echo "Wrote Winget multifile manifest to $OUT"
ls -la "$OUT"
