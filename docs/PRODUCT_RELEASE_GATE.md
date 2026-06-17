# Product release gate notes

`product-release.yml` runs `pre-release-gate.sh --product-release --skip-triage --skip-ci-check` before building installers.

- **`--skip-triage`:** Dependabot triage is tracked separately in weekly health-check; product tags rely on `wait-for-ci` instead.
- **`--skip-ci-check`:** Avoids duplicate polling — the workflow already waits for CI on the triggering SHA via `wait-for-ci`.

Local releases should run the full gate when cutting tags manually:

```bash
bash scripts/pre-release-gate.sh --product-release
```

See [LOCAL_RELEASE.md](LOCAL_RELEASE.md) for signed zip/MSI orchestration.
