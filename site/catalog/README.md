# 3D Game Catalog (GitHub Pages)

Public sortable browser for `catalog-v2.json` — deployed to `/catalog/` on GitHub Pages.

## Develop

```bash
python3 scripts/sync-catalog/merge-catalog.py
cd site/catalog
npm ci
npm run dev
```

`prebuild` copies `data/compatibility/catalog-v2.json` into `public/data/`.

## Build

```bash
npm run build
```

Set `VITE_BASE_PATH=/your-repo-name/catalog/` for production (CI sets this automatically).
