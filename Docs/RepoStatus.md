# Repository status

Validated against commit: HEAD
Last updated: 2026-04-13
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Follow-up tightened `PitExit.TimeToExitSec` input validity: `PitExit.RemainingCountdownSec <= 0` now counts as unavailable for blend input selection.
- Added `Car.Player.TrackPct` as a plugin-owned player lap-distance export from player `CarIdxLapDistPct`, normalized to `0..1` with invalid/unavailable fallback to `0`.
- Added additive `PitExit.TimeToExitSec` as a blended pit-exit time-to-exit export for dash use.
- Blending remains intentionally simple: early phase follows `PitExit.RemainingCountdownSec`, later phase converges toward `PitExit.TimeS` as speed approaches limiter speed.
- Limiter-speed source chain implemented as `DataCorePlugin.GameData.PitLimiterSpeed` (primary) then parsed `DataCorePlugin.GameRawData.SessionData.WeekendInfo.TrackPitSpeedLimit` (fallback).
- Existing `PitExit.RemainingCountdownSec` and `PitExit.TimeS` exports were kept unchanged.
- Updated canonical subsystem/export docs and internal changelog to match behavior.

## Reviewed documentation set
### Changed in player track-pct + pit-exit blend task
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/SimHubLogMessages.md`
- `README.md`
- `CHANGELOG.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`

## Delivery status highlights
- Kept ownership boundaries intact: Opponents remains race-scoped pit-exit prediction owner; CarSA ownership was not widened.
- Added plugin-owned `Car.Player.TrackPct` under the existing `Car.Player.*` family with strict normalization/sanitization behavior.
- Added plugin-owned `PitExit.TimeToExitSec` without changing Opponents predictor internals or removing legacy pit-exit exports.
- No new log lines were added; `Docs/Internal/SimHubLogMessages.md` remained unchanged.

## Validation note
- Validation recorded against `HEAD` (`PitExit.TimeToExitSec zero-countdown availability fix`).
