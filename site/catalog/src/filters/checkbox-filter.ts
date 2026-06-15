export type CheckboxFilterValue = string[];

export function matchesCheckboxFilter(
  rowKeys: string | string[],
  filterValue: unknown,
): boolean {
  const selected = filterValue as CheckboxFilterValue | undefined;
  if (!selected?.length) return true;
  const keys = Array.isArray(rowKeys) ? rowKeys : [rowKeys];
  return keys.some((k) => selected.includes(k));
}

export function allOptionsSelected(selected: string[] | undefined, options: string[]): boolean {
  if (!selected?.length) return true;
  return selected.length >= options.length;
}
