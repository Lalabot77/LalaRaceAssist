# Pit Timing and Pit Loss

Validated against commit: 298accf
Last updated: 2026-04-13
Branch: work

## Purpose
The Pit Timing & Pit Loss subsystem measures **real pit lane time loss** during live sessions and exposes a **stable pit-loss figure** used by:
- Fuel Model pit projections.
- Fuel Planner pit loss calculations.
- Dashboards showing expected vs actual pit impact.

It converts raw pit entry/exit signals into **lane travel**, **box time**, and **net pit loss** figures, with safeguards to avoid publishing invalid or misleading data.

Canonical references:
- Log semantics: `Docs/Internal/SimHubLogMessages.md`
- Fuel consumption & pit integration: `Docs/FuelProperties_Spec.md`
- Export list & cadence: `Docs/Internal/SimHubParameterInventory.md`

---

## Scope and boundaries

This doc covers:
- Detection of pit entry/exit.
- Measurement of pit lane travel and stationary time.
- Computation of pit loss (DTL vs direct).
- Publication rules and fallbacks.

Out of scope:
- Fuel-to-add logic (see `Fuel_Model.md`).
- Planner usage of pit loss (see `Fuel_Planner_Tab.md`).
- Dash presentation logic (see `Subsystems/Dash_Integration.md`).

## Canonical pit-loss definition (drive-through baseline)

- **Learned/stored pit lane loss is drive-through baseline only** (clean limiter-speed pass through pit lane, no box stop).
- Boxed-stop runtime predictions add stopped-box components separately:
  - boxed service model (`max(fuelTime, tireTime) + 1.0s` stationary service overhead, repair-aware),
  - fixed pit-box transition allowance `+2.75s` (slow-in/settle/launch-out to limiter).

Canonical boxed-stop prediction contract:

`boxed stop loss = learned drive-through baseline + boxed service model + 2.75s transition allowance`

---

## Inputs (source + cadence)

### Telemetry inputs (runtime, 500 ms cadence)
- Pit lane entry/exit flags.
- Vehicle speed (used to detect stop vs drive-through).
- Lap timing (in-lap, pit-lap, out-lap).
- Session state (race vs non-race).

These are consumed inside the plugin’s main `DataUpdate` loop.

---

## Internal state

### Pit cycle state
A pit cycle is tracked as a **state machine**:

1. **Idle**
2. **Entry armed**
3. **In pit lane**
4. **Stopped / box time**
5. **Exit detected**
6. **Out-lap completion**
7. **Evaluation / publish**
8. **Reset to idle**

Each cycle is isolated; partial or invalid cycles are discarded.

---

### Latched timing values
For a valid pit cycle, the subsystem latches:
- **Lane time** (entry → exit).
- **Box time** (stationary portion inside pit).
- **Direct lane travel** (lane minus box).
- **In-lap time**
- **Out-lap time**
- **Baseline lap time** (average pace reference).

These values are frozen once latched to avoid post-hoc drift.

---

## Calculation blocks (high level)

### 1) Pit entry detection
Pit entry is detected using:
- Pit lane flag transitions.
- Edge detection to avoid re-arming mid-lane.
- PitLite entry arming is guarded against pit-stall state or very low entry speed (≤5 kph) to avoid false arming during pit service or reset edge cases.

On entry:
- Previous pit metrics are cleared.
- Entry timestamp is latched.
- State transitions to “in pit”.

Logs are emitted to confirm arming.

---

### 2) Box time detection
While in pit lane:
- Vehicle speed is monitored.
- Sustained near-zero speed indicates **box stop**.
- Box start and end timestamps are latched.

Drive-throughs are detected when:
- No valid stationary period occurs.
- Box time remains zero.

---

### 3) Pit exit detection
On pit lane exit:
- Exit timestamp is latched.
- Lane time is computed.
- Direct lane travel = lane time − box time.
- Cycle moves to “awaiting out-lap completion”.

If exit occurs without valid entry, the cycle is invalidated.

---

