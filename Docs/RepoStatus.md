# Repository status

Validated against commit: HEAD
Last updated: 2026-03-18
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Plugin_UI_Tooltips.md` updated for the TRACKS planner ownership migration and wet-multiplier relocation in the Profiles UI.
- `Docs/Project_Index.md` refreshed to note that `Plugin_UI_Tooltips.md` now covers the TRACKS tab's track-scoped planner inputs.
- `Docs/RepoStatus.md` updated with the validation summary for the track-scoped planner migration.

## Delivery status highlights
- `TrackStats` now owns the TRACKS planner values for `Wet Fuel Multiplier`, `Race Pace Delta`, `Fuel Contingency Value`, and contingency mode, while the legacy `CarProfile` copies remain in place only as compatibility fallbacks for older JSON.
- Existing profiles migrate lazily and safely: when a track is loaded or refreshed and one of the new track-scoped planner values is missing, the code seeds it from the legacy car-profile value (or the existing wet-condition multiplier for wet factor) and then saves the track record back out.
- `FuelCalcs` now loads and saves those four planner inputs from the resolved `TrackStats` record instead of the selected car profile, while `PreRaceMode`, `TireChangeTime`, `RefuelRate`, `BaseTankLitres`, `Pit Entry Decel`, and `Pit Entry Buffer` remain in their existing scopes.
- The `TRACKS` tab now uses a `Track Planning` group for contingency/mode and race pace delta, and moves `Wet Fuel Multiplier` into the `Wet vs Dry Avg Deltas` section near the wet-condition workflow.
- No schema-breaking rewrite was introduced: older profile JSON still loads, untouched legacy car-level fields are still accepted, and track values converge naturally on first use/save.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
