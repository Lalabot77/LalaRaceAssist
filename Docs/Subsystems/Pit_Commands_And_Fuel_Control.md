# Pit Commands and Fuel/Tyre Control

Validated against commit: HEAD
Last updated: 2026-04-23
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
- Stateful-toggle before/after verification status (effect-confirmed vs attempted-only).

### Pit Fuel Control state
- Source state (`STBY`/runtime modes) and AUTO arming semantics.
- Mode state where AUTO is plugin-owned; non-AUTO mode mirrors MFD truth (`OFF`/`MAN` from `dpFuelFill`).
- Target litres and override-active semantics for command generation.

### Tyre Control state
- Mode state machine: `OFF -> DRY -> WET -> AUTO -> OFF`.
- Retry/cooldown bookkeeping for compound sends.
- Service-state enforcement seam aligned to all-four tyre selection truth.
- Manual mode-change confirmation window (`OFF`/`DRY`/`WET`) delays immediate external-truth remap long enough for first bounded enforcement send/confirmation pass.

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
5. Maintain Pit Fuel Control ownership rules:
   - AUTO can cancel on external requested-fuel or MFD-enable edges,
   - lifecycle resets (`IsOnTrackCar` edges, iRacing AutoFuel ownership, offline suppression) force inert/disarmed safety state.
6. Maintain Tyre Control enforcement:
   - OFF forces service off,
   - DRY/WET/AUTO keep service on and drive compound request semantics,
   - AUTO follows declared wetness,
   - service enforcement attempt budget is consumed only when four-flag tyre-service truth is authoritative; unknown service truth (partial/missing tyre flags) holds enforcement/retries.

## Outputs (exports + logs)
Canonical export names live in `Docs/Internal/SimHubParameterInventory.md`; key families:
- `Pit.Command.*` (display text, active, last action/raw, max-toggle state),
- `Pit.FuelControl.*` (source, mode, target, override),
- `Pit.TyreControl.*` (mode text/state).

Canonical log wording and meaning live in `Docs/Internal/SimHubLogMessages.md`; key themes:
- transport mode/attempt path (`postmessage` vs `sendinput`),
- fallback reason context and suppression cases,
- effect-confirmed vs unverified delivery semantics,
- tyre compound attempt/retry diagnostics.

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
- Transport success for custom/raw/stateless commands is attempt-only; in-sim effect is unverified by design.
- Tyre compound send failure paths are bounded by retry/cooldown budget to avoid per-tick resend spam.
- External pit-menu edits can cancel AUTO once and force safety recovery state in fuel control.
- AUTO exit (`AUTO -> OFF`) is guarded by live MFD truth (`dpFuelFill`): if fuel fill is already OFF, the engine must not send a fuel toggle and should only publish OFF state recovery (`Source=STBY`, AUTO cleared).
- Fuel `OFF -> MAN` toggle flow must stay non-blocking in `PitFuelControlEngine.TryToggleFuelFillEnabled(...)`: no wait/re-poll loops, but a single immediate post-send snapshot read must still verify `dpFuelFill` matches expected ON/OFF before reporting success.

## Test checklist
- Bind and press representative built-in pit actions from SimHub Controls & Events.
- Validate transport mode behavior in each mode (`Auto`, `Legacy`, `Direct only`) with logs.
- Confirm stateful toggle actions show effect-confirmed semantics and custom/raw actions stay unverified semantics.
- Confirm Pit Fuel Control AUTO cancel triggers on external fuel request and MFD enable changes.
- Confirm Tyre Control mode cycle order and AUTO wet/dry switching follow declared-wet authority.
- Confirm `IsOnTrackCar` edges reset fuel/tyre control states to safe defaults.
