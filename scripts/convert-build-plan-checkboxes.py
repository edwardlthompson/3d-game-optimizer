#!/usr/bin/env python3
"""Use visible Unicode status glyphs in BUILD_PLAN task lists."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILD_PLAN = ROOT / "BUILD_PLAN.md"

OPEN = "\u2B1C"  # ⬜ open
DONE = "\u2705"  # ✅ done


def convert_line(line: str) -> str:
    patterns = [
        (r'^- <input type="checkbox" checked disabled> (.*)$', DONE),
        (r'^- <input type="checkbox" disabled> (.*)$', OPEN),
        (r"^- \u2611 (.*)$", DONE),  # legacy ☑
        (r"^- \u2610 (.*)$", OPEN),  # legacy ☐
        (r"^- \[ \] (.*)$", OPEN),
        (r"^- \[[xX]\] (.*)$", DONE),
    ]
    for pattern, glyph in patterns:
        if m := re.match(pattern, line):
            return f"- {glyph} {m.group(1)}"
    return line


def main() -> None:
    text = BUILD_PLAN.read_text(encoding="utf-8")
    lines = [convert_line(line) for line in text.splitlines()]
    new_text = "\n".join(lines) + "\n"

    new_text = re.sub(
        r"\*\*Task format:\*\*.*",
        f"**Task format:** `- {OPEN} [OWNER] Description` · mark done with `{DONE}`",
        new_text,
        count=1,
    )
    new_text = re.sub(
        r"Count open:.*",
        f"Count open: `grep -c '^- {OPEN}' BUILD_PLAN.md`",
        new_text,
        count=1,
    )

    # Ensure status key exists once after legend table
    key = f"\n**Status key:** {DONE} done · {OPEN} open\n"
    if "**Status key:**" not in new_text:
        new_text = new_text.replace(
            "| `AUTO` | CI/scripts/bots |\n",
            f"| `AUTO` | CI/scripts/bots |\n{key}",
        )

    BUILD_PLAN.write_text(new_text, encoding="utf-8")
    open_count = sum(1 for line in lines if line.startswith(f"- {OPEN} "))
    done_count = sum(1 for line in lines if line.startswith(f"- {DONE} "))
    print(f"converted: {open_count} open, {done_count} done")


if __name__ == "__main__":
    main()
