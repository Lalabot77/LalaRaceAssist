# Pit Commands and Fuel/Tyre Control

Validated against commit: HEAD
Last updated: 2026-06-05
Branch: work

## Purpose
This subsystem owns plugin-driven pit-lane command dispatch and command-state surfaces used by dashboards and hardware bindings.

UI/control surface note: Dash Control -> Global Dash Functions -> Fuel exposes `Fuel Data Plan Mode` (OFF=`LIVE`, ON=`PLAN`) for Pit Fuel Control DATA, and Settings -> Pit Commands exposes `Fuel Data Cycle` (`LalaLaunch.Pit.FuelControl.CycleData`).

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
- iRacing process/window availability for plugin-owned direct window-message transport.
- Pit telemetry seams used by fuel-control ownership/cancel logic:
  - requested pit fuel (`PitSvFuel`),
  - MFD fuel-enable truth (`dpFuelFill`),
  - iRacing AutoFuel state,
  - `Telemetry.IsOnTrackCar` lifecycle edges.
- Declared wetness + tyre-related telemetry for Tyre Control AUTO mode.

## Internal state
### Command transport state
- Transport is plugin-owned and fixed to the direct iRacing window-message path (`postmessage`). Users no longer choose a transport mode in Settings.
- Legacy foreground `SendInput` and Auto fallback are no longer part of the normal user workflow or dispatch path.
- Direct-path sequencing state is retained to avoid unsafe duplicate-open behavior after bounded direct-path aborts.
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
- Driver-facing failure feedback is specific where existing code paths can distinguish the source; all remain `Warning` severity:
  - `PIT CMD WINDOW FAIL` = no usable iRacing process/main window,
  - `PIT CMD CHAT FAIL` = chat-open stage failed after a window was resolved,
  - `PIT CMD SEND FAIL` = empty/invalid command text, text/submit transport failure, or raw/fuel/tyre send returned false without finer source detail,
  - `PIT CMD CONFIRM FAIL` = stateful built-in command was sent but before/after telemetry could not confirm the expected toggle state,
  - `PIT CMD TIMEOUT FAIL` = reserved/documented failure text, but not currently emitted because no existing generic failure publish path has a true timeout distinction.
- Reset seams that clear command feedback now clear:
  - `Pit.Command.DisplayText` to empty,
  - active pulse window,
  - severity to `0` / `None`.
- Stateful-toggle before/after verification status (effect-confirmed vs attempted-only).

### Pit Fuel Control state
- DATA state (`LIVE`/`PLAN`) that selects the burn-assumption basis for `PUSH`/`SAVE`.
- Source state (`STBY`/`NORM`/`PUSH`/`SAVE`) and AUTO arming semantics.
- Mode state where AUTO is plugin-owned; non-AUTO mode mirrors MFD truth (`OFF`/`MAN` from `dpFuelFill`).
- Target litres and override-active semantics for command generation.
- DATA defaults to `LIVE` on session/control reset. Changing DATA always forces `SOURCE=STBY`, disarms AUTO, and sends no fuel command.
- `SOURCE=PLAN` has been retired. The one-release compatibility action `Pit.FuelControl.SetPlan` now maps to `DATA=PLAN` + `SOURCE=STBY` and publishes `FUEL DATA PLAN`.
- Legacy `Pit.FuelControl.PushSaveMode*` compatibility exports/actions remain removed; issue #698 cleanup confirmed no live `PushSaveMode`/`PushSaveModeText` exports or legacy cycle action are present. Use `Pit.FuelControl.Data*` only.
- `NORM` now follows DATA like `PUSH`/`SAVE`: `DATA LIVE` uses runtime/live stable normal target; `DATA PLAN` uses planner/profile normal target. `PUSH`/`SAVE` behavior is unchanged (`LIVE` selects live push/save targets; `PLAN` selects planner/profile memory push/save targets with the existing guarded fallback behavior).
- Fault export state (`Pit.FuelControl.Fault`) for post-settle selector disagreement diagnostics only (`0/1/2/3` contract). Plugin-owned requested-fuel expectations now have a bounded confirmation expiry: after the existing post-send suppression window plus a short confirmation allowance, a still-unconfirmed `PitSvFuel` mismatch is treated as external/manual MFD takeover, clearing the stale pending request and fault for that tick rather than latching a long-lived request fault.

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
3. Dispatch via fixed plugin-owned direct window-message transport.
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
  - after a raw tyre send failure, a short failure-hold window suppresses only passive truth-mirror feedback publication so the warning remains visible; truth-mode remap still occurs,
  - plugin-driven actions use CHANGE wording (`TYRE CHANGE OFF/DRY/WET/AUTO`) only,
  - AUTO manual takeover feedback is `TYRE AUTO CANCELLED`,
   - unknown/ambiguous tyre truth is held fail-safe (no mode flip, no send),
   - `PIT CMD SEND FAIL` is transport-failure only (raw send returned false), with no timeout-resend loop.

