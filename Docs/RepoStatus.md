# Repository status

Validated against commit: HEAD
Last updated: 2026-04-07
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Completed an analysis-first design pass for removing Opponents dependency on Extra Properties class-leaderboard feeds (`iRacing_ClassLeaderboard_Driver_XX_*`) in controlled phases.
- Documented a concrete native leaderboard migration model split into identity resolution, class mapping, ordering, gap model, and neighbour selection.
- Implemented a narrow first safe step: Opponents player identity now resolves natively from `SessionData.DriverInfo.*` using `Telemetry.PlayerCarIdx`, with bounded fallback to `IRacingExtraProperties.iRacing_Player_*`.
- Kept subsystem boundaries intact: CarSA remains spatial/timing foundation, Opponents remains race selector + pit-exit owner, and H2H remains selector consumer only.
- No Opp/H2H export names changed; no dashboard contract changes were introduced.

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/H2H.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/Code_Snapshot.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `LalaLaunch.cs`

## Delivery status highlights
- Opponents no longer hard-requires `IRacingExtraProperties.iRacing_Player_ClassColor` / `iRacing_Player_CarNumber` when session identity exists in native SessionData.
- Extra Properties class leaderboard rows remain the active source for same-class row ordering and `RelativeGapToLeader` during this phase.
- Documentation now explicitly separates current implementation state from planned native migration phases to avoid stale claims.

## Validation note
- Validation recorded against `HEAD` (`Opponents native leaderboard analysis/design pass + player identity native-first first step`).
