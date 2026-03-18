# Repository status

Validated against commit: f379358
Last updated: 2026-03-18
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Plugin_UI_Tooltips.md` updated for the final TRACKS planner ownership model and wet-multiplier relocation in the Profiles UI.
- `Docs/Project_Index.md` refreshed to keep the TRACKS tab planner documentation discoverable from the canonical doc entry points.
- `Docs/Subsystems/Fuel_Planner_Tab.md` updated to describe the final track-scoped planner persistence and stored-vs-live race-pace delta behaviour.
- `Docs/RepoStatus.md` updated with the validation summary for the final track-planning cleanup.

## Delivery status highlights
- `TrackStats` now owns the TRACKS planner values for `Wet Fuel Multiplier`, `Race Pace Delta`, `Fuel Contingency Value`, and contingency mode as the single active ownership model.
- Legacy car-level planner JSON keys are now treated as one-time migration inputs only; load-time migration seeds missing track values once, clears the legacy values, and no longer keeps mixed ownership paths alive.
- Wet-factor migration now prefers the true legacy planner wet multiplier before any default-generated wet-condition multiplier, so customised old planner values are not overwritten by a synthetic `90%` default.
- `FuelCalcs` now loads and saves those four planner inputs from the resolved `TrackStats` record instead of the selected car profile, while `PreRaceMode`, `TireChangeTime`, `RefuelRate`, `BaseTankLitres`, `Pit Entry Decel`, and `Pit Entry Buffer` remain in their existing scopes.
- Loading stored per-track `Race Pace Delta` now populates the track default without forcing manual leader-delta mode, so live leader-delta telemetry continues to take over automatically unless the driver explicitly edits the value.
- The `TRACKS` tab now uses a `Track Planning` group for contingency/mode and race pace delta, and moves `Wet Fuel Multiplier` into the `Wet vs Dry Avg Deltas` section near the wet-condition workflow.
- Save chatter was reduced by removing selection-time migration saves and by only saving when a real migration/default mutation occurs.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
