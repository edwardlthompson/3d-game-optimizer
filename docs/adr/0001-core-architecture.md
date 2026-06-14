# ADR-0001: Core Architecture for 3D Game Optimizer

- **Status:** Accepted
- **Date:** 2026-06-14
- **Deciders:** Core maintainers

## Context

3D Game Optimizer is a Windows desktop application intended to provide a one-click setup flow for glasses-free and stereoscopic 3D PC gaming. The product must remain usable offline, protect user privacy by default, support silent automation for external tools, and ingest compatibility/device metadata from versioned seed files.

## Decision

The project adopts **MVVM on top of Clean Architecture**:

- **Presentation (WinUI 3 + MVVM):**
  - `View` handles XAML layout and accessibility semantics.
  - `ViewModel` exposes commands/state and orchestrates user workflows.
  - No filesystem, registry, process, network, or Steam API calls in views.
- **Application (Use Cases):**
  - Encapsulates tasks such as display detection, game compatibility lookup, preset generation, and tool execution planning.
  - Enforces privacy and policy checks before any side effects.
- **Domain:**
  - Pure business models: `DisplayProfile`, `GameCompatibility`, `ToolEntry`, `PerformanceTier`.
  - Contains deterministic validation and recommendation rules.
- **Infrastructure (Adapters):**
  - Steam integration, EDID/registry adapters, process execution wrappers, and file repositories.
  - External APIs and binaries remain behind interfaces.

## Non-Negotiable Constraints

### Silent Install and Automation

- External tool installation and updates must support non-interactive flows where tool licensing allows it.
- Command arguments for unattended installs are defined in `data/tools/tool-manifest-v1.json`.
- All process launches are mediated by a single runner that logs only operational metadata, never personal data.

### Zero Data Sharing by Default

- No telemetry, usage analytics, or cloud sync is enabled by default.
- Compatibility and preset data are loaded from local versioned JSON assets.
- Optional network actions (for metadata refresh) require explicit user action and clear in-product explanation.

### Legal Disclaimers in Product Surface

- The app must clearly state that:
  - third-party trademarks belong to their owners,
  - game/tool/display compatibility is best-effort guidance,
  - some 3D effects may impact comfort or accessibility.
- Legal copy source of truth: `docs/LEGAL.md` and `docs/TRADEMARKS.md`.

### API-First External Data Contract

- Seed data uses schema-first JSON contracts in `data/compatibility/schema.json`.
- Internal code consumes repositories and DTOs, never raw ad-hoc JSON dictionaries.
- New data providers (Steam API, vendor catalogs, community feeds) must map to the same canonical schema before entering core logic.

## Consequences

- Easier unit testing of recommendation logic and compatibility ranking.
- Strong boundary between UX and volatile external tool/platform dependencies.
- Slightly higher upfront structure cost, with lower long-term maintenance risk.

## Alternatives Considered

| Alternative | Why not selected |
|---|---|
| UI-centric code-behind architecture | Fast to start, but poor testability and weak boundary control for process/network operations. |
| Pure Clean without MVVM conventions | Strong core separation, but less ergonomic for WinUI command/state binding patterns. |
| Plugin-first architecture from day one | Useful later, but premature for initial product scope and team size. |
