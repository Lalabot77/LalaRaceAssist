# Pit Timing and Pit Loss

Validated against commit: HEAD
Last updated: 2026-06-12
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
  - regulation-aware boxed service model from the shared `PitServiceTimeModel` seam plus `+1.0s` stationary service overhead, repair-aware. Service regulations are user-selected in Strategy and persisted in Race Presets: `Default` means sequential fuel then tyres (`fuelTime + tireTime`), `IMSA` means simultaneous fuel and tyres (`max(fuelTime, tireTime)`), and `NEC` means simultaneous fuel and tyres with the car-profile `NecRefuelRatePercent` factor applied to the normal car/profile refuel rate before computing fuel time. Runtime `tireTime` remains selected-tyre-count aware (`Fuel.Live.TireChangeCount` + scaled `Fuel.Live.TireChangeTime_S`, fail-open to 4 tyres only when tyre flags/evidence are unavailable or partial, strategy-slider independent (profile full-4 base)); the boxed-target latch uses current-stop tyre-selection evidence captured in pit lane, keeps accepting pre-service selection changes (including `0` → non-zero MFD updates), freezes that evidence only when valid box service/countdown starts, and clears it on manual/session reset paths before any planner-safe live-recovery early return, so fuel-only stops with confirmed `0` tyres do not carry a stale/default 4-tyre target, reset-in-lane cases fail open instead of reusing prior-stop evidence, and real tyre stops are not reduced by in-service flag clear-down,
  - fixed pit-box transition allowance `+2.00s` (slow-in/settle/launch-out to limiter).

Canonical boxed-stop prediction contract:

Tyre service-time learning remains a car-profile seam (not pit-loss baseline ownership): it learns from a clean all-four tyre stop using per-wheel tyre clear timing and persists a corrected service-time estimate (derived model preferred, fixed-tail fallback), then should be driver-locked in Profiles once validated.

`boxed stop loss = learned drive-through baseline + boxed service model + 2.00s transition allowance`

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
- `Fuel.Live.RefuelRate_Lps` (active effective runtime pit-service refuel rate for displays/prediction: Default/IMSA publish the normal stored profile/FuelCalculator baseline; NEC publishes baseline × `NecRefuelRatePercent / 100` while NEC is selected. Regulation selection does not rewrite stored `CarProfile.RefuelRate`.)
- `Fuel.Live.TireChangeTime_S`
- `PitExit.DistanceM` / `PitExit.TimeS` (pit-exit waypoint distance/time using stored exit marker + live speed). These remain zero outside the pit lane and refresh on the 250 ms poll cadence, matching pit-lane state to avoid noisy updates when circulating on track.
- `PitExit.TimeToExitSec` (plugin-owned blended pit-exit time-to-exit for dash timing): early/low-speed phase follows `PitExit.RemainingCountdownSec`, then blends toward `PitExit.TimeS` as player speed approaches pit-limiter speed. Blend uses limiter authority chain `DataCorePlugin.GameData.PitLimiterSpeed` then parsed `DataCorePlugin.GameRawData.SessionData.WeekendInfo.TrackPitSpeedLimit`; publishes `0` outside pit lane and guards invalid values.
- `Pit.Box.DistanceM` / `Pit.Box.TimeS` (player pit-box waypoint distance/time using native `DriverPitTrkPct`, player track percent, session track length, and live speed). These publish in pit lane, and may also publish in a short pit-owned pre-entry window when authority is valid (`PitPhase.EnteringPits` or fallback player track-percent band near start/finish: `[0.80..1.00] ∪ [0.00..0.20]`). Outside that gate—or when authority inputs are missing/invalid—they publish `0`.
- `Pit.Box.BrakeNow` (plugin-owned BoxEntry “BRAKE NOW” visibility helper). Publishes `true` only when pit-box authority + pit-limit authority are valid, speed is sane (`>2 kph`), distance is positive, pit box is within dynamic threshold `25.0 * (pitLimitKph / 80.0)`, and the same in-lane/pre-entry visibility gate is active. Limiter authority chain: `DataCorePlugin.GameData.PitLimiterSpeed` primary, parsed `DataCorePlugin.GameRawData.SessionData.WeekendInfo.TrackPitSpeedLimit` fallback; publishes `false` when unavailable/invalid.
- `Pit.Box.Active` / `Pit.Box.ElapsedSec` / `Pit.Box.RemainingSec` / `Pit.Box.TargetSec` / `Pit.Box.LastDeltaSec` (driver-facing in-box service countdown contract). `Active` is true only while the player is in a valid in-box service state (`in pit lane && in pit stall && PitPhase.InBox`). `ElapsedSec` reuses the existing pit stop elapsed timer from `PitEngine` (no second timer). During the short settle period (1.0s elapsed), the live effective target is `max(modeledTargetSec, repairRemainingSec)` where `modeledTargetSec` is the shared regulation-aware fuel/tyre service result plus the existing `+1.0s` boxed-service overhead, and `repairRemainingSec` is boxed native repair-left authority (`PitRepairLeft`, plus `PitOptRepairLeft` when optional repairs are enabled). Under the Option A contract, `Pit.Box.TargetSec` means expected stop time if all currently selected/active services complete under the selected Strategy pit-service regulation: fuel, tyres, mandatory repairs, and optional repairs when selected/active/telemetry-reported as part of service. The `+1.0s` overhead is stationary box-service allowance only (boxing/settle/pickup-release slop), not lane travel. At settle threshold, that effective target is latched/frozen for the stop and exported as `Pit.Box.TargetSec`, so late stop telemetry drift no longer moves the countdown target. `RemainingSec` stays repair-aware and is computed each tick as `max(max(0, TargetSec - ElapsedSec), repairRemainingSec)`. `Pit.Box.LastDeltaSec` is computed on stop end as `(latched TargetSec - final ElapsedSec)` where positive means quicker/better than target and negative means slower/worse, preserving the existing dashboard-facing pit popup contract; it remains latched after stop for review, clears when the next boxed stop becomes active, and clears on session/reset paths. All exports publish `0`/`false` when inactive or unavailable (including drive-throughs and missed-box states).

