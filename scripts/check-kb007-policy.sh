#!/usr/bin/env bash
# Verify KB-007 transitive CVE policy: npm overrides present, major Dependabot gated.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ERRORS=0

if [ ! -f KNOWLEDGE_BASE.md ] || ! grep -q "KB-007" KNOWLEDGE_BASE.md; then
  echo "MISSING: KB-007 in KNOWLEDGE_BASE.md"
  ERRORS=$((ERRORS + 1))
fi

if [ -f examples/web/package.json ] && ! grep -q '"overrides"' examples/web/package.json; then
  echo "WARN examples/web/package.json has no npm overrides (add when transitive CVEs appear)"
fi

if [ ! -f .github/workflows/dependabot-automerge.yml ]; then
  echo "MISSING: dependabot-automerge.yml"
  ERRORS=$((ERRORS + 1))
elif ! grep -q "semver-major" .github/workflows/dependabot-automerge.yml; then
  echo "FAIL dependabot-automerge.yml missing major-version gate"
  ERRORS=$((ERRORS + 1))
elif ! grep -q "HUMAN" .github/workflows/dependabot-automerge.yml; then
  echo "FAIL dependabot-automerge.yml missing HUMAN label requirement for major bumps"
  ERRORS=$((ERRORS + 1))
else
  echo "OK   Dependabot auto-merge gates major bumps (KB-007)"
fi

if [ "$ERRORS" -gt 0 ]; then
  exit 1
fi

echo "KB-007 policy check passed"
