# Pit Commands and Fuel/Tyre Control

Validated against commit: HEAD
Last updated: 2026-04-24
Branch: work

## Purpose
This subsystem owns plugin-driven pit-lane command dispatch and command-state surfaces used by dashboards and hardware bindings.

It combines:
- built-in pit command actions (`LalaLaunch.Pit.*`),
- custom message actions (`LalaLaunch.CustomMessage01..10`),
- Pit Fuel Control (`LalaLaunch.Pit.FuelControl.*` + `Pit.FuelControl.*` exports),
- Tyre Control (`LalaLaunch.Pit.TyreControl.*` + `Pit.TyreControl.*` exports),
- short-lived command feedback exports (`Pit.Command.*`).

This is the canonical technical document for the pit/custom command stack and replaces ad-hoc split references across unrelated subsystem pages.

## Inputs (source + cadence)
- Driver actions from SimHub Controls & Events / dash touch bindings.
- Live telemetry/state confirmation inputs used by stateful pit toggles (fuel/tyres/repair/auto-fuel).
- iRacing process/window availability for transport selection.
- Pit telemetry seams used by fuel-control ownership/cancel logic:
  - requested pit fuel (`PitSvFuel`),
  - MFD fuel-enable truth (`dpFuelFill`),
  - iRacing AutoFuel state,
  - `Telemetry.IsOnTrackCar` lifecycle edges.
- Declared wetness + tyre-related telemetry for Tyre Control AUTO mode.

## Internal state
### Command transport state
- Selected transport mode (`Auto`, `Legacy foreground SendInput only`, `Direct message only`).
- Direct-path sequencing state used to avoid unsafe duplicate-open/double-send fallback behavior.
- Last action/raw command strings for bounded diagnostics (`Pit.Command.LastAction`, `Pit.Command.LastRaw`).

### Feedback state
- Short-lived command feedback text/active latch (`Pit.Command.DisplayText`, `Pit.Command.Active`).
- `Pit.Command.Active` is a restartable visibility pulse: every feedback publish re-arms the active window, including repeated identical `Pit.Command.DisplayText` values.
- Active hold window is owned in `PitCommandEngine` and is currently 3000 ms (`MessageHoldMs`).
- Severity is owned/published with feedback in `PitCommandEngine`:
  - `Pit.Command.Severity` (`0=None`, `1=Info`, `2=Advisory`, `3=Caution`, `4=Warning`)
  - `Pit.Command.SeverityText` (`None`, `Info`, `Advisory`, `Caution`, `Warning`)
- Severity-priority publishing rule (owned inside `PitCommandEngine` feedback publisher):
  - if no feedback is currently active, publish normally;
  - if feedback is currently active and new severity is higher/equal, replace immediately and restart hold;
  - if feedback is currently active and new severity is lower, suppress the new feedback and keep the current message/hold unchanged.
- Dash severity visual contract:
  - Severity 0 None:
    - no display
  - Severity 1 Info:
    - black background
    - white text
    - no blink
  - Severity 2 Advisory:
    - black background
    - cyan text
    - no blink
  - Severity 3 Caution:
    - yellow background
    - red text
    - no blink
  - Severity 4 Warning:
    - red background
    - yellow text
    - blink for 1 second at 750ms
- Reset seams that clear command feedback now clear:
  - `Pit.Command.DisplayText` to empty,
  - active pulse window,
  - severity to `0` / `None`.
- Stateful-toggle before/after verification status (effect-confirmed vs attempted-only).

### Pit Fuel Control state
- Source state (`STBY`/runtime modes) and AUTO arming semantics.
- Mode state where AUTO is plugin-owned; non-AUTO mode mirrors MFD truth (`OFF`/`MAN` from `dpFuelFill`).
- Target litres and override-active semantics for command generation.
- Fault export state (`Pit.FuelControl.Fault`) for post-settle selector disagreement diagnostics only (`0/1/2/3` contract).

### Tyre Control state
- Mode state machine: `OFF -> DRY -> WET -> AUTO -> OFF`.
- Single-send model: each driver action or AUTO correction sends at most one tyre command.
- Command model: `OFF => #cleartires$`, `DRY => #t tc 0$`, `WET => #t tc 2$` (transport normalizes trailing `$`).
- Truth-mirror requested-compound family mapping uses telemetry `PitSvTireCompound` semantics (`0 => DRY`, `1 => WET`), which are intentionally separate from outgoing chat command values.
- One short settle hold (1.0 s) after plugin-issued tyre commands before truth reconciliation/cancel checks.
- Outside AUTO, plugin mirrors known MFD truth and does not fight manual pit-menu tyre changes.
- Fault export state (`Pit.TyreControl.Fault`) for post-settle selector disagreement diagnostics only (`0/1/2/3` contract).

## Calculation blocks (high level)
1. Receive an action from plugin-owned binding surface.
2. Resolve built-in pit/custom/fuel-control/tyre-control command intent.
3. Dispatch via selected transport mode:
   - Auto: direct-message first, bounded fallback where safe.
   - Legacy: foreground-only SendInput.
   - Direct-only: no fallback.
