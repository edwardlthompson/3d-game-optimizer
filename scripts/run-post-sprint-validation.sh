#!/usr/bin/env bash
# Wrapper for run-post-sprint-validation.ps1 on Unix CI.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
bash scripts/check-file-encoding.sh
case "$(uname -s)" in
  Linux|Darwin)
    echo "NOTE: dotnet test skipped on $(uname -s) — WinUI tests run on windows-latest CI only"
    ;;
  *)
    dotnet build SpatialLabsOptimizer.sln -c Release --verbosity minimal
    dotnet test SpatialLabsOptimizer.sln -c Release --no-build --verbosity minimal
    ;;
esac
bash scripts/check-file-limits.sh
bash scripts/check-local-release-scripts.sh
bash scripts/check-qa-matrix-coverage.sh
bash scripts/check-compatibility-seed.sh
bash scripts/build-verification-gate.sh --quick --skip-dotnet --skip-pre-commit
python3 scripts/generate-brand-assets.py
echo "run-post-sprint-validation passed"
