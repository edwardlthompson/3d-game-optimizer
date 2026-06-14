# Local Game Folders

User-defined watch folders let SpatialLabs Optimizer discover games installed outside Steam, Epic, or GOG (offline copies, DRM-free installs, manual extracts).

## Privacy

- Folder paths are stored only in local SQLite (`settings.db` and `library.db` on your PC).
- No folder paths or filenames are uploaded unless you explicitly enable future opt-in telemetry.
- Scans never walk entire drives — only directories you add in **Settings → Library folders**.

## Watch list

- Persisted as JSON array key `local_game_folders` via `LocalGameFolderRepository`.
- Add/remove folders in **Library Settings** (`LibrarySettingsView`) using the Windows folder picker.
- Click **Refresh scan** to re-run discovery without restarting the app.

## Scan rules (`LocalFolderGameScanner`)

| Rule | Value |
|------|-------|
| Max depth | 2 levels below each watch root |
| Candidates | `*.exe` in scanned folders |
| Excluded names | `unins*`, `setup`, `redist`, `EasyAntiCheat`, `BattlEye`, `install`, `vcredist`, `dxsetup` |
| Stable ID | `ExternalStoreIdMapper.StableAppId("Local", "{normalizedFolder}|{exeName}")` |
| Title | Folder name containing the chosen executable |

When multiple executables exist in one folder, the largest non-excluded `.exe` is chosen.

## Library merge

During `LibraryIndexer.IndexAsync`, local installs merge like Epic/GOG titles:

- Upsert into `local_game_installs` (`stable_app_id`, `folder_path`, `launch_exe`, `display_title`, `last_scanned_at`, `is_stale`).
- Upsert catalog row with `ReviewDescriptor = "Local"` and `CompatibilityTier.Experimental`.
- Removing a folder or deleting games marks missing entries `is_stale = 1`.

## Launch behavior

`LocalGameInstallResolver` wraps `GameInstallPathResolver`:

1. Resolve stable ID against `local_game_installs` (in-memory cache + SQLite).
2. Launch the stored `launch_exe` directly via `TrueGameLauncher` / `UevrLauncher` / `ReShadeLauncher`.
3. Fall back to Steam manifest lookup when no local record exists.

Local games do **not** use `steam.exe -applaunch`.

## Library UI

- **Local installs** filter and **Local** tile badge.
- **Why not ready?** inverse filter with actionable hints.
- **Smart collections**: favorites + tier, never played in 3D, local-only.
- **Recent launches** table (`recent_launches` in `library.db`).
- Per-game **compatibility notes** (local settings keys `compat_note:{appId}`).
- **Preset freshness** label and refresh action on selected title.

## Tests

- `LocalFolderGameTests.cs` — scanner heuristics, stable IDs, resolver, index merge, stale marking.
- `LibraryIntelligenceTests.cs` — filters, badges, recent launches, notes, preset freshness.

QA matrix: `V14_MAP` (library intelligence) and `V15_MAP` (local folders) in `scripts/check-qa-matrix-coverage.sh`.
