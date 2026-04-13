# Repository status

Validated against commit: HEAD
Last updated: 2026-04-13
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Added driver-facing `Pit.Box.*` in-box countdown exports: `Pit.Box.Active`, `Pit.Box.ElapsedSec`, `Pit.Box.RemainingSec`, and `Pit.Box.TargetSec`.
- Countdown uses existing pit timing + service math only: elapsed comes from `PitEngine.PitStopElapsedSec`; target now comes from concurrent service model `max(fuelTime, tireTime, mandatoryRepairTime[, optionalRepairTime when enabled])` while boxed.
- Countdown is boxed-phase only and conservative: active only in valid in-box service state; all countdown fields publish `0` when inactive/unavailable (including drive-throughs and missed-box phases).

## Reviewed documentation set
### Changed in pit-box countdown sweep
- `LalaLaunch.cs`
- `GlobalSettingsView.xaml`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Subsystems/Launch_Mode.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubLogMessages.md`

## Delivery status highlights
- Added repair-aware boxed service targeting in `LalaLaunch` so mandatory repair-left time is included concurrently while boxed; optional repair-left time is included only when the new setting is enabled.
- Added user setting toggle `Pit.Box: include optional repairs in service countdown` in `GlobalSettingsView` (default off).
- Contract tidy-up: optional-repair toggle remains a persisted/UI plugin setting (not a `Settings.*` SimHub export), and parameter inventory wording now reflects that.
- Kept strict safe defaults: countdown exports are hard-zero when inactive or unavailable.

## Validation note
- Validation recorded against `HEAD` (`Pit.Box in-box countdown exports + docs sync`).
