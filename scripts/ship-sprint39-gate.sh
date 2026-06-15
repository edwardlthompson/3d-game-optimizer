#!/usr/bin/env bash
# Sprint 39 ship gate — delegates to PowerShell orchestrator.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if command -v pwsh >/dev/null 2>&1; then
  PS=pwsh
elif command -v powershell >/dev/null 2>&1; then
  PS=powershell
else
  echo "ERROR: pwsh or powershell required" >&2
  exit 1
fi

exec "$PS" -NoProfile -ExecutionPolicy Bypass -File "$ROOT/scripts/ship-sprint39-gate.ps1" "$@"
