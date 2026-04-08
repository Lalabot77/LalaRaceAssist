# Repository status

Validated against commit: HEAD
Last updated: 2026-04-08
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Completed an analysis-first design pass for removing Opponents dependency on Extra Properties class-leaderboard feeds (`iRacing_ClassLeaderboard_Driver_XX_*`) in controlled phases.
- Documented a concrete native leaderboard migration model split into identity resolution, class mapping, ordering, gap model, and neighbour selection.
- Implemented and hardened a narrow first safe step for player identity: Opponents now keeps `IRacingExtraProperties.iRacing_Player_*` identity first for leaderboard-format compatibility, and only uses native `SessionData.DriverInfo.*` fallback when Extra identity is incomplete and native class color + car number are both present.
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
- Opponents rejects partial native identity and no longer allows `?:<carNumber>` style keys to suppress stronger fallback identity.
- Opponents player identity path now stays format-compatible with the still-active Extra-Properties leaderboard row source by preferring complete `iRacing_Player_*` identity first.
- Extra Properties class leaderboard rows remain the active source for same-class row ordering and `RelativeGapToLeader` during this phase.
- Documentation now explicitly separates current implementation state from planned native migration phases to avoid stale claims.

## Validation note
- Validation recorded against `HEAD` (`Opponents identity fallback hardening + PR532 follow-up doc sync`).
