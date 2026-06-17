# Pit Assist

This page covers the driver-facing pit support in Lala Race Assist Plugin.

![Pit Entry Assist example](Images/PitEntryAssist.png)

For a full post-install SimHub walkthrough of the plugin tabs and setup flow, see: [YouTube walkthrough (~30 min)](https://youtu.be/Ug9BRo0WRbE).

## 1. What Pit Assist includes

The pit-facing driver aids include:

- **pit popups** and pit-context screens,
- **Pit Entry Assist** braking guidance,
- **Pit Box Assist** stopping/box-entry guidance,
- **Pit Limiter** assist visibility,
- profile-backed pit-loss and marker behavior that affects what the driver sees.

These systems are there to reduce avoidable mistakes under pressure. They do not remove the need for driver judgment.

## 2. Pit popups and pit screens

Pit popups are the driver-facing pit-context prompts shown on supported dashboards. Settings → Dash Control → Dash Visibility now exposes pit presentation as separate per-dash-family controls for **Pit Entry Assist**, **Pit Box Assist**, **Pit Limiter**, and **Show Automatic Pit Screen**. They are useful for things like:

- pit screen context,
- focused pit-entry, pit-box, or pit-limiter visibility,
- automatic pit-related prompts.

### What to trust

Trust pit popups most when your pit data is good. That usually means:

- pit-loss has been learned cleanly,
- pit markers are sensible,
- the track record has been validated and locked where appropriate.

### When to cancel or override

- Use **Toggle Pit Screen** if you want to force the pit screen on or off.
- Use **Cancel Message** if a popup needs to be dismissed right now.

### Pit command button mapping (Strategy Dash + PitPopUp)

Pit command buttons should now be bound to plugin-owned Controls & Events actions:

Technical contract note: this page is driver-facing usage guidance. The canonical subsystem ownership/exports/log contract for pit/custom commands + fuel/tyre control lives in [Docs/Subsystems/Pit_Commands_And_Fuel_Control.md](Subsystems/Pit_Commands_And_Fuel_Control.md).

- `LalaLaunch.Pit.ClearAll`
- `LalaLaunch.Pit.ClearTires`
- `LalaLaunch.Pit.ToggleFuel`
- `LalaLaunch.Pit.FuelSetZero`
- `LalaLaunch.Pit.FuelAdd1`
- `LalaLaunch.Pit.FuelRemove1`
- `LalaLaunch.Pit.FuelAdd10`
- `LalaLaunch.Pit.FuelRemove10`
- `LalaLaunch.Pit.FuelSetMax`
- `LalaLaunch.Pit.ToggleTiresAll`
- `LalaLaunch.Pit.ToggleFastRepair`
- `LalaLaunch.Pit.ToggleAutoFuel`
- `LalaLaunch.Pit.Windshield`
- `LalaLaunch.Pit.FuelControl.SourceCycle`
- `LalaLaunch.Pit.FuelControl.ModeCycle`
- `LalaLaunch.Pit.FuelControl.SetPush`
- `LalaLaunch.Pit.FuelControl.SetNorm`
- `LalaLaunch.Pit.FuelControl.SetSave`
- `LalaLaunch.Pit.FuelControl.SetDataLive`
- `LalaLaunch.Pit.FuelControl.SetDataPlan`
- `LalaLaunch.Pit.FuelControl.CycleData`
- `LalaLaunch.Pit.FuelControl.SetPlan`
- `LalaLaunch.Pit.TyreControl.ModeCycle`
- `LalaLaunch.Pit.TyreControl.SetOff`
- `LalaLaunch.Pit.TyreControl.SetDry`
- `LalaLaunch.Pit.TyreControl.SetWet`
- `LalaLaunch.Pit.TyreControl.SetAuto`

These actions replace any old dashboard bindings that directly called `IRacingExtraProperties` pit-command actions.

Pit Fuel Control behavior notes for these bindings:
- `LalaLaunch.Pit.FuelSetMax` is now a true transport toggle: press sequence alternates **MAX**, **ZERO**, **MAX**, **ZERO** ...
- Full-tank short-circuit only applies to the MAX phase; ZERO phase still sends (so a full tank does not block `#fuel 0.01`).
- `LalaLaunch.Pit.FuelControl.ModeCycle` now uses explicit raw fuel command ownership (no internal fuel-toggle semantics):
  - `OFF -> MAN` explicitly sends refuel ON (`#+fuel$`) and then mirrors `MAN STBY`,
  - `MAN -> AUTO` enters plugin AUTO ownership,
  - `AUTO -> OFF` attempts explicit `#-fuel$`; successful send exits AUTO, failed send leaves AUTO unchanged.
- AUTO entry send behavior:
  - entering AUTO from `PUSH`/`NORM`/`SAVE` sends immediately and arms AUTO only on successful send,
  - entering AUTO from `STBY` remains disarmed (`FUEL AUTO STBY`) until a live source is selected.
- OFF hard guard:
  - while effective mode is `OFF`, source actions (`SourceCycle`, `SetPush`, `SetNorm`, `SetSave`) do not send `#fuel` commands and remain `OFF STBY`.
- DATA/SOURCE contract:
  - `DATA` is `LIVE` or `PLAN`; it controls only the burn basis used by `PUSH`/`SAVE`,
  - `SOURCE` is now `STBY`, `NORM`, `PUSH`, or `SAVE`; `SOURCE=PLAN` has been removed,
  - changing DATA (`SetDataLive`, `SetDataPlan`, or `CycleData`) always forces `SOURCE=STBY` and sends no fuel command,
  - `NORM` always uses runtime/live burn,
  - `PUSH`/`SAVE` use live burn when DATA is `LIVE`, or planner/profile memory burn when DATA is `PLAN`.
- Source cycle contract:
  - `SourceCycle` order is `STBY -> NORM -> PUSH -> SAVE -> STBY` in MAN and AUTO,
  - landing on `STBY` is a parked no-send state.
- `SetPlan` remains for one release so old dashboards do not break; it now sets `DATA=PLAN`, forces `SOURCE=STBY`, publishes `FUEL DATA PLAN`, and does not send a planner fuel amount.
- AUTO cancel/ownership rules:
  - AUTO external MFD/off/on/fuel-change handling is mirror-only (no plugin send) and publishes `AUTO REFUEL CANCELLED BY MFD`,
  - AUTO cancels to `STBY` (with `AutoArmed=false`) when iRacing AutoFuel is active,
  - outside AUTO, external mirror events publish `REFUEL SET OFF BY MFD`, `REFUEL SET ON BY MFD`, or `FUEL CHANGED BY MFD` and mirror to OFF/MAN STBY,
  - MAN-owned Fuel Control sends are tracked and consumed as owned mirror echoes so delayed telemetry does not trigger false external-takeover resets,
  - Offline Testing suppresses Pit Fuel Control to inert `STBY` with mode still derived from MFD truth,
  - any `Telemetry.IsOnTrackCar` edge (`false -> true` or `true -> false`) resets to `Source=STBY` + `AutoArmed=false` without forcing plugin-owned `OFF`/`MAN`.
- In `AUTO`, explicitly selecting `PUSH`, `NORM`, or `SAVE` immediately sends and keeps AUTO armed on successful send.

Tyre Control behavior notes for these bindings:
- Mode cycle order is fixed: `OFF -> DRY -> WET -> AUTO -> OFF`.
- `OFF` actively keeps tyre service OFF.
- `DRY` actively keeps tyre service ON and requests dry next tyres.
- `WET` actively keeps tyre service ON and requests wet next tyres.
- `AUTO` actively keeps tyre service ON and follows declared-wet authority (`Telemetry.WeatherDeclaredWet`) to keep requested next tyres DRY/WET.
- Tyre-service enforcement truth is authoritative from the four individual tyre-change flags: service ON/OFF is authoritative only when all four tyre-change flags are available; partial/missing tyre flags are treated as unknown, so service enforcement is held (no additional resend attempts while truth is unavailable).
- Tyre Control no longer uses toggle semantics internally (`Pit.ToggleTyresAll` remains available only as a direct user action). Engine command model is explicit raw commands only:
  - `OFF` enforcement sends `#cleartires$`
  - `DRY`/`WET`/`AUTO` enforcement sends only `#tc ...$` (`dry=0`, `wet=2`) with no `#t$` pre-send
- Outside AUTO (`OFF`/`DRY`/`WET`), tyre mode is a bounded 2-way truth-sync contract with actual MFD truth:
  - manual mode selections are treated as requests,
  - if MFD truth confirms within the short confirmation window, mode stays selected,
  - if not confirmed, mode falls back to actual MFD truth (`service OFF -> OFF`, `service ON + dry-family requested compound -> DRY`, `service ON + wet-family requested compound -> WET`),
  - manual truth reconciliation runs before manual enforcement, so when external MFD truth has drifted the plugin follows truth first instead of re-applying stale `OFF`/`DRY`/`WET` intent,
  - `ResetToOff()` safety resets keep mode latched at `OFF` (no immediate `OFF -> DRY/WET` remap on the next telemetry tick).
  - ambiguous/unavailable truth is held fail-safe (no twitchy flip-flopping).
- In AUTO, tyre control remains plugin-owned authoritative mode only while MFD ownership is still plugin-owned:
  - a bounded plugin-owned suppression window follows plugin tyre sends so immediate resulting MFD changes are treated as plugin-owned,
  - DRY/WET/AUTO `#tc ...$` sends record both pending compound intent and pending service-ON intent (no separate `#t$` path),
  - delayed truth convergence is treated as plugin-owned only when observed truth matches the full relevant pending plugin service/compound intent (if both are pending, both must match),
  - MFD tyre truth changes outside plugin-owned protection cancel AUTO only when a concrete manual truth remap exists (`OFF`/`DRY`/`WET`); ambiguous/unavailable truth does not cancel AUTO and does not force `OFF`.
- Tyre control mode resets to `OFF` on `Telemetry.IsOnTrackCar` edge transitions (`false->true` or `true->false`) via the existing pit-control reset seam.
- Tyre command confirmation is single-send/single-window: each manual action or AUTO target change attempts one send, waits a short confirmation window, and on unconfirmed result publishes specific pit command failure feedback then remaps mode to actual MFD truth (no retry resend loop).

In Settings → **Pit Commands**, tyre control shows a single built-in binding row (`Tyre Mode Cycle`) for normal use (no raw chat command editing). Direct tyre mode actions (`SetOff` / `SetDry` / `SetWet` / `SetAuto`) remain registered for SimHub Controls & Events / Dash Studio binding.

### Custom message buttons (Settings → Custom Messages)

Settings now also includes a **Custom Messages** expander with 10 custom slots.

Each slot has:
- a friendly label,
- message text content,
- its own bindable plugin action (`LalaLaunch.CustomMessage01` ... `LalaLaunch.CustomMessage10`).

Custom message slot values persist across SimHub restarts. Slot collection changes save immediately; slot text edits use a short debounce and are flushed on normal runtime/shutdown so settled edits are retained.

Use these for common race-chat messages you want on hardware buttons, keyboard keys, or dashboard virtual buttons, without exposing chat transport details in normal workflow.

### Runtime caveat

LalaLaunch injects iRacing pit/custom chat messages directly (no dedicated user macro-hotkey setup required).

Transport is plugin-owned and fixed to direct iRacing window-message delivery. **Settings → Pit Commands** no longer exposes Auto/Legacy/Direct transport choices, and the legacy foreground `SendInput` path is no longer part of the normal workflow.

If direct transport cannot find/use the iRacing window, LalaLaunch publishes `PIT CMD WINDOW FAIL` and logs a pit-command warning so the failure is visible.
For custom messages, raw commands, and stateless built-in pit actions, a successful direct send is transport-attempt only (queued/unverified), not authoritative proof that iRacing applied the command.
Stateful toggle actions (`ToggleFuel`, tyre/fix toggles, etc.) remain the only pit actions with authoritative before/after telemetry confirmation.


## Pit service regulations

Pit-box countdown and total stop-loss prediction follow the Strategy-selected pit service regulation:

- **Default:** fuel then tyres, sequential service.
- **IMSA:** fuel and tyres together.
- **NEC:** fuel and tyres together, using the car-profile NEC refuel-rate factor when calculating fuel time.

The selector is manual and is persisted by Race Presets. Car Profiles hold the physical timing values (normal refuel rate, tyre change time, base tank, and NEC factor). The plugin does not auto-detect service rules from series, track, car, session name, or preset name.

## 3. Pit Entry Assist

Pit Entry Assist helps you arrive at the pit entry line at the right speed. It uses saved marker context plus braking guidance to tell you whether you are:

- comfortably early,
- getting close,
- braking now,
- already late,
- or already below the pit speed limit.

Dashboards can show the plugin-owned brake label directly (`Pit.EntryBrakeCueText`) when they want simple wording such as `READY`, `BRAKE IN 120m`, `BRAKE NOW`, `BRAKE HARD`, `SLOW DOWN`, `SPEED OKAY`, `BELOW LIMIT`, or `TOO SLOW`; `Pit.EntryBrakeCueState` provides the matching numeric colour/animation state. The older `Pit.EntryCueText` remains the stable legacy state token for widgets that build their own message.

Pit Entry Assist debriefs use a small 1.0 kph line-speed margin for review wording: an entry that is only slightly over the limit (for example `+0.3kph`) is reported as normal/within margin instead of a bad entry. Larger overspeed remains bad.

### What to trust

Trust Pit Entry Assist most when:

- the track markers are correct,
- the pit-entry settings fit the car,
- you have validated the system with at least one clean pit entry.

### When to override with judgment

Override it with your own judgment when:

- track conditions are unusual,
- traffic forces a compromised line,
- the saved settings are not yet validated for that car/track.

## 4. If pit behavior feels repeatedly wrong

If pit popups or Pit Entry Assist keep feeling wrong, review:

- saved pit markers,
- saved pit-loss data,
- pit entry deceleration settings,
- pit entry buffer settings,
- whether the current car/track data was learned cleanly.

## 5. Practical trust model

A good rule is:

- **Trust the system once the underlying data is good.**
- **Override it when the moment demands it.**
- **Relearn or review the saved data if it is repeatedly wrong.**

## Completed stop debrief

During and after a pit stop, the plugin can publish a compact pit debrief summary for dashboards, for example `ENTRY GOOD (Δ +0.0s) | BOX GOOD (Δ +0.7s) | SVC 44.0L & 4Ts | STRAT Δ -2.9s`. The summary becomes progressively useful as entry, box/service, and final strategy-delta evidence arrives, then freezes after finalization. For safe/normal pit-entry compliance, the entry headline remains a performance readout from entry time loss. A genuine current-stop bad line-speed compliance verdict now overrides the compact headline to `ENTRY BAD` with compliance detail such as `+7.9kph / 3.3m late`, so a zero/no-positive-estimate time-loss export no longer makes a bad entry appear as `ENTRY GOOD`; stale previous-stop bad tokens without current-stop speed/late evidence are ignored. Marginal line overspeed within 1.0 kph is still reported as normal/within margin and keeps `LimiterQualityText=NORMAL`; overspeed above that remains poor/bad. Box delta uses the existing `Pit.Box.LastDeltaSec` completed-box seam inverted to debrief sign convention (`actual elapsed - predicted target`; positive = slower, negative = faster) and is displayed for finite completed evidence; `Δ PENDING` means the completed source is missing, invalid, or not completed yet. PitExit prediction values remain in debug exports and the final log line, but driver-facing `SummaryText` no longer includes `EXIT ...` verdict text. This readout does not change pit assist, fuel, or opponent calculations.


### 2026-06-08 Pit Debrief diagnostic note

Pit Debrief box/fuel source-trace diagnostics are SimHub log-only and do not require dashboard JSON/layout updates. Existing `Pit.Debrief.*` exports keep their names; `Pit.Box.LastDeltaSec` keeps its dashboard sign contract (`target - actual`, positive quicker/better). Box targets and Pit Debrief service tyre counts use the frozen/current-stop latched tyre count, so confirmed fuel-only stops do not carry stale/default 4-tyre timing and real tyre stops are not overwritten by in-service flag clear-down; the 4-tyre fallback is retained when tyre evidence is unavailable before service. `Pit.Debrief.Service.FuelTargetLitres` remains debug/readout-only, preserves positive requested-add evidence through normal refuel completion/reset or deselect when positive fuel movement exists and added fuel is within the completion-tolerance window (including a same-tick current fuel sample before the gauge refresh), and still clears explicit in-box refuel-cancel before natural completion or true no-refuel/pre-flow cancel cases.