## Outputs (exports + logs)
Canonical export names live in `Docs/Internal/SimHubParameterInventory.md`; key families:
- `Pit.Command.*` (display text, active, last action/raw, max-toggle state),
- `Pit.FuelControl.*` (data, source, mode, target, override, fault, compatibility push/save mode aliases),
- `Pit.TyreControl.*` (mode text/state + fault).

Fault export contract (diagnostic/visual only):
- `Pit.FuelControl.Fault`: `0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`. Stale plugin-owned requested-fuel expectations expire after the bounded confirmation window; expiry is handled as manual MFD authority takeover and clears the stale request/fault without changing fuel math or sending a command.
- `Pit.TyreControl.Fault`: `0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`.
- Both exports intentionally suppress evaluation during each subsystem’s existing post-command settle/suppression windows to avoid normal latency flash. Pit Fuel Control also exposes MsgCx as a no-command recovery check that routes already-stale/expired pending requests through the same external/manual takeover handling as telemetry expiry; it is not a live fault clear and not a normal pending-command cancel.
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
  - blocked/no-send branches emit explicit reasons (`snapshot-null`, `suppressed:<reason>`, `off-hard-guard`, `source-stby`, `target-invalid`, `send-failed`, `auto-not-armed`, `lap-cross-no-material-delta`, `iracing-autofuel-ownership`, `external-mirror-change`, `owned-mirror-consumed`);
  - telemetry suppression diagnostics are transition/throttled only (no per-tick spam), with explicit suppression-clear transition logging.
- tyre mode transitions, one-shot command sends, and truth-mirror / AUTO-cancel reasons.

## Dependencies / ordering assumptions
- This subsystem owns transport + command dispatch and must remain the only authority for pit/custom command sends.
- Dashboards are consumers only; they should bind plugin actions and read exports, not reimplement command logic.
- Fuel planner/runtime fuel math remains outside this subsystem; pit command/fuel-control uses those outputs but does not redefine fuel model behavior.
- `Fuel.Refuel.*` and `Fuel.Delta.AfterStop.Selected` are owned by Fuel Model runtime logic; this subsystem provides DATA/SOURCE state inputs (`LIVE/PLAN`, `NORM/PUSH/SAVE/STBY`) as read-only selectors only. `SOURCE STBY` remains command-neutral and advisory NORM for selected-delta projection. Pit command transport/send behavior does not own or mutate these fuel-runtime semantics.

## Reset rules
- `Telemetry.IsOnTrackCar` edge transitions reset Pit Fuel Control ownership state and Tyre Control mode to safe defaults.
- Session/lifecycle reset seams clear short-lived command feedback and stale command state.
- External ownership events (for example iRacing AutoFuel) force Pit Fuel Control to inert/disarmed state.

