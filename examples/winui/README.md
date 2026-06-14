# WinUI Golden Path Example

Reference patterns for agents implementing UI in `src/SpatialLabsOptimizer`.

## What this demonstrates

- `DesignSystem.xaml` token usage
- `OperationProgressHub` event wiring
- Shell layout with `InfoBar` activity readout

## Source of truth

The runnable app is `src/SpatialLabsOptimizer`. This folder documents patterns; copy from main app:

- `Resources/DesignSystem.xaml`
- `Infrastructure/Progress/OperationProgressHub.cs`
- `Views/ShellPage.xaml`

## Checklist for new views

1. Use `AppButton` / `AppIconButton` from `Controls/`
2. Add tooltips to icon-only actions
3. Report progress for operations > 2s
4. Respect `ResponsiveStateService` breakpoints

See [modules/winui/MODULE.md](../../modules/winui/MODULE.md).
