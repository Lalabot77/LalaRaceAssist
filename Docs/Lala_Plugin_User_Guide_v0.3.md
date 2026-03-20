# Lala Plugin – Plugin & Dashboards User Guide

**Applies to:** SimHub v9+

**Supported sims:** iRacing only.

This guide explains the supported user-facing parts of the plugin and dashboard package, how they interact, and how to use them without corrupting data or distracting the driver.

> **Scope note:** the broader message-dash / global message system is not currently active on release dashes and is not covered here as a normal user feature. Functional pit and rejoin popups are covered normally.

## 1. System Overview

Lala Plugin separates responsibilities like this.

### Plugin

- Learns data.
- Stores data.
- Protects data.
- Performs calculations and publishes outputs.

### Dashboards

- Display the results.
- Provide situational awareness.
- Offer limited interaction through touch areas or hardware bindings.
- Do not learn or store data themselves.

If something looks wrong on a dash, the fix is usually in the plugin inputs, saved values, or current session state rather than in the dashboard art.

## 2. Data Model & Philosophy

Data is stored at three levels:

- **Car profile**
- **Track profile / layout-specific record**
- **Condition data** (dry / wet)

Typical lifecycle: **auto-learn → validate → lock → trust**.

Locks exist so traffic, replays, weather transitions, incomplete pit cycles, or bad laps do not overwrite good reference data.

## 3. Dash Controls & Live Adjustments

Top-level plugin tabs are ordered left-to-right as **Strategy**, **Profiles**, **Dash Control**, **Launch Analysis**, and **Settings**.

### 3.1 Recommended bindings

- **Cancel Msg Button**: cancels pit and rejoin popups and temporarily suppresses repeats.
- **Pit Screen Toggle**: manually shows or hides pit-related screens.

Other bindings may exist, but those two are the important everyday controls.

### 3.2 Visibility and pit-screen behaviour

Main dash, message/strategy dash, and overlay visibility are controlled separately. These toggles change where information is shown; they do not change the underlying calculations.

Pit screens can appear automatically when pit context is active, or manually when you force them on. If a pit screen appears at the wrong moment, **Pit Screen Toggle** is the driver override.

### 3.3 User variables

Some live-adjustment controls tune thresholds rather than core calculations. Typical examples include:

- Rejoin linger time and clear-speed thresholds.
- Spin detection sensitivity.
- Pit entry deceleration behaviour and pit entry buffer.

Adjust these only when you are chasing a specific false positive or a repeatable mismatch with your driving style.

## 4. Launch Settings & Analysis

Launch Settings now lives inside the top-level **Settings** tab under a collapsed **Launch Settings** expander. It still controls launch behaviour and launch telemetry capture, and per-car defaults can still be stored in profiles and then applied to live use.

- Typical launch controls include target RPM / throttle, tolerances, bite point behaviour, bog-down detection, and anti-stall sensitivity.
- Launch telemetry recording is for post-run analysis and does not change what the dashboards do live.
- **Launch Analysis** is the top-level tab used to review saved traces, summaries, and graph overlays.

## 5. Fuel System

The Fuel system combines live telemetry, stored profile data, confidence logic, and deterministic maths to produce a stable strategy instead of reacting to every lap fluctuation.

It operates in two modes:

- **Live Snapshot mode** = observe the current session and compare against live conditions.
- **Profile mode** = commit to planning inputs and keep the strategy stable until you deliberately change them.

### 5.1 Why live numbers take time to settle

Not every lap is accepted for pace and fuel learning. Pit laps, obvious incidents, invalid laps, cold-tyre out-laps, and major anomalies are filtered so the planner is not poisoned by bad data.

### 5.2 Confidence and stable values

Confidence is the plugin’s answer to “how representative is this live data?” Until enough clean laps exist, live values remain tentative and profile values may still be safer for planning.

### 5.3 Live Snapshot mode

