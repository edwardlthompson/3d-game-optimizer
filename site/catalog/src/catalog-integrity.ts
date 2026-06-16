const SYNC_HASH_KEY = "3d-catalog-last-hash";

async function sha256Hex(text: string): Promise<string> {
  const data = new TextEncoder().encode(text);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return [...new Uint8Array(digest)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

export async function verifyCatalogIntegrity(
  baseUrl: string,
  catalogText: string,
): Promise<{ ok: boolean; hash: string | null }> {
  try {
    const resp = await fetch(`${baseUrl}data/catalog-v2.sha256`);
    if (!resp.ok) return { ok: true, hash: null };
    const expected = (await resp.text()).trim().split(/\s+/)[0]?.toLowerCase();
    if (!expected) return { ok: true, hash: null };
    const actual = (await sha256Hex(catalogText)).toLowerCase();
    return { ok: actual === expected, hash: actual };
  } catch {
    return { ok: true, hash: null };
  }
}

export function checkCatalogSync(mergedAt: string, catalogHash: string | null, onUpdate: () => void): void {
  const priorMerged = localStorage.getItem("3d-catalog-last-sync");
  const priorHash = localStorage.getItem(SYNC_HASH_KEY);
  const changed =
    (priorMerged && priorMerged !== mergedAt) || (catalogHash && priorHash && priorHash !== catalogHash);
  if (changed) onUpdate();
  localStorage.setItem("3d-catalog-last-sync", mergedAt);
  if (catalogHash) localStorage.setItem(SYNC_HASH_KEY, catalogHash);
}
