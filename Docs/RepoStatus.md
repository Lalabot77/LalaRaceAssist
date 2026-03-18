# Repository status

Validated against commit: HEAD
Last updated: 2026-03-18
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Plugin_UI_Tooltips.md` updated for the Profiles tab tidy-up (`CAR` / `LAUNCH` / `SHIFT` / `TRACKS`), the Launch Settings host move, the Dash Control regrouping, and the Global Settings cleanup.
- `Docs/Project_Index.md` refreshed to surface `Plugin_UI_Tooltips.md` in the canonical docs list.
- `Docs/RepoStatus.md` updated with the validation summary for this UI-only tidy-up.
- `Docs/Subsystems/H2H.md` added as the canonical subsystem doc for phase-1 Head-to-Head.
- `Docs/Subsystems/CarSA.md` and `Docs/Subsystems/Opponents.md` updated to clarify their selector-only relationship to H2H.
- `Docs/SimHubParameterInventory.md` updated for the new `H2HRace.*` / `H2HTrack.*` export families.
- `Docs/Project_Index.md` refreshed to include the H2H subsystem in the canonical docs map.

## Delivery status highlights
- Added a new standalone `H2HEngine.cs` phase-1 subsystem with concurrent `H2HRace.*` and `H2HTrack.*` export families.
- `H2HRace.*` uses Opponents only for race target identity selection, then resolves that identity back to live `CarIdx` for standalone H2H timing.
- `H2HTrack.*` uses CarSA only for current track ahead/behind slot selection and accepts that local track targets can swap more often than race targets.
- Phase 1 uses fixed 6 segments with latched deltas, active-segment outputs, and simple dash-facing lap summary values for player/ahead/behind.
- No plugin UI/settings were added, no debug CSV logging was introduced, and CarSA/Opponents ownership boundaries were preserved.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
