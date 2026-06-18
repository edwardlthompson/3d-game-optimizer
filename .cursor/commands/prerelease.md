# Pre-release gate

**Product release** (SpatialLabsOptimizer-v*):

```bash
bash scripts/pre-release-gate.sh --product-release --skip-dotnet --skip-triage --skip-ci-check
```

**Template track** (v*):

```bash
bash scripts/pre-release-gate.sh
```

Confirm CI + Security Scan + CodeQL green on `main`, zero Critical/High Dependabot alerts.
Do not tag or `/push` until this gate passes. See `docs/PRODUCT_RELEASE_GATE.md`.

Begin now.
