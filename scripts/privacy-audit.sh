#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
echo "=== privacy-audit ==="
FAIL=0
for pattern in ApplicationInsights Microsoft.AppCenter Sentry Google.Analytics; do
  if rg -q "$pattern" src/SpatialLabsOptimizer --glob '*.cs' 2>/dev/null; then
    echo "FAIL: Forbidden SDK: $pattern"; FAIL=1
  fi
  if rg -q "$pattern" src/SpatialLabsOptimizer.ElevatedHelper --glob '*.cs' 2>/dev/null; then
    echo "FAIL: Forbidden SDK in ElevatedHelper: $pattern"; FAIL=1
  fi
done
for pattern in HtmlAgilityPack AngleSharp Selenium Playwright; do
  if rg -q "$pattern" src/ --glob '*.csproj' 2>/dev/null; then
    echo "FAIL: HTML scraping package: $pattern"; FAIL=1
  fi
done
HTTPCLIENT_FILES=$(rg -l 'new HttpClient' src/SpatialLabsOptimizer src/SpatialLabsOptimizer.ElevatedHelper --glob '*.cs' 2>/dev/null || true)
ALLOWED_HTTPCLIENT='Infrastructure/Privacy|Infrastructure/Data/ExternalDataGateway|ElevatedHelper/Program'
for file in $HTTPCLIENT_FILES; do
  if ! echo "$file" | rg -q "$ALLOWED_HTTPCLIENT"; then
    echo "FAIL: HttpClient outside PrivacyGuard: $file"; FAIL=1
  fi
done
if rg -q 'Console\.WriteLine.*password|Console\.WriteLine.*token' src/SpatialLabsOptimizer.ElevatedHelper --glob '*.cs' -i 2>/dev/null; then
  echo "FAIL: Possible secret logging in ElevatedHelper"; FAIL=1
fi
if [ "$FAIL" -ne 0 ]; then exit 1; fi
echo "privacy-audit passed"

