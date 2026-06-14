# Design System

## Design Principles

- **Clarity first:** prioritize actionable status over decorative UI.
- **Safe automation:** every automated change explains scope and impact.
- **Progressive disclosure:** beginner defaults with advanced controls available.
- **Accessible by default:** support keyboard-first workflows and readable contrast.

## Core Tokens

- **Color roles:** `Surface`, `SurfaceAlt`, `Accent`, `Success`, `Warning`, `Danger`, `MutedText`.
- **Typography:** Segoe UI variable stack, minimum 14px body text.
- **Spacing scale:** 4, 8, 12, 16, 24, 32.
- **Radius:** 8 for cards, 6 for controls, 12 for dialogs.

## Component Set

- `AppShell`: left nav + top status region.
- `StatusCard`: titled card with icon, state, and action button.
- `TierBadge`: vendor-specific compatibility tier pill.
- `ActionBar`: primary/secondary action cluster with loading states.
- `DisclosurePanel`: advanced options with warning callout.

## Feedback Rules

- Use inline validation for configuration errors.
- Show toasts only for cross-page completion events.
- For automation failures, provide copyable diagnostics and next-step guidance.

## Accessibility Requirements

- WCAG 2.2 AA contrast targets.
- Visible focus indicators on all interactive controls.
- Screen-reader labels for vendor names, tool states, and compatibility tiers.
