import { escapeHtml } from "./utils";

export interface PricePoint {
  date: string;
  priceUsd: number;
  onSale?: boolean;
}

export interface AppPriceHistory {
  lowUsd?: number;
  highUsd?: number;
  points: PricePoint[];
}

export interface PriceHistoryDocument {
  version: string;
  updatedAt: string;
  apps: Record<string, AppPriceHistory>;
}

export function showPriceChart(
  root: HTMLElement,
  title: string,
  appId: number | undefined,
  history: AppPriceHistory | undefined,
  currentPrice: number | undefined,
): void {
  const overlay = document.createElement("div");
  overlay.className = "price-overlay";
  const points = history?.points ?? [];
  const low = history?.lowUsd ?? currentPrice;
  const high = history?.highUsd ?? currentPrice;

  let chart = `<p class="muted">Price tracking started recently — history builds with each catalog sync.</p>`;
  if (points.length >= 2) {
    const width = 420;
    const height = 140;
    const prices = points.map((p) => p.priceUsd);
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    const range = Math.max(max - min, 0.01);
    const coords = points
      .map((p, i) => {
        const x = (i / (points.length - 1)) * (width - 20) + 10;
        const y = height - 10 - ((p.priceUsd - min) / range) * (height - 20);
        return `${x},${y}`;
      })
      .join(" ");
    const saleDots = points
      .map((p, i) => {
        if (!p.onSale) return "";
        const x = (i / (points.length - 1)) * (width - 20) + 10;
        const y = height - 10 - ((p.priceUsd - min) / range) * (height - 20);
        return `<circle cx="${x}" cy="${y}" r="3" class="sale-dot" />`;
      })
      .join("");
    chart = `<svg viewBox="0 0 ${width} ${height}" class="price-chart" role="img" aria-label="Price history"><polyline points="${coords}" />${saleDots}</svg>`;
  }

  overlay.innerHTML = `
    <div class="price-modal" role="dialog" aria-label="Price history for ${title}">
      <header><h2>${escapeHtml(title)}</h2><button type="button" class="close-btn" aria-label="Close">×</button></header>
      ${chart}
      <dl class="price-stats">
        <div><dt>Current</dt><dd>${currentPrice != null ? `$${currentPrice.toFixed(2)}` : "—"}</dd></div>
        <div><dt>Record low</dt><dd>${low != null ? `$${low.toFixed(2)}` : "—"}</dd></div>
        <div><dt>Record high</dt><dd>${high != null ? `$${high.toFixed(2)}` : "—"}</dd></div>
        <div><dt>Steam App</dt><dd>${appId ?? "—"}</dd></div>
      </dl>
      <p class="muted">Self-tracked from catalog sync (Steam Store API). See ADR-0005.</p>
    </div>
  `;

  overlay.querySelector(".close-btn")?.addEventListener("click", () => overlay.remove());
  overlay.addEventListener("click", (e) => {
    if (e.target === overlay) overlay.remove();
  });
  root.append(overlay);
}

export async function loadPriceHistory(baseUrl: string): Promise<PriceHistoryDocument | null> {
  try {
    const response = await fetch(`${baseUrl}data/price-history-v1.json`);
    if (!response.ok) return null;
    return (await response.json()) as PriceHistoryDocument;
  } catch {
    return null;
  }
}
