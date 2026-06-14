#!/usr/bin/env bash
# Wrapper for run-post-sprint-validation.ps1 on Unix CI.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
bash scripts/check-file-encoding.sh
dotnet test SpatialLabsOptimizer.sln -c Release --verbosity minimal
bash scripts/check-local-release-scripts.sh
bash scripts/check-qa-matrix-coverage.sh
bash scripts/check-compatibility-seed.sh
python3 scripts/generate-brand-assets.py
echo "run-post-sprint-validation passed"
