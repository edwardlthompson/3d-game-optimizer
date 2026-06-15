const KEY = "3d-catalog-wishlist-v1";

export function loadWishlist(): Set<string> {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return new Set();
    const parsed = JSON.parse(raw) as string[];
    return new Set(Array.isArray(parsed) ? parsed : []);
  } catch {
    return new Set();
  }
}

export function saveWishlist(ids: Set<string>): void {
  localStorage.setItem(KEY, JSON.stringify([...ids]));
}

export function toggleWishlist(ids: Set<string>, gameId: string): Set<string> {
  const next = new Set(ids);
  if (next.has(gameId)) next.delete(gameId);
  else next.add(gameId);
  saveWishlist(next);
  return next;
}

export function exportWishlist(ids: Set<string>): string {
  return JSON.stringify([...ids], null, 2);
}

export function importWishlist(json: string): Set<string> {
  const parsed = JSON.parse(json) as string[];
  const next = new Set(Array.isArray(parsed) ? parsed : []);
  saveWishlist(next);
  return next;
}
