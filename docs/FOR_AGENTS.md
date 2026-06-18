# For Agents

## Phased Loading

SessionStart -> START_HERE.md -> Mode -> AGENTS.md -> BUILD_PLAN Sequential -> Active module -> WEB_PROJECT_LAYOUT (web hosting) -> DESIGN_GUIDE (web/android UI) -> Plan Mode -> Execute

## Token Economy

1. Never read all of `examples/` - active stack only
2. Never fill KNOWLEDGE_BASE.md with generic framework docs
3. Update memory files only at session start, milestone end, or architectural pivot
4. Read-before-write: @filename before edits
5. Sequential before Parallel in BUILD_PLAN

## Parallel Guardrails

- Branch: `feature/agent-[task-name]` per agent, separate worktree
- No overlapping file scopes (run `scripts/check-parallel-scope.sh` before dispatch)
- Shared schema/types: sequential agent only first
- Scope map: `docs/PARALLEL_AGENT_SCOPES.md`

## 3-Strike Rule

After 3 failed fix attempts: halt, summarize conflict, request human direction.

Do not loop on the same file with identical errors. Escalate with:
- Failing command output (last attempt only)
- Files touched
- Proposed next options for human pick

## Session Checkpoint

1. Copy `.cursor-session-state.example.json` to `.cursor-session-state.json`
2. Fill `stack`, `active_sprint`, `sequential_step`, `last_files_touched`
3. Clear chat; on restart read the state file before BUILD_PLAN Parallel lane
4. Delete `.cursor-session-state.json` after successful restore

Stack selection from init lives in `.cursor/stack-selection.json`.

## Failure Playbook

### CI poll after push

```bash
bash scripts/check-github-ci.sh --wait 300
# Windows: pwsh scripts/check-github-ci.ps1 -WaitSeconds 300
```

Required green workflows: **CI**, **Security Scan**, **CodeQL**.

If a job is missing, wait - GitHub may not have enqueued it yet. If `FAIL` persists:
1. Open the run URL from script output
2. Fix locally; re-run `validate-bootstrap.sh` before pushing again
3. Do not mark BUILD_PLAN `[AUTO]` items complete while red

### GH_TOKEN / gh CLI

- `validate-workflow-actions.sh` and `check-github-ci.sh` need `gh auth login`
- In CI, `GITHUB_TOKEN` is injected automatically; locally export `GH_TOKEN` if using a PAT
- `gh: HTTP 401` -> re-authenticate; `404` -> confirm repo remote and `gh repo set-default`

### Dependabot conflicts

1. Triage Critical/High first (`docs/SECURITY_TRIAGE.md`)
2. For conflicting lockfile PRs: checkout branch, `npm ci` / `uv sync --locked`, run tests, push
3. Transitive CVEs without direct bump: see KNOWLEDGE_BASE KB-007 overrides policy
4. Never merge with failing **dependency-review** on PRs

### Parallel scope collision

Before launching parallel agents:

```bash
bash scripts/check-parallel-scope.sh
```

If overlap is reported, split tasks or serialize the conflicting rows in BUILD_PLAN.

### Encoding failures on Windows

Run `python3 scripts/check-file-encoding.py` after edits. Write text with UTF-8 (no BOM); never UTF-16.

### Bash gates on Windows (no WSL)

Many scripts (`validate-bootstrap.sh`, `feature-gate.sh`, `watch-agent-gates.sh`, `check-repo-hygiene.sh`) require **bash**. On Windows without WSL they fail with "no installed distributions".

**Fallback (product stack):**

```powershell
python scripts/check-file-encoding.py
dotnet test src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj -c Release
cd site/catalog; npm test
cd workers/steam-library; npm test
pwsh scripts/check-github-ci.ps1 -WaitSeconds 300   # after push
```

Treat **GitHub Actions on `main`** as authoritative when local bash is unavailable. Install WSL or Git Bash for full `/gates` parity.

## WinUI host and UI thread contract

The app builds the DI host on a background thread (`App.FinishStartupAsync` uses `Task.Run`) so startup splash stays responsive. Singleton ViewModels are constructed during that build, so `SynchronizationContext.Current` is **null** in their constructors.

**Rules:**

1. `MainWindow.ShowShell` assigns `UiThreadDispatcher.Enqueue` to marshal work onto the WinUI `DispatcherQueue`.
2. `ViewModelBase.RunOnUiThread` posts to captured `SynchronizationContext` when present, otherwise falls back to `UiThreadDispatcher.Run`.
3. Progress hub handlers that touch bound collections (e.g. cover tile refresh) must use `RunOnUiThread` — never assume VM creation thread is the UI thread.
4. Do not move host construction to the UI thread without measuring splash/index blocking regressions.

**Key paths:** `App.xaml.cs`, `MainWindow.xaml.cs`, `ViewModels/ViewModelBase.cs`, `Infrastructure/UiThreadDispatcher.cs`
