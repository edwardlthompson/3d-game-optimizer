# Local validation gates

Run Sprint 0 / pre-push validation (Git Bash on Windows; WSL or CI for full bash suite):

```bash
bash scripts/validate-bootstrap.sh --quick
bash scripts/feature-gate.sh --stack product
bash scripts/check-repo-hygiene.sh
```

Windows without bash: `python scripts/check-file-encoding.py`, `dotnet test`, `npm test` in `site/catalog` and `workers/steam-library`.

Report pass/fail per script. Fix failures in scope before marking BUILD_PLAN items complete.

Begin now.
