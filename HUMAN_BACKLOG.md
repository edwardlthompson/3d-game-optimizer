# Human Backlog

> Items automation cannot complete. Mirror of open `[HUMAN]` / device work; BUILD_PLAN stays authoritative for sprint status.

| Deferred | Sprint / track | Owner | Task | Reason |
|----------|----------------|-------|------|--------|
| Steam Connect | Product v1.5+ | HUMAN | Cloudflare KV namespace id → `workers/steam-library/wrangler.toml` | Requires Cloudflare account action |
| Steam Connect | Product v1.5+ | HUMAN | GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `STEAM_WEB_API_KEY` | Secrets cannot be set by agent |
| Steam Connect | Product v1.5+ | HUMAN | Post-deploy smoke — see `docs/STEAM_CATALOG_SYNC.md` | Needs live worker + browser |
| Screenshots | Parallel HUMAN | HUMAN | Real WinUI README screenshots | Needs installed app + display |
| Hardware QA | Parallel HUMAN | HUMAN | GPU / display QA — `docs/HARDWARE_QA_OUT_OF_BAND.md` | Physical hardware |
| Headset VR | Parallel HUMAN | HUMAN | Headset VR launch (SteamVR + UEVR) | Device + titles |
| Odyssey Hub | Parallel HUMAN | HUMAN | Odyssey Hub CSV export from installed app | Vendor app on device |
| CodeQL strict | Parallel HUMAN | HUMAN | CodeQL SARIF upload for product-release `--strict` | Repo settings / gate policy |
| Template hooks | Bootstrap align | HUMAN | Optionally enable `.cursor/hooks.json` from `.cursor/hooks.json.example` after local smoke | Opt-in shell guard |