These values feed directly into:
- Fuel Model pit projections.
- Fuel Planner pit loss math.
- PitExit prediction (`PitExit.RemainingCountdownSec`, `PredictedPositionInClass`, and ahead/behind gaps) through the shared total-stop-loss seam. During an active pit cycle, the Opponents/PitExit predictor preserves the latched total-loss seed unless the shared estimate increases or the deliberate pit-service model key changes. The key covers selected regulation, NEC factor, refuel amount/rate, tyre count/time, and modeled service overhead; it excludes repair remaining, so lower estimates from natural repair countdown ageing are not re-latched because elapsed pit-cycle time is already subtracted from the seed. Longer regulation/NEC/service estimates can still extend the remaining countdown and gap math, and deliberate lower service-model changes can lower the active seed. Position can remain unchanged when no class car crosses the revised projected-exit point.

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
- Current-stop tyre-selection evidence used by the boxed-target latch is cleared before any planner-safe live-recovery early return and with the pit-box countdown state on full reset.
- No stale pit loss or prior-stop tyre count is reused.
- Next valid cycle starts fresh; if live tyre flags are unavailable/partial, the boxed-target model uses the conservative 4-tyre fallback until current-stop evidence is available.

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


## Debug logging gate note (2026-06-08)
- PitExit math-audit/detail logging is now controlled by the global Debug Options `Enable Debug Logging` gate and only emits while master `Enable debugging mode` is also on. The visible legacy `PitExit Verbose Logging` toggle was removed from the UI; normal Pit/PitLite/PitExit INFO/WARN operational logs are unchanged.
## Pit Debrief consumer layer

Pit Debrief is an additive latching/readout layer under Pit/PitExit presentation ownership. It consumes PitEngine timing outputs at lifecycle edges and does not replace or alter PitEngine phase detection, pit timers, DTL computation, direct-travel fallback, learned drive-through pit-lane-loss storage, or `Fuel.Live.TotalStopLoss`.

For timing review, `Pit.Debrief.Timing.PredictedTotalLossSec` freezes the existing boxed-stop prediction (`Fuel.Live.TotalStopLoss`) at pit-entry baseline. Because current PitEngine/PitLite final DTL/direct seams are lane-equivalent values that exclude stationary box time, `Pit.Debrief.Timing.ActualTotalLossSec` normalizes them to total-stop-equivalent by adding the latched stationary box duration when a boxed stop occurred; drive-throughs add `0`. `Pit.Debrief.Timing.LossDeltaSec` is only the readout comparison (`actual total - predicted total`) and is not fed back into pit-loss learning or fuel prediction. `SummaryText` publishes this final comparison as `STRAT Δ`, not the old `LOSS` label.

