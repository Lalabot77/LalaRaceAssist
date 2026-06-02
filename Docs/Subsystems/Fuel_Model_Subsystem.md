# Fuel Model

Validated against commit: HEAD
Last updated: 2026-06-02
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
- Pre-green planning adapter (`LalaLaunch.PreRace.*` / `StrategyDash.*`) includes Formation Lap Fuel only for actionable pre-start/grid/formation guidance. `LalaLaunch.PreRace.FormationFuelPlanned` mirrors the planner setting, `LalaLaunch.PreRace.FormationFuelRemaining` is planned before formation (`SessionState 1/2`), burns down from a live-fuel baseline during formation (`SessionState 3`) when valid fuel telemetry exists, and becomes `0` once race-running starts (`SessionState >=4`). Runtime race-running families (`Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, `Pit.FuelControl.*`) intentionally do not add formation fuel again because current fuel already reflects that burn after formation.
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
- Pit Fuel Control DATA/SOURCE/MODE behavior (for example DATA PLAN assisted Push/Save target selection), which is owned by the Pit Commands/Fuel Control subsystem.

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

### Fuel burn analysis popup state
- `Fuel.Burn.Analysis.*` is an additive dashboard-analysis observer fed only by the existing accepted-fuel-lap insertion seam.
- Analysis samples are one combined chronological fresh accepted-lap stream across wet and dry; seeded profile/race-start model values are intentionally excluded.
- Independently resettable backing groups preserve action semantics: Avg3/Avg5/Avg10 rolling list (also `AvgSampleCount`), current-stint sum/count (also `StintSampleCount`), session-average sum/count (also `SessionSampleCount` and compatibility alias `SampleCount`), max-observed value, and min-observed value. `LastLap` updates on every accepted fuel lap and has no manual reset action.
- `RemainingLapsMin` is the conservative `current fuel litres / MaxObserved` bound; `RemainingLapsMax` is the optimistic `current fuel litres / MinObserved` bound. They reuse the existing runtime fuel cache and publish `0.0` when current fuel or the matching observed burn is invalid, non-finite, empty, or non-positive.
- One dedicated burn-analysis lock protects accepted-sample recording, scoped resets, lifecycle reset, and property reads so aggregate pairs and remaining-laps bound reads cannot be observed mid-update or mid-reset.
- `CurrentStint` resets on the existing confirmed pit-exit edge. Temporary telemetry gaps retain the last accepted analysis state, matching the broader Fuel Model lifecycle behavior.

### Stable burn state
- A continuously evaluated candidate burn.
- A stable exported burn value held with a deadband so dashboards do not thrash on minor changes.
- Source/confidence tags so consumers can tell whether the value is live, profile-backed, or fallback-driven.

### Wet/dry routing
- Wet mode follows tyre-compound telemetry for learning/persistence routing.
- Track wetness is exported for UI use, but tyre mode remains the canonical routing choice for fuel learning.
- Cross-condition fallback is allowed, but confidence is penalized when dry data is being used in wet mode or vice versa.
- Track Learning dash condition exports (`TrackLearning.Condition.*`) consume this same active wet/dry routing and do not introduce a parallel condition authority seam.

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
The runtime stable/active burn authority must keep source and numeric value aligned: when valid profile fuel exists and accepted live laps are unavailable, profile fuel is the active burn basis; SimHub/DataCore fallback is last-resort only.
The runtime chooses a stable burn candidate from:
1. trustworthy live window data,
2. profile condition averages,
3. safe fallback behavior.

Fallback promotion guardrail:
- Fallback must never become or remain stable authority when a valid profile candidate exists, or when trusted live confidence is reached.
- If current stable source is fallback and a valid Profile/Live candidate appears, stable value and source are replaced together immediately (no deadband hold against authority replacement).

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
- live tyre-service estimate (`Fuel.Live.TireChangeTime_S`) and runtime selected tyre count (`Fuel.Live.TireChangeCount`) from per-wheel DP tyre flags (fail-open to 4 tyres when flags are unavailable or partial).


Runtime tank-cap ownership:
- Runtime pit exports (`Fuel.Pit.TankSpaceAvailable`, `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`) now resolve tank cap from the live session authority seam first (`EffectiveLiveMaxTank` raw/fresh fallback path).
- Live cap detection now prefers the earliest iRacing DriverInfo restricted seam when available: `DriverCarFuelMaxLtr * DriverCarMaxFuelPct` (with defensive `0..1` or `0..100` percent normalization). If DriverInfo is unavailable, fallback remains `GameData.MaxFuel/CarSettings_MaxFUEL` with pct applied.
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
Pit window state is race-only and depends on stable confidence plus contingency-aware tank feasibility.
Typical states include:
- `N/A`
- `NO DATA YET`
- `SET FUEL!`
- `TANK ERROR`
- open-window states such as `CLEAR PUSH`, `RACE PACE`, or `FUEL SAVE`
- `TANK SPACE` when required fuel cannot fit under the current assumptions.
- Open feasibility (`CLEAR PUSH` / `RACE PACE` / `FUEL SAVE`) now evaluates contingency-aware required add per mode: `needAdd = lapsRemaining * burn + contingencyForMode - currentFuel`.
- If contingency is configured in litres, the same litres value is applied across PUSH/STD/ECO pit-window checks.
- If contingency is configured in laps, contingency litres are resolved per burn basis (PUSH uses push burn, RACE PACE uses stable burn, FUEL SAVE uses save burn).
- Fuel-only `N/A` now means no stop is needed after reserve protection too; base-distance no-stop without reserve coverage still enters pit-window feasibility states.

## Outputs (exports + logs)
### Core exports
The full authoritative export list lives in `Docs/Internal/SimHubParameterInventory.md`. The key Fuel Model families are:
- `Fuel.LiveFuelPerLap*`
- `Fuel.Burn.DisplayAnalysis` (presentation-only popup/page state toggled by `LalaLaunch.BurnDisplayToggle`)
- `Fuel.Burn.Analysis.*` (fresh accepted-lap popup analysis: `LastLap`, `Avg3`, `Avg5`, `CurrentStint`, `SessionAvg`, `MaxObserved`, `AvgSampleCount`, `StintSampleCount`, `SessionSampleCount`, and compatibility alias `SampleCount`)
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
- `LalaLaunch.PreRace.*` as the separate pre-race/on-grid info layer. PreRace planner-authority matching consumes the resolved Live Detect race-definition seam (`IsLimitedSessionLaps` / `IsLimitedTime`, compatibility fields, then `SessionsXX` fallback) rather than independently interpreting raw session fields.
- `LalaLaunch.PreRace.*` current-fuel basis uses a narrow fallback seam for pre-grid telemetry gaps: live current fuel when valid/positive; setup fallback (`Fuel.Setup.FuelLevel` when valid) is allowed only during pre-race/grid/formation phases (SessionState `<4`); active race-running (SessionState `==4`) stays live-fuel authoritative even if live fuel is `0`; otherwise `0`.
- `LalaLaunch.PreRace.TotalFuelNeeded` is the driver-facing pre-race/start requirement seam. It first uses gated Strategy planner authority when planner total is valid and planner/live car, canonical track key, resolved Live Detect race basis, strict race length, and manually forced Dry/Wet condition gates pass; manual Dry/Wet requires a known matching live condition. The accepted path is `max(0, FuelCalculator.TotalFuelNeeded - FormationFuelPlanned + FormationFuelRemaining)`. If any authority gate fails, the rejected path retains `base race fuel + active contingency + FormationFuelRemaining`. `FormationFuelRemaining` is `0` for invalid/unknown session state and race-running/post-race, so runtime fuel families remain formation-excluded and unchanged.

Setup-fuel fallback export semantics:
- `Fuel.Setup.FuelLevel` publishes setup-derived litres when a valid setup value is available, else `0`.
- `Fuel.Setup.FuelLevelValid` is `true` only when setup-derived litres are valid and `>0`.
- `Fuel.Setup.FuelLevelSource` publishes `brakesdriveunit`, `chassis.front`, `chassis.rear`, `suspension.rear`, or `none`.
- String setup values must be explicitly litre-labelled (`L`, `litre/litres`, `liter/liters`); known non-litre units (for example `gal`, `gallon`) and bare numeric strings are rejected for safety.
- This seam is read-only and does not overwrite `Telemetry.FuelLevel`, `Fuel.LiveFuelPerLap`, pit math, planner math, max tank, or PreRace strategy logic.

Contingency-aware tactical contract:
- Runtime race-running next-stop guidance is canonical on `Fuel.Refuel.*` (`NextLitres`, `NextLitresCeil`, `NextText`, `Valid`, `BurnSource`, `LapSource`, `DataMode`, `BurnMode`).
- `StrategyDash.NextRefuel*` remains supported for pre-green/planning pages and is not obsolete, but new race-running dash work should prefer `Fuel.Refuel.*`.
- Export cleanup caution: do not remove/rename exports until both checks are complete: dashboard JSON usage audit and internal C# reference/consumer audit.
- `Fuel.RequiredBurnToEnd*` provides driver-facing burn-to-end guidance while protecting active contingency reserve.
- Active contingency authority is planner-first, then profile track fallback, then default fallback.
- `Fuel.Delta.LitresCurrent/Plan/WillAdd` and Push/Save variants protect active contingency on the **required-to-finish side only**. `LitresPlan/PlanPush/PlanSave` remain after-stop/after-planned-add deltas (name unchanged), and their burn/lap basis is now DATA-aware (`DATA LIVE` uses live/stable basis; `DATA PLAN` uses planner/profile basis only, never LIVE/SIM authority). `Fuel.Delta.AfterStop.Selected` is plugin-owned selected after-stop delta (`SOURCE PUSH/SAVE/NORM/STBY` => `PlanPush/PlanSave/Plan/Plan`, STBY advisory NORM).
- race-end phase classification is finish-detection owned; dash-side stability behavior near finish should consume `Race.EndPhase` / `Race.EndPhaseText` / `Race.EndPhaseConfidence` and `Race.LastLapLikely` rather than player-scoped flag inference.
- `Fuel.Pit.WillAdd` remains pure clamp mirror (`min(requestedAdd, tankSpaceAvailable)`) and does not add contingency directly.
- `Fuel.Refuel.*` is the canonical runtime next-stop refuel guidance family:
  - `Fuel.Refuel.NextLitres` is next-stop actionable guidance (not always raw finish-from-here deficit),
  - runtime computes `BaseToFinish=(projectedLaps*selectedBurn)-currentFuel`, `FinalStopNeed=BaseToFinish+contingency`, and compares against runtime effective restricted tank capacity from the live-cap authority seam (`ResolveRuntimeLiveMaxTankCapacity`: live restricted cap first, planner/profile fallback only when live cap is unavailable),
  - if `FinalStopNeed <= decision capacity`, publish `max(0, FinalStopNeed)` (final-stop guidance; contingency included once),
  - if `FinalStopNeed > decision capacity`, publish conservative multi-stop add guidance from runtime add-cap seam (`Fuel.Pit.TankSpaceAvailable`) rather than reporting full tank size as an add amount; contingency is not repeatedly stacked on non-final max-fill stops,
  - `Fuel.Refuel.NextLitresCeil` is `ceil(max(0, NextLitres))` and `Fuel.Refuel.NextText` is short dash-safe (`CHECK FUEL`, `NO REFUEL`, `REFUEL {N}L`),
  - Pit Fuel Control DATA/SOURCE (`DATA=LIVE/PLAN`, `SOURCE=STBY/NORM/PUSH/SAVE`) informs basis selection read-only; PLAN is DATA (not SOURCE), and command-send ownership remains unchanged in Pit Fuel Control.

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

Driving → Race transitions can seed race state from the just-learned baseline instead of forcing a full cold start. Those seeds intentionally do not enter `Fuel.Burn.Analysis.*`, which remains fresh-sample-only.

Fuel-burn popup analysis reset rules:
- normal Fuel Model lifecycle reset clears `LastLap`, Avg3/Avg5, `AvgSampleCount`, `CurrentStint`, `StintSampleCount`, `SessionAvg`, `SessionSampleCount`, compatibility alias `SampleCount`, and `MaxObserved`,
- confirmed pit exit clears only `CurrentStint` and `StintSampleCount`,
- `LalaLaunch.BurnAnalysisResetAverages` clears only Avg3/Avg5 and `AvgSampleCount`,
- `LalaLaunch.BurnAnalysisResetCurrentStint` clears only `CurrentStint` and `StintSampleCount`,
- `LalaLaunch.BurnAnalysisResetSessionAverage` clears only `SessionAvg`, `SessionSampleCount`, and compatibility alias `SampleCount`,
- `LalaLaunch.BurnAnalysisResetMaxObserved` clears only `MaxObserved`.

## Failure modes / edge cases
- **Replay timing anomalies:** acceptance/projection behavior may need log verification.
- **Missing live tank cap:** runtime keeps the last valid cap for stability, while UI can still clear user-facing displays when no current valid cap exists.
- **Live-cap authority seam:** Strategy live-cap consumption now uses the same runtime-authoritative seam used by fuel runtime recovery (`raw -> bounded fallback` only) so stale cached caps cannot outlive the fallback freshness window when raw reads disappear.
- **Weak early-session data:** profile-backed fallback may legitimately remain safer than live values until confidence improves.
- **Cross-condition reuse:** dry/wet fallback works, but confidence should be interpreted as lower.
- **Session-change starvation hardening:** refuel-learning cooldown now gates only the refuel-learning block; it no longer exits the full `DataUpdate` tick. This keeps downstream fuel/session/strategy refresh paths running while cooldown is active.

## v1 documentation note
Use **Strategy** terminology for GitHub-facing explanations. Treat older “Fuel tab” wording as legacy technical language only; the canonical UI story is now the Strategy tab plus the separate runtime Fuel Model.

- `LalaLaunch.PreRace.TotalFuelNeeded` first uses the gated Strategy planner total with dynamic formation replacement (`max(0, FuelCalculator.TotalFuelNeeded - FormationFuelPlanned + FormationFuelRemaining)`); rejected authority retains the live/session fallback (`base race fuel + active contingency + FormationFuelRemaining`). It no longer applies the legacy hardcoded `+2 laps` PreRace buffer.
- Added additive `StrategyDash.*` pre-green advice seam; it does not replace runtime `Fuel.*`, `Fuel.Pit.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, or `Pit.FuelControl.*` ownership.


## Fuel Revamp Phase 3B — Temporal semantics and lifecycle classification (analysis snapshot)

This section documents current **as-implemented** temporal semantics without changing runtime behavior.

### Canonical seam split (unchanged)
- `Fuel.Refuel.*` = runtime next-stop tactical guidance seam (race-running authority).
- `StrategyDash.*` + `LalaLaunch.PreRace.*` = pre-green/planning guidance seam.
- These seams are intentionally separate and must not be merged.

### Lifecycle map by family
| Family / export group | Temporal type | Owner | Reset / invalidation trigger | Dash preference | Risk if changed |
|---|---|---|---|---|---|
| `Fuel.LiveFuelPerLap` | live/raw | Fuel Model | 500 ms poll; session/runtime reset; next valid sample | support/debug input; not primary UI value | medium |
| `Fuel.LiveFuelPerLap_Stable` | stable-held | Fuel Model | 500 ms poll with deadband hold; reset on session/runtime reset | preferred fuel-burn seam | do not touch |
| `Fuel.LiveFuelPerLap_StableSource`, `...StableConfidence` | provenance/source-label | Fuel Model | updates with stable authority transitions | debug/provenance + trust widgets | high |
| `Fuel.LiveLapsRemainingInRace` | stable-held (runtime mirror) | Fuel Model + Pace & Projection input | 500 ms poll; projection invalidation/reset | use only if `_Stable` unavailable; otherwise treat as compatibility mirror | medium |
| `Fuel.LiveLapsRemainingInRace_Stable` | stable-held | Fuel Model | 500 ms poll; reset/invalidation follows projection validity | preferred numeric laps-to-end seam | do not touch |
| `Fuel.LiveLapsRemainingInRace_S`, `Fuel.LiveLapsRemainingInRace_Stable_S` | smoothed-display | Dashboard helper (Fuel model EMA state) | 500 ms poll EMA; reset when projection invalid or runtime reset | display-only widgets; do not use for control logic | high |
| `Fuel.RequiredBurnToEnd*` | live/raw + debounced-state (state text/classification) | Fuel Model | 500 ms poll; invalid when basis missing | preferred tactical burn-to-end seam | do not touch |
| `Fuel.Refuel.*` | live/raw + provenance labels + command-context mirrors (`DataMode`,`BurnMode`) | Fuel Model (with Pit Fuel Control DATA/SOURCE selector inputs) | 500 ms poll; invalid on missing basis/cap context; reset on runtime/session reset | preferred runtime next-stop seam | do not touch |
| `Fuel.Delta.*` incl `AfterStop.Selected` | live/raw (selected seam includes source selection) | Fuel Model | 500 ms poll; selected variant follows SOURCE each tick | preferred tactical delta seams | high |
| `Fuel.Contingency.*` | live/raw + provenance/source-label | Fuel Model (+ planner/profile fallback) | 500 ms poll; changes on planner/profile/manual contingency changes | preferred reserve visibility seam | high |
| `Fuel.StintBurnTarget*`, `Fuel.FuelBurnPredictor*` | live/raw + helper-text/source labels | Fuel Model | 500 ms poll; reset on session/runtime reset | secondary guidance; useful on strategy pages | medium |
| `Fuel.Pit.*` | mixed: live/raw + smoothed-display + latched (`Fuel.Pit.Box.*`) | Fuel Model + Pit Timing | 500 ms poll; box variants per-tick; box latches reset on deselect/box exit | runtime pit widgets should use these directly | do not touch |
| `Pit.Box.*` | live/raw + frozen-for-stop target (`TargetSec`) | Pit Timing | per-tick in box; reset on box exit/session reset | preferred in-box lifecycle seam | do not touch |
| `Fuel.Live.TireChangeCount`, `Fuel.Live.TireChangeTime_S`, `Fuel.Live.TotalStopLoss`, `Fuel.Live.PitLaneLoss_S`, `Pit.LastPaceDeltaNetLoss` | mixed: live/raw and smoothed-display | Pit Timing + Fuel Model consumer seams | per pit lifecycle / 500 ms poll; reset/session transitions | preferred pit prediction/perf seams | high |
| `Fuel.Live.RemainingStints`, `Fuel.MaxTank` | live/raw | Fuel Model | 500 ms poll; cap seam invalidation/reset | preferred runtime capacity context | high |
| `LalaLaunch.PreRace.*`, `StrategyDash.*`, `Fuel.Setup.*` | planner-deterministic + helper-text + session-static fallback (`Fuel.Setup.*`) | Strategy Planner + PreRace adapter | per-tick/500 ms; session phase transitions; live/setup availability | preferred pre-green seams only | do not use for race-running refuel widgets |
| `Fuel.ProjectionLapTime_Stable`, `Fuel.ProjectionLapTime_StableSource` | stable-held + provenance/source-label | Pace & Projection | 500 ms poll; deadband hold; reset on runtime/session reset | preferred projection seam | do not touch |
| `Fuel.Live.DriveTimeAfterZero`, `Fuel.Live.ProjectedDriveSecondsRemaining`, `Fuel.After0.*` | live/raw + held-through-gap fallback between planner/live after-zero | Pace & Projection + Fuel Model | 500 ms poll; timer-zero/lap-cross transitions; reset | support/runtime projection seams; not primary dash headline | medium |
| `Race.EndPhase*`, `Race.LastLapLikely` | debounced-state | RaceFinish lifecycle | per-tick with confidence/debounce; session/lifecycle reset | preferred finish-phase seam | do not touch |
| `RaceFinish.*` | frozen-post-event + staged latch (class snapshot then player snapshot) | RaceFinish lifecycle | latch on finish triggers; reset when leaving finish lifecycle/session reset | preferred post-finish widgets | do not touch |

### Direct answers for Phase 3B questions
1. Stable exports genuinely needed: `Fuel.LiveFuelPerLap_Stable`, `Fuel.LiveFuelPerLap_StableSource`, `Fuel.LiveFuelPerLap_StableConfidence`, `Fuel.LiveLapsRemainingInRace_Stable`, `Fuel.ProjectionLapTime_Stable`, `Fuel.ProjectionLapTime_StableSource`.
2. `Fuel.LiveLapsRemainingInRace` is effectively already stable in current runtime flow, but should be treated as a compatibility mirror seam rather than the explicit contract anchor.
3. `Fuel.LiveLapsRemainingInRace_Stable` adds explicit contract clarity/intent for dashboards/debug tools and isolates future rationalisation from the ambiguous non-suffixed name.
4. `_Stable_S` adds EMA display smoothing only (visual anti-jitter), not new calculation authority.
5. Dashboards should prefer `Fuel.LiveLapsRemainingInRace_Stable` for numeric logic, `_Stable_S` only for visual text/needle smoothing.
6. `_S` display-smoothing families include `Fuel.LiveLapsRemainingInRace_S`, `Fuel.LiveLapsRemainingInRace_Stable_S`, plus other `_S` pit display exports (for example `Fuel.Pit.TotalNeededToEnd_S`, pit delta `_S` variants, `Fuel.Live.PitLaneLoss_S`, `Fuel.Live.TireChangeTime_S`).
7. Latched/frozen/held/debounced examples:
   - latched: `Fuel.Pit.Box.EntryFuel`, `Fuel.Pit.Box.WillAddLatched`, boxed stop target `Pit.Box.TargetSec`.
   - frozen-post-event: `RaceFinish.*` snapshots.
   - held-through-gap: stable fuel/projection source/value holds, after-zero planner/live handoff holds.
   - debounced-state: pit-window state text transitions, `Race.EndPhase*` confidence staging.
8. Preferred dashboard seams: runtime race work => `Fuel.Refuel.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Delta.*`, `Fuel.Contingency.*`, `Fuel.Pit.*`, `Pit.Box.*`, `Fuel.ProjectionLapTime_Stable`, `Race.EndPhase*`, `RaceFinish.*`.
9. Debug/provenance/internal-first seams: `...StableSource`, `...StableConfidence`, `Fuel.Refuel.BurnSource/LapSource/DataMode/BurnMode/SelectedBurnPerLap`, most explicit helper text fields.
10. Future rationalisation candidates (not removal now): redundant mirror pairs around `LiveLapsRemainingInRace` vs `_Stable`, overlapping `_S` smoothing duplicates, helper/provenance naming consistency across fuel/projection families.

### Do-not-touch-yet list (high coupling)
- `Fuel.Refuel.*` runtime next-stop contract.
- `Fuel.RequiredBurnToEnd*` state classification contract.
- `Fuel.Delta.*` tactical contingency-protected semantics.
- `Fuel.Pit.Box.*` latch lifecycle.
- `Race.EndPhase*` and `RaceFinish.*` finish lifecycle exports.

### Property Snapshot contract check
- Property Snapshot list reviewed: **yes**.
- Reason: this task documents semantics only and does not add/remove/rename/change behavior of any export/property, so snapshot group membership remains unchanged.
