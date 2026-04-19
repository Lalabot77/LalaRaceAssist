# Pace and Projection

Validated against commit: HEAD
Last updated: 2026-04-18
Branch: work

## Purpose
The Pace & Projection subsystem owns the runtime lap-time reference used by the plugin when it needs a defensible answer to:
- “What lap pace should the runtime trust right now?”
- “How many laps are realistically left in this timed race?”
- “How should fuel-to-end and pit-window math be driven when live pace is weak or noisy?”

It explicitly separates:
- **observed pace** from recent live laps,
- **stable projection pace** for runtime use,
- **fallback pace** when live data is not ready.

Canonical companion docs:
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Fuel_Planner_Tab.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`

## Scope and boundaries
This subsystem covers:
- live lap-time acceptance / rejection,
- recent-lap and leader-lap pace windows,
- runtime projection source selection,
- stable projection lap-time export,
- after-zero-aware race-distance input into Fuel Model.

Out of scope:
- the Strategy tab’s deterministic planning math,
- fuel-burn modeling,
- dashboard layout/rendering.

## Inputs (source + cadence)
### Runtime inputs
- Lap completion events and lap times.
- Session type / time remaining / timer-zero state.
- Pit-trip state and pit-exit warmup context.
- Incident / off-track state used to reject compromised laps.
- Leader-lap timing when available.

### Baselines and fallbacks
- Profile lap-time averages.
- Session PB fallback where appropriate.
- Sim/session fallback values when live pace cannot yet be trusted.

## Internal state
### Recent pace windows
- Recent accepted clean laps for live pace.
- Last-5 / rolling-average style windows.
- Leader-lap rolling average when the feed is available.

### Stable projection state
- Current stable projection lap time.
- Current stable source label.
- Last logged source/value so source changes can be debounced and explained.

### Confidence state
- Pace confidence from accepted sample count and quality.
- Combined/overall confidence signals published for dashboards and runtime consumers.

## Calculation blocks (high level)
### 1) Pace lap acceptance
Recent lap times are accepted only when they look representative.
Common reject paths include:
- race warmup / earliest laps,
- pit laps and first lap after pit exit,
- off-track / incident-latched laps,
- impossible or grossly outlying lap times.

### 2) Rolling pace maintenance
Accepted laps update the runtime clean-lap windows.
Leader-lap timing is maintained separately so the plugin can use it as context without letting it silently replace the player-focused live pace path.

### 3) Baseline selection
When enough live pace is not available, the subsystem falls back through guarded baseline choices such as:
- profile average,
- session-PB-style fallback,
- safe default/fallback behavior.

The exact chosen source is published and logged so downstream behavior stays explainable.

### 4) Stable projection selection
The subsystem selects a runtime projection lap time and then stabilizes it so dashes and fuel math do not oscillate constantly.
The exported source label reflects whether the runtime is using live pace, profile-backed pace, session PB, or a deeper fallback path.

### 5) Timed-race projection support
The resulting projection lap time feeds the Fuel Model’s timed-race math together with:
- session time remaining,
- planner after-zero allowance,
- live after-zero estimate once timer-zero has genuinely been observed.

Session phase split:
- SessionState `2/3` (grid/formation) uses session-definition race length authority (`CurrentSessionInfo._SessionTime`) with elapsed `SessionTime` as the timed-race remaining seam.
- SessionState `4` uses the normal live running timed-race projection seam.

## Outputs (exports + logs)
### Core exports
Representative outputs include:
- `Pace.StintAvgLapTimeSec`
- `Pace.Last5LapAvgSec`
- `Pace.LeaderLapAvgSec`
- `ProjectionLapTime_Stable`
- `ProjectionLapTime_StableSource`
- overall/live pace confidence values used elsewhere in the plugin

See `Docs/Internal/SimHubParameterInventory.md` for the authoritative inventory.

### Logs
Important log families include:
- pace acceptance / rejection summaries,
- baseline selection changes,
- projection source changes,
- after-zero source changes when the runtime moves from planner to live behavior.

## Dependencies / ordering assumptions
- Lap detection must complete before pace windows are updated.
- Pace / projection must settle before Fuel Model consumes the result for fuel-to-end and pit-window math.
- Strategy can display runtime pace context, but it remains a separate human-owned planner.

## Reset rules
Pace and projection state resets on:
- session identity changes,
- combo changes,
- broader fuel/runtime resets that must prevent stale live pace from leaking into a new session.

## Failure modes / safeguards
- **Heavy traffic / compromised laps:** confidence falls and fallback pace may legitimately take over.
- **Dropped leader feed:** leader average clears rather than silently reusing stale values.
- **Short or noisy sessions:** projection may stay profile/fallback driven for longer than the driver expects.
- **Replay timing:** source changes should be verified with logs.

## Test checklist
- Run clean laps and confirm pace confidence rises while the stable source stays sensible.
- Trigger pit/off-track rejection scenarios and confirm the affected laps do not contaminate projection pace.
- Remove/lose leader timing and confirm leader context clears without reviving stale values.
- In timed sessions, confirm planner after-zero is used first and live after-zero can later take over when valid.

## v1 documentation note
Use **Strategy** for the user-facing planning story, but keep **Pace & Projection** as the canonical runtime subsystem that feeds fuel math and stable live race-distance behavior.
