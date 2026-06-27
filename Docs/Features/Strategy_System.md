# Strategy System

This page explains the user-facing Strategy workflow in Lala Race Assist Plugin.

![Strategy tab overview](../Images/StrategyTab.png)

For a full post-install SimHub walkthrough of the plugin tabs and setup flow, see: [YouTube walkthrough (~30 min)](https://youtu.be/Ug9BRo0WRbE).

## 1. What Strategy is

**Strategy** is the main planning tab and the single user-facing entry point for race planning.

It brings together:

- saved profile and track data,
- live session snapshot data,
- deterministic planner outputs,
- preset management,
- PreRace display support.

User-facing docs should treat this as **Strategy**. Older “Fuel Planner” wording belongs to technical/internal documentation only.

## 2. Strategy vs live observation

### Runtime boundary (important)
Strategy owns planner intent and pre-race guidance inputs. Runtime race-running fuel authority remains in plugin-owned runtime exports (`Fuel.*`, `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`). Dashboards consume these exports and do not replace plugin calculations.


The plugin keeps a deliberate split between:

- the **live fuel and pace systems**, which observe the current session and build confidence, and
- the **Strategy planner**, which creates a stable race plan from selected inputs.

That split is intentional. Stable planning should not drift every lap just because live conditions are still settling.

## 3. Planning sources

### Profile/manual planning

Use this when you want a stable plan built from:

- saved track values,
- saved profile values,
- manually chosen planning inputs.

This is the right choice when predictability matters most.

### Live Snapshot

Use this when you want Strategy to follow the current session snapshot.

When valid live values exist, the matching manual controls are disabled. That keeps the source of the plan clear instead of blending manual and live ownership.

## 4. What Live Snapshot means in practice

Live Snapshot is about **using the session as the source** for the relevant planning values. That can include:

- live lap-time context,
- live fuel-per-lap context,
- live confidence,
- live tank-cap information,
- live pace-versus-leader behavior when available.

Important rule:

> Live Snapshot helps you adapt to the session, but it does not erase the difference between observing the session and committing to a stable plan.

## 5. Why confidence matters

The plugin does not blindly trust every lap. Confidence exists because:

- pit laps are different,
- incident laps can poison the data,
- early laps are often noisy,
- wet and dry conditions need separate treatment.

Until confidence builds, saved profile values may still be the safer planning basis. For the fuel-learning side of that story, see [Fuel Model](Fuel_Guidance.md).

## 6. Presets

Presets are part of the Strategy workflow.

### Current preset flow

- Choose a race preset from Strategy.
- Open **`Presets...`** to create, rename, edit, save, or delete presets.
- Return to Strategy to apply and use them.
- The Preset Manager popup now uses the same working ComboBox behavior as the main Strategy page, keeping the selected value readable against the dark theme without breaking dropdown selection.

There is no separate top-level Presets tab.

### What presets are good for

Use presets for repeatable race assumptions such as:

- laps vs timed race format,
- contingency,
- tyre-change time,
- max-fuel assumption,
- preferred PreRace mode.

## 7. Track-scoped planning values

Some planning values belong to the current track/layout record rather than the whole car profile.

That helps prevent carrying the wrong assumptions between venues. Examples include:

- wet multiplier,
- pace delta to leader,
- fuel contingency settings.


## Pit Service Regulations

Strategy includes a manual **Pit Service Regulations** selector. It is a strategy/preset assumption, not an automatic series detector.

- **Default:** fuel then tyres, sequential service.
- **IMSA:** fuel and tyres together, simultaneous service.
- **NEC:** fuel and tyres together, simultaneous service, with the selected car profile's **NEC Refuel Rate Factor (%)** applied to the normal refuel rate. A factor of `100%` means no restriction; `75%` means refuelling is modelled at 75% of the normal profile rate.

Race Presets persist this selection. Existing/legacy presets without the field load as **Default**. The plugin does not infer IMSA/NEC from series name, preset name, track, car, or session metadata. Runtime boxed-stop prediction and Strategy stop modelling use the same shared service-time authority so `Fuel.Live.TotalStopLoss`, PitExit prediction, and planner stop timing stay aligned. Stationary service time is not treated as additional race-driving time for fuel-to-end projections, so choosing a slower refuel regulation can increase stop-loss/PitExit timing without inflating `Fuel.Refuel.*` targets by itself.

## 8. PreRace

PreRace belongs under the Strategy story because it helps the driver understand the planned situation before the start.

Its role is intentionally limited:

- it is **display-oriented**,
- it is for **on-grid/pre-start guidance**,
- it does **not** replace Strategy calculations,
- it does **not** change the live fuel model.

PreRace status contract (dash-facing):
- `StatusText` now reports explicit outcomes (`NO STOP OKAY`, `SINGLE STOP OKAY`, `MAX FUEL REQUIRED`, `STRATEGY MISMATCH`, etc.).
- `StatusColour` publishes `green` / `orange` / `red` so dashboards can style the state without re-implementing logic.
- In `Auto`, PreRace first classifies the required strategy from stints (`<=1.0 no-stop`, `<=2.0 one-stop`, `>2.0 multi-stop`) and then evaluates status as that required strategy; Auto does not inherit manual mismatch warnings.
- In `Auto`, fuel-per-lap selection is live-stable first; when stable live fuel is unavailable it now falls back to selected planner fuel (`FuelCalculator.FuelPerLap`) before profile/generic fallback.
- In non-Auto modes, planner/live combo or race-definition mismatches are shown as an **orange caution** (`STRATEGY MISMATCH`) only when planner/live inputs are comparable; transient unknown values do not trigger mismatch.
- `FuelDelta` is now live for grid workflow:
  - required one-stop path uses `(current fuel + requested add) - total fuel needed`,
  - no-stop/multi-stop paths use `current fuel - total fuel needed`.
- One-stop feasibility now uses pit-stop refill capacity (`fuel still needed > effective stop-fill capacity` => red `ONE STOP NOT POSSIBLE`) before normal underfuel/overfuel checks.

## 9. What users should trust

Once the system has enough clean data, users should usually trust:

- the Strategy outputs,
- the stable fuel basis,
- the saved track data they have validated and locked,
- the profile data that has been learned and reviewed.

If Strategy looks repeatedly wrong, the usual causes are:

- poor live confidence,
- stale or bad saved track data,
- a profile value locked too early,
- a mismatch between live observation mode and stable planning intent.

## 10. Practical workflow

### Before the race

- Check the selected profile and track.
- Pick the right preset if you use presets.
- Decide whether you want Profile/manual planning or Live Snapshot.
- Confirm tank assumptions and contingency look sensible.
- Use PreRace for on-grid context only.

### During the race

- Let the plugin keep building live confidence.
- Avoid constant planner tweaking unless something is clearly wrong.
- Use the strategy displays as stable guidance, not as a reason to chase every lap.
- For end-of-race fuel pacing, prefer plugin-owned tactical guidance:
  - `Fuel.RequiredBurnToEnd*` for burn-to-end/state/source,
  - `Fuel.Delta.Litres*` tactical deltas (now contingency-protecting on the required-to-finish side).

### After the race

- Review anything that looked repeatedly wrong.
- Relearn only the affected saved values when needed.
- Keep good locks in place once validated.


## 11. Strategy ownership updates (May 2026)
- Fuel-per-lap source helper now sits under the input for narrow-width readability.
- Track condition ownership is explicit: `Auto`, `Dry`, `Wet`.
- Race type ownership now includes persistent `Live Detect`; detected race format/length are pulled from declared race metadata rather than current practice/qualifying session length.
- Race-definition ownership is now exclusive:
  - `Live Detect`: race length controls remain telemetry-owned while selected; preset/profile/manual setup state is preserved.
  - Live Detect no longer clears selected/applied preset state; switching owners preserves preset/profile/manual setup values and only changes race-basis authority while selected.
  - `Preset` owner keeps race type/length aligned to the selected/applied preset; use the `↻` reapply button for a deliberate reset of preset-owned values.

- PreRace fuel-needed basis uses the matching Strategy planner total when planner/live car, track, resolved Live Detect race basis, strict race length, and any manually forced wet/dry condition agree; manual Dry/Wet also requires a known live condition. It dynamically replaces planned formation fuel with remaining formation fuel. If those gates do not pass, PreRace keeps its live/session fallback calculation rather than trusting a mismatched plan.
- StrategyDash V2 adds pre-green advice exports only; race-running fuel/pit/control contracts remain on existing runtime seams.

- Strategy Race Basis now explicitly selects owner: Preset, Lap-Limited, Time-Limited, or Live Detect.
- Live Detect only owns race type/length while selected; preset/manual setup values are retained.
- Refresh Calcs now recomputes calculated strategy outputs/stints from current effective inputs only; it does not reload profile/live data, reapply presets, or change owner/source selections.

- Owner mode and effective basis are now treated separately in planner internals: owner radios (`Preset`/`Lap`/`Time`/`Live Detect`) select authority, while strategy calculations, preset dirty state, and preset serialization use the resolved effective race basis.


## 12. Tyre-stop intent ownership (May 2026)
- Presets now own **Pit Service Regulations** and **Tyres Expected** intent (whether planned stops include tyre service).
- Tyre timing seconds are not preset-owned; they come from profile-seeded Strategy tyre slider and can be manually overridden for what-if planning.
- Strategy stop math gates tyre time with Tyres Expected (OFF = 0s, ON = slider value), then routes fuel/tyre service timing through the shared regulation-aware pit service model.
- Live pit prediction/export names remain unchanged, but the target/stop-loss values now use the same regulation-aware service-time authority as Strategy.


- Live Detect startup default: when a live session context is active, Strategy now prefers Live Detect as the initial race-basis owner only before the driver has explicitly chosen Preset, Lap-Limited, or Time-Limited or edited Race Minutes/Laps.
- Preset apply while Live Detect is selected now updates setup values without forcing race-basis ownership away from Live Detect.
