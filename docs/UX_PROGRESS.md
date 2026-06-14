# UX Progress Tracker

## Current UX North Star

Deliver a one-click flow that helps users select a supported display, detect compatible games, apply safe presets, and launch with clear rollback options.

## Milestones

| Milestone | Scope | Status | Exit Criteria |
|---|---|---|---|
| M1 Foundation | shell layout, navigation, theme tokens | In progress | keyboard navigation + baseline accessibility pass |
| M2 Setup Flow | display detection and recommended defaults | Planned | user reaches playable profile in <= 5 clicks |
| M3 Game Compatibility | game catalog, filter, tier indicators | Planned | top 20 seeded games display compatibility reliably |
| M4 Tool Automation | install/verify external tools | Planned | unattended tool setup with clear error messages |

## UX Risks

- Confusion between "display supported" and "game fully supported".
- Overly technical terms around stereoscopic rendering methods.
- Silent tool automation trust concerns if status feedback is weak.

## Immediate UX Actions

- Add glossary for terms like SBS, depth, convergence, and tier levels.
- Include "What changed" summary before applying presets.
- Ensure every automated action has a rollback or undo guidance link.
