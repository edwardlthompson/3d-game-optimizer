"""Tests for scripts/sync-catalog/merge-catalog.py"""
from __future__ import annotations

import importlib.util
import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MERGE_PATH = ROOT / "scripts" / "sync-catalog" / "merge-catalog.py"


def load_merge_module():
    spec = importlib.util.spec_from_file_location("merge_catalog", MERGE_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    sys.modules["merge_catalog"] = module
    spec.loader.exec_module(module)
    return module


merge = load_merge_module()


class MergeCatalogTests(unittest.TestCase):
    def test_best_level_prefers_ultra(self) -> None:
        self.assertEqual(
            merge.best_level(["playable3d", "ultra3d", "native3d"]),
            "ultra3d",
        )

    def test_merge_same_steam_app_unions_sources(self) -> None:
        rows = [
            {
                "title": "Portal 2",
                "steamAppId": 620,
                "steamMatchConfidence": 1.0,
                "sources": [
                    {
                        "sourceId": "manual-curated",
                        "level": "playable3d",
                        "label": "Curated",
                        "syncedAt": "2026-06-15",
                    }
                ],
                "tiersByVendorSeed": {
                    "acer": "playable",
                    "samsung": "playable",
                    "nvidia": "playable",
                    "generic": "experimental",
                },
            },
            {
                "title": "Portal 2",
                "steamAppId": 620,
                "steamMatchConfidence": 0.95,
                "sources": [
                    {
                        "sourceId": "nvidia-3d-vision",
                        "level": "native3d",
                        "label": "3D Vision Ready",
                        "syncedAt": "2026-06-15",
                    }
                ],
            },
        ]
        games = merge.merge_rows(rows, {})
        self.assertEqual(len(games), 1)
        self.assertEqual(len(games[0]["sources"]), 2)
        self.assertEqual(games[0]["bestLevel"], "native3d")
        self.assertIn("nvidia-3d-vision", games[0]["platforms"])

    def test_exclusive_hardware_only_single_vendor_path(self) -> None:
        sources = [
            {
                "sourceId": "nvidia-3d-vision",
                "level": "native3d",
                "label": "3D Vision Ready",
                "syncedAt": "2026-06-15",
            }
        ]
        hw = merge.compute_hardware(sources)
        self.assertEqual(hw["exclusiveTo"], ["nvidia-3d-vision-generic"])
        self.assertIn("nvidia-geforce", hw.get("gpu", []))

    def test_multi_source_not_exclusive(self) -> None:
        sources = [
            {
                "sourceId": "acer-truegame",
                "level": "native3d",
                "label": "3D",
                "syncedAt": "2026-06-15",
            },
            {
                "sourceId": "uevr-profiles",
                "level": "optimized3d",
                "label": "Works Well",
                "syncedAt": "2026-06-15",
            },
        ]
        hw = merge.compute_hardware(sources)
        self.assertEqual(hw["exclusiveTo"], [])

    def test_built_catalog_meets_minimum_games(self) -> None:
        catalog = merge.build_catalog()
        self.assertGreaterEqual(catalog["meta"]["gameCount"], 25)
        self.assertEqual(catalog["version"], "v2")


if __name__ == "__main__":
    unittest.main()
