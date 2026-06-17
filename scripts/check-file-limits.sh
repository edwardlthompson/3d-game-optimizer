#!/usr/bin/env bash
# Enforce file line limits: 250 for views, 150 for logic
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VIEW_LIMIT=250
LOGIC_LIMIT=150
ERRORS=0
WARNINGS=0
EXEMPTIONS_FILE="$ROOT/scripts/file-limit-exemptions.txt"

is_exempt() {
  local rel="$1"
  [ -f "$EXEMPTIONS_FILE" ] && grep -qxF "$rel" "$EXEMPTIONS_FILE"
}

check_file() {
  local file="$1"
  local limit="$2"
  local label="$3"
  local rel="${file#"$ROOT"/}"

  lines=$(wc -l < "$file" | tr -d ' ')
  if [ "$lines" -le "$limit" ]; then
    return 0
  fi

  if is_exempt "$rel"; then
    echo "WARN [exempt $label] $rel: $lines lines (max $limit)"
    WARNINGS=$((WARNINGS + 1))
    return 0
  fi

  echo "FAIL [$label] $rel: $lines lines (max $limit)"
  ERRORS=$((ERRORS + 1))
}

check_web_views() {
  while IFS= read -r -d '' file; do
    check_file "$file" "$VIEW_LIMIT" "view"
  done < <(find "$ROOT" -type f \( -name "*.tsx" -o -name "*.jsx" -o -name "*.vue" -o -name "*_view.*" \) \
    ! -path "*/node_modules/*" ! -path "*/.venv/*" ! -path "*/.git/*" ! -path "*/dist/*" -print0 2>/dev/null)
}

check_example_logic() {
  while IFS= read -r -d '' file; do
    check_file "$file" "$LOGIC_LIMIT" "logic"
  done < <(find "$ROOT/examples" -type f \( -name "*.ts" -o -name "*.py" -o -name "*.kt" \) \
    ! -name "*.test.*" ! -name "*.spec.*" ! -path "*/node_modules/*" ! -path "*/.venv/*" ! -path "*/.git/*" -print0 2>/dev/null)
}

check_winui_csharp() {
  local product="$ROOT/src/SpatialLabsOptimizer"
  [ -d "$product" ] || return 0

  while IFS= read -r -d '' file; do
    rel="${file#"$ROOT"/}"
    case "$rel" in
      */Views/*|*/ViewModels/*)
        check_file "$file" "$VIEW_LIMIT" "csharp-view"
        ;;
      *)
        check_file "$file" "$LOGIC_LIMIT" "csharp-logic"
        ;;
    esac
  done < <(find "$product" -type f -name "*.cs" \
    ! -path "*/obj/*" ! -path "*/bin/*" -print0 2>/dev/null)
}

echo "Checking web view file limits (max $VIEW_LIMIT lines)..."
check_web_views

echo "Checking WinUI/C# file limits (views max $VIEW_LIMIT, logic max $LOGIC_LIMIT)..."
check_winui_csharp

echo "Checking example logic file limits (max $LOGIC_LIMIT lines)..."
check_example_logic

echo "Checking catalog site logic file limits (max $LOGIC_LIMIT lines)..."
if [ -d "$ROOT/site/catalog/src" ]; then
  while IFS= read -r -d '' file; do
    check_file "$file" "$LOGIC_LIMIT" "catalog-logic"
  done < <(find "$ROOT/site/catalog/src" -type f -name "*.ts" \
    ! -name "*.test.*" ! -name "*.spec.*" ! -path "*/node_modules/*" -print0 2>/dev/null)
fi

if [ "$WARNINGS" -gt 0 ]; then
  echo "$WARNINGS exempt file(s) still exceed limits (tracked in scripts/file-limit-exemptions.txt)"
fi

if [ "$ERRORS" -gt 0 ]; then
  echo "$ERRORS file(s) exceed line limits"
  exit 1
fi

echo "All file line limits OK"
