"""Tests for scripts/sync-catalog/resolve-steam-appids.py"""
from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RESOLVE_PATH = ROOT / "scripts" / "sync-catalog" / "resolve-steam-appids.py"


def load_resolve_module():
    spec = importlib.util.spec_from_file_location("resolve_steam_appids", RESOLVE_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    sys.modules["resolve_steam_appids"] = module
    spec.loader.exec_module(module)
    return module


resolve = load_resolve_module()


class ResolveSteamAppIdsTests(unittest.TestCase):
    def test_normalize_title_strips_suffix(self) -> None:
        self.assertEqual(
            resolve.normalize_title("Portal 2 Game of the Year"),
            "portal 2",
        )

    def test_normalize_title_removes_trademark(self) -> None:
        self.assertEqual(resolve.normalize_title("Game™ Title®"), "game title")

    def test_load_lock_reads_locked_ids(self) -> None:
        locked = resolve.load_lock()
        self.assertIsInstance(locked, dict)
        if locked:
            key, value = next(iter(locked.items()))
            self.assertIsInstance(key, str)
            self.assertIsInstance(value, int)


if __name__ == "__main__":
    unittest.main()
