# Repository status

Validated against commit: bef8bed
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/Fuel_Planner_Tab.md` updated so the canonical planner doc now states that Live Snapshot max fuel is locked to the live detected cap only, while Profile mode retains manual/preset max-fuel behavior.
- `Docs/Plugin_UI_Tooltips.md` updated so the Strategy-tab max-fuel tooltip text explicitly calls out the Profile-mode edit path and the Live Snapshot lock-to-live-cap behavior.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- The top-level planner tab is now labeled `STRATEGY`, while the existing planner content and FuelCalcs-backed strategy workflow remain intact.
- The former top-level `PRESETS` tab has been removed; preset management now opens as a modal Preset Manager from the `Presets...` button beside the Strategy tab's Race Preset combo.
- The modal reuses the existing preset editor and shared `FuelCalcs` state, preserving preset selection/application semantics, save-current behavior, and preset persistence without changing preset storage or planner math.
- Strategy-tab preset UX follow-up: the inline `Presets...` action now uses the primary blue button style, the modal auto-selects the active preset (or first available preset) on open, dark-theme preset-manager text/input colours are explicitly readable, and successful `Save Changes` closes the modal without a second success popup.
- Strategy-tab preset-manager follow-up: the modal now keeps the PreRace Mode combo on the same dark input styling as the rest of the plugin, spaces the active-preset helper text away from the name field, detaches its `FuelCalcs.PropertyChanged` listener when the dialog unloads/closes, and uses a small Strategy-tab guard to avoid accidental re-entrant dialog opens.
- `Docs/User Docs/Changelog_Since_PR240.md` extended with concise user-facing highlights through PR #481, including Track planner migration, H2H, H2H follow-up fixes, and the latest Fuel Planner Live Snapshot leader-delta cleanup.
- `Docs/Lala_Plugin_Quick_Start_Guide_v0.3.md` added as the review-friendly source version of the tester quick-start, matching the current setup flow, fuel-planning model, track-marker/pit-learning workflow, and supported race-context aids.
- `Docs/Lala_Plugin_User_Guide_v0.3.md` added as the review-friendly source version of the ship-ready user guide, reflecting current fuel/planner behaviour, track-scoped planner defaults, H2H, pit/rejoin aids, and the current inactive status of the broader message-dash system.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
