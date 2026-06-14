#!/usr/bin/env bash
# Local product release orchestrator (bash wrapper for build-product-local.ps1).
# Usage: scripts/build-product-local.sh [--skip-gate] [--sign] [--skip-msix] [--skip-msi]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ARGS=()
while [ $# -gt 0 ]; do
  case "$1" in
    --skip-gate) ARGS+=("-SkipGate"); shift ;;
    --sign) ARGS+=("-Sign"); shift ;;
    --skip-msix) ARGS+=("-SkipMsix"); shift ;;
    --skip-msi) ARGS+=("-SkipMsi"); shift ;;
    --pfx-path)
      ARGS+=("-PfxPath" "${2:-}")
      shift 2
      ;;
    --pfx-password)
      ARGS+=("-PfxPassword" "${2:-}")
      shift 2
      ;;
    -h|--help)
      echo "Usage: $0 [--skip-gate] [--sign] [--skip-msix] [--skip-msi] [--pfx-path PATH] [--pfx-password PASS]"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

pwsh -NoProfile -File scripts/build-product-local.ps1 "${ARGS[@]}"
