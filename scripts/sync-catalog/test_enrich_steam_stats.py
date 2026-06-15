#!/usr/bin/env python3
"""Unit tests for enrich-steam-stats helpers."""
from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MODULE = Path(__file__).resolve().parent / "enrich-steam-stats.py"
spec = importlib.util.spec_from_file_location("enrich_steam_stats", MODULE)
mod = importlib.util.module_from_spec(spec)
assert spec.loader is not None
sys.modules["enrich_steam_stats"] = mod
spec.loader.exec_module(mod)


class EnrichSteamStatsTests(unittest.TestCase):
    def test_build_stats_steam_reviews_not_metacritic(self) -> None:
        details = {
            "release_date": {"date": "Oct 17, 2024"},
            "price_overview": {"final": 2999},
            "metacritic": {"score": 93},
            "genres": [{"description": "Action"}],
        }
        stats = mod.build_stats(details, (92, 885586), 25604, ["VR"], None)
        self.assertEqual(stats["reviewPercent"], 92)
        self.assertEqual(stats["reviewCount"], 885586)
        self.assertEqual(stats["currentPlayers"], 25604)
        self.assertEqual(stats["priceUsd"], 29.99)
        self.assertNotEqual(stats["reviewPercent"], 93)

    def test_build_stats_preserves_prior_when_partial(self) -> None:
        prior = {"releaseDate": "Jan 1, 2020", "priceUsd": 9.99, "tags": ["Indie"]}
        stats = mod.build_stats(None, (80, 100), 42, [], prior)
        self.assertEqual(stats["releaseDate"], "Jan 1, 2020")
        self.assertEqual(stats["priceUsd"], 9.99)
        self.assertEqual(stats["reviewPercent"], 80)
        self.assertEqual(stats["currentPlayers"], 42)


if __name__ == "__main__":
    raise SystemExit(unittest.main())
