# Trainer & Mod Manager Coexistence

Sprint 33 adds coexistence detection for external tools that may conflict with UEVR/ReShade injection.

## Detected tools

| Category | Process names |
|----------|---------------|
| Trainers | `WeMod`, `Wand` |
| Mod managers | `Vortex`, `ModOrganizer` |

Detection uses `IRunningProcessProbe` (production: `RunningProcessProbe`; tests: fakes).

## User settings

Stored in SQLite via `UserPreferencesService`:

| Key | Default | Meaning |
|-----|---------|---------|
| `trainer_coexistence` | `true` | Allow launch when trainers are running (game-first path) |
| `mod_manager_coexistence` | `true` | Allow launch when mod managers are running (game-first path) |

Configure in **Toolchain Health** (trainer and mod manager toggles).

## Launch policies

`CoexistenceLaunchPolicy`:

- **Block** — external tool running and coexistence disabled for that category → `3DGO-0004`
- **GameFirst** — coexistence enabled and tool detected → launch game exe/Steam directly, skip UEVR injector wrapper, wait for game process
- **SafeLaunch** — global/per-game safe launch (no injectors); handled before coexistence evaluation in `PlayIn3D`

## Flow (`PlayIn3D`)

1. Resolve launch plan and display profile.
2. If safe launch (global or per-game) → Steam `-applaunch` only.
3. Read trainer/mod manager coexistence prefs.
4. `ExternalToolCoexistenceService.Evaluate` → block or build `LaunchContext`.
5. If **GameFirst** → `GameFirstLaunchOrchestrator.LaunchAsync`.
6. Otherwise → `AutoFallbackLaunchService.LaunchWithFallbackAsync(plan, context)`.

## Adapter behavior

`LaunchContext` is passed to all launch adapters:

- **UevrLauncher** — when `context.IsGameFirst`, skips `UEVRInjector.exe` and starts the game binary or Steam.
- **ReShadeLauncher** — when `context.IsGameFirst`, skips ReShade config apply before launch (avoids fighting mod manager overlays).

## Error code

| Code | When |
|------|------|
| `3DGO-0004` | Trainer or mod manager running with coexistence off |

Recovery: enable coexistence in Toolchain Health, close the external tool, or use Safe launch.

## Testing

`CoexistenceLaunchTests` uses a fake `IRunningProcessProbe` to simulate running tools without requiring WeMod/Vortex installed.

QA matrix V13 scenarios are enforced by `scripts/check-qa-matrix-coverage.sh` (`V13_MAP`).

## Compliance

Trainers may violate game terms of service. Coexistence mode only adjusts launch order; the user remains responsible for ToS compliance.
