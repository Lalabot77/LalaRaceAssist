# Repository status

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `H2HEngine.cs` now publishes `H2HRace.*` / `H2HTrack.*` `S1..S6State` and `S1..S6DeltaSec` from the CarSA-owned fixed-6-sector cache instead of the old target-bound H2H stopwatch runtime.
- `LalaLaunch.cs` now wires the narrow CarSA read seam (`TryGetFixedSectorCacheSnapshot`) into H2H without changing race/track selector ownership.
- `Docs/Subsystems/H2H.md`, `Docs/Subsystems/CarSA.md`, and `Docs/SimHubParameterInventory.md` now document the cache-based sector contract, the locked `0/1/2` state semantics, and the immediate target-swap behavior.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- H2H sector publication now reads the player/target fixed-6-sector snapshots from CarSA every tick and computes `S1..S6State` / `S1..S6DeltaSec` directly from cache presence plus cached durations.
- The old bind-aware H2H segment rebuild behavior is gone by design: target swaps now publish immediately from the new target's existing cache if present, otherwise the row naturally shows `empty`/`0`.
- Selector architecture did not change: `H2HRace` still follows Opponents-resolved identity and `H2HTrack` still follows CarSA same-class ahead/behind slot selection.
- H2H still owns lightweight presentation/runtime outputs such as `ActiveSegment`, `LapRef`, `LiveGapSec`, `LiveDeltaToBestSec`, lap summary values, colors, and class-session-best publication.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
