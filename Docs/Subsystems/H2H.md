# Head-to-Head (H2H)

Validated against commit: HEAD
Last updated: 2026-04-21
Branch: work

## Purpose
Head-to-Head is a standalone dash-facing comparison subsystem that publishes two concurrent export families:
- `H2HRace.*` = class race targets ahead/behind
- `H2HTrack.*` = class on-track targets ahead/behind

LapRef (`LapRef.*`) is a separate offline player-reference subsystem and does not alter H2H contracts or behavior.

The feature provides a flat, explicit contract for Dash Studio consumption with player/ahead/behind lap summaries, direct `LastLapColor` exports, fixed 6-sector state, sector deltas published from the CarSA-owned cache, canonical `#RRGGBB` class colors, and active-segment outputs.

## Ownership and subsystem boundaries
- **H2H owns** family/side publication, selector-driven identity/cosmetic outputs, live-gap values, lap-summary values, active-segment outputs, and presentation of the flat dash contract.
- **Opponents owns only race-target selection** for `H2HRace.*` via current class-ahead / class-behind identity (`Opp.Ahead1` / `Opp.Behind1`).
- **CarSA owns track-target selection** for `H2HTrack.*` via the current ahead/behind slot set, `CarIdx`, and already-resolved cosmetic metadata.
- **CarSA also owns the fixed-6-sector timing cache** for all cars. H2H is now a read-only consumer of that cache through the narrow CarSA accessor seam; H2H no longer owns a target-bound sector stopwatch runtime for published `S1..S6*`.
- H2H does **not** move timing responsibility into Opponents, and selector ownership remains unchanged.

## Sector publication model
H2H sector publication is intentionally simple:
- fixed **6 sectors** sourced from the CarSA-owned per-car cache (not official dynamic sectors)
- **no** live in-segment fill bars
- sector state contract is now purely cache-presence based:
  - `0 = empty`
  - `1 = pending`
  - `2 = valid`
- delta contract:
  - `0` when the target sector is missing/invalid
  - `0` when the player sector is missing/invalid
  - `target.DurationSec - player.DurationSec` only when both sectors are valid
- active segment contract:
  - `0 = unknown / not started`
  - `1..6 = current active segment`

## Target behavior
### H2HTrack
- Scans CarSA ahead/behind slots and uses the nearest valid same-class local track target.
- Can be more volatile than race mode because local on-track slots can swap as nearby cars change.
- When the selected track target changes, H2H immediately republishes `S1..S6State` / `S1..S6DeltaSec` from the new target's existing CarSA cache if present. If the new target has no cached value for a sector, that sector naturally publishes `empty`/`0`. There is no bind-clear rebuild window.

### H2HRace
- Uses Opponents class-ahead / class-behind identity as the race selector input.
- Availability follows Opponents selector publication scope (live opponent sessions such as Practice, Qualifying/Open Qualify, Lone Qualify, and Race). If Opponents clears selector identity, `H2HRace.*` naturally clears/inactivates.
- Continues to track the selected identity even when that car is no longer nearby, provided the identity can still be resolved to a live `CarIdx`.
- If direct session-info identity lookup misses for a known race target, H2H can use a narrow local live-session fallback by probing current CarSA slots: exact normalized H2H identity remains the first path, then a bounded car-number fallback can recover the live `CarIdx` when the nearby CarSA slot has the same car number and, when available, the same resolved driver name even if class-color identity differs. Selector ownership still stays in Opponents.
- If the identity stays the same but the resolved `CarIdx` changes, the published `S1..S6State` / `S1..S6DeltaSec` outputs switch immediately to the newly resolved car's CarSA cache contents. If no cached sector exists yet, the row shows `empty`/`0` naturally.
- If same-identity reverse resolution blips for a tick, H2H keeps the previous `CarIdx` binding/state instead of forcing a false selector reset.
- If Opponents intentionally clears the race selector, H2H clears/inactivates the race target instead of reviving a stale previous identity.
- If the target identity genuinely cannot currently be resolved, the identity/cosmetic fields can remain latched while `Valid` falls false.

