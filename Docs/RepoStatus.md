# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Added a persisted car-level refuel-rate lock (`CarProfile.RefuelRateLocked`) with backward-compatible default-unlocked JSON behavior.
- Added a `Locked` control to the existing Profiles → CAR refuel-rate row, following existing lock UX patterns used elsewhere in the plugin.
- Gated refuel-rate auto-persist so valid learned rates no longer overwrite a locked profile value; unlocked flow remains unchanged.
- Added concise verbose-debug observability for blocked overwrite attempts (`[LalaPlugin:Refuel Rate] Locked; blocked learned overwrite ...`).

## Reviewed documentation set
### Changed in this sweep
- `CarProfiles.cs`
- `ProfilesManagerView.xaml`
- `ProfilesManagerViewModel.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Fuel_Planner_Tab.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `FuelCalcs.cs`
- `FuelCalculatorView.xaml`

## Delivery status highlights
- Refuel-rate learning now respects a persisted lock gate, preventing auto-overwrite of trusted stored rates when locked.
- Existing unlocked behavior is preserved: valid learned refuel rates continue to save to the active profile.
- Existing profiles remain backward-compatible and default to unlocked when the new field is absent.
- Profile create/copy/default flows now preserve/carry the refuel-rate lock field consistently.

## Validation note
- Validation recorded against `HEAD` (`Refuel-rate lock gate + Profiles UI lock control + docs sync`).
