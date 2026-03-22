# Repository status

Validated against commit: HEAD
Last updated: 2026-03-22
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Fixed the Preset Manager `PreRace Mode` combo by removing the fragile custom popup template and reusing the same standard ComboBox interaction pattern already working on the main Strategy page.
- Hardened the Strategy `Presets...` popup open path so null preset entries are skipped before binding and open-time failures are caught/logged instead of bubbling out as a host-level hard fail.
- Kept scope inside the existing Fuel Planner / Strategy and Settings/UI ownership boundaries without changing fuel math, learning, Shift Assist logic, CarSA, H2H, track-marker behavior, or telemetry gating.

## Reviewed documentation set
### Changed in this sweep
- `CHANGELOG.md`
- `FuelCalculatorView.xaml.cs`
- `FuelCalcs.cs`
- `PresetsManagerView.xaml`
- `PresetsManagerView.xaml.cs`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Subsystems/Fuel_Planner_Tab.md`
- `Docs/Strategy_System.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `README.md`
- `Docs/Project_Index.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `Docs/Dashboards.md`
- `Docs/Shift_Assist.md`
- `Docs/Launch_System.md`
- `Docs/Rejoin_Assist.md`
- `Docs/Pit_Assist.md`
- `Docs/H2H_System.md`
- `Docs/Profiles_System.md`
- `Docs/Fuel_Model.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Subsystems/Pit_Entry_Assist.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Subsystems/Rejoin_Assist.md`
- `Docs/Subsystems/Opponents.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/H2H.md`
- `Docs/Subsystems/Profiles_And_PB.md`
- `Docs/Subsystems/Track_Markers.md`
- `Docs/Subsystems/Trace_Logging.md`
- `Docs/Subsystems/Message_System_V1.md`
- `Docs/Subsystems/MessageEngineV1_Notes.md`

## Delivery status highlights
- The Preset Manager `PreRace Mode` selector now uses the same working ComboBox interaction model as the main Strategy tab, restoring normal open/select behavior while staying readable on the popup’s dark theme.
- The Strategy `Presets...` modal open flow now skips null preset rows during binding and shows a logged warning dialog instead of hard-failing if window creation still throws.

## Validation note
- Validation recorded against `HEAD` (`preset manager combo behavior and popup-open hardening`).