## Numeric semantics
- `PositionInClass = 0` when unavailable.
- Published `PositionInClass` for `H2HRace.*` and `H2HTrack.*` now includes both target rows and `Player.*` rows, and follows effective/live race-order semantics when available (RaceProgress-first context from Opponents), with local selector/native fallback only when effective context is unavailable.
- `LiveDeltaToBestSec = 0` when insufficient data exists.
- Sector delta values reset to `0` whenever their sector state is not `valid`.
- Sector publication is **not bind-aware** after the cache switchover. H2H does not rebuild rows from target-bound stopwatch completions and does not carry a special lap-wrap sector-6 timing path; sector 6 simply publishes whenever CarSA records the `50→0` completion into cache.
- `LastLapColor` uses direct dash-ready `#RRGGBB` outputs: white `#FFFFFF` for a normal last lap, lime `#00FF00` when the corresponding published last lap is also that participant's valid PB, and magenta `#FF00FF` when that valid PB also matches the same-class session-best; session-best overrides PB, and invalid faster raw last laps do not trigger PB/session-best coloring. Class-state authority is intentionally simple and shared across consumers: `GameData.HasMultipleClassOpponents=false` means single-class and bypasses class matching entirely (field-best scan), while `true` means multiclass and resolves player-class membership using `GameData.CarClass` plus the per-car `DriverInfo` class label for bounded same-class filtering. This same class-best holder seam now also feeds plugin-owned `ClassBest.*` exports in `LalaLaunch.cs`.
- `Valid` is true only when a side has a resolved live target and usable H2H timing context.

## Export contract summary
Both `H2HRace.*` and `H2HTrack.*` expose the same flat shape:
- family-level field: `ClassSessionBestLapSec`
- `Ahead.*` / `Behind.*` identity fields: `Valid`, `CarIdx`, `IdentityKey`, `Name`, `CarNumber`, `ClassColor`, `PositionInClass`
- `Player.*`, `Ahead.*`, `Behind.*` lap-summary fields: `LastLapSec`, `BestLapSec`, `LastLapDeltaToBestSec`, `LiveDeltaToBestSec`, `LastLapColor`, `ActiveSegment`, `LapRef`
- `Player.*` identity/context field: `PositionInClass` (effective/live race-order context when available)
- `Ahead.*` / `Behind.*` comparison fields: `LastLapDeltaToPlayerSec`, `LiveGapSec`
- `Ahead.S1..S6DeltaSec`, `Ahead.S1..S6State`
- `Behind.S1..S6DeltaSec`, `Behind.S1..S6State`

## Update flow
- `LalaLaunch.cs` reads the existing raw telemetry/session-info inputs and builds race/track selector snapshots.
- `CarSAEngine` maintains the per-car fixed-6-sector cache derived from the existing 60-checkpoint progression and exposes it via `TryGetFixedSectorCacheSnapshot`.
- The standalone `H2HEngine` updates both families every tick.
- `H2HEngine` now reads the player/target cache snapshots from CarSA and derives `S1..S6State` / `S1..S6DeltaSec` directly from those snapshots.
- `AttachCore` publishes the flat `H2HRace.*` and `H2HTrack.*` exports for dash use.

## Runtime timing notes
- H2H does not synthesize a lap-start timestamp from the current tick when a participant is first observed or rebound mid-lap; live-delta context remains incomplete until a real lap boundary is seen.
- H2H still keeps lightweight participant context for `ActiveSegment`, `LapRef`, `LiveGapSec`, `LiveDeltaToBestSec`, and lap-summary publication.
- H2H no longer owns target-bound sector completion timing, bind-aware row rebuild mechanics, or sector-6 lap-wrap carryover. Those published sector outputs are now entirely driven by the CarSA cache.
- `ClassColor` is published in one canonical uppercase hex format: `#RRGGBB`.

- 2026-04-30 Phase 1 League Race Class infrastructure: no H2HRace/H2HTrack selector behavior changes yet; H2HTrack remains CarSA-native selection.
- 2026-05-03 League Race Phase 3:
  - `H2HRace.*` target identity can now change in enabled+valid League Class mode because its selector seam (`Opp.Ahead1` / `Opp.Behind1`) is now effective-class-cohort aware in Opponents;
  - `H2HTrack.*` selection remains unchanged and CarSA-native;
  - H2H lap/sector/delta calculation ownership and behavior remain unchanged.
