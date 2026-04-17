# Repository status

Validated against commit: HEAD
Last updated: 2026-04-15
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Extended `Pit.Box.DistanceM` and `Pit.Box.TimeS` publication window so pit-box countdown can appear slightly before pit-lane entry when pit-owned authority is valid.
- New visibility gate: in pit lane OR pit-owned pre-entry context (`PitPhase.EnteringPits`) OR fallback player track-percent band `[0.80..1.00] ∪ [0.00..0.20]`; outside gate or invalid authority still publishes `0`.
- Added plugin-owned dash helper export `Pit.Box.BrakeNow` with pit-limit-aware threshold scaling (`25.0 * pitLimitKph / 80.0`) using existing limiter authority chain (`GameData.PitLimiterSpeed` then parsed `WeekendInfo.TrackPitSpeedLimit`).
- Kept ownership boundaries intact: pit-owned logic remains in plugin runtime (`LalaLaunch.cs` + `PitEngine` authority seams), dashboards stay presentation-only.
- Updated subsystem/internal docs and development changelog to match final runtime/export contract.

## Reviewed documentation set
### Changed in pit-box pre-entry visibility + pit-limit-aware brake-now helper task
- `LalaLaunch.cs`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Internal/SimHubParameterInventory.md`
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
- Kept ownership boundaries intact: pit-box distance/time + brake-now helper remain plugin-owned pit logic; no dashboard JSON edits were required.
- Preserved existing export names (`Pit.Box.DistanceM`, `Pit.Box.TimeS`) while extending visibility gating only.
- No new log lines were added; `Docs/Internal/SimHubLogMessages.md` remained unchanged.

## Validation note
- Validation recorded against `HEAD` (`Pit.Box pre-entry visibility extension + new pit-limit-aware Pit.Box.BrakeNow helper export`).
