const KEY = "3d-catalog-library-v1";

export function loadLibrary(): Set<string> {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return new Set();
    const parsed = JSON.parse(raw) as string[];
    return new Set(Array.isArray(parsed) ? parsed : []);
  } catch {
    return new Set();
  }
}

export function saveLibrary(ids: Set<string>): void {
  localStorage.setItem(KEY, JSON.stringify([...ids]));
}

export function toggleLibrary(ids: Set<string>, gameId: string): Set<string> {
  const next = new Set(ids);
  if (next.has(gameId)) next.delete(gameId);
  else next.add(gameId);
  saveLibrary(next);
  return next;
}
