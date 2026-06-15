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
