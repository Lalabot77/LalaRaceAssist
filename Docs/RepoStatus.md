# Repository status

Validated against commit: f071ec440ef30e7a243dbaa51107493af1a47343
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/Fuel_Planner_Tab.md` updated so the canonical planner doc now describes the top-level Strategy tab name, the preserved inline Race Preset selector, and the modal Preset Manager workflow that replaces the former separate Presets tab.
- `Docs/Plugin_UI_Tooltips.md` updated so the tooltip inventory reflects the Strategy-tab navigation, the new `Presets...` button beside the Race Preset combo, and the Strategy wording inside the preset manager.
- `Docs/Project_Index.md` updated so the subsystem map and tooltip reference describe the Strategy-tab / modal-preset workflow.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- The top-level planner tab is now labeled `STRATEGY`, while the existing planner content and FuelCalcs-backed strategy workflow remain intact.
- The former top-level `PRESETS` tab has been removed; preset management now opens as a modal Preset Manager from the `Presets...` button beside the Strategy tab's Race Preset combo.
- The modal reuses the existing preset editor and shared `FuelCalcs` state, preserving preset selection/application semantics, save-current behavior, and preset persistence without changing preset storage or planner math.
- Strategy-tab preset UX follow-up: the inline `Presets...` action now uses the primary blue button style, the modal auto-selects the active preset (or first available preset) on open, dark-theme preset-manager text/input colours are explicitly readable, and successful `Save Changes` closes the modal without a second success popup.
- `Docs/User Docs/Changelog_Since_PR240.md` extended with concise user-facing highlights through PR #481, including Track planner migration, H2H, H2H follow-up fixes, and the latest Fuel Planner Live Snapshot leader-delta cleanup.
- `Docs/Lala_Plugin_Quick_Start_Guide_v0.3.md` added as the review-friendly source version of the tester quick-start, matching the current setup flow, fuel-planning model, track-marker/pit-learning workflow, and supported race-context aids.
- `Docs/Lala_Plugin_User_Guide_v0.3.md` added as the review-friendly source version of the ship-ready user guide, reflecting current fuel/planner behaviour, track-scoped planner defaults, H2H, pit/rejoin aids, and the current inactive status of the broader message-dash system.
- `Docs/RepoStatus.md` refreshed for the current validation summary.
- User-facing documentation now has review-friendly markdown source guides in the repo: Quick Start is focused on install/bind/learn/lock workflow, while the full User Guide explains the current fuel planner, track-scoped data, race aids, and H2H without implying the broader message-dash system is active.
- The user changelog now covers the recent H2H and fuel-planner updates through PR #481 in a concise tester-facing format instead of stopping short of the latest merged work.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
