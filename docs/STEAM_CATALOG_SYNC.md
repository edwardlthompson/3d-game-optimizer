# Steam catalog library sync

Connect the [3D Game Catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) to a user's Steam library via **Steam OpenID** and a **Cloudflare Worker** proxy. Owned App IDs are matched to catalog titles and stored as local **Lib** checkmarks.

## User flow

1. Click **Connect Steam** (visible only when the worker URL is configured at build time).
2. Sign in on Steam; the worker fetches owned games and redirects back with a one-time token.
3. The catalog exchanges the token, marks matching titles in your library, and shows a summary banner.
4. If the library is empty (private **Game details**), set [Game details](https://steamcommunity.com/my/edit/settings) to **Public** and connect again.

## Operator checklist `[HUMAN]`

### 1. Steam Web API key

1. Sign in at [steamcommunity.com/dev/apikey](https://steamcommunity.com/dev/apikey).
2. Register a key for domain `edwardlthompson.github.io` (or your Pages host).
3. Save as GitHub Actions secret **`STEAM_WEB_API_KEY`**.

### 2. Cloudflare Worker

1. Create a [Cloudflare](https://dash.cloudflare.com/) account (free tier is sufficient).
2. Install Wrangler: `npm install -g wrangler` or use the package in `workers/steam-library`.
3. Create KV namespace:
   ```bash
   cd workers/steam-library
   npm install
   wrangler kv namespace create SYNC_KV
   ```
4. Copy the namespace **id** into `workers/steam-library/wrangler.toml` (`[[kv_namespaces]]` → `id`).
5. Set the API key secret:
   ```bash
   wrangler secret put STEAM_WEB_API_KEY
   ```
6. Deploy:
   ```bash
   npm run deploy
   ```
7. Note the worker URL (e.g. `https://steam-library-sync.<subdomain>.workers.dev`).

### 3. GitHub repository

| Secret / variable | Purpose |
|-------------------|---------|
| `CLOUDFLARE_API_TOKEN` | Deploy worker from CI |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare account |
| `STEAM_WEB_API_KEY` | Worker secret (also set via `wrangler secret`) |
| `STEAM_SYNC_WORKER_URL` | **Repository variable** — worker base URL for catalog build |

After setting `STEAM_SYNC_WORKER_URL`, push to `main` to rebuild GitHub Pages with **Connect Steam** enabled.

### 4. Local development

```bash
cp workers/steam-library/.dev.vars.example workers/steam-library/.dev.vars
# Edit .dev.vars with STEAM_WEB_API_KEY
cd workers/steam-library && npm run dev
```

In another terminal:

```bash
cd site/catalog
VITE_STEAM_SYNC_URL=http://127.0.0.1:8787 npm run dev
```

## Privacy

- Opt-in only; no sync until the user clicks **Connect Steam**.
- Worker stores tokens in KV for **5 minutes** only; App ID lists are not logged.
- Production worker rejects requests without a matching `Origin` header.
- User API keys are **not** stored in the catalog browser; `/sync/owned` is disabled in production.
- Library checkmarks remain in `localStorage` (`3d-catalog-library-v1`).

## Architecture

See [`.cursor/plans/steam_library_sync.plan.md`](../.cursor/plans/steam_library_sync.plan.md).
