export const MAX_IMPORT_BYTES = 512 * 1024;

export function parseStringIdList(json: string, maxBytes = MAX_IMPORT_BYTES): string[] {
  if (json.length > maxBytes) {
    throw new Error(`Import exceeds ${maxBytes} bytes`);
  }

  const parsed: unknown = JSON.parse(json);
  if (!Array.isArray(parsed)) {
    throw new Error("Import must be a JSON array");
  }

  const ids: string[] = [];
  for (const entry of parsed) {
    if (typeof entry !== "string" || entry.length === 0 || entry.length > 128) {
      throw new Error("Import entries must be non-empty strings up to 128 chars");
    }

    ids.push(entry);
  }

  return ids;
}
