const NAMED_ENTITIES: Record<string, string> = {
  amp: "&",
  lt: "<",
  gt: ">",
  quot: '"',
  nbsp: " ",
};

/** Decode one HTML entity match; used in a single replace pass (avoids js/double-escaping). */
function decodeEntity(match: string, body: string): string {
  if (body[0] === "#") {
    const code =
      body[1] === "x" || body[1] === "X"
        ? Number.parseInt(body.slice(2), 16)
        : Number.parseInt(body.slice(1), 10);
    if (!Number.isFinite(code) || code < 0) return match;
    try {
      return String.fromCodePoint(code);
    } catch {
      return match;
    }
  }
  const mapped = NAMED_ENTITIES[body.toLowerCase()];
  return mapped !== undefined ? mapped : match;
}

export function stripHtml(value: string): string {
  return value
    .replace(/<[^>]*>/g, " ")
    .replace(/&(#(?:x[0-9a-f]+|\d+)|[a-z]+);/gi, decodeEntity)
    .replace(/\s+/g, " ")
    .trim();
}

export function displayTitle(value: string): string {
  return stripHtml(value);
}

export function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

export function includesFilter(rowValue: string, filterValue: string): boolean {
  const needle = filterValue.trim().toLowerCase();
  if (!needle) return true;
  return rowValue.toLowerCase().includes(needle);
}
