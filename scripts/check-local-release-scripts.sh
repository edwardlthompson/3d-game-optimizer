#!/usr/bin/env bash
# Verify Sprint 32 local release scripts exist and expose expected parameters.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

FAIL=0

require_file() {
  local path="$1"
  if [ ! -f "$path" ]; then
    echo "FAIL missing $path"
    FAIL=1
  else
    echo "OK   $path"
  fi
}

require_pattern() {
  local file="$1"
  local pattern="$2"
  local label="$3"
  if grep -qE "$pattern" "$file"; then
    echo "OK   $file ($label)"
  else
    echo "FAIL $file missing $label"
    FAIL=1
  fi
}

echo "=== check-local-release-scripts ==="

require_file scripts/sign-product-release.ps1
require_file scripts/codesign-common.ps1
require_file scripts/sign-product-msi.ps1
require_file scripts/publish-product-msi.ps1
require_file scripts/build-product-local.ps1
require_file scripts/build-product-local.sh
require_file scripts/verify-product-signatures.ps1
require_file scripts/check-local-release-prereqs.ps1
require_file packaging/msi/Product.wxs
require_file packaging/msi/Product.wixproj
require_file packaging/msi/README.md
require_file docs/LOCAL_RELEASE.md

require_pattern scripts/sign-product-release.ps1 'StagingDir|PfxPath|PfxPassword' 'local signing params'
require_pattern scripts/codesign-common.ps1 'CODESIGN_PFX_BASE64' 'CI env compat'
require_pattern scripts/publish-product-msi.ps1 'Product\.wixproj|Product\.wxs' 'WiX packaging'
require_pattern scripts/build-product-local.ps1 'SkipGate|SkipMsi' 'orchestrator switches'
require_pattern packaging/msi/Product.wxs 'UpgradeCode' 'fixed UpgradeCode'
require_pattern .github/workflows/product-release.yml 'publish-product-msi|product-msi' 'MSI CI attach'

if [ "$FAIL" -ne 0 ]; then
  exit 1
fi

echo "check-local-release-scripts passed"
