import type { Table } from "@tanstack/table-core";
import { ColumnFilterPopover } from "./filters/ColumnFilterPopover";
import { formatPlayMethodOption } from "./filters/play-method-format";
import type { CatalogGame } from "./types";

export function renderGridHeader(
  tableWrap: HTMLDivElement,
  table: Table<CatalogGame>,
  filterOptions: Record<string, string[]>,
): void {
  const thead = tableWrap.querySelector("thead")!;
  const sortRow = document.createElement("tr");
  const filterRow = document.createElement("tr");
  filterRow.className = "filter-row";

  for (const header of table.getHeaderGroups()[0]?.headers ?? []) {
    const th = document.createElement("th");
    const meta = header.column.columnDef.meta as { steam?: boolean; tooltip?: string } | undefined;
    if (meta?.steam) th.dataset.source = "steam-store";
    if (meta?.tooltip) th.title = meta.tooltip;
    if (header.column.getCanSort()) {
      th.addEventListener("click", (event) => {
        header.column.toggleSorting(undefined, event.shiftKey);
      });
    }
    th.textContent = String(header.column.columnDef.header ?? header.column.id);
    th.dataset.col = header.column.id;
    sortRow.append(th);

    const filterTh = document.createElement("th");
    if (header.column.getCanFilter()) {
      const options = filterOptions[header.column.id] ?? [];
      const format =
        header.column.id === "playMethods" ? formatPlayMethodOption : (v: string) => v;
      const popover = new ColumnFilterPopover(
        options,
        String(header.column.columnDef.header ?? header.column.id),
        (selected) => header.column.setFilterValue(selected),
        format,
      );
      filterTh.append(popover.createButton());
    }
    filterRow.append(filterTh);
  }

  thead.replaceChildren(sortRow, filterRow);
}
