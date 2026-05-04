# Fuel Model

Validated against commit: HEAD
Last updated: 2026-04-22
Branch: work

## Purpose
The Fuel Model is the runtime fuel-learning and fuel-projection engine. It:
- captures accepted live fuel-burn samples lap by lap,
- maintains dry/wet rolling windows,
- publishes a stable fuel-per-lap value for dashboards and runtime calculations,
- persists trustworthy condition-specific values into profiles,
- projects race distance / after-zero behavior for timed races,
- feeds pit-need, pit-window, and stint-burn guidance outputs.

For GitHub readers, the practical split is:
- **`Docs/Fuel_Model.md`** = driver-facing explanation of what to trust.
- **`Docs/Subsystems/Fuel_Model.md`** = canonical runtime behavior and ownership.
- **`Docs/Subsystems/Fuel_Planner_Tab.md`** = the separate Strategy-tab planning workflow that consumes fuel-model outputs but does not replace the runtime engine.

Canonical companion docs:
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/Pace_And_Projection.md`
- `Docs/Subsystems/Fuel_Planner_Tab.md`

## Scope and boundaries
This doc covers the **runtime** fuel path:
- lap acceptance,
- rolling fuel windows,
- stable burn selection,
- race-distance projection inputs,
- pit math,
- pit-window states,
- profile persistence of learned fuel values.

Out of scope:
- Strategy-tab input ownership and preset behavior,
- user-facing planner workflow,
- dashboard artwork/layout decisions,
- Pit Fuel Control source-selection mode behavior (for example profile-assisted Push/Save mode), which is owned by the Pit Commands/Fuel Control subsystem.

## Inputs (source + cadence)
### Runtime telemetry
- Fuel level / fuel delta at lap transitions.
- Session type, session time remaining, timer-zero state, and laps-completed state.
- Pit-lane / pit-trip state.
- Refuel request and live tank-capacity inputs.
- Tyre compound / wetness context used to separate dry vs wet learning.

### Profile baselines
- Track/car dry and wet fuel averages.
- Track/car lap-time baselines used to keep acceptance and projection behavior defensible.
- Condition lock state that prevents telemetry-driven overwrites when a condition has been intentionally frozen.
- Car-level refuel rate (`CarProfile.RefuelRate`) plus a persisted lock (`CarProfile.RefuelRateLocked`) that can block auto-learned refuel-rate overwrites while still allowing runtime learning/validation to run.

### Pace / projection dependency
- Projection lap-time selection from the Pace & Projection subsystem.
- Strategy-derived after-zero allowance when no live after-zero estimate is ready yet.

## Internal state
### Rolling fuel windows
- Separate dry and wet accepted-lap windows.
- Per-session max-burn tracking with spike protection.
- Seed markers so carry-over baseline values are not immediately evicted on a session transition.

### Stable burn state
- A continuously evaluated candidate burn.
- A stable exported burn value held with a deadband so dashboards do not thrash on minor changes.
- Source/confidence tags so consumers can tell whether the value is live, profile-backed, or fallback-driven.

### Wet/dry routing
- Wet mode follows tyre-compound telemetry for learning/persistence routing.
- Track wetness is exported for UI use, but tyre mode remains the canonical routing choice for fuel learning.
- Cross-condition fallback is allowed, but confidence is penalized when dry data is being used in wet mode or vice versa.

### Persistence state
- Dry and wet condition stats persist independently.
- Each condition has its own sample count, last-updated metadata, and lock gate.
- Refuel rate persistence is car-scoped and now includes a dedicated lock gate; when locked with a usable stored value, runtime refuel learning does not overwrite the stored profile value and runtime/planner keep using the locked stored rate.
- Locked fail-safe first-fill: if a profile is locked but has no usable stored refuel rate, the first valid learned rate is allowed to populate once; subsequent locked overwrite attempts are blocked normally.
- Session-transition seeding is used to reduce race-session cold starts.

### Projection and pit-window state
- Last valid live max tank for stable tank-space behavior.
- Current after-zero source (`planner` vs `live`).
- Debounced pit-window state / label so logs and dashes do not chatter.

## Calculation blocks (high level)
### 1) Lap acceptance
On lap completion, the subsystem calculates the lap fuel delta and rejects non-representative samples.

Common reject cases include:
- race warmup / earliest laps,
- pit-involved laps or the first lap after pit exit,
- incident-latched laps when that signal is active,
- impossible or clearly bad fuel deltas,
- deltas that fall too far outside the saved profile baseline when a baseline exists.

Wet/dry mode does **not** change the validity rules; it only decides which condition window receives an accepted sample.
For persistence routing, lap condition is latched at accepted-lap time so downstream profile/PB writes use the same wet/dry value even if live tyre signal changes later in the tick.

### 2) Rolling window maintenance
Accepted samples are inserted into the active condition window.
The subsystem then:
- enforces maximum window size,
- updates min/max guidance values,
- refreshes session max-burn state inside guarded bounds,
- protects newly seeded values until enough fresh live data exists.
- pace/avg-lap persistence is driven by **pace-accepted laps** (not gated behind fuel-sample acceptance), so valid wet pace can continue feeding Strategy/Profile lap-time data even when a lap's fuel delta is rejected.

### 3) Stable burn selection
The runtime chooses a stable burn candidate from:
1. trustworthy live window data,
2. profile condition averages,
3. safe fallback behavior.

A deadband holds the previous stable value when the new candidate is only trivially different, while source and confidence labels can still update.

### 4) Confidence growth
Fuel confidence rises as accepted live samples accumulate and becomes weaker when:
- samples are sparse,
- opposite-condition data is being reused,
- the session is too young,
- the accepted sample set is still noisy or incomplete.

### 5) Race-distance projection input
The Fuel Model consumes the selected runtime projection lap time and combines it with:
- session time remaining,
- planner after-zero allowance,
- live after-zero estimate once timer-zero has actually been observed.

Session-phase authority is intentionally split:
- SessionState `2/3` (grid/formation): timed-race lookahead uses race definition authority `CurrentSessionInfo._SessionTime` with runtime elapsed `SessionTime` (`remaining = max(0, _SessionTime - SessionTime)`), so baseline fuel-to-end outputs are not driven by near-zero live countdown behavior before green.
- SessionState `4` (race-running): projection returns to the normal live running path.

If the runtime projection path is invalid, the subsystem can still fall back to sim-provided laps-remaining behavior.

### 6) Pit math and stint guidance
Using current fuel, stable burn, projection laps, runtime tank-cap authority, and pit-menu add intent, the subsystem computes:
- laps remaining in tank,
- total fuel needed to end,
- fuel to add,
- stops required,
- current-stint burn target and burn band.


Runtime tank-cap ownership:
- Runtime pit exports (`Fuel.Pit.TankSpaceAvailable`, `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`) now resolve tank cap from the live session authority seam first (`EffectiveLiveMaxTank` raw/fresh fallback path).
- Strategy/Profile `MaxFuelOverride` remains planner-owned and does not clamp these runtime pit exports when live cap authority exists.
- If no live cap authority is currently available, runtime safely falls back to planner/profile cap (`MaxFuelOverride`) so outputs remain bounded instead of collapsing.

`Fuel.Pit.WillAdd` remains runtime math output (`min(requestedAdd, tankSpaceAvailable)`) and is intentionally clamp-driven. During an in-box fill, rising current fuel can reduce remaining tank space, so `WillAdd` may step down near end-of-stop; this is expected from the clamp model and is retained for runtime semantics.

For pit-popup gauge stability, a dedicated boxed-refuel seam now exports:
- `Fuel.Pit.Box.EntryFuel` (latched fuel at in-box refuel entry),
- `Fuel.Pit.Box.WillAddLatched` (latched purple target captured early from `Fuel.Pit.WillAdd` when boxed service is active and refuel is selected),
- `Fuel.Pit.AddedSoFar` (`max(0, FuelNow - EntryFuel)`),
- `Fuel.Pit.WillAddRemaining` (`max(0, Fuel.Pit.Box.WillAddLatched - AddedSoFar)`).

Latch semantics are lifecycle-based (not `WillAdd`-driven): within valid boxed service, `WillAddLatched` latches immediately when refuel is selected so boxed UI targets are available on box entry, while `EntryFuel` still latches on first refuel-flow detection (`fuel rise` or refuel-learning active seam). If refuel is deselected while still boxed, boxed refuel latches clear to zero immediately (`EntryFuel`, `WillAddLatched`, `AddedSoFar`, `WillAddRemaining`) so no stale pending-fuel countdown remains. Values also reset when boxed service ends. This keeps dashboard logic simple and avoids clamp-driven active-state twitching.

### 7) Pit-window state machine
Pit window state is race-only and depends on both stable confidence and tank feasibility.
Typical states include:
- `N/A`
- `NO DATA YET`
- `SET FUEL!`
- `TANK ERROR`
- open-window states such as `CLEAR PUSH`, `RACE PACE`, or `FUEL SAVE`
- `TANK SPACE` when required fuel cannot fit under the current assumptions.

## Outputs (exports + logs)
### Core exports
The full authoritative export list lives in `Docs/Internal/SimHubParameterInventory.md`. The key Fuel Model families are:
- `Fuel.LiveFuelPerLap*`
- `Fuel.LiveLapsRemainingInRace*`
- `Fuel.LapsRemainingInTank`
- `Fuel.RequiredBurnToEnd*`
- `Fuel.Contingency.*`
- `Fuel.TargetFuelPerLap`
- `Fuel.Delta*`
- `Fuel.Pit.*`
- `Fuel.Setup.*` (read-only setup-session fallback export family for pre-grid/pre-race when live tank telemetry is unavailable/zero)
- `Fuel.StintBurnTarget*`
- `Fuel.Live.ProjectedDriveSecondsRemaining`
- `LalaLaunch.PreRace.*` as the separate pre-race/on-grid info layer (Auto uses live race-definition authority first: `_SessionTime` for timed races, `_SessionLaps` for lap-limited races)

Setup-fuel fallback export semantics:
- `Fuel.Setup.FuelLevel` publishes setup-derived litres when a valid setup value is available, else `0`.
- `Fuel.Setup.FuelLevelValid` is `true` only when setup-derived litres are valid and `>0`.
- `Fuel.Setup.FuelLevelSource` publishes `brakesdriveunit`, `chassis.front`, `chassis.rear`, `suspension.rear`, or `none`.
- This seam is read-only and does not overwrite `Telemetry.FuelLevel`, `Fuel.LiveFuelPerLap`, pit math, planner math, max tank, or PreRace strategy logic.

Contingency-aware tactical contract:
- `Fuel.RequiredBurnToEnd*` provides driver-facing burn-to-end guidance while protecting active contingency reserve.
- Active contingency authority is planner-first, then profile track fallback, then default fallback.
- `Fuel.Delta.LitresCurrent/Plan/WillAdd` and Push/Save variants protect active contingency on the **required-to-finish side only**; when contingency is configured in laps, contingency litres are resolved per burn basis (stable for Normal, push burn for Push, save burn for Save).
- race-end phase classification is finish-detection owned; dash-side stability behavior near finish should consume `Race.EndPhase` / `Race.EndPhaseText` / `Race.EndPhaseConfidence` and `Race.LastLapLikely` rather than player-scoped flag inference.
- `Fuel.Pit.WillAdd` remains pure clamp mirror (`min(requestedAdd, tankSpaceAvailable)`) and does not add contingency directly.

### Logging expectations
The subsystem logs:
- lap acceptance / rejection summaries,
- projection source changes,
- after-zero source changes,
- pit-window state changes,
- wet/dry mode flips with surface context.

Canonical wording remains in `Docs/Internal/SimHubLogMessages.md`.

## Dependencies / ordering assumptions
- Lap detection must happen before live fuel capture for that lap.
- Pace/projection selection must be ready before final fuel-to-end and pit-window outputs are finalized.
- The Strategy tab reads the live snapshot from this subsystem but remains the separate human-owned planning layer.

## Reset rules
Fuel-model state resets on:
- session-token changes,
- session-type changes,
- broader combo/identity resets,
- any reset path that must clear stale live windows, projection state, and pit-window labels.

Runtime recovery now distinguishes between:
- **planner-safe targeted runtime recovery** for fuel/live-snapshot health issues (preferred),
- broader transient runtime reset paths for full re-arm scenarios.

Planner-safe targeted recovery is intended to rebuild live-cap/runtime truth without silently clearing Strategy manual overrides or preset intent.
Manual recovery may short-circuit on planner-safe success only while an active live session is present; outside active live session, manual reset continues into the broad reset path.

Driving → Race transitions can seed race state from the just-learned baseline instead of forcing a full cold start.

## Failure modes / edge cases
- **Replay timing anomalies:** acceptance/projection behavior may need log verification.
- **Missing live tank cap:** runtime keeps the last valid cap for stability, while UI can still clear user-facing displays when no current valid cap exists.
- **Live-cap authority seam:** Strategy live-cap consumption now uses the same runtime-authoritative seam used by fuel runtime recovery (`raw -> bounded fallback` only) so stale cached caps cannot outlive the fallback freshness window when raw reads disappear.
- **Weak early-session data:** profile-backed fallback may legitimately remain safer than live values until confidence improves.
- **Cross-condition reuse:** dry/wet fallback works, but confidence should be interpreted as lower.
- **Session-change starvation hardening:** refuel-learning cooldown now gates only the refuel-learning block; it no longer exits the full `DataUpdate` tick. This keeps downstream fuel/session/strategy refresh paths running while cooldown is active.

## v1 documentation note
Use **Strategy** terminology for GitHub-facing explanations. Treat older “Fuel tab” wording as legacy technical language only; the canonical UI story is now the Strategy tab plus the separate runtime Fuel Model.