### 4) In-lap / out-lap capture
- **In-lap**: lap ending at pit entry.
- **Out-lap**: lap starting at pit exit.

Both laps must complete cleanly to compute DTL (delta time loss).
If either lap is invalid (incident, missing data), DTL path is aborted.

---

### 5) Pit loss computation

#### A) DTL-based loss (preferred)
If valid baseline pace exists and both laps are valid:

DTL = (InLap + OutLap) − (2 × BaselineLap)

This reflects **net race time lost** due to pitting.

#### B) Direct lane loss (fallback)
If baseline or laps are unavailable:

DirectLoss = LaneTime − BoxTime


This represents raw pit lane traversal time.

Selection rules:
- Prefer DTL when available.
- Fallback to direct lane loss otherwise.
- Never publish negative or zero losses.

Canonical behaviour is documented in logs and fuel spec.

---

### 6) Publication rules
Pit loss is published only when:
- A full pit cycle completes.
- Loss value passes sanity checks.
- Session context is valid (typically Race).

Published values update:
- `Fuel.Live.PitLaneLoss_S`
- `Fuel.Live.TotalStopLoss`
- Planner-accessible pit loss fields.

Once published, the value remains until superseded by a newer valid cycle.

### Driver workflow requirement for learning

When learning pit-lane-loss baseline for a combo:

1. Record pit entry/exit markers as usual.
2. Complete the normal in-lap / out-lap capture flow.
3. Run a **clean drive-through at pit limiter speed** to establish baseline.
4. **Do not stop in the box** for the baseline learning pass.
5. Lock the learned value in Profiles once validated.

This keeps stored pit-lane loss semantically clean and avoids mixing box-transition overhead into the baseline.

#### Locking and blocked candidates
- Per-track pit loss values can be **locked** in Profiles. When locked, new candidates are **not** applied to the stored pit loss value.
- Blocked candidates are captured with timestamp and source so the UI can show what was rejected while locked.
- When unlocked, the next valid cycle can overwrite the stored pit loss normally.

---

## Outputs (exports + logs)

### Core exports
(See `Docs/Internal/SimHubParameterInventory.md` for authoritative list.)

Typical outputs include:
- `Fuel.Live.PitLaneLoss_S`
- `Fuel.Live.TotalStopLoss`
- `Fuel.Live.RefuelRate_Lps`
- `Fuel.Live.TireChangeTime_S`
- `PitExit.DistanceM` / `PitExit.TimeS` (pit-exit waypoint distance/time using stored exit marker + live speed). These remain zero outside the pit lane and refresh on the 250 ms poll cadence, matching pit-lane state to avoid noisy updates when circulating on track.
- `PitExit.TimeToExitSec` (plugin-owned blended pit-exit time-to-exit for dash timing): early/low-speed phase follows `PitExit.RemainingCountdownSec`, then blends toward `PitExit.TimeS` as player speed approaches pit-limiter speed. Blend uses limiter authority chain `DataCorePlugin.GameData.PitLimiterSpeed` then parsed `DataCorePlugin.GameRawData.SessionData.WeekendInfo.TrackPitSpeedLimit`; publishes `0` outside pit lane and guards invalid values.
- `Pit.Box.DistanceM` / `Pit.Box.TimeS` (player pit-box waypoint distance/time using native `DriverPitTrkPct`, player track percent, session track length, and live speed). These remain zero outside the pit lane, and also fail-safe to zero when authority inputs are missing/invalid.
- `Pit.Box.Active` / `Pit.Box.ElapsedSec` / `Pit.Box.RemainingSec` / `Pit.Box.TargetSec` / `Pit.Box.LastDeltaSec` (driver-facing in-box service countdown contract). `Active` is true only while the player is in a valid in-box service state (`in pit lane && in pit stall && PitPhase.InBox`). `ElapsedSec` reuses the existing pit stop elapsed timer from `PitEngine` (no second timer). During the short settle period (1.0s elapsed), the live effective target is `max(modeledTargetSec, repairRemainingSec)` where `modeledTargetSec = (max(fuelTime, tireTime) + 1.0s boxed-service overhead)` and `repairRemainingSec` is boxed native repair-left authority (`PitRepairLeft`, plus `PitOptRepairLeft` when optional repairs are enabled). The `+1.0s` overhead is stationary box-service allowance only (boxing/settle/pickup-release slop), not lane travel. At settle threshold, that effective target is latched/frozen for the stop and exported as `Pit.Box.TargetSec`, so late stop telemetry drift no longer moves the countdown target. `RemainingSec` stays repair-aware and is computed each tick as `max(max(0, TargetSec - ElapsedSec), repairRemainingSec)`. `Pit.Box.LastDeltaSec` is computed on stop end as `(latched TargetSec - final ElapsedSec)` where positive means quicker than target and negative means slower than target; it is non-zero only for 5 seconds after stop end and then automatically returns to `0`. All exports publish `0`/`false` when inactive or unavailable (including drive-throughs and missed-box states).

