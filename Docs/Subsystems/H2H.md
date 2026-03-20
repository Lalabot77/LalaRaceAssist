# Head-to-Head (H2H)

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

## Purpose
Head-to-Head is a standalone dash-facing comparison subsystem that publishes two concurrent export families:
- `H2HRace.*` = class race targets ahead/behind
- `H2HTrack.*` = class on-track targets ahead/behind

The feature provides a flat, explicit contract for Dash Studio consumption with player/ahead/behind lap summaries, direct `LastLapColor` exports, fixed 6-segment state, latched segment deltas, canonical `#RRGGBB` class colors, and active-segment outputs.

## Ownership and subsystem boundaries
- **H2H currently owns** target timing context, live-gap values, lap-summary values, fixed-6-segment state, latched segment deltas, and active-segment outputs.
- **Opponents owns only race-target selection** for `H2HRace.*` via current class-ahead / class-behind identity (`Opp.Ahead1` / `Opp.Behind1`).
- **CarSA owns track-target selection** for `H2HTrack.*` via the current ahead/behind slot set, `CarIdx`, and already-resolved cosmetic metadata; in the current foundation phase CarSA also owns the new per-car fixed-6-sector cache, but H2H does not consume it yet.
- CarSA now also owns a car-centric fixed-6-sector cache foundation for a later H2H timing-source swap, but **this phase does not switch H2H consumption yet**; the published H2H outputs still come from the existing H2H runtime.
- H2H does **not** move timing responsibility into Opponents, and the CarSA-owned cache remains internal to CarSA until the later consumer switchover task.

## Phase-1 model
Phase 1 is intentionally simple:
- fixed **6 segments** (not official dynamic sectors)
- **no** live in-segment fill bars
- segment deltas latch only after both player and target have crossed the same segment
- segment state contract:
  - `0 = empty`
  - `1 = pending`
  - `2 = valid`
- active segment contract:
  - `0 = unknown / not started`
  - `1..6 = current active segment`

## Target behavior
### H2HTrack
- Scans CarSA ahead/behind slots and uses the nearest valid same-class local track target.
- Can be more volatile than race mode because local on-track slots can swap as nearby cars change.
- When the selected track target changes, the published `S1..S6State` / `S1..S6DeltaSec` outputs for that side are cleared immediately and rebuild from the next valid segment completion onward; H2H does not wait for a whole lap. Publication stays bind-aware on the target side, so stale pre-bind target segment completions cannot leak into the new comparison window, while the player side can still compare using its current valid segment timing as soon as the rebound target logs a clean completion.

### H2HRace
- Uses Opponents class-ahead / class-behind identity as the race selector input.
- Continues to track the selected identity even when that car is no longer nearby, provided the identity can still be resolved to a live `CarIdx`.
- If direct session-info identity lookup misses for a known race target, H2H can use a narrow local live-session fallback by probing current CarSA slots: exact normalized H2H identity remains the first path, then a bounded car-number fallback can recover the live `CarIdx` when the nearby CarSA slot has the same car number and, when available, the same resolved driver name even if class-color identity differs. Selector ownership still stays in Opponents.
- If the identity stays the same but the resolved `CarIdx` changes, the published `S1..S6State` / `S1..S6DeltaSec` outputs for that side are cleared immediately and rebuild from the next valid segment completion onward; ordinary same-target lap-wrap carryover still remains intact. Rebuilt publication is bind-aware for the current target binding and excludes stale pre-bind target completions from the new comparison window without forcing the player side to wait for a fresh post-bind completion.
- If same-identity reverse resolution blips for a tick, H2H keeps the previous `CarIdx` binding/state instead of forcing a false selector reset.
- If Opponents intentionally clears the race selector, H2H clears/inactivates the race target instead of reviving a stale previous identity.
- If the target identity genuinely cannot currently be resolved, the identity/cosmetic fields can remain latched while `Valid` falls false.

## Numeric semantics (phase 1)
- `PositionInClass = 0` when unavailable.
- `LiveDeltaToBestSec = 0` when insufficient data exists.
- Segment delta values reset to `0` whenever their segment state is not `valid`.
- When a true target rebind happens (identity change or resolved `CarIdx` swap), the published segment row for that side is cleared immediately so stale deltas from the previous target cannot leak into the new binding; rebuilding begins from the next valid segment completion, not the next lap. H2H still requires the target-side segment completion to be post-bind before publishing, so stale pre-bind target timestamps cannot produce nonsense swap deltas, but it no longer starves the row by also demanding a player-side post-bind completion before rebuild can resume.
- When a lap wraps for the same bound participant, the just-finished closing segment is carried across the ordinary new-lap runtime reset so sector 6 can still publish valid timing after start-finish; true participant discard/reset/rebind still clears that carryover, while previously published valid segment states/deltas stay latched on the dash until the new lap rebuilds that same segment.
- `LastLapColor` uses direct dash-ready `#RRGGBB` outputs: white `#FFFFFF` for a normal last lap, lime `#00FF00` when the corresponding published last lap is also that participant's valid PB, and magenta `#FF00FF` when that valid PB also matches the same-class session-best; session-best overrides PB, and invalid faster raw last laps do not trigger PB/session-best coloring. Class session-best resolution keeps the normal same-class H2H source first and falls back only when needed to `IRacingExtraProperties.iRacing_Session_PlayerClassBestLapTime` parsed to seconds.
- `Valid` is true only when a side has a resolved live target and usable H2H timing context.

## Export contract summary
Both `H2HRace.*` and `H2HTrack.*` expose the same flat shape:
- family-level field: `ClassSessionBestLapSec`
- `Ahead.*` / `Behind.*` identity fields: `Valid`, `CarIdx`, `IdentityKey`, `Name`, `CarNumber`, `ClassColor`, `PositionInClass`
- `Player.*`, `Ahead.*`, `Behind.*` lap-summary fields: `LastLapSec`, `BestLapSec`, `LastLapDeltaToBestSec`, `LiveDeltaToBestSec`, `LastLapColor`, `ActiveSegment`, `LapRef`
- `Ahead.*` / `Behind.*` comparison fields: `LastLapDeltaToPlayerSec`, `LiveGapSec`
- `Ahead.S1..S6DeltaSec`, `Ahead.S1..S6State`
- `Behind.S1..S6DeltaSec`, `Behind.S1..S6State`

## Update flow
- `LalaLaunch.cs` reads the existing raw telemetry/session-info inputs and builds race/track selector snapshots.
- `CarSAEngine` now also maintains a per-car fixed-6-sector cache derived from the existing 60-checkpoint progression, but H2H does not consume that cache yet in this phase.
- The standalone `H2HEngine` updates both families every tick.
- `AttachCore` publishes the flat `H2HRace.*` and `H2HTrack.*` exports for dash use.

## Runtime timing notes
- H2H does not synthesize a lap-start timestamp from the current tick when a participant is first observed or rebound mid-lap; live-delta and segment timing remain incomplete until a real lap boundary is seen.
- When a lap wraps, the outgoing active segment is finalized and its completion time is carried across the ordinary new-lap runtime reset so all six fixed segments, including sector 6, remain publishable; true participant discard/reset/rebind paths still clear that carryover, and already-published segment outputs stay visible until the new lap revisits and replaces them.
- `ClassColor` is published in one canonical uppercase hex format: `#RRGGBB`.
