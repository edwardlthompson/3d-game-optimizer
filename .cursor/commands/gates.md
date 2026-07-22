# Local validation gates

> Skills: `.cursor/skills/validate-bootstrap/`, `.cursor/skills/check-repo-hygiene/`, `.cursor/skills/canvas-bootstrap-status/`

Run Sprint 0 / pre-push validation (Git Bash on Windows; WSL or CI for full bash suite):

```bash
bash scripts/validate-bootstrap.sh --quick
bash scripts/feature-gate.sh --stack product
bash scripts/check-repo-hygiene.sh
```

Windows without bash: `python scripts/check-file-encoding.py`, `dotnet test`, `npm test` in `site/catalog` and `workers/steam-library`.

Optional: `python3 scripts/agent-run.py validate-bootstrap --quick` when preferring the agent-run wrapper.

Report pass/fail per script. Fix failures in scope before marking BUILD_PLAN items complete.

Optional status overview: invoke skill `canvas-bootstrap-status` (Canvas, or markdown table fallback).

Begin now.
