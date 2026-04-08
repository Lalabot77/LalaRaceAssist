# Repository status

Validated against commit: HEAD
Last updated: 2026-04-08
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Opponents has been cut over to a native-only implementation for identity, same-class ordering, lap enrichment, and pit-exit prediction.
- Opponents no longer reads `IRacingExtraProperties.iRacing_Player_*`, `IRacingExtraProperties.iRacing_ClassLeaderboard_Driver_XX_*`, `IRacingExtraProperties.iRacing_DriverAheadInClass_*`, or `IRacingExtraProperties.iRacing_DriverBehindInClass_*`.
- H2H ownership/seam remains unchanged (`H2HRace.*` still consumes `Opp.Ahead1` / `Opp.Behind1`).
- CarSA ownership remains unchanged (read seam only; Opponents did not absorb CarSA responsibilities).
- Opponents now logs explicit native invalid-state reasons and leaves outputs invalid/empty when native prerequisites are incomplete.
- Native class-position ordering now pushes unknown/non-positive `PositionInClass` rows to the end so they cannot displace valid class neighbors.
- Native class-position ordering now also requires a valid positive player `PositionInClass`; otherwise Opponents uses lap-progress ordering until class positions stabilize.
- Pit-exit final-120s bypass latch (`_pendingSettledPitOut`) now self-clears after use, restoring normal suppression behavior.
- Invalid-native-snapshot handling now resets the pit-exit predictor to prevent stale snapshot/audit leakage.
- `Opp.Ahead1`/`Opp.Behind1` gap publication now prefers CarSA checkpoint-time gap seam when available, with progress/pace fallback preserved.
- Opponents class-color normalization now parses decimal-native `CarClassColor` strings before hex fallback so integer color feeds produce correct `0xRRGGBB` identity colors.

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Subsystems/H2H.md`
- `LalaLaunch.cs`
- `Docs/Subsystems/CarSA.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`

## Validation note
- Validation recorded against `HEAD` (Opponents native-only cutover; Extra Properties dependency removed from Opponents runtime path).
