import { PLATFORM_LABELS } from "../game-accessors";

export class ColumnFilterPopover {
  private popover: HTMLDivElement | null = null;
  private selected = new Set<string>();

  constructor(
    private readonly options: string[],
    private readonly label: string,
    private readonly onApply: (selected: string[] | undefined) => void,
    private readonly formatOption: (value: string) => string = (v) => v,
  ) {}

  createButton(): HTMLButtonElement {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "filter-btn";
    btn.setAttribute("aria-label", `Filter ${this.label}`);
    btn.textContent = "▾";
    btn.addEventListener("click", (event) => {
      event.stopPropagation();
      this.toggle(btn);
    });
    return btn;
  }

  private toggle(anchor: HTMLElement): void {
    if (this.popover) {
      this.close();
      return;
    }
    this.selected = new Set(this.options);
    this.popover = document.createElement("div");
    this.popover.className = "filter-popover";
    this.popover.innerHTML = `
      <div class="filter-popover-head">Filter: ${this.label}</div>
      <input type="search" class="filter-popover-search" placeholder="Search values…" />
      <label class="filter-select-all"><input type="checkbox" checked /> Select all</label>
      <div class="filter-popover-list"></div>
      <div class="filter-popover-actions">
        <button type="button" data-act="clear">Clear</button>
        <button type="button" data-act="apply">Apply</button>
      </div>
    `;

    const list = this.popover.querySelector(".filter-popover-list")!;
    const search = this.popover.querySelector<HTMLInputElement>(".filter-popover-search")!;
    const selectAll = this.popover.querySelector<HTMLInputElement>(".filter-select-all input")!;

    const renderList = (query: string) => {
      list.innerHTML = "";
      for (const option of this.options) {
        const display = this.formatOption(option);
        if (query && !display.toLowerCase().includes(query.toLowerCase())) continue;
        const row = document.createElement("label");
        row.className = "filter-option";
        const input = document.createElement("input");
        input.type = "checkbox";
        input.checked = this.selected.has(option);
        input.addEventListener("change", () => {
          if (input.checked) this.selected.add(option);
          else this.selected.delete(option);
          selectAll.checked = this.selected.size === this.options.length;
        });
        row.append(input, document.createTextNode(` ${display}`));
        list.append(row);
      }
    };

    search.addEventListener("input", () => renderList(search.value));
    selectAll.addEventListener("change", () => {
      this.selected = selectAll.checked ? new Set(this.options) : new Set();
      renderList(search.value);
    });

    this.popover.querySelector("[data-act=clear]")?.addEventListener("click", () => {
      this.selected.clear();
      selectAll.checked = false;
      renderList(search.value);
    });

    this.popover.querySelector("[data-act=apply]")?.addEventListener("click", () => {
      const values = [...this.selected];
      if (values.length === 0 || values.length === this.options.length) {
        this.onApply(undefined);
      } else {
        this.onApply(values);
      }
      this.close();
    });

    renderList("");
    anchor.closest("th")?.append(this.popover);
    document.addEventListener("click", this.onDocumentClick, true);
  }

  private onDocumentClick = (event: MouseEvent): void => {
    if (this.popover && !this.popover.contains(event.target as Node)) {
      this.close();
    }
  };

  private close(): void {
    document.removeEventListener("click", this.onDocumentClick, true);
    this.popover?.remove();
    this.popover = null;
  }
}

export function formatPlayMethodOption(key: string): string {
  const pipe = key.indexOf("|");
  if (pipe < 0) return key;
  const platformKey = key.slice(0, pipe);
  const label = key.slice(pipe + 1);
  const platform = PLATFORM_LABELS[platformKey] ?? platformKey;
  return `${platform} · ${label}`;
}
