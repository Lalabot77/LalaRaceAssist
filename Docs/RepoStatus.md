# Repository status

Validated against commit: HEAD
Last updated: 2026-04-10
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Opponents Pit Exit now exports active-cycle countdown state for dashboards: `PitExit.RemainingCountdownSec` and `PitExit.ActivePitCycle`, sourced directly from the existing active pit-cycle remaining-time predictor.
- Active pit-cycle prediction now seeds same-class rival pit-road baseline state at cycle start before transition detection, then tracks post-start pit-road entries and excludes rivals behind who pit after our cycle starts from normal on-track pass-before-exit threat treatment while they remain on pit road.
- Preserved subsystem ownership boundaries (Opponents race-order owner and Pit Exit owner, CarSA spatial/timing owner, H2H consumer/publisher).

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Subsystems/CarSA.md`
- `Docs/Internal/SimHubLogMessages.md`

## Delivery status highlights
- Pit Exit pre-pit behavior remains unchanged; this task only surfaces the active-cycle countdown and active-cycle boolean through the existing `PitExit.*` export family for dash use.
- Export semantics are explicit: `PitExit.RemainingCountdownSec` publishes `>0` only while active countdown is running and `0` when inactive/unavailable; `PitExit.ActivePitCycle` is `true` only during active pit-cycle prediction.
- Pit Exit remains Opponents-owned, race-scoped, and full same-class-field comparison based; late-race last-120s suppression remains unchanged.

## Validation note
- Validation recorded against `HEAD` (`Opponents Pit Exit export follow-up: dash-facing active-cycle countdown + state`).
