# Head-to-Head (H2H)

Validated against commit: HEAD
Last updated: 2026-03-18
Branch: work

## Purpose
Head-to-Head is a standalone dash-facing comparison subsystem that publishes two concurrent export families:
- `H2HRace.*` = class race targets ahead/behind
- `H2HTrack.*` = class on-track targets ahead/behind

The feature provides a flat, explicit contract for Dash Studio consumption with player/ahead/behind lap summaries, fixed 6-segment state, latched segment deltas, and active-segment outputs.

## Ownership and subsystem boundaries
- **H2H owns** target timing context, live-gap values, lap-summary values, fixed-6-segment state, latched segment deltas, and active-segment outputs.
- **Opponents owns only race-target selection** for `H2HRace.*` via current class-ahead / class-behind identity (`Opp.Ahead1` / `Opp.Behind1`).
- **CarSA owns only track-target selection** for `H2HTrack.*` via current `Ahead01` / `Behind01` slot identity, `CarIdx`, and already-resolved cosmetic metadata.
- H2H does **not** move timing responsibility into Opponents or CarSA.

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
- Uses CarSA slot 1 ahead/behind as the current local track target.
- Can be more volatile than race mode because local on-track slots can swap as nearby cars change.
- When the selected track target changes, phase-1 timing/segment state is cleared and rebuilt.

### H2HRace
- Uses Opponents class-ahead / class-behind identity as the race selector input.
- Continues to track the selected identity even when that car is no longer nearby, provided the identity can still be resolved to a live `CarIdx`.
- If the identity stays the same but the resolved `CarIdx` changes, unsafe per-target timing state is cleared and rebuilt.
- If same-identity reverse resolution blips for a tick, H2H keeps the previous `CarIdx` binding/state instead of forcing a false selector reset.
- If the target identity genuinely cannot currently be resolved, the identity/cosmetic fields can remain latched while `Valid` falls false.

## Numeric semantics (phase 1)
- `PositionInClass = 0` when unavailable.
- `LiveDeltaToBestSec = 0` when insufficient data exists.
- Segment delta values reset to `0` whenever their segment state is not `valid`.
- `Valid` is true only when a side has a resolved live target and usable H2H timing context.

## Export contract summary
Both `H2HRace.*` and `H2HTrack.*` expose the same flat shape:
- `Ahead.*` / `Behind.*` identity fields: `Valid`, `CarIdx`, `IdentityKey`, `Name`, `CarNumber`, `ClassColor`, `PositionInClass`
- `Player.*`, `Ahead.*`, `Behind.*` lap-summary fields: `LastLapSec`, `BestLapSec`, `LastLapDeltaToBestSec`, `LiveDeltaToBestSec`, `ActiveSegment`, `LapRef`
- `Ahead.*` / `Behind.*` comparison fields: `LastLapDeltaToPlayerSec`, `LiveGapSec`
- `Ahead.S1..S6DeltaSec`, `Ahead.S1..S6State`
- `Behind.S1..S6DeltaSec`, `Behind.S1..S6State`

## Update flow
- `LalaLaunch.cs` reads the existing raw telemetry/session-info inputs and builds race/track selector snapshots.
- The standalone `H2HEngine` updates both families every tick.
- `AttachCore` publishes the flat `H2HRace.*` and `H2HTrack.*` exports for dash use.

## Runtime timing notes
- H2H does not synthesize a lap-start timestamp from the current tick when a participant is first observed or rebound mid-lap; live-delta and segment timing remain incomplete until a real lap boundary is seen.
- When a lap wraps, the outgoing active segment is finalized before the runtime resets for the next lap so the closing segment can latch valid timing.
