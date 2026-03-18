# Repository status

Validated against commit: HEAD
Last updated: 2026-03-18
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Plugin_UI_Tooltips.md` updated for the Profiles tab tidy-up (`CAR` / `LAUNCH` / `SHIFT` / `TRACKS`) and the Global Settings cleanup.
- `Docs/Project_Index.md` refreshed to surface `Plugin_UI_Tooltips.md` in the canonical docs list.
- `Docs/RepoStatus.md` updated with the validation summary for this UI-only tidy-up.

## Delivery status highlights
- Profiles UI sub-tabs now read `CAR`, `LAUNCH`, `SHIFT`, and `TRACKS`; the old `DASH` and `FUEL` tabs were removed.
- The `CAR` tab now contains the existing car-profile-backed refuel, base-tank, rejoin, overtake, spin, and pit-entry controls without changing their bindings or persistence scope.
- The `TRACKS` tab now hosts the existing `Wet Fuel Multiplier`, `Race Pace Delta`, and contingency controls at the top of the workflow, but they still bind to the same existing car-profile properties as before.
- Global Settings no longer shows the duplicated per-profile `USER VARIABLES` block; the remaining sections continue to cover true global settings only.
- No persistence/schema/runtime behavior changed in this tidy-up: car-profile save/load, default-profile copy seeding, fuel planner/runtime math, launch behavior, shift behavior, pit-entry math, and telemetry behavior remain as before.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
