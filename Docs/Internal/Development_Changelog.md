# Development Changelog

This file tracks internal development history between releases.
The public user-facing release history is maintained in the root `CHANGELOG.md`.

## Pre-v1 public development history

### Initial release foundations
- Established the first public plugin packaging path for SimHub + iRacing testing.
- Set the core split between runtime calculations and dashboard presentation.
- Landed early launch support, fuel tooling, and dashboard package foundations.

### v0.1
- Early user-facing launch workflow and launch dashboards.
- Basic fuel and race-support features for live use.
- Initial profile-based setup flow for repeatable car settings.

### v0.2
- Expanded profile-driven workflow and saved-data usage.
- Improved dashboard package structure for live driving support.
- Added stronger race-session support around planning and dash presentation.

### v0.3
- Added fuller quick-start and user-guide material for testers.
- Strengthened learning/store/lock workflow around fuel and track data.
- Expanded practical dash usage guidance for race sessions.

### v0.4
- Matured pit entry and pit-loss workflows, including learned markers and cleaner pit-cycle guidance.
- Improved dry/wet data separation and condition-aware storage.
- Expanded opponent and race-context tooling, including stronger same-class race support.
- Made track-scoped planning data more practical for venue-specific strategy setup.

## Post-v1.0 development

### Dashboard packaging
- Updated dash control visibility labels to `DRIVER`, `STRATEGY`, and `OVERLAY` while preserving clear tooltips.
- Added a release-facing warning below the debug-mode master toggle to reduce accidental always-on troubleshooting.

### Documentation
- Continued v1 documentation alignment between public guides and subsystem ownership docs.
- Kept Strategy-first wording and live-vs-committed plan distinctions explicit in user-facing docs.

### Release preparation
- Hardened Strategy `Presets...` modal open flow against null preset entries and popup open failures.
- Fixed Preset Manager `PreRace Mode` dropdown behavior by reusing the validated ComboBox interaction pattern.
- Embedded shipped preset defaults and expanded track-marker defaults in owning code paths for predictable first-run packaging.

### Plugin UI
- Added a new top-level `OVERVIEW` tab as the plugin landing page/front door.
- Added quick-link buttons, lightweight runtime status text, and a fail-soft GitHub latest-release check (`Check Now` + `Open Releases`).
- Added dashboard preview placeholders that document the exact drop-in paths/names for future images (`Assets/Overview/*.png`) without shipping binaries in this branch.

### Shift Assist learning
- Refined Learning v2 adjacent-gear crossover solve with a small internal relative early-bias tolerance (`a_next >= a_curr * (1 - pct)`) so learned shift RPMs land modestly earlier without flat RPM subtraction, hard cap shaping, or reintroducing the prior absolute accel-margin bias.
- Preserved all existing Learning v2 invariants: speed-domain curves, ratio-based RPM conversion, stability buffer before publish, gear-1 exclusion, no-fallback publication, and safe clamp (`redline - 200`).

### Opponents / H2H session eligibility
- Expanded Opponents leaderboard-neighbor publication scope from race-only to live opponent sessions (Practice, Qualifying/Open Qualify, Lone Qualify, Race), while keeping Offline Testing out of scope.
- Preserved the existing H2HRace selector seam (`Opp.Ahead1` / `Opp.Behind1`) so H2HRace now lights up naturally in practice/qualifying sessions when leaderboard identity is available.
- Kept race-specific pit-exit prediction bounded to Race sessions and reset pit-exit outputs outside Race.

### Opponents activation log wording cleanup
- Updated the Opponents activation log text to match current behavior: `Opponents subsystem active (eligible live session).`
- Synced affected docs to remove stale race-only/lap-gate wording where it referenced Opponents activation.

### Opponents pit-exit hot-loop reset cleanup
- Changed race-scoped pit-exit reset handling to clear once on Race → non-Race transition instead of resetting every non-race telemetry tick.
- Added a small OpponentsEngine race-active latch that is also cleared by `OpponentsEngine.Reset()`.

### Opponents native-only cutover
- Removed Opponents runtime dependency on `IRacingExtraProperties` player, class leaderboard, and class-ahead/behind feeds.
- Replaced Opponents identity, same-class ordering, and lap enrichment with native session-driver + telemetry arrays (`PlayerCarIdx`, `CarIdxLap`, `CarIdxLapDistPct`, `CarIdxClassPosition`, `CarIdxBestLapTime`, `CarIdxLastLapTime`, `CarIdxOnPitRoad`, `CarIdxTrackSurface`).
- Replaced Opponents gap and pit-exit math with a native progress/pace model (lap+distance based) and retained explicit invalid outputs when prerequisites are missing.
- Added bounded Opponents invalid-state logging for native prerequisite failures; no hidden fallback path to Extra Properties remains in Opponents.
- Kept H2H selector seam and CarSA ownership boundaries unchanged.

### Opponents native follow-up correctness fixes
- Prevented unknown/non-positive `PositionInClass` rows from sorting ahead of valid class positions in the class-position ordering path.
- Cleared the temporary pit-out bypass latch after it serves the final-120s suppression bypass, restoring normal late-race suppression.
- Reset pit-exit predictor state on invalid native snapshot so stale pit-exit snapshot/audit data cannot leak.

### Opponents CarSA true-gap seam (Ahead1/Behind1)
- Added a narrow CarSA checkpoint-time seam (`TryGetCheckpointGapSec`) and wired Opponents to prefer it for `Opp.Ahead1.GapToPlayerSec` and `Opp.Behind1.GapToPlayerSec`.
- Kept existing progress/pace gap math as fallback and left neighbor selection ownership unchanged in Opponents.

### Opponents class-position warm-up fallback guard
- Prevented class-position ordering from activating while the player class position is unset/non-positive, so neighbor selection falls back to lap-progress ordering until player class position is valid.
