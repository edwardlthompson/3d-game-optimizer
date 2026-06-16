import { PAGE_SIZES } from "./constants";
import type { Table } from "@tanstack/table-core";
import type { CatalogGame } from "./types";

export function renderPager<T extends CatalogGame>(
  pager: HTMLElement,
  table: Table<T>,
  pagination: { pageIndex: number; pageSize: number },
): void {
  const { pageIndex, pageSize } = pagination;
  const pageCount = table.getPageCount();
  pager.innerHTML = `
    <button type="button" data-nav="first" ${pageIndex === 0 ? "disabled" : ""}>First</button>
    <button type="button" data-nav="prev" ${pageIndex === 0 ? "disabled" : ""}>Prev</button>
    <span>Page ${pageIndex + 1} of ${Math.max(pageCount, 1)}</span>
    <button type="button" data-nav="next" ${pageIndex >= pageCount - 1 ? "disabled" : ""}>Next</button>
    <button type="button" data-nav="last" ${pageIndex >= pageCount - 1 ? "disabled" : ""}>Last</button>
    <label>Rows <select data-page-size>${PAGE_SIZES.map((n) => `<option value="${n}" ${n === pageSize ? "selected" : ""}>${n}</option>`).join("")}</select></label>
  `;
  pager.querySelectorAll<HTMLButtonElement>("button[data-nav]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const nav = btn.dataset.nav;
      if (nav === "first") table.setPageIndex(0);
      if (nav === "prev") table.previousPage();
      if (nav === "next") table.nextPage();
      if (nav === "last") table.setPageIndex(Math.max(pageCount - 1, 0));
    });
  });
  pager.querySelector<HTMLSelectElement>("select[data-page-size]")?.addEventListener("change", (e) => {
    table.setPageSize(Number((e.target as HTMLSelectElement).value));
  });
}

export function fitColumnWidths(tableWrap: HTMLElement, tbody: HTMLElement): void {
  const table = tableWrap.querySelector("table")!;
  table.style.tableLayout = "auto";
  table.querySelector("colgroup")?.remove();

  requestAnimationFrame(() => {
    const thead = table.querySelector("thead")!;
    const headerRow = thead.querySelector("tr:first-child");
    if (!headerRow) return;

    const bodyRows = [...tbody.querySelectorAll("tr")].filter((row) => !row.querySelector(".empty"));
    const colCount = headerRow.children.length;
    if (colCount === 0 || bodyRows.length === 0) return;

    const widths: number[] = [];
    for (let i = 0; i < colCount; i += 1) {
      let max = (headerRow.children[i] as HTMLElement).offsetWidth;
      for (const row of bodyRows) {
        const cell = row.children[i] as HTMLElement | undefined;
        if (cell) max = Math.max(max, cell.offsetWidth);
      }
      widths.push(max);
    }

    const colgroup = document.createElement("colgroup");
    colgroup.replaceChildren(
      ...widths.map((width) => {
        const col = document.createElement("col");
        col.style.width = `${width}px`;
        return col;
      }),
    );
    table.insertBefore(colgroup, thead);
    table.style.tableLayout = "fixed";
  });
}
