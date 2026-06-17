#!/usr/bin/env bash
# Enforce ADR-0005: no SteamDB scraping/import in automation paths.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

FAIL=0

echo "=== check-steamdb-policy ==="

ADR="docs/adr/0005-steamdb-price-history.md"
if [ ! -f "$ADR" ]; then
  echo "FAIL missing $ADR"
  exit 1
fi

if ! grep -qE '^Accepted$' "$ADR" && ! grep -qE '^## Status' "$ADR"; then
  echo "FAIL $ADR missing Status section"
  FAIL=1
else
  status="$(awk '/^## Status/{found=1; next} found && NF {sub(/^[ \t]+/,""); print; exit}' "$ADR" | tr -d '\r')"
  if [ "$status" != "Accepted" ]; then
    echo "FAIL $ADR Status must be Accepted (got: ${status:-empty})"
    FAIL=1
  else
    echo "OK   ADR-0005 Accepted"
  fi
fi

# Block steamdb.info in automation / sync code (allow docs, ADR, plans, comments in catalog UI)
FORBIDDEN_PATHS=(
  scripts/sync-catalog
  scripts
  .github/workflows
  site/catalog/src
  workers
)

for dir in "${FORBIDDEN_PATHS[@]}"; do
  [ -d "$dir" ] || continue
  while IFS= read -r hit; do
    case "$hit" in
      *check-steamdb-policy.sh*) continue ;;
      *data-coverage.ts*) continue ;;
    esac
    echo "FAIL steamdb reference in automation path: $hit"
    FAIL=1
  done < <(grep -RIl --include='*.py' --include='*.sh' --include='*.ps1' --include='*.yml' --include='*.yaml' --include='*.ts' --include='*.js' \
    -i 'steamdb\.info\|scrape.*steamdb\|crawl.*steamdb' "$dir" 2>/dev/null || true)
done

if grep -qi 'steamdb' scripts/sync-catalog/append-price-history.py 2>/dev/null; then
  echo "FAIL append-price-history.py must not reference SteamDB"
  FAIL=1
else
  echo "OK   append-price-history.py Steam-only"
fi

if ! grep -q 'self-tracked' site/catalog/src/data-coverage.ts; then
  echo "FAIL data-coverage.ts must document self-tracked price history"
  FAIL=1
else
  echo "OK   catalog footer documents self-tracked prices"
fi

if [ "$FAIL" -ne 0 ]; then
  echo "check-steamdb-policy failed"
  exit 1
fi

echo "check-steamdb-policy passed"
