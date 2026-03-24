# Rejoin Assist

Validated against commit: c40b3a8fdd1df4c8c1251ff7f573812b915d726f
Last updated: 2026-03-24
Branch: work

## Purpose
- Detects and classify loss-of-control, off-track, pit-exit, and wrong-way situations, then surfaces warnings to the dash and message overlays. Alerts linger dynamically to keep the driver informed until both time and speed gates are satisfied.
- Provides threat-aware messaging for spins (e.g., “HOLD BRAKES”) by blending traffic proximity with vehicle speed.
- Exposes pit-phase passthroughs for HUD widgets and blocks Launch Mode when a serious incident is active to avoid conflicting driver aids.

## Inputs (source + cadence)
- Vehicle state each tick: speed, gear, yaw rate, pit-lane flag, on-track flag, lap distance %, and session type/flags (start-ready/set/go).
- Surface classification from `PlayerTrackSurfaceMaterial` to detect off-track conditions (>=15).
- Pit phase from `PitEngine` (used to mark exiting pits without duplicating timers).
- Traffic scan (iRacing only): `CarIdxLapDistPct`, `CarIdxEstTime`, `CarIdxTrackSurface`, `CarIdxOnPitRoad`, player index, and track length from session data, refreshed every update tick for threat scoring.
- Profile knobs injected at construction time: linger time, minimum clear speed, and spin yaw threshold (yaw threshold scaled from `SpinYawRateThreshold / 10`).
- Update cadence: `RejoinAssistEngine.Update` is called once per `DataUpdate` loop after pit processing.

## Internal State
- `_currentLogicReason` vs `_detectedReason` capture the active alert and the most recent detection result (can diverge during linger/delays).
- Timers: `_delayTimer` (arm delays per alert type), `_lingerTimer` (post-clear hold), `_spinHoldTimer` (3 s spin emphasis), `_msgCxTimer` (manual override hold).
- Recent lap distance `_previousLapDistPct` and last speed/yaw context for wrong-way and spin detection. `_rejoinSpeed` caches speed at linger start.
- Threat assessor state: smoothed TTC, demotion hysteresis targets, last-good TTC timestamp, and debug string for diagnostics.
- Public flags mirrored to dashboards: pit phase, exiting-pits boolean, threat level, time-to-threat, linger/override timer readouts, and serious-incident sentinel (spin/stopped/wrong-way).

## Calculation Blocks (high level)
1) **Reason detection (pre-priority):**
   - Suppressed states: not in car, in pit lane, offline practice, race-start flag window, or launch mode active short-circuit the system.
   - Incident detection: yaw rate over threshold ⇒ `Spin`; surface >=15 ⇒ `OffTrackLow/High` based on speed threshold; speed <20 kph on track ⇒ `StoppedOnTrack`; decreasing lap distance while moving and not crossing S/F ⇒ `WrongWay`.
2) **Priority resolver:**
   - Manual override (`MsgCx`) wins; first second also hard-resets state.
   - Suppressed states (including launch active) replace the current logic and clear timers.
   - Active timers: spin hold (3 s) and pit-exit phase trump new detections.
   - Linger: when an alert clears, timer runs with dynamic duration (threat-aware minimum 2 s, scaled by speed) and requires speed above the configured threshold to dismiss.
   - New alerts respect per-type delays (1.5 s high-speed off-track, 2 s stopped, else 1 s) before latching; spin latches immediately and starts the spin hold timer.
3) **Threat assessment:**
   - Scans opponents up to 60 % of a lap behind; derives distance and TTC from EstTime or a fallback formula, ignores NIW/pit cars, and applies guards for far-distance/short-spike noise.
   - Smooths improving TTC (EMA) and applies sensitivity scaling when rejoining or slow, adjusting time/distance gates and hysteresis demotion holds.
   - Classifies `CLEAR/CAUTION/WARNING/DANGER` and emits debug string for trace overlays.
4) **Message selection:**
   - Maps logic reason to text; suppresses lingering text once the detected reason is clear.
   - Spin text escalates to “HOLD BRAKES” only when slow (≤50 kph) *and* threat under 6 s or WARNING/DANGER.

