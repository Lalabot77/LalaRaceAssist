# Repository status

Validated against commit: HEAD
Last updated: 2026-04-10
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Pit Entry Assist distance authority is now stored/plugin-owned pit-entry markers only.
- Removed pit-entry fallback branches that previously read `IRacingExtraProperties.iRacing_DistanceToPitEntry` and `IRacingExtraProperties.iRacing_PitEntryTrkPct`.
- Added a one-time warning log when stored marker authority is unavailable and legacy pit-entry Extra Properties fallbacks are disabled.

## Reviewed documentation set
### Changed in this sweep
- `PitEngine.cs`
- `Docs/Subsystems/Pit_Entry_Assist.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Subsystems/Track_Markers.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`

## Delivery status highlights
- Removed only Pit Entry Assist pit-entry distance fallback reads from iRacing Extra Properties (`DistanceToPitEntry`, `PitEntryTrkPct`).
- Kept pit-speed fallback behavior unchanged (`WeekendInfo.TrackPitSpeedLimit` primary, `iRacing_PitSpeedLimitKph` fallback).
- Updated pit subsystem/log/export docs to reflect stored-marker-only authority for pit-entry distance.

## Validation note
- Validation recorded against `HEAD` (`Pit Entry Assist stored-marker authority cutover + fallback removal docs sync`).