## Failure modes / edge cases
- No usable iRacing process/window: command send attempt fails and feedback/log surfaces should make this visible.
- Direct transport partial-state uncertainty fails closed for that press; there is no legacy fallback retry.
- Chat-open leak prevention is explicit in direct transport: command transport force-sends `Esc` before `T` so stale-open chat does not absorb the opener key into outgoing raw/custom command payload (`t#...` / `tt#...` corruption).
- Transport success for custom/raw/stateless commands is attempt-only; in-sim effect is unverified by design.
- Tyre control has no resend loop: each target change/correction sends once at most, with a 1.0s settle hold and truth-following remap outside AUTO.
- Tyre command failure feedback is `PIT CMD SEND FAIL` when raw send returns false, and passive truth-mirror feedback is briefly suppressed after send failure so failure text is not immediately overwritten.
- External pit-menu edits can cancel AUTO once and force safety recovery state in fuel control.
- Fuel Control mode ownership is explicit-command only (no internal `Pit.ToggleFuel` use):
  - suppression gate is now reserved for truly invalid snapshot contexts (`no-plugin-manager`, `no-session`) and does not blanket-block active in-car/offline-testing pit-control button use;
  - `OFF -> MAN` explicitly sends MFD refuel ON (`#fuel$`) and then mirrors MAN truth (`Source=STBY`, AUTO disarmed);
  - OFF is a hard guard: `SourceCycle`/`SetPush`/`SetNorm`/`SetSave` send nothing and hold `OFF STBY`;
  - DATA actions (`SetDataLive`, `SetDataPlan`, `CycleData`, and legacy `SetPlan`) never send fuel commands; they publish `FUEL DATA <LIVE|PLAN>` and force `SOURCE=STBY`;
  - `MAN -> AUTO`: `PUSH`/`NORM`/`SAVE` send immediately and arm AUTO on success with `AUTO REFUEL SET <SRC> X L` feedback; `STBY` enters `AUTO STBY` with no send and `AUTO REFUEL STBY` feedback;
  - `AUTO -> OFF` always attempts explicit raw OFF command `#-fuel$`; on send failure AUTO remains unchanged, on success exits AUTO and mirrors OFF truth with `REFUEL OFF` feedback;
  - AUTO source sends (`SourceCycle`/`SetPush`/`SetNorm`/`SetSave`) use AUTO feedback wording (`AUTO REFUEL SET <SRC> X L`) and AUTO over-space wording (`AUTO FUEL <requested>L >MAX`), while MAN over-space wording is `REFUEL <SRC> <requested>L >MAX`;
  - Source cycle order is `STBY -> NORM -> PUSH -> SAVE -> STBY` in both MAN and AUTO; landing on `STBY` is a no-send parked state.
  - diagnostics-only instrumentation now logs every action-path early return reason before returning; command payload semantics remain unchanged.
  - External MFD/fuel-request changes are mirror-only:
    - while in AUTO, plugin sends nothing and publishes `AUTO REFUEL CANCELLED BY MFD`, then mirrors to `OFF STBY`/`MAN STBY` based on MFD truth;
    - while in MAN/OFF, plugin sends nothing and mirrors with `REFUEL SET OFF BY MFD`, `REFUEL SET ON BY MFD`, or `FUEL CHANGED BY MFD`;
    - MAN-owned plugin sends are tracked so delayed telemetry echoes are consumed as owned mirror updates (not reclassified as external MFD takeover).
    - Pending owned-mirror expectations are expired only on no-change ticks when observed telemetry already matches expected values, so same-tick owned ON/OFF transitions are consumed first and not misclassified as external MFD edits.

## Test checklist
- Bind and press representative built-in pit actions from SimHub Controls & Events.
- Confirm representative pit/custom/fuel/tyre commands dispatch via direct postmessage and fail visibly with `PIT CMD WINDOW FAIL` when no usable iRacing window exists.
- Confirm Fuel Control mode/source actions stay on explicit raw-command attempt semantics (single attempt, no retries/poll loops).
- Confirm Pit Fuel Control AUTO cancel triggers on external fuel request and MFD enable changes.
- Confirm Tyre Control mode cycle order and AUTO wet/dry switching follow declared-wet authority.
- Confirm `IsOnTrackCar` edges reset fuel/tyre control states to safe defaults.
