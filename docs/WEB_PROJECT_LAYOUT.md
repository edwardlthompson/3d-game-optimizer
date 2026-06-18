# Web Project Layout

> Where website source, agent documentation, and published output live. Read when your stack includes web or GitHub Pages hosting.

## Folder roles (this product)

| Path | Purpose | Publish to GitHub Pages? |
|------|---------|----------------------------|
| `docs/` | Agent prompts, security playbooks, design guide, ADRs | **No** |
| [`site/catalog/`](../site/catalog/) | **Primary public site** — 3D Game Catalog (Vite, TypeScript, Vitest) | **Yes** at `/catalog/` |
| `site/catalog/dist/` | Catalog production build output | **Yes** (CI artifact) |
| [`examples/web/`](../examples/web/) | Template PWA **demo stub** (Golden Path reference) | **Yes** at repo root path |
| `examples/web/dist/` | Demo build output | **Yes** (merged into Pages artifact) |
| [`workers/steam-library/`](../workers/steam-library/) | Connect Steam API (Cloudflare Worker) | Deployed separately to Cloudflare |
| [`design-tokens/`](../design-tokens/) | Colors, spacing, typography tokens | **No** |

**`docs/` is not your public website.** Agents read `docs/` for project instructions. Never put marketing HTML or the catalog app in `docs/`.

## Golden Path (3D Game Optimizer)

```text
site/catalog/              # PRIMARY — edit catalog browser here
  src/
    locales/               # user-visible strings (if present)
    *.ts                   # grid, filters, Connect Steam client
  public/data/             # catalog-v2.json copy at build
  dist/                    # npm run build — do not commit

examples/web/              # template demo stub (secondary Pages deploy)
  dist/                    # npm run build — merged at site root

workers/steam-library/     # Steam OpenID + owned games (not Pages)
  wrangler.toml            # KV namespace — [HUMAN] required for live sync

.github/workflows/pages.yml   # build catalog + demo; deploy combined artifact
docs/                          # agent documentation only — never deploy
```

### Pages deploy flow

1. Push changes under `site/catalog/`, `examples/web/`, `data/compatibility/catalog-v2.json`, or worker client code.
2. [`.github/workflows/pages.yml`](../.github/workflows/pages.yml) runs:
   - `check-steamdb-policy.sh` (ADR-0005)
   - `examples/web`: `npm ci && npm run build` with `VITE_BASE_PATH=/${repo}/`
   - `site/catalog`: `npm ci && npm test && npm run build && npm run smoke && npm run smoke:rank`
   - Merges both `dist/` trees into one Pages artifact
3. Catalog is served at `https://{user}.github.io/{repo}/catalog/`
4. Demo stub at `https://{user}.github.io/{repo}/`

### Connect Steam (`VITE_STEAM_SYNC_URL`)

The catalog **Connect Steam** toolbar requires a deployed worker URL at build time:

| Variable | Set by |
|----------|--------|
| `VITE_STEAM_SYNC_URL` | GitHub repo variable `STEAM_SYNC_WORKER_URL` (from worker deploy workflow) |
| `VITE_BASE_PATH` | Pages workflow (`/repo/catalog/`) |

Without `STEAM_SYNC_WORKER_URL`, the catalog builds successfully but Connect Steam is hidden/disabled. Operator checklist: [STEAM_CATALOG_SYNC.md](STEAM_CATALOG_SYNC.md).

## GitHub repository settings

| Setting | Required value |
|---------|----------------|
| **Pages source** | **GitHub Actions** (not "Deploy from `/docs` branch folder") |
| **Analytics** | None in workflow (FOSS, no tracking scripts) |

`[HUMAN]` enables Pages under **Settings → Pages** → **GitHub Actions**.

## Localization vs styles (catalog)

Keep user-visible copy out of stylesheets and theme code.

| Layer | Location (catalog) | API |
|-------|-------------------|-----|
| **Strings** | `site/catalog/src/` modules or locale files | Prefer `t()` / dedicated string modules |
| **Styles** | CSS modules / `style.css` | CSS variables `var(--gp-*)` where synced |
| **Theme** | theme preference modules | Labels from i18n, not CSS `content:` |

See [`docs/DESIGN_GUIDE.md`](DESIGN_GUIDE.md) for cross-stack rules.

## Inactive template stubs

`examples/{android,python,lightroom,rust,go}/` remain as **inactive** bootstrap references. They are not deployed. See [`OPTIONAL_STACKS.md`](OPTIONAL_STACKS.md).

## Pruning the web stack

If removing the catalog entirely (unlikely for this product):

1. Disable catalog steps in `pages.yml`; keep or remove `examples/web` demo as needed.
2. Update README catalog links and `BUILD_PLAN.md`.
3. Turn off GitHub Pages if no site remains.

## Anti-patterns

| Do not | Why |
|--------|-----|
| Put the catalog in `docs/` | Collides with agent read order |
| Commit `site/catalog/dist/` or `examples/web/dist/` | CI builds artifacts |
| Put user-facing copy in CSS | Breaks localization |
| Enable "Publish from `/docs`" | Serves agent markdown as a website |
| Scrape SteamDB for prices | ADR-0005; use Steam Store API only |

## Related docs

- [`site/catalog/README.md`](../site/catalog/README.md) — local dev, env vars, tests
- [`docs/STEAM_CATALOG_SYNC.md`](STEAM_CATALOG_SYNC.md) — worker deploy and smoke tests
- [`modules/web/MODULE.md`](../modules/web/MODULE.md) — PWA / Vite patterns
- [`modules/node/MODULE.md`](../modules/node/MODULE.md) — worker HTTP patterns
- [`docs/DESIGN_GUIDE.md`](DESIGN_GUIDE.md) — tokens, themes, i18n
