# Pace and Projection

Validated against commit: HEAD
Last updated: 2026-06-08
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
- `Docs/Subsystems/Strategy/Fuel_Planner_Tab.md`
- `Docs/Subsystems/Pit_System/Pit_Timing_And_PitLoss.md`

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
- Overall race-leading pace timing when available from native CarIdx telemetry.

### Baselines and fallbacks
- Profile lap-time averages.
- Session PB fallback where appropriate.
- Sim/session fallback values when live pace cannot yet be trusted.

## Internal state
### Recent pace windows
- Recent accepted clean laps for live pace.
- Last-5 / rolling-average style windows.
- Overall leader rolling pace: last 3 valid samples from the car that is overall P1 at each sample point.

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
For timed-race projection, Leader Lap authority means **overall race-leading pace**, not player-class leader pace:
- identity resolves current overall P1 using the same semantics as finish timing (`CarIdxPosition == 1 && CarIdxClassPosition == 1`, then `CarIdxPosition == 1`, requiring an in-world car),
- the primary sample is current overall P1 `CarIdxLastLapTime`, validated by absolute plausibility only (`>20s`/`<900s`, finite/positive) with no player-pace comparison floor,
- the rolling window keeps the last 3 valid overall-P1 samples and is not tied to one `CarIdx`, so overall-leader changes do not clear the window by themselves,
- if the overall-P1 identity/feed is temporarily unavailable after valid samples exist, the previous rolling average may be held through the same completed-player-lap interval for projection continuity,
- once that bounded hold expires without a valid overall P1 returning, leader authority fails closed,
- when the rolling window is empty, current overall P1 `CarIdxBestLapTime` may seed the published leader pace as a low-confidence fallback, but it is not ingested into the rolling window,
- class leader, class best, H2H class session-best, LapRef, and iRacing ExtraProperties are not valid Leader Lap authorities.

Leader Lap observability keeps always-on transition/rate-limited Info for authority loss/fail-closed, hold start, hold expiry, and authority recovery. Routine candidate sampling, accept/use, reject, and low-confidence fallback details are Soft-Debug-gated Info so normal sessions do not spam SimHub logs while retaining focused diagnostics.

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
- PreRace Auto now also consumes this same runtime projection seam (`ProjectionLapTime_Stable`) instead of planner lap-time authority.
- Race-end interpretation should use finish-phase authority exports (`Race.EndPhase*` / `Race.LastLapLikely`) from finish detection ownership; Pace & Projection remains a pace/time projection subsystem and does not own race-finish classification.

Session phase split:
- SessionState `2/3` (grid/formation) uses session-definition race length authority (`CurrentSessionInfo._SessionTime`) with elapsed `SessionTime` as the timed-race remaining seam.
- SessionState `4` uses the normal live running timed-race projection seam.

## Outputs (exports + logs)
### Core exports
Representative outputs include:
- `Pace.StintAvgLapTimeSec`
- `Pace.Last5LapAvgSec`
- `Pace.LeaderAvgLapTimeSec` (overall race-leading rolling/fallback pace)
- `ProjectionLapTime_Stable`
- `ProjectionLapTime_StableSource`
- overall/live pace confidence values used elsewhere in the plugin

### `Fuel.ProjectionLapTime_StableSource` raw token catalogue (code-derived)
The stable-source export is owned by `GetProjectionLapSeconds(...)` and is populated from projection candidate selection + stable deadband/hold behavior.

| Raw token | Assigned where | Condition that produces it | Suggested driver-facing label |
| --- | --- | --- | --- |
| `pace.stint` | `LalaLaunch.cs` (`GetProjectionLapSeconds`) | Pace confidence is at/above switch threshold and `Pace_StintAvgLapTimeSec > 0`; live candidate resolves to stint average. | `LIVE STINT` |
| `pace.last5` | `LalaLaunch.cs` (`GetProjectionLapSeconds`) | Pace confidence is at/above switch threshold, stint average is unavailable/non-positive, and `Pace_Last5LapAvgSec > 0`; live candidate resolves to last-5 average. | `LIVE AVG5` |
| `profile.avg` | `LalaLaunch.cs` (`GetProjectionLapSeconds`) | Live-pace candidate is not eligible and `GetProfileAvgLapSeconds() > 0`. | `PROFILE` |
| `fuelcalc.estimated` | `LalaLaunch.cs` (`GetProjectionLapSeconds`) | Live and profile candidates are unavailable, and `FuelCalculator.EstimatedLapTime` parses to a positive duration. | `PLANNER EST` |
| `telemetry.lastlap` | `LalaLaunch.cs` (`GetProjectionLapSeconds`) | Estimator was not available/valid, and telemetry last-lap time is positive. | `LAST LAP` |
| `fallback.none` | `LalaLaunch.cs` (`GetProjectionLapSeconds`) + reset path | No valid live/profile/estimator candidate exists and there is no prior stable value to hold; also set on reset/initialization. | `NO DATA` |

Hold behavior notes (still from code, not extra tokens):
- If current candidate is invalid (`<=0`) but previous stable value exists, stable value and its previous source token are held.
- If current candidate is valid but inside stable deadband, stable value is held and source token can remain previous stable source.
- Therefore dash consumers should treat the token as the provenance of the *currently published stable value*, not strictly “this tick’s fresh candidate”.

### Dash-facing source text companion export
- `Fuel.ProjectionLapTime_StableSource` remains the canonical diagnostic provenance token.
- `Fuel.ProjectionLapTime_StableSourceText` is a plugin-owned presentation helper for dashboards.
- Mapping contract:
  - `pace.stint` → `LIVE STINT`
  - `pace.last5` → `LIVE AVG5`
  - `profile.avg` → `PROFILE`
  - `fuelcalc.estimated` → `PLANNER EST`
  - `telemetry.lastlap` → `LAST LAP`
  - `fallback.none` → `NO DATA`
- Unknown/unmapped non-blank values pass through unchanged; blank/null defensively resolves to `NO DATA`.

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

Runtime-health handling is now expected to prefer targeted fuel/live-snapshot recovery first, with pace/projection transient re-arm only when dependency ordering requires it.

## Failure modes / safeguards
- **Heavy traffic / compromised laps:** confidence falls and fallback pace may legitimately take over.
- **Dropped overall leader feed:** a missing overall-P1 identity/feed can hold the previous rolling average through the same completed-player-lap interval for projection continuity, then clears/fails closed if no valid overall P1 returns.
- **Short or noisy sessions:** projection may stay profile/fallback driven for longer than the driver expects.
- **Replay timing:** source changes should be verified with logs.

## Test checklist
- Run clean laps and confirm pace confidence rises while the stable source stays sensible.
- Trigger pit/off-track rejection scenarios and confirm the affected laps do not contaminate projection pace.
- Remove/lose leader timing and confirm leader context clears without reviving stale values.
- In timed sessions, confirm planner after-zero is used first and live after-zero can later take over when valid.

## v1 documentation note
Use **Strategy** for the user-facing planning story, but keep **Pace & Projection** as the canonical runtime subsystem that feeds fuel math and stable live race-distance behavior.


## Race End / Finish Lifecycle
Race end/finish contract remains distributed by design: exports are owned by `Docs/Internal/SimHubParameterInventory.md` (`RaceFinish.*`, `Race.EndPhase.*`), observability is owned by `Docs/Internal/SimHubLogMessages.md`, and validated-change history belongs in `Docs/RepoStatus.md` and `Docs/Internal/Development_Changelog.md`. This subsystem references lifecycle behavior but is not the canonical contract owner for those internal finish exports/log tags.
