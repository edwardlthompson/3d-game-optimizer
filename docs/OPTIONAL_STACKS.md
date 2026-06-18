# Optional Stack Modules

The init script stack picker (`web` / `python` / `android` / `multi` / **`product`**) controls which modules are active in `AGENT_MEMORY.md`.

## This product (active)

| Module | Guide | Production path |
|--------|-------|-----------------|
| WinUI desktop | `modules/winui/MODULE.md` | `src/SpatialLabsOptimizer/` |
| Web catalog | `modules/web/MODULE.md` | `site/catalog/` |
| Node / Worker | `modules/node/MODULE.md` | `workers/steam-library/` |
| Python tooling | `modules/python/MODULE.md` | `scripts/sync-catalog/` |

Sync stack selection:

```bash
python3 scripts/init-stack-sync.py product . false
```

## Inactive stubs (kept, not deployed)

Template reference code remains for bootstrap parity. **Do not read unless extending optional stacks.**

| Module | Guide | Stub path | CI |
|--------|-------|-----------|-----|
| Android / F-Droid | `modules/android/MODULE.md` | `examples/android/` | Job runs only when path changes |
| Lightroom plugin | `modules/lightroom/MODULE.md` | `examples/lightroom/` | Job runs only when path changes |
| Rust | `modules/rust/MODULE.md` | `examples/rust/` | Job runs only when path changes |
| Go | `modules/go/MODULE.md` | `examples/go/` | Job runs only when path changes |
| Node API demo | `modules/node/MODULE.md` | `examples/node/` | Hono stub; product uses worker |

Policy **(B) keep stubs**: inactive modules are unchecked in `AGENT_MEMORY.md`. Pruning is optional — see `scripts/init-project.sh`.

## Pruning (optional)

```bash
# Example: drop stacks you will never use
rm -rf examples/rust examples/go examples/lightroom
rm -rf modules/rust modules/go modules/lightroom
```

Update `ci.yml` path filters and re-run `validate-bootstrap.sh` after pruning.

## CI behavior

Optional example CI jobs run only when the corresponding directory **exists** and **changed** on the push/PR (or on `workflow_dispatch`).
