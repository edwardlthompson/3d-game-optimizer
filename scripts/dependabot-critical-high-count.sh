#!/usr/bin/env bash
# Print open Critical/High Dependabot alert count for owner/repo.
# Exit 0 with integer on stdout; exit 1 when the API is unavailable.
set -euo pipefail

REPO="${1:-}"
if [ -z "$REPO" ]; then
  echo "Usage: dependabot-critical-high-count.sh owner/repo" >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  exit 1
fi

count="$(gh api "repos/${REPO}/dependabot/alerts?state=open&per_page=100" \
  --jq '[.[] | select(.security_vulnerability.severity == "critical" or .security_vulnerability.severity == "high")] | length' 2>/dev/null)" || exit 1

if ! [[ "$count" =~ ^[0-9]+$ ]]; then
  exit 1
fi

echo "$count"