- Shows live fuel burn, live pace, confidence, session context, and live tank-cap information when available.
- The live-session panel is intentionally live-only; when there is no current live sample, it should read as unavailable rather than silently pretending profile values are live.
- Pace-vs-leader follows the current live leader pace delta when that live source exists.
- If live leader pace is unavailable, the planner falls back safely instead of keeping a stale hidden manual override.

### 5.4 Profile mode

- Uses stored car and track data plus your current planning inputs.
- Keeps the plan deterministic until you deliberately change the inputs.
- This is where presets and locked reference values are meant to be used.

### 5.5 Planning inputs that are now track-scoped

Some planning values now belong to the selected track/layout record rather than the general car profile. The important ones are wet multiplier, pace-vs-leader, and fuel contingency settings. That makes the planner less likely to carry the wrong assumptions from one venue to another.

### 5.6 Strategy window basics

The strategy calculation uses race type, race length, lap time, fuel burn, tank capacity, refuel rate, pit-lane loss, tyre-change time, contingencies, and mandatory stop rules to produce a practical stop plan.

- Timed races include the expected drive time after the clock reaches zero.
- Pace delta to leader affects effective race distance in long races.
- Pit-lane loss should be learned cleanly, then locked once trusted.
- Preset max fuel is treated as a percentage of base tank so presets remain portable across cars.

## 6. Profiles & TRACKS Data

Profiles are the plugin’s long-term memory.

### 6.1 Car profile

Stores car-specific launch, fuel, and related defaults. **Apply to Live Session** pushes those values into the active session; saving persists them to disk.

### 6.2 TRACKS / per-layout storage

- Pit-lane time loss.
- Pit entry / exit markers.
- Dry and wet best / average lap times.
- Dry and wet fuel-per-lap data.
- Track-scoped planning defaults such as wet multiplier, pace-vs-leader, and contingency settings.

Pit-lane loss and markers should be auto-learned, checked, then locked when they are known-good. Dry/wet pace and fuel values should only be locked once they are representative.

### 6.3 Locking philosophy

**Locked** means automatic learning will not overwrite the saved value. **Unlocked** means learning is allowed. Lock too early and you freeze bad data; never lock and noise can keep leaking in.

### 6.4 When to zero and relearn

- Track layout changed.
- Major car physics or setup assumptions changed.
- Saved data is clearly corrupted or untrustworthy.
- A specific learned item no longer matches reality.

## 7. Driver Aids & Race Context

### 7.1 Pit Entry Assist

Pit Entry Assist uses learned/saved pit markers and braking maths to show how early or late you are for the pit entry line. It is most useful once the track markers are validated and locked.

### 7.2 Rejoin warnings and pit popups

Pit and rejoin popups are active user-facing features. Use **Cancel Msg** if they become distracting in a specific moment, but if they are repeatedly wrong the better fix is usually to review the saved data or thresholds behind them.

### 7.3 Opponents and Head-to-Head (H2H)

Supported dashes can show nearby race-context information from Opponents plus H2H comparison widgets.

- **Race H2H** compares your class targets ahead and behind in the race order.
- **Track H2H** compares nearby same-class cars around you on track.
- H2H is a read-only race-context aid for the driver; it is not something you constantly manage in the plugin UI.

## 8. Dashboards

Dashboards consume plugin outputs and automatically change pages based on session state. Touch areas and SimHub page bindings are fallbacks, not the primary workflow.

- **Primary race dash** = main driver view.
- **Strategy / support dash** = extra strategy and utility information.
- **Overlay** = supplementary alerts and compact helpers.
- Visibility toggles and declutter-style options should be used to decide what appears where, rather than editing the calculations.

## 9. Best Practice

- Let the systems learn first.
- Lock good values once they are representative.
- Avoid mid-race tuning unless something is clearly wrong.
- Treat the plugin like a race engineer, not a lap-by-lap calculator.
