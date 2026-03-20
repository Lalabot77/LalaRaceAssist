# Repository status

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/H2H.md` updated for this scoped H2H follow-up so the canonical subsystem doc now covers bind-aware segment publication after true target rebinds plus the class-session-best fallback/export behavior.
- `Docs/SimHubParameterInventory.md` updated so the published H2H export contract now includes `H2HRace.ClassSessionBestLapSec` / `H2HTrack.ClassSessionBestLapSec` and documents bind-aware `S1..S6State` / `S1..S6DeltaSec` rebuild semantics.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- `H2HRace.*` and `H2HTrack.*` now clear the published `S1..S6State` / `S1..S6DeltaSec` row immediately on a true ahead/behind target rebind, then repopulate segments only from post-bind player/target completions so swap-time stale timestamps cannot produce nonsense deltas.
- Ordinary same-target lap-wrap carryover remains intact, so the just-finished closing segment can still publish after start-finish, including sector 6, while true participant reset/context discard/rebind paths still clear that carryover.
- `H2HRace.ClassSessionBestLapSec` and `H2HTrack.ClassSessionBestLapSec` now publish the resolved same-class session-best lap time directly for dash/debug use.
- Class session-best resolution still uses the normal same-class H2H scan first, then falls back only when unavailable to `IRacingExtraProperties.iRacing_Session_PlayerClassBestLapTime`; PB/session-best coloring continues to use that trusted resolved value, and no plugin UI/settings were added, no debug CSV logging was introduced, and CarSA/Opponents ownership boundaries were preserved.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
