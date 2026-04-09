# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Follow-up fixed Opponents pit-lap cache lifecycle so `LapsSincePit` state is cleared on full reset/session transition but preserved across normal per-tick model rebuilds.
- Added stale carIdx pruning for pit-lap cache entries against currently visible rows.
- Added optional Opp per-slot metadata exports (`IRating`, `SafetyRating`, `Licence`, `LicLevel`, `UserID`, `TeamID`, `IsFriend`, `IsTeammate`, `IsBad`) sourced from existing DriverInfo and existing friend/tag sets.
- Refreshed touched documentation headers (`Last updated`/review stamps where used) and synced canonical docs to final behavior.

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/H2H.md`
- `Docs/Subsystems/CarSA.md`
- `H2HEngine.cs`

## Delivery status highlights
- Closed the stale `LapsSincePit` cache leak risk by splitting transient reset vs full reset inside Opponents NativeRaceModel and pruning stale carIdx entries.
- Kept Opp 5-slot naming and legacy slot compatibility unchanged.
- Added optional Opp metadata fields without widening ownership boundaries or redesigning pit-exit/CarSA logic.
- Kept pit-exit predictor algorithm shape unchanged.

## Validation note
- Validation recorded against `HEAD` (`Opp follow-up: pit-lap cache reset fix + optional metadata + doc header hygiene`).
