import { PLATFORM_LABELS } from "../game-accessors";

export class ColumnFilterPopover {
  private popover: HTMLDivElement | null = null;
  private anchor: HTMLElement | null = null;
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
    this.anchor = anchor;
    this.selected = new Set(this.options);
    this.popover = document.createElement("div");
    this.popover.className = "filter-popover";
    this.popover.innerHTML = `
      <div class="filter-popover-toolbar">
        <button type="button" class="filter-popover-apply" data-act="apply">Apply</button>
        <button type="button" data-act="select-all">Select all</button>
        <button type="button" data-act="select-none">Select none</button>
      </div>
      <div class="filter-popover-head">Filter: ${this.label}</div>
      <input type="search" class="filter-popover-search" placeholder="Search values…" />
      <div class="filter-popover-list"></div>
    `;

    const list = this.popover.querySelector(".filter-popover-list")!;
    const search = this.popover.querySelector<HTMLInputElement>(".filter-popover-search")!;

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
        });
        const text = document.createElement("span");
        text.className = "filter-option-label";
        text.textContent = display;
        row.append(input, text);
        list.append(row);
      }
      this.positionPopover();
    };

    search.addEventListener("input", () => renderList(search.value));

    this.popover.querySelector("[data-act=select-all]")?.addEventListener("click", () => {
      this.selected = new Set(this.options);
      renderList(search.value);
    });

    this.popover.querySelector("[data-act=select-none]")?.addEventListener("click", () => {
      this.selected.clear();
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

    document.body.append(this.popover);
    renderList("");
    window.addEventListener("resize", this.onViewportChange);
    window.addEventListener("scroll", this.onViewportChange, true);
    document.addEventListener("click", this.onDocumentClick, true);
  }

  private positionPopover = (): void => {
    if (!this.popover || !this.anchor) return;
    const margin = 8;
    const maxWidth = window.innerWidth - margin * 2;
    const maxHeight = window.innerHeight - margin * 2;

    this.popover.style.visibility = "hidden";
    this.popover.style.left = "-9999px";
    this.popover.style.top = "0";
    this.popover.style.width = "max-content";
    this.popover.style.maxWidth = `${maxWidth}px`;
    this.popover.style.maxHeight = `${maxHeight}px`;

    const pop = this.popover.getBoundingClientRect();
    const rect = this.anchor.getBoundingClientRect();
    let left = rect.left;
    let top = rect.bottom + 4;
    if (left + pop.width > window.innerWidth - margin) {
      left = Math.max(margin, window.innerWidth - pop.width - margin);
    }
    if (top + pop.height > window.innerHeight - margin) {
      top = Math.max(margin, rect.top - pop.height - 4);
    }
    this.popover.style.left = `${left}px`;
    this.popover.style.top = `${top}px`;
    this.popover.style.visibility = "visible";
  };

  private onViewportChange = (): void => {
    this.positionPopover();
  };

  private onDocumentClick = (event: MouseEvent): void => {
    if (this.popover && !this.popover.contains(event.target as Node)) {
      this.close();
    }
  };

  private close(): void {
    window.removeEventListener("resize", this.onViewportChange);
    window.removeEventListener("scroll", this.onViewportChange, true);
    document.removeEventListener("click", this.onDocumentClick, true);
    this.popover?.remove();
    this.popover = null;
    this.anchor = null;
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
