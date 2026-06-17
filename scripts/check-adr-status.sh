#!/usr/bin/env bash
# Fail when BUILD_PLAN lists an ADR approval gate but the ADR Status is not Accepted.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ERRORS=0

read_adr_status() {
  local path="$1"
  local status=""

  if grep -qE '^- \*\*Status:\*\*' "$path"; then
    status="$(grep -E '^- \*\*Status:\*\*' "$path" | head -1 | sed 's/^[^:]*:[[:space:]]*//')"
  elif grep -qE '^## Status' "$path"; then
    status="$(awk '/^## Status/{found=1; next} found && NF {sub(/^[ \t]+/,""); sub(/[ \t]+$/,""); print; exit}' "$path")"
  fi

  status="$(printf '%s' "$status" | tr -d '\r*' | xargs)"
  printf '%s' "$status"
}

check_adr() {
  local num="$1"
  local path
  path="$(find docs/adr -maxdepth 1 -name "${num}-*.md" 2>/dev/null | head -1 || true)"

  if [ -z "$path" ] || [ ! -f "$path" ]; then
    echo "MISSING: ADR file for ${num}"
    ERRORS=$((ERRORS + 1))
    return
  fi

  local status
  status="$(read_adr_status "$path")"

  local short="${num#0}"
  short="${short#0}"
  local blocking=false

  if [ "$num" = "0001" ]; then
    blocking=true
  elif grep -qE "^- \[ \].*Approve ADR-${short}" BUILD_PLAN.md 2>/dev/null; then
    blocking=true
  elif grep -qE "^- \[ \].*Approve ADR-${num}" BUILD_PLAN.md 2>/dev/null; then
    blocking=true
  fi

  if [ "$blocking" = true ] && [ "$status" != "Accepted" ]; then
    echo "FAIL ADR-${short}: Status is '${status:-unknown}' (expected Accepted) — $path"
    ERRORS=$((ERRORS + 1))
  else
    echo "OK   ADR-${short}: ${status:-unknown}"
  fi
}

check_adr "0001"
check_adr "0002"
check_adr "0003"
check_adr "0004"
check_adr "0005"

if [ "$ERRORS" -gt 0 ]; then
  echo "${ERRORS} ADR status check(s) failed"
  exit 1
fi

echo "ADR status check passed"
