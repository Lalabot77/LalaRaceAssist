# Repository status

Validated against commit: HEAD
Last updated: 2026-03-17
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/SimHubParameterInventory.md` updated for the revised `LalaLaunch.PreRace.*` public contract (Auto delta basis fix + new `StatusText` export).
- `Docs/Plugin_UI_Tooltips.md` updated to describe pit mode selection as a PreRace on-grid info layer control.
- `Docs/Subsystems/Fuel_Model.md` updated to replace Strategy export guidance with the PreRace adapter/export contract and source-label behavior.
- `Docs/Subsystems/Fuel_Planner_Tab.md` updated to clarify planner authority vs PreRace display intent.

## Delivery status highlights
- Decoupled persisted PreRace mode from planner logic: new `SelectedPreRaceMode` (FuelCalcs runtime) with persisted `PreRaceMode` fields in profiles/presets; legacy `PitStrategyMode` JSON keys map into the PreRace-only field for compatibility.
- Removed PreRace mode influence from planner calculations (`CalculateSingleStrategy` no longer branches on mode intent), so mode changes no longer trigger planner recalculation side effects.
- Refactored race-start dash-facing outputs from `LalaLaunch.Strategy.*` to `LalaLaunch.PreRace.*` in `LalaLaunch.cs`.
- Implemented unified PreRace outputs with one stints value, one total-fuel value, and one delta value; Auto now mirrors planner outputs when planner values are available and falls back at runtime when unavailable.
- Preserved selected mode persistence/label behavior (0 No Stop, 1 Single Stop, 2 Multi Stop, 3 Auto).
- PreRace race distance basis remains `DataCorePlugin.GameRawData.CurrentSessionInfo._SessionTime` (+ after-zero allowance).
- Implemented explicit source ordering for pre-race manual-mode inputs and a fixed +2-lap manual allowance:
  - Fuel burn: planner/profile value -> SimHub computed burn -> hard fallback (3.0 L/lap).
  - Lap time: planner/profile value -> SimHub/iRacing predicted value chain -> hard fallback (120.0 s).
- `Single Stop` PreRace delta continues to use current fuel + pit-menu refuel intent (`PitSvFuel`) for live on-grid response.
- `Auto` PreRace totals/stints remain planner-first; Auto delta uses live pit-menu add intent only when planner-required next add (`PlannerNextAddLitres`) is greater than zero, and otherwise falls back to planner fuel-basis delta without pit-menu add intent.
- Added `LalaLaunch.PreRace.StatusText` export (`STRATEGY OKAY` / `STRATEGY MARGINAL` / `UNABLE STRATEGY`) with initial mode+stints thresholding and explicit +0.2 stints marginal tolerance for No Stop and Single Stop.
- Planner and continuous live `Fuel.*` model behavior remain unchanged beyond read-only reuse of existing inputs.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
