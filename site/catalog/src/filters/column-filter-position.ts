export function positionFilterPopover(popover: HTMLElement, anchor: HTMLElement): void {
  const margin = 8;
  const maxWidth = window.innerWidth - margin * 2;
  const maxHeight = window.innerHeight - margin * 2;

  popover.style.visibility = "hidden";
  popover.style.left = "-9999px";
  popover.style.top = "0";
  popover.style.width = "max-content";
  popover.style.maxWidth = `${maxWidth}px`;
  popover.style.maxHeight = `${maxHeight}px`;

  const pop = popover.getBoundingClientRect();
  const rect = anchor.getBoundingClientRect();
  let left = rect.left;
  let top = rect.bottom + 4;
  if (left + pop.width > window.innerWidth - margin) {
    left = Math.max(margin, window.innerWidth - pop.width - margin);
  }
  if (top + pop.height > window.innerHeight - margin) {
    top = Math.max(margin, rect.top - pop.height - 4);
  }
  popover.style.left = `${left}px`;
  popover.style.top = `${top}px`;
  popover.style.visibility = "visible";
}
