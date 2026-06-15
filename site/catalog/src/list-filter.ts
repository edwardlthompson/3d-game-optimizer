export type ListFilterMode = "all" | "only" | "exclude";

export function matchesListFilter(inSet: boolean, mode: ListFilterMode): boolean {
  if (mode === "all") return true;
  if (mode === "only") return inSet;
  return !inSet;
}
