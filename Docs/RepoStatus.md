# Repository status

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/H2H.md` updated so the canonical H2H doc now records that true target rebinds still clear the published row immediately, stale pre-bind target completions are still blocked, and segment rebuild resumes from the next clean target completion without requiring a player-side post-bind completion.
- `Docs/SimHubParameterInventory.md` updated so the H2H segment export contract now matches the relaxed bind-aware rebuild rule while preserving sector-6 carryover and `ClassSessionBestLapSec` fallback behavior.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- H2H segment publication no longer starves after a true Ahead/Behind target rebind: the row still clears immediately, then rebuilds from the next clean target-side segment completion instead of waiting for a whole new lap.
- Bind-aware protection remains on the target side, so stale pre-bind target segment timestamps still cannot leak into the new comparison window and recreate the large garbage swap deltas seen previously.
- `H2HRace.ClassSessionBestLapSec` / `H2HTrack.ClassSessionBestLapSec`, the same-class primary source plus `IRacingExtraProperties.iRacing_Session_PlayerClassBestLapTime` fallback, and same-target sector-6 lap-wrap carryover remain unchanged.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
