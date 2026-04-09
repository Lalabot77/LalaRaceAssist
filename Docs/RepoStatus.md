# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Follow-up fix: locked refuel-rate overwrite blocks now preserve both persistence and runtime/planner applied value when a usable stored locked rate exists.
- Added locked first-fill fail-safe: if locked but no usable stored refuel rate exists, the first valid learned candidate may populate once, after which lock blocking resumes normally.
- Updated Profiles → CAR refuel slider tooltip text to explicitly say live refuel events update the value while unlocked.
- Kept existing unlocked flow unchanged and retained concise verbose-debug observability for blocked overwrite attempts (`[LalaPlugin:Refuel Rate] Locked; blocked learned overwrite ...`).

## Reviewed documentation set
### Changed in this sweep
- `LalaLaunch.cs`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Fuel_Planner_Tab.md`
- `ProfilesManagerView.xaml`
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
- `CarProfiles.cs`
- `ProfilesManagerViewModel.cs`
- `Docs/Internal/Plugin_UI_Tooltips.md`

## Delivery status highlights
- Locked refuel-rate overwrite attempts now keep runtime/planner on the locked stored value (not the blocked learned candidate).
- Locked profiles with unusable stored refuel rates can self-heal via one-time first-fill from the first valid learned candidate.
- Existing unlocked behavior remains unchanged: valid learned refuel rates continue to save/apply.
- Existing profiles remain backward-compatible and default unlocked when the lock field is absent.

## Validation note
- Validation recorded against `HEAD` (`Refuel-rate lock runtime fix + locked first-fill fail-safe + docs sync`).
