#!/usr/bin/env bash
# README health: GFM tables, relative links, shields.io badge reachability.
# Usage: scripts/check-readme-health.sh [--quick]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

QUICK=false
for arg in "$@"; do
  case "$arg" in
    --quick) QUICK=true ;;
  esac
done

ERRORS=0

if [ ! -f README.md ]; then
  echo "MISSING: README.md"
  exit 1
fi

if ! bash scripts/check-markdown-tables.sh; then
  ERRORS=$((ERRORS + 1))
fi

# Relative markdown links: [text](path)
while IFS= read -r link; do
  [ -z "$link" ] && continue
  case "$link" in
    http://*) continue ;;
    https://*) continue ;;
    mailto:*) continue ;;
    \#*) continue ;;
  esac
  link="${link%%#*}"
  if [ -z "$link" ]; then
    continue
  fi
  if [ ! -e "$link" ]; then
    echo "BROKEN relative link in README.md: $link"
    ERRORS=$((ERRORS + 1))
  fi
done < <(grep -oE '\]\([^)]+\)' README.md | sed 's/^](//;s/)$//' | sort -u)

# Badge URLs (shields.io and img.shields.io)
if [ "$QUICK" = false ]; then
  while IFS= read -r url; do
    [ -z "$url" ] && continue
    code="$(curl -fsSL -o /dev/null -w '%{http_code}' --max-time 15 --retry 2 "$url" 2>/dev/null || echo "000")"
    if [ "$code" -lt 200 ] || [ "$code" -ge 400 ]; then
      echo "BADGE unreachable ($code): $url"
      ERRORS=$((ERRORS + 1))
    else
      echo "OK   badge: $url"
    fi
  done < <(grep -oE 'https://img\.shields\.io/[^)]+' README.md | sort -u)
else
  echo "SKIP badge HTTP checks (--quick)"
fi

if [ -f .template-version ]; then
  version="$(tr -d '[:space:]' < .template-version)"
  if grep -q "template-${version}" README.md 2>/dev/null; then
    echo "OK   README references template-${version}"
  elif grep -q 'img.shields.io/badge/template-' README.md 2>/dev/null; then
    echo "WARN README has template badge but version sync not verified for ${version}"
  fi
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "${ERRORS} README health check(s) failed"
  exit 1
fi

echo "README health check passed"
