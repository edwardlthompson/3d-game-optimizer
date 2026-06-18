# Sprint 0 bootstrap

Guide through BUILD_PLAN Child Repo Playbook Sprint 0 Sequential lane.

1. Confirm repo was created via **Use this template** and @docs/INITIALIZATION_PROMPT.md placeholders are filled ([HUMAN] if not).
2. For this child repo, run `python scripts/init-stack-sync.py product . false` (stack already customized).
3. Run `scripts/setup-github-repo.sh` when `gh` is authenticated ([HUMAN] on API 422 — follow printed checklist).
4. Run `bash scripts/validate-bootstrap.sh --quick` and `bash scripts/feature-gate.sh --stack product`.
5. Pick Cursor mode per @docs/CURSOR_MODES.md; follow Section 8 Startup Sequence.

Begin now.
