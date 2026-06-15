# Catalog table layout and column trim

## Problems (from live site)

1. **Overflow:** [`site/catalog/src/style.css`](site/catalog/src/style.css) sets `white-space: nowrap` on all `th, td` (line 85), forcing the full grid wider than the viewport — especially **Play methods** badges and long **Title** strings (VRto3D wiki entries still contain raw HTML in [`catalog-v2.json`](data/compatibility/catalog-v2.json)).
2. **Redundant columns:** **TrueGame** and **3D Vision** duplicate information already shown in **Play methods** (`platformSupport[]`).
3. **Filter popover alignment:** Checkbox rows should align flush left like Sheets/Excel.
4. **Buy links:** **Buy on Steam** anchors in [`catalog-columns.ts`](site/catalog/src/catalog-columns.ts) lack `target="_blank"` — they navigate in the same tab instead of opening a new tab or handing off to the Steam client.

## Changes

### 1. Remove TrueGame and 3D Vision columns

**[`site/catalog/src/catalog-columns.ts`](site/catalog/src/catalog-columns.ts)**
- Delete column defs `trueGame` and `vision`.
- Remove unused `visionLabel` import.

**[`site/catalog/src/filters/buckets.ts`](site/catalog/src/filters/buckets.ts)**
- Remove `trueGame` / `vision` from `collectUniqueValues()`.

Keep toolbar **3D Vision certified** quick filter in [`main.ts`](site/catalog/src/main.ts).

### 2. Wrap text; contain table width

- Add `meta: { wrap: true }` on `title`, `bestExperience`, `playMethods`, `hardware`.
- [`grid.ts`](site/catalog/src/grid.ts): set `data-col` + `cell-wrap` class when `meta.wrap`.
- [`style.css`](site/catalog/src/style.css): remove global `nowrap`; apply wrap + `max-width` on wrap columns; `table-layout: fixed; width: 100%`.
- Add `stripHtml()` in [`utils.ts`](site/catalog/src/utils.ts) for title cell display.

### 3. Left-justify filter popover checkboxes

**[`style.css`](site/catalog/src/style.css)** — `text-align: left` on popover; `align-items: stretch` + `justify-content: flex-start` on `.filter-option` / `.filter-select-all`.

### 4. Buy on Steam — new tab + Steam app handoff

**Current:** `<a href="…" rel="noopener noreferrer">Buy on Steam</a>` (same-tab navigation).

**Target behavior:**
- Always open in a **new browser tab** via `target="_blank"` and `rel="noopener noreferrer"`.
- When `steamAppId` is known, also expose a **Steam client** URL so installed Steam can intercept or user can open the store in-app.

**Implementation** in [`catalog-columns.ts`](site/catalog/src/catalog-columns.ts) (Buy column cell):

```html
<a href="https://store.steampowered.com/app/{appId}/"
   target="_blank"
   rel="noopener noreferrer">Buy on Steam</a>
```

Optional secondary control or `title` tooltip: `steam://openurl/https://store.steampowered.com/app/{appId}/` — Steam desktop on Windows/macOS/Linux registers this protocol and opens the store page in the client when the user has Steam running. GitHub Pages cannot force the client without user click; a small **Open in Steam** link next to Buy (same row) using `steam://store/{appId}` is the standard pattern:

| Link | href | Opens |
|------|------|--------|
| Buy on Steam | `https://store.steampowered.com/app/{id}/` | New tab (Steam client may still intercept HTTPS on some setups) |
| Steam icon/link (optional) | `steam://store/{appId}` | Steam app if installed |

**Recommended (minimal):** single **Buy on Steam** link with `target="_blank"`. Add `steam://store/{appId}` as `href` only on a separate compact **Steam** button if user wants explicit app launch — default plan uses one link with `target="_blank"` plus `steam://store/{appId}` as fallback link text "Open in Steam" when `steamAppId` present.

**Security:** keep `rel="noopener noreferrer"` on all external store links.

### 5. Verify and ship

- `npm run build && npm run smoke`
- Commit + push → Pages redeploy

### Critique

- `steam://` links fail silently if Steam is not installed — HTTPS Buy link with `target="_blank"` remains the primary action.
- Two links in Buy cell slightly widens column — use short label "Steam app" or icon-only with `aria-label`.

## Todos

1. drop-columns — Remove trueGame/vision from columns and filter index
2. wrap-css — Wrap layout, stripHtml titles, fixed table
3. filter-align — Left-align popover checkboxes
4. buy-links — `target="_blank"` + optional `steam://store/{appId}` handoff
5. ship — Build, smoke, push
