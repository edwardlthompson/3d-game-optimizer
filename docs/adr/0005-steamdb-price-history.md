# ADR-0005: SteamDB Price History — Not Used

## Status

Accepted

## Context

The public catalog (`site/catalog/`) shows price sparklines from `price-history-v1.json`, appended weekly by `scripts/sync-catalog/append-price-history.py` using **Steam Store API** prices from `catalog-v2.json`.

Phase 4b considered importing historical lows from [SteamDB](https://steamdb.info/) to backfill graphs before enough self-tracked data exists.

SteamDB's FAQ explicitly states: no public API, no scraping/crawling (risk of ban), and no bulk data dumps. Academic access requires prior written permission and still prohibits republishing datasets at scale.

## Decision

- **Do not** scrape, crawl, or bulk-import SteamDB price history into this repository or GitHub Pages.
- Continue **Steam Store API only** for price enrichment (`enrich-steam-stats.py`, `append-price-history.py`).
- Catalog footer and `data-coverage.ts` document self-tracked history; SteamDB backfill is **cancelled**, not deferred.
- CI enforces the policy via `scripts/check-steamdb-policy.sh`.

## Alternatives considered

| Alternative | Rejected because |
|-------------|------------------|
| Automated SteamDB scraper in CI | Violates SteamDB ToS; Cloudflare blocks; FOSS redistribution risk |
| Manual CSV backfill from SteamDB | Does not scale; gray area for public static site |
| Contact SteamDB for academic dump | Out of scope for a community catalog; access not guaranteed |
| Third-party scraper APIs | Proprietary; violates SteamDB policy indirectly |

## Consequences

- Price graphs remain accurate only **since tracking started** (weekly CI).
- UI copy stays honest (`tracking since` / self-tracked disclaimer).
- No `[HUMAN]` ToS review gate — policy is codified here and in CI.

## Boundaries (non-goals)

- Replacing SteamDB for human browsing (users may still visit steamdb.info manually).
- Historical data before the first `append-price-history.py` run.