## Rejoin reasons, triggers, and priorities
- **SettingDisabled (0):** Returned only by legacy callers; not actively produced by detection logic. Treated as suppressed (priority branch that clears timers and prevents alerting).
- **NotInCar (1):** Fires when `IsOnTrack` is false; hard-resets state and clears timers during the suppressed priority path (launch mode also suppressed).
- **InPit (2):** Detected via `IsInPitLane`; forces suppressed state so dash messages stay hidden while in lane. Pit-exit alerts are instead driven by `PitEngine` phase later in the priority ladder.
- **OfflinePractice (3):** Session type equals “Offline Testing”; suppresses alerts so test sessions stay quiet.
- **RaceStart (4):** Session flags in start window (`SessionState` 1–3 or StartReady/Set/Go); suppresses alerts to avoid spurious warnings during grid/launch. Launch Mode active also enters the same suppressed branch.
- **LaunchModeActive (5):** Applied when the launch module is active; follows suppressed-state path to avoid overlapping driver aids.
- **MsgCxPressed (6):** Manual cancel button. First second forces a full reset and then holds the override for up to 30 s (priority 1). During the hold, reason code stays `MsgCxPressed` and all alerts are suppressed. After 30 s, the override timer self-expires.
- **None (50):** Neutral state. Any lingering timers must clear before returning here (time + speed gates).
- **PitExit (60):** Mirrors `PitEngine` phase `ExitingPits`; outranks linger/new detections (priority 3). Dismisses when phase ends; no delay/linger timers applied.
- **StoppedOnTrack (100):** Speed <20 kph on track surface; uses a 2.0 s arming delay before latching and then obeys linger/time-to-clear rules. Message: “STOPPED ON TRACK - HAZARD!”.
- **OffTrackLowSpeed (110):** Off-track surface (>=15) and speed below threshold; uses 1.0 s delay before latching. Message: “OFF TRACK - REJOIN WHEN SAFE”.
- **OffTrackHighSpeed (120):** Off-track surface (>=15) and speed at/above threshold; uses 1.5 s delay before latching. Message: “OFF TRACK - CHECK TRAFFIC”.
- **Spin (130):** Yaw over threshold; latches immediately, starts 3 s spin-hold timer, and invokes spin-specific messaging (HOLD BRAKES gate + threat-aware linger). Message stays `Spin` during hold and linger unless MsgCx overrides.
- **WrongWay (140):** Lap distance percentage decreasing outside S/F crossing (with forward gear and speed >5 kph); latches immediately (no delay) and follows normal linger rules. Message: “WRONG WAY - TURN AROUND SAFELY!”.

### MsgCx (cancel) behaviour per reason
- Triggers priority 1 override: replaces the active logic reason with `MsgCxPressed`, suppresses all alert text, and zeros timers during the first second (full Reset).
- While active (<30 s), the reason code published to SimHub remains `MsgCxPressed`, preventing downstream dashboards from showing rejoin warnings regardless of prior state. After expiry, detection/priorities resume as normal at the next `Update` tick.

## Outputs (exports + logs)
- SimHub exports: `RejoinAlertReasonCode/Name/Message`, pit phase (`RejoinCurrentPitPhase(Name)`, `RejoinIsExitingPits`), and threat metrics (`RejoinThreatLevel/Name`, `RejoinTimeToThreat`).
- Serious-incident flag (`IsSeriousIncidentActive`) feeds Launch Mode blocker to cancel/deny launches when spin/stopped/wrong-way is detected.
- Dash visibility toggles (`LalaDashShowRejoinAssist`, `MsgDashShowRejoinAssist`) live alongside other dash controls so users can hide the overlay without disabling logic.
- Log: manual cancel trigger logs `[LalaPlugin:Rejoin Assist] MsgCx override triggered.` via `TriggerMsgCxOverride()`.

## Dependencies / ordering assumptions
- Requires `PitEngine` instance to mirror pit phases; rejoin update runs after pit update so pit-exit state is current before evaluating priorities.
- Traffic/threat block is iRacing-only; non-iRacing titles remain at `CLEAR` with default TTC.
- Profile inputs must stay within defensive bounds: linger time is clamped to 0.5–10 s when wiring `PitEngine`; spin yaw threshold is divided by 10 before passing to the engine.

## Reset Rules
- Session-token change resets the engine, pit phase, lap-distance tracking, and threat state to defaults before continuing the new session.
- Manual cancel (`MsgCx`) resets state during the first second of the override; leaving the car (`NotInCar`) also clears timers and reasons.
- Spin hold and manual override timers self-expire at 3 s and 30 s respectively; linger clears once both time and speed gates are met.

## Failure Modes / Safeguards
- Threat scan falls back to `CLEAR` if required iRacing arrays or lap distance data are missing; debug string records the guard cause for diagnostics.
- Wrong-way detection guards against start/finish crossings (lap pct wrap) to avoid false positives; still relies on forward gear check to reduce reverse-stint noise.
- High-speed off-track and stopped-on-track alerts require a minimum delay before latching, reducing transient false alarms but delaying the first warning slightly.
- Linger dismissal requires both time and speed gates; if the car is kept below the configured clear-speed threshold, the visible alert will persist longer by design.

## Test Checklist
- Trigger each alert path (spin, wrong-way, stopped, off-track high/low, pit exit) and verify correct message text and delay/linger behaviour.
- Confirm threat levels climb and decay with nearby traffic during a rejoin (including spin “HOLD BRAKES” gating).
- Validate MsgCx override clears messages immediately and holds suppression for up to 30 s.
- Verify session change resets state (reason code/time-to-threat clear) and Launch Mode refuses to arm during active serious incidents.
- Check pit-exit flagging matches PitEngine phases on dash exports while on-track alerts remain suppressed in pit lane.
- Adjust profile knobs (linger time, clear speed, yaw threshold) and ensure changes flow through without restart.

## TODO / VERIFY
- TODO/VERIFY: Validate spin yaw-rate threshold scaling (`/10`) against telemetry units to ensure cross-sim correctness.
- TODO/VERIFY: Threat assessor currently ignores cars more than 60 % of a lap behind; confirm this window is sufficient on ultra-short tracks.
