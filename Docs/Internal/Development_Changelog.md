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

### Opponents 5-slot export expansion + live PositionInClass alignment
- Expanded Opponents race-order publication from 2 to 5 slots each side under the existing flat `Opp.*` namespace (`Opp.Ahead1..5.*`, `Opp.Behind1..5.*`) while preserving backward compatibility for existing `Ahead1/2` and `Behind1/2` bindings.
- Added richer race-order-safe per-slot metadata (identity/cosmetic, validity/pit state, effective `PositionInClass`, lap-context fields, and gap/fight metrics) without importing CarSA-local traffic/status semantics.
- Kept top-level `Opponents_SummaryAhead/Behind` readability stable (first-two-slot shape) and added per-slot summaries through slot 5.
- Aligned published race-context `PositionInClass` semantics to effective/live RaceProgress-first ordering across Opponents and H2H outputs, while preserving ownership boundaries (Opponents = race order owner, CarSA = track selector owner).

### Opponents follow-up: pit-lap cache lifecycle + optional metadata
- Fixed Opponents `LapsSincePit` state lifetime by splitting NativeRaceModel per-tick transient reset from full reset, so pit-lap history is preserved within-session but cleared on subsystem/session reset.
- Added stale carIdx pruning for cached pit-lap state against currently visible rows to avoid stale/reused-carIdx leakage.
- Added optional Opp slot metadata exports (`IRating`, `SafetyRating`, `Licence`, `LicLevel`, `UserID`, `TeamID`, `IsFriend`, `IsTeammate`, `IsBad`) using existing DriverInfo fields and existing friend/tag sets.

### Session-change stall recovery hardening
- Fixed `LalaLaunch.DataUpdate` starvation by removing the refuel-cooldown full-tick early-return; cooldown now gates only refuel-learning internals so downstream runtime updates continue.
- Added a bounded transient runtime recovery seam (`ManualRecoveryReset`) and wired both Overview reset button and `PrimaryDashMode` action to the same path for manual re-arm.
- Unified session token/type transition runtime reset execution through the same recovery seam while preserving existing fuel-model transition handling.

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

### Opponents class-color normalization parse-order fix
- Fixed class-color normalization to parse plain numeric strings as decimal by default, preserving correct `ClassColor:CarNumber` identity keys when native `CarClassColor` values arrive as decimal text.

### PR534 rerun: Opponents class-color normalization
- Re-applied the PR534 decimal-first class-color normalization change on `work` so it can be submitted again as a fresh PR after the original PR was closed unmerged.

### Opponents RaceProgress-first live ordering fix
- Changed native Opponents same-class neighbor ordering to always use live race progress (`CarIdxLap` + `CarIdxLapDistPct`) for Ahead/Behind target selection so overtakes switch immediately.
- Kept native `CarIdxClassPosition` as anchor/validation context only (and fallback positional context when present), preventing delayed official class-position updates from blocking live target swaps.
- Preserved existing `Opp.Ahead1/2` + `Opp.Behind1/2` output contracts and H2HRace selector seam behavior.
