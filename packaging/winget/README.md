# Winget Manifest Submission

Product manifests are generated in CI by `product-release.yml` into `packaging/winget-product/manifest.stub.yaml`.

## Maintainer checklist `[HUMAN]`

1. Download the product release artifact from the latest `SpatialLabsOptimizer-v*` GitHub release.
2. Verify SHA256 of the signed zip matches the manifest `InstallerSha256`.
3. Copy/adapt `packaging/winget-product/manifest.stub.yaml` to the Winget multi-file schema.
4. Open a PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) under `manifests/e/edwardlthompson/SpatialLabsOptimizer/`.
5. Link the PR in `DECISION_LOG.md` at the release milestone.

Local stub generation:

```bash
bash scripts/generate-winget-manifest.sh "edwardlthompson.SpatialLabsOptimizer" "1.0.1" packaging/winget-product path/to/SpatialLabsOptimizer.zip
```

See https://github.com/microsoft/winget-pkgs/blob/master/doc/manifest/schema/1.6.0/installer.md for schema details.