4. Publish feedback + diagnostics:
   - stateful built-ins use effect confirmation when available,
   - custom/raw/stateless built-ins are transport-attempt only.
   - all pit/fuel/tyre/custom feedback severity classification is centralized in `PitCommandEngine` (dash consumers should not parse text for behavior).
5. Maintain Pit Fuel Control ownership rules:
   - AUTO can cancel on external requested-fuel or MFD-enable edges,
   - lifecycle resets (`IsOnTrackCar` edges, iRacing AutoFuel ownership, offline suppression) force inert/disarmed safety state.
6. Maintain Tyre Control enforcement:
   - OFF sends one clear command (`#cleartires$`) and uses settle-hold before reconciliation,
   - DRY sends one combined command (`#t tc 0$`),
   - WET sends one combined command (`#t tc 2$`),
  - AUTO follows declared wetness (`false => DRY`, `true => WET`),
  - entering AUTO is feedback-only (`TYRE CHANGE AUTO`) and does not blindly send,
  - if AUTO initial evaluation occurs while tyre truth is unknown, initial evaluation remains pending (no send, no desired-state latch) until truth becomes known,
  - AUTO sends one correction command only when known MFD truth disagrees with declared-wet target (`TYRE AUTO CHANGE DRY/WET`),
  - once known truth is available for that first evaluation, AUTO either sends one correction on mismatch or clears pending with no send on match,
  - outside AUTO, known MFD truth remaps mode (`OFF`/`DRY`/`WET`) with no corrective command send and passive mirror wording only (`TYRE OFF` / `TYRE DRY` / `TYRE WET`) when the mirrored mode actually changes,
  - after a raw tyre send failure, a short failure-hold window suppresses only passive truth-mirror feedback publication so `PIT CMD FAIL` remains visible; truth-mode remap still occurs,
  - plugin-driven actions use CHANGE wording (`TYRE CHANGE OFF/DRY/WET/AUTO`) only,
  - AUTO manual takeover feedback is `TYRE AUTO CANCELLED`,
   - unknown/ambiguous tyre truth is held fail-safe (no mode flip, no send),
   - `PIT CMD FAIL` is transport-failure only (raw send returned false), with no timeout-resend loop.

## Outputs (exports + logs)
Canonical export names live in `Docs/Internal/SimHubParameterInventory.md`; key families:
- `Pit.Command.*` (display text, active, last action/raw, max-toggle state),
- `Pit.FuelControl.*` (source, mode, target, override, fault),
- `Pit.TyreControl.*` (mode text/state + fault).

Fault export contract (diagnostic/visual only):
- `Pit.FuelControl.Fault`: `0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`.
- `Pit.TyreControl.Fault`: `0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`.
- Both exports intentionally suppress evaluation during each subsystem’s existing post-command settle/suppression windows to avoid normal latency flash.
- Both exports compute from final post-tick state after mirror/remap/cancel handling; they are not latched from pre-remap state.
- During intentional same-tick mirror/remap/cancel transitions (external mirror, truth-mirror, AUTO cancel), fault export is suppressed to `0` for that tick to avoid one-tick false non-zero flashes.
- Fuel request-fault evaluation is ownership-gated (`AUTO`/armed ownership only), and external mirror/takeover handling clears pending owned mirror expectations so stale request ownership cannot leak after surrender.
- Tyre AUTO correction-send ticks are explicitly fault-suppressed (`0`) in the same tick as correction issue, and settle-window gating is evaluated from post-handler state.
- `Pit.TyreControl.Fault` additionally suppresses DRY/WET evaluation when requested-compound truth is present but cannot be mapped to a known dry/wet family (unknown/unmappable truth => `0`).
- Fault exports never trigger command sends/retries/corrections.

Canonical log wording and meaning live in `Docs/Internal/SimHubLogMessages.md`; key themes:
- transport mode/attempt path (`postmessage` vs `sendinput`),
- fallback reason context and suppression cases,
- effect-confirmed vs unverified delivery semantics,
- tyre compound attempt + single-window confirmation diagnostics.
- Fuel Control action-path diagnostics:
  - `LalaLaunch` Fuel Control action entry logs are intentionally emitted (`PitFuelControl* action received`) to prove SimHub binding reached plugin action methods;
  - `PitFuelControlEngine` emits compact entry snapshots (`entry action=... mode=... source=... suppressReason=...`) for button paths and gated AUTO lap-cross paths;
  - blocked/no-send branches emit explicit reasons (`snapshot-null`, `suppressed:<reason>`, `off-hard-guard`, `auto-plan-blocked`, `plan-invalid`, `source-stby`, `target-invalid`, `send-failed`, `auto-not-armed`, `lap-cross-no-material-delta`, `iracing-autofuel-ownership`, `external-mirror-change`, `owned-mirror-consumed`);
  - telemetry suppression diagnostics are transition/throttled only (no per-tick spam), with explicit suppression-clear transition logging.
- tyre mode transitions, one-shot command sends, and truth-mirror / AUTO-cancel reasons.

