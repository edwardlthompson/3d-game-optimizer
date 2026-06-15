#!/usr/bin/env bash
# Validate Winget 1.6.0 multi-file manifest triplet structure.
# Usage: validate-winget-manifest.sh <ManifestDirectory>
set -euo pipefail

MANIFEST_DIR="${1:?Manifest directory required}"

if [ ! -d "$MANIFEST_DIR" ]; then
  echo "Manifest directory not found: $MANIFEST_DIR"
  exit 1
fi

shopt -s nullglob
yaml_files=("$MANIFEST_DIR"/*.yaml)
if [ "${#yaml_files[@]}" -lt 3 ]; then
  echo "Expected at least 3 YAML files in $MANIFEST_DIR, found ${#yaml_files[@]}"
  exit 1
fi

require_field() {
  local file="$1"
  local field="$2"
  if ! grep -q "^${field}:" "$file"; then
    echo "Missing ${field} in $(basename "$file")"
    return 1
  fi
}

errors=0
version_file=""
locale_file=""
installer_file=""

for file in "${yaml_files[@]}"; do
  require_field "$file" PackageIdentifier || errors=$((errors + 1))
  require_field "$file" PackageVersion || errors=$((errors + 1))
  require_field "$file" ManifestType || errors=$((errors + 1))
  require_field "$file" ManifestVersion || errors=$((errors + 1))

  case "$(grep '^ManifestType:' "$file" | awk '{print $2}')" in
    version) version_file="$file" ;;
    defaultLocale) locale_file="$file" ;;
    installer) installer_file="$file" ;;
  esac
done

if [ -z "$version_file" ] || [ -z "$locale_file" ] || [ -z "$installer_file" ]; then
  echo "Manifest triplet incomplete (version/defaultLocale/installer)"
  errors=$((errors + 1))
fi

if [ -n "$installer_file" ]; then
  require_field "$installer_file" Installers || errors=$((errors + 1))
  if ! grep -q 'InstallerSha256:' "$installer_file"; then
    echo "Missing InstallerSha256 in $(basename "$installer_file")"
    errors=$((errors + 1))
  fi
  if ! grep -q 'InstallerUrl:' "$installer_file"; then
    echo "Missing InstallerUrl in $(basename "$installer_file")"
    errors=$((errors + 1))
  fi
fi

if [ "$errors" -gt 0 ]; then
  echo "${errors} winget manifest validation error(s)"
  exit 1
fi

if command -v winget >/dev/null 2>&1; then
  winget validate "$MANIFEST_DIR"
else
  echo "winget CLI not found — structural checks passed"
fi

echo "Winget manifest validation passed: $MANIFEST_DIR"
