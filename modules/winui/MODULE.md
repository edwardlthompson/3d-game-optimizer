# WinUI 3 Module — 3D Game Optimizer

Golden Path for the Windows desktop hub (`SpatialLabsOptimizer`).

## Stack

- WinUI 3 + .NET 8 + Windows App SDK 1.6+
- MVVM with `CommunityToolkit.Mvvm` (optional) or manual `INotifyPropertyChanged`
- Clean Architecture folders: `Domain`, `Application`, `Infrastructure`, `Views`, `ViewModels`

## Key patterns

| Pattern | Location |
|---------|----------|
| DI host | `Infrastructure/Hosting/ServiceCollectionExtensions.cs` |
| Progress pub/sub | `Infrastructure/Progress/OperationProgressHub.cs` |
| HTTP allowlist | `Infrastructure/Privacy/PrivacyGuard.cs` |
| Design tokens | `Resources/DesignSystem.xaml` |
| Vendor adapters | `Infrastructure/Displays/` (Sprint 4+) |

## Build

```powershell
dotnet build src/SpatialLabsOptimizer/SpatialLabsOptimizer.csproj
```

## Tests

```powershell
dotnet test src/SpatialLabsOptimizer.Tests/SpatialLabsOptimizer.Tests.csproj
```

## Agent rules

- Compose UI from `Controls/` — no orphan button styles in feature views
- All HTTP via `PrivacyGuard` / `ExternalDataGateway`
- Long-running work reports to `OperationProgressHub`
- See `docs/DESIGN_SYSTEM.md` and `docs/UX_PROGRESS.md`