## Dependencies / ordering assumptions
- This subsystem owns transport + command dispatch and must remain the only authority for pit/custom command sends.
- Dashboards are consumers only; they should bind plugin actions and read exports, not reimplement command logic.
- Fuel planner/runtime fuel math remains outside this subsystem; pit command/fuel-control uses those outputs but does not redefine fuel model behavior.

## Reset rules
- `Telemetry.IsOnTrackCar` edge transitions reset Pit Fuel Control ownership state and Tyre Control mode to safe defaults.
- Session/lifecycle reset seams clear short-lived command feedback and stale command state.
- External ownership events (for example iRacing AutoFuel) force Pit Fuel Control to inert/disarmed state.

## Failure modes / edge cases
- No usable iRacing process/window: command send attempt fails and feedback/log surfaces should make this visible.
- Direct transport partial-state uncertainty: Auto mode suppresses unsafe fallback on that press to avoid duplicate corruption.
- Chat-open leak prevention is explicit in both transport paths: command transport force-sends `Esc` before `T` so stale-open chat does not absorb the opener key into outgoing raw/custom command payload (`t#...` / `tt#...` corruption).
- Transport success for custom/raw/stateless commands is attempt-only; in-sim effect is unverified by design.
- Tyre control has no resend loop: each target change/correction sends once at most, with a 1.0s settle hold and truth-following remap outside AUTO.
- Tyre command `PIT CMD FAIL` feedback is transport-failure only (raw send returned false), and passive truth-mirror feedback is briefly suppressed after send failure so failure text is not immediately overwritten.
- External pit-menu edits can cancel AUTO once and force safety recovery state in fuel control.
- Fuel Control mode ownership is explicit-command only (no internal `Pit.ToggleFuel` use):
  - suppression gate is now reserved for truly invalid snapshot contexts (`no-plugin-manager`, `no-session`) and does not blanket-block active in-car/offline-testing pit-control button use;
  - `OFF -> MAN` explicitly sends MFD refuel ON (`#fuel$`) and then mirrors MAN truth (`Source=STBY`, AUTO disarmed);
  - OFF is a hard guard: `SourceCycle`/`SetPush`/`SetNorm`/`SetSave`/`SetPlan` send nothing and hold `OFF STBY`;
  - `MAN -> AUTO`: `PUSH`/`NORM`/`SAVE` send immediately and arm AUTO on success with `AUTO REFUEL SET <SRC> X L` feedback; `STBY` and `PLAN` both enter `AUTO STBY` with no send and `AUTO REFUEL STBY` feedback (PLAN is inhibited in AUTO switching);
  - `AUTO -> OFF` always attempts explicit raw OFF command `#-fuel$`; on send failure AUTO remains unchanged, on success exits AUTO and mirrors OFF truth with `REFUEL OFF` feedback;
  - `SetPlan` is MAN-only direct send (`REFUEL SET PLAN X L` semantics). PLAN is blocked in OFF/AUTO and blocked when PLAN validity/session match is false (`Pit Cmd Fail` in MAN when PLAN validity fails);
  - AUTO source sends (`SourceCycle`/`SetPush`/`SetNorm`/`SetSave`) use AUTO feedback wording (`AUTO REFUEL SET <SRC> X L`) and AUTO over-space wording (`AUTO FUEL <requested>L >MAX`), while MAN over-space wording is `REFUEL <SRC> <requested>L >MAX`;
  - impossible `AUTO + PLAN` state is guarded as no-send recovery (`AUTO STBY`, disarmed) and must not fall through to a PUSH send.
  - diagnostics-only instrumentation now logs every action-path early return reason before returning; command payload semantics remain unchanged.
  - External MFD/fuel-request changes are mirror-only:
    - while in AUTO, plugin sends nothing and publishes `AUTO REFUEL CANCELLED BY MFD`, then mirrors to `OFF STBY`/`MAN STBY` based on MFD truth;
    - while in MAN/OFF, plugin sends nothing and mirrors with `REFUEL SET OFF BY MFD`, `REFUEL SET ON BY MFD`, or `FUEL CHANGED BY MFD`;
    - MAN-owned plugin sends are tracked so delayed telemetry echoes are consumed as owned mirror updates (not reclassified as external MFD takeover).
    - Pending owned-mirror expectations are expired only on no-change ticks when observed telemetry already matches expected values, so same-tick owned ON/OFF transitions are consumed first and not misclassified as external MFD edits.

## Test checklist
- Bind and press representative built-in pit actions from SimHub Controls & Events.
- Validate transport mode behavior in each mode (`Auto`, `Legacy`, `Direct only`) with logs.
- Confirm Fuel Control mode/source actions stay on explicit raw-command attempt semantics (single attempt, no retries/poll loops).
- Confirm Pit Fuel Control AUTO cancel triggers on external fuel request and MFD enable changes.
- Confirm Tyre Control mode cycle order and AUTO wet/dry switching follow declared-wet authority.
- Confirm `IsOnTrackCar` edges reset fuel/tyre control states to safe defaults.
