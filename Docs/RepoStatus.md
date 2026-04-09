# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Added standalone `LapRef.*` runtime export family for offline player-reference lap comparison (player, session best, profile best, and per-sector comparisons).
- Added new `LapReferenceEngine` with validated-lap snapshot capture, in-memory session-best tracking, and profile-best materialization.
- Extended profile `TrackStats` with compatibility-safe optional dry/wet PB sector fields (`BestLapSector1..6Dry/WetMs`) and wired persistence via existing PB seam.
- Removed manual PB editing workflow from Profiles Track data UI: dry/wet PB now display read-only and remain telemetry-owned.
- Neutralized `TrackStats` manual PB text mutation path so PB text setters no longer parse/write PB millisecond values.

## Reviewed documentation set
### Changed in this sweep
- `CarProfiles.cs`
- `ProfilesManagerView.xaml`
- `Docs/Subsystems/Profiles_And_PB.md`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Project_Index.md`
- `Docs/Subsystems/H2H.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/Fuel_Model.md`
- `ProfilesManagerViewModel.cs`
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`

## Delivery status highlights
- `LapRef.*` now publishes player/session/profile reference lap rows with fixed 6-sector states and condition-aware comparisons.
- H2H, CarSA derivation ownership, and Opponents contracts remain unchanged; LapRef consumes CarSA fixed-sector cache read-only.
- Profile PB updates continue through `TryUpdatePBByCondition(...)` and now persist condition-specific PB sectors only when real sector data exists.
- Profiles UI no longer allows manual dry/wet PB editing; PB remains telemetry-only to keep profile-best lap/sector consistency for LapRef.

## Validation note
- Validation recorded against `HEAD` (`PB telemetry-only follow-up: remove manual PB editing path + docs sync`).
