# Repository status

Validated against commit: HEAD
Last updated: 2026-04-08
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Implemented the session-change starvation fix in `LalaLaunch.DataUpdate`: refuel cooldown is now localized to the refuel-learning block and no longer exits the full tick.
- Added a unified transient runtime recovery seam: `ManualRecoveryReset(string reason)` in `LalaLaunch`.
- Repurposed existing `PrimaryDashMode` action to trigger manual recovery reset while keeping action name compatibility intact.
- Wired landing-tab `ResetPlugin_Click` to the same manual recovery reset seam.
- Consolidated session token and session-type transition reset execution through `ManualRecoveryReset("Session transition")` (with fuel-model transition handling preserved).
- Added a canonical INFO log for the recovery seam trigger reason.

## Reviewed documentation set
### Changed in this sweep
- `LalaLaunch.cs`
- `OverviewTabView.xaml.cs`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Shift_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Subsystems/Fuel_Planner_Tab.md`
- `Docs/Subsystems/Message_System_V1.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/H2H.md`
- `OverviewTabView.xaml`
- `FuelCalcs.cs`
- `ShiftAssistEngine.cs`
- `ShiftAssistAudio.cs`
- `ShiftAssistLearningEngine.cs`

## Delivery status highlights
- Fixed DataUpdate starvation path by removing the global early-return from refuel cooldown handling.
- Added bounded runtime-only recovery reset orchestration without altering profile persistence, learned targets, or subsystem algorithms.
- Session transitions now use the same reset orchestration path, reducing reset asymmetry risk.
- Opponents rewrite remained out of scope; `Opponents.cs` was not modified.

## Validation note
- Validation recorded against `HEAD` (`DataUpdate starvation fix + manual recovery reset wiring + docs sync`).