These values feed directly into:
- Fuel Model pit projections.
- Fuel Planner pit loss math.

---

### Logs
The subsystem emits structured INFO logs for:
- Pit entry arming.
- Exit detection and latched times.
- In-lap / out-lap capture.
- DTL computation and fallback usage.
- Publication decision (DTL vs direct).
- Locking blocks (candidate blocked + logged) when the profile pit loss is locked.
- NaN, infinite, or non-positive pit-loss candidates are skipped and logged before any persistence attempt.

Log semantics are canonical in `Docs/Internal/SimHubLogMessages.md`.

---

## Dependencies / ordering assumptions
- Pace subsystem must provide a stable baseline for DTL to be valid.
- Lap detector must be reliable to correctly identify in-lap and out-lap.
- Fuel Model consumes pit loss only after publication (not mid-cycle).

---

## Reset rules

Pit Timing state resets on:
- Session identity change.
- Car or track change.
- Manual invalidation (e.g. aborted cycle).

On reset:
- All latched pit timings are cleared.
- No stale pit loss is reused.
- Next valid cycle starts fresh.

Reset semantics are centralised in:
`Docs/Reset_And_Session_Identity.md`.

---

## Failure modes / edge cases

- **Drive-through penalties**
  - No box time → DTL may still compute if laps valid.
  - Direct lane loss may be misleading; logs indicate drive-through.

- **Invalid out-lap**
  - DTL path aborted.
  - Direct lane loss used if sane.

- **Replay sessions**
  - Pit signals may be delayed or reordered.
  - Validate behaviour using logs.

- **Traffic on out-lap**
  - DTL includes traffic impact by design.
  - This is intentional and represents real race loss.

---

## Test checklist

1. **Standard pit stop**
   - Box stop detected.
   - Lane, box, and direct times latched.
   - DTL published after out-lap.

2. **Drive-through**
   - No box time detected.
   - Direct loss published or DTL if laps valid.

3. **Aborted cycle**
   - Enter pit, exit immediately, invalidate cycle.
   - No pit loss published.

4. **Session reset**
   - Change subsession.
   - Confirm all pit timing state cleared.

5. **Planner integration**
   - Planner pit loss updates only after valid publication.

---

## TODO / VERIFY

- TODO/VERIFY: Confirm exact speed threshold and dwell time used to classify “box stop” vs drive-through.
- TODO/VERIFY: Confirm whether DTL uses session-average or stint-average pace as baseline.
- TODO/VERIFY: Confirm pit loss publication gating for non-race sessions (practice/qualifying).
- TODO/VERIFY: Native/SDK pit service prediction remains limited in plugin scope. Current runtime authority confirms elapsed stall timing (`PlayerCarInPitStall`, `PitStopElapsedSec`, `GameData.LastPitStopDuration` validation), pit service selection flags (`dpFuelFill`, tyre change selectors), and boxed repair-left time where available (`PitRepairLeft`, optional `PitOptRepairLeft` when the user setting is enabled). Setup-adjustment timing remains intentionally out of scope.
