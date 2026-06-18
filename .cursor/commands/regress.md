# Post-release regression

After product tag `SpatialLabsOptimizer-vX.Y.Z` or template tag `vX.Y.Z`:

1. Run `bash scripts/pre-release-gate.sh --product-release --skip-dotnet --skip-triage --skip-ci-check` (or full gate for template).
2. Verify [GitHub Release](https://github.com/edwardlthompson/3d-game-optimizer/releases) includes zip + MSI for product tags.
3. Confirm GitHub Pages: [catalog](https://edwardlthompson.github.io/3d-game-optimizer/catalog/) and demo stub deployed.
4. Run `dotnet test`, `npm test` in `site/catalog` and `workers/steam-library`.
5. Append regressions to @KNOWLEDGE_BASE.md and BUILD_PLAN `[AUTO]` items.

Begin now.