For driver-facing review, `SummaryText` now updates progressively as existing lifecycle evidence arrives: entry after pit entry, service from actual fuel-added evidence and the same current-stop tyre count used by the boxed-target resolver once a confirmed boxed-service state exists (`PlayerCarInPitStall` and pit-road state with `PitPhase.InBox`, or the existing active `Pit.Box` countdown authority), box delta from the existing `Pit.Box.LastDeltaSec` completed-box seam (inverted internally for Pit Debrief sign), and `STRAT Δ` after final out-lap timing. A raw or stale pit-stall signal while the player is not on pit road, while PitEngine is not in the boxed-service phase, or while PitEngine is already `ExitingPits` is not a Pit Debrief box-entry edge and does not call `LatchBoxEntry`. The entry headline is performance-oriented from the latched raw-double entry line time loss (formatted to one decimal in compact `SummaryText`) for `safe`/`normal` line-compliance tokens, while the Pit Entry Assist safety/compliance token remains available in limiter/debug/log evidence. Marginal pit-entry line overspeed within +1.0 kph is intentionally kept as the existing `normal` compliance token, so `Pit.Debrief.Entry.LimiterQualityText` remains `NORMAL` and the timing headline remains eligible. True overspeed above +1.0 kph remains `POOR` and overrides the compact entry section to `ENTRY BAD` with structured compliance detail (`+x.xkph` and late metres when available), not the timing-loss bracket. Box delta means actual boxed/service duration minus predicted boxed/service duration (positive slower, negative faster); the countdown exit latch falls back to the last live elapsed value if PitEngine stop duration is not ready on the first box-exit edge, and Pit Debrief also refreshes from the completed `Pit.Box.LastDeltaSec` seam after countdown finalization using a per-stop validity flag so exact `0.0s` deltas are valid without reusing a previous stop's delta. Pit Debrief does not suppress large finite completed box deltas; `BOX ... (Δ PENDING)` means the completed source was unavailable, invalid, or not completed yet; preliminary box-entry target/elapsed evidence is not treated as a completed box delta. Large deltas are intentionally shown because mandatory repairs, optional repairs, driver swaps, or other in-box service timing can legitimately move actual boxed time far from target. Optional repair abandonment can produce a large negative Pit Debrief box delta because the repair-aware target included service time that did not complete. When repair time influenced the boxed target, the driver-facing `SummaryText` box label becomes `BOX MAND REPAIR`, `BOX OPT REPAIR`, or generic `BOX REPAIRS`; `Pit.Debrief.Box.QualityText` remains the quality enum (`GOOD` for valid non-missed boxed stops, `OVERSHOT`/`MISSED` for missed-box phases, `UNKNOWN` when unavailable). Verbose `[LalaPlugin:PitDebriefBoxDiag]` diagnostics trace the existing countdown/debrief latch path only when master debug and `Enable Debug Logging` are both enabled; they do not change PitEngine phase or timing ownership. The final `[LalaPlugin:PitDebrief]` summary log remains always-on. Exit prediction remains exported and logged under `Pit.Debrief.Exit.*`, but it is no longer included in `SummaryText`.

`Pit.Debrief.Service.FuelTargetLitres` is still a debug/readout target sourced only from existing `Fuel.Pit.Box.WillAddLatched` / `Fuel.Pit.WillAdd` evidence. Verbose `[LalaPlugin:PitDebriefFuelDiag]` source-trace diagnostics are emitted only when master debug and `Enable Debug Logging` are both enabled; normal operation keeps only the final `[LalaPlugin:PitDebrief]` summary log always-on. Pit Debrief preserves positive requested-add evidence when `_isRefuelSelected` drops false after natural refuel completion/reset only after positive fuel movement is present and added fuel is within the completion-tolerance window; when Pit Debrief refreshes before the fuel gauge update on the same tick, the natural-completion check also includes current-fuel-derived added evidence so a normal iRacing completion/reset is not mislatched as an explicit cancel from stale `Pit_AddedSoFar`. A selected→deselected edge while boxed before natural completion is still treated as an explicit cancel and clears the current debrief target to `0.0`; true no-refuel/pre-flow cancel stops also clear stale target evidence. `FuelAddedLitres` remains separate actual-added evidence and is not used as a fake target. When verbose debug logging is enabled, `[LalaPlugin:PitDebriefFuelDiag]` logs report the source evidence, current-fuel added evidence, explicit-cancel latch, natural-completion result, and clear/preserve action.
