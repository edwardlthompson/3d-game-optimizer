#!/usr/bin/env python3
"""Sync AGENT_MEMORY module checkboxes and emit .cursor/stack-selection.json."""
from __future__ import annotations

import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

# Labels must match AGENT_MEMORY.md checkbox text (after "- [x] " / "- [ ] ").
MODULE_LINES = {
    "winui": "WinUI 3 desktop",
    "web": "Web catalog",
    "node": "Node / Cloudflare Worker",
    "python": "Python sync tooling",
    "android": "Android / F-Droid (inactive stub",
    "lightroom": "Lightroom Classic (inactive stub",
    "rust": "Rust (inactive stub",
    "go": "Go (inactive stub",
}

PRODUCT_STACK_MODULES = ["winui", "web", "node", "python"]

PARALLEL_NOTES = {
    "product": "Product Parallel: one scope per row in docs/PARALLEL_AGENT_SCOPES.md",
    "winui": "Parallel: `src/SpatialLabsOptimizer/**` only",
    "web": "Parallel: `site/catalog/**` only",
    "node": "Parallel: `workers/steam-library/**` only",
    "python": "Parallel: `scripts/sync-catalog/**` only",
    "multi": "Parallel: one agent per active stack; no overlapping paths",
    "none": "Parallel: scope per AGENT_MEMORY active modules",
}


def active_modules(stack: str) -> list[str]:
    if stack == "product":
        return list(PRODUCT_STACK_MODULES)
    if stack in MODULE_LINES:
        return [stack]
    if stack in ("multi", "none"):
        return list(MODULE_LINES.keys())
    return list(PRODUCT_STACK_MODULES)


def sync_agent_memory(root: Path, stack: str) -> None:
    path = root / "AGENT_MEMORY.md"
    text = path.read_text(encoding="utf-8")
    active = set(active_modules(stack))
    for key, label_prefix in MODULE_LINES.items():
        mark = "x" if key in active else " "
        pattern = rf"^- \[.\] {re.escape(label_prefix)}"
        text = re.sub(pattern, f"- [{mark}] {label_prefix}", text, count=1)
    path.write_text(text, encoding="utf-8")


def write_stack_selection(root: Path, stack: str, pruned: bool) -> None:
    cursor_dir = root / ".cursor"
    cursor_dir.mkdir(exist_ok=True)
    payload = {
        "stack": stack,
        "pruned": pruned,
        "active_modules": active_modules(stack),
        "parallel_scope_note": PARALLEL_NOTES.get(stack, PARALLEL_NOTES["product"]),
        "selected_at": datetime.now(timezone.utc).replace(microsecond=0).isoformat(),
    }
    (cursor_dir / "stack-selection.json").write_text(
        json.dumps(payload, indent=2) + "\n", encoding="utf-8"
    )


def main() -> None:
    if len(sys.argv) != 4:
        print(
            "Usage: init-stack-sync.py <stack> <root> <pruned:true|false>\n"
            "  stack: product | winui | web | node | python | multi | none",
            file=sys.stderr,
        )
        sys.exit(1)
    stack, root_s, pruned_s = sys.argv[1], sys.argv[2], sys.argv[3]
    root = Path(root_s)
    sync_agent_memory(root, stack)
    write_stack_selection(root, stack, pruned_s.lower() == "true")


if __name__ == "__main__":
    main()
