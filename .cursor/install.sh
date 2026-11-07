#!/usr/bin/env bash
# Cloud Agent repository bootstrap for 3D Game Optimizer.
# Installs dependencies for the Linux-viable product stacks:
#   - site/catalog          (Vite/TypeScript public catalog)
#   - workers/steam-library (Cloudflare Worker sync service)
#   - scripts/sync-catalog  (Python catalog pipeline)
# The WinUI desktop app (src/, SpatialLabsOptimizer.sln) is Windows-only and
# is not built here; CI covers it on windows-latest.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

echo "==> site/catalog: npm ci"
( cd site/catalog && npm ci )

echo "==> workers/steam-library: npm ci"
( cd workers/steam-library && npm ci )

# Refresh the catalog data + integrity hash the dev server serves from
# site/catalog/public/data. The catalog UI verifies catalog-v2.json against a
# sibling catalog-v2.sha256; the GitHub Pages build generates that hash before
# publishing (compute-catalog-hash.py). `npm run dev` does not, so without this
# the dev server returns the SPA index.html fallback for the missing hash file
# and the integrity check fails, blocking the catalog from loading locally.
echo "==> site/catalog: refresh served catalog data + integrity hash (dev)"
( cd site/catalog
  node scripts/copy-catalog.mjs
  node -e 'const c=require("crypto"),f=require("fs");const p="public/data/catalog-v2.json";const h=c.createHash("sha256").update(f.readFileSync(p)).digest("hex");f.writeFileSync("public/data/catalog-v2.sha256",h+"  catalog-v2.json\n");console.log("catalog-v2.sha256:",h)' )

echo "==> scripts/sync-catalog: pip install requirements"
python3 -m pip install --user --disable-pip-version-check -r scripts/sync-catalog/requirements-catalog.txt

echo "==> install complete"
