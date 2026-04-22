# Pit Assist

This page covers the driver-facing pit support in Lala Race Assist Plugin.

![Pit Entry Assist example](Images/PitEntryAssist.png)

For a full post-install SimHub walkthrough of the plugin tabs and setup flow, see: [YouTube walkthrough (~30 min)](https://youtu.be/Ug9BRo0WRbE).

## 1. What Pit Assist includes

The pit-facing driver aids include:

- **pit popups** and pit-context screens,
- **Pit Entry Assist** braking guidance,
- profile-backed pit-loss and marker behavior that affects what the driver sees.

These systems are there to reduce avoidable mistakes under pressure. They do not remove the need for driver judgment.

## 2. Pit popups and pit screens

Pit popups are the driver-facing pit-context prompts shown on supported dashboards. They are useful for things like:

- pit screen context,
- pit-assist visibility,
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
- `LalaLaunch.Pit.TyreControl.ModeCycle`
- `LalaLaunch.Pit.TyreControl.SetOff`
- `LalaLaunch.Pit.TyreControl.SetDry`
- `LalaLaunch.Pit.TyreControl.SetWet`
- `LalaLaunch.Pit.TyreControl.SetAuto`

These actions replace any old dashboard bindings that directly called `IRacingExtraProperties` pit-command actions.

Pit Fuel Control behavior notes for these bindings:
- `LalaLaunch.Pit.FuelSetMax` is now a true transport toggle: press sequence alternates **MAX**, **ZERO**, **MAX**, **ZERO** ...
- Full-tank short-circuit only applies to the MAX phase; ZERO phase still sends (so a full tank does not block `#fuel 0.01`).
- `LalaLaunch.Pit.FuelControl.ModeCycle` now only owns AUTO state:
  - outside AUTO, displayed mode mirrors real iRacing MFD fuel-enable truth (`OFF` when fuel unchecked, `MAN` when fuel checked),
  - entering AUTO while source is `PLAN` or already `STBY` still forces `STBY` and stays disarmed (`AutoArmed=false`) until a live source is selected and sent,
  - exiting AUTO drops back to MFD-derived `OFF`/`MAN` and forces `Source=STBY`.
- AUTO cancel/ownership rules:
  - AUTO cancels once (`AUTO CANCELLED`) when either live requested pit fuel (`PitSvFuel`) or MFD fuel-enable (`dpFuelFill`) changes outside plugin-owned send/toggle suppression,
  - AUTO cancels to `STBY` (with `AutoArmed=false`) when iRacing AutoFuel is active,
  - Offline Testing suppresses Pit Fuel Control to inert `STBY` with mode still derived from MFD truth,
  - any `Telemetry.IsOnTrackCar` edge (`false -> true` or `true -> false`) resets to `Source=STBY` + `AutoArmed=false` without forcing plugin-owned `OFF`/`MAN`.
- In `AUTO + STBY`, explicitly selecting `PUSH`, `NORM`, or `SAVE` immediately re-arms AUTO and sends the normal plugin-owned fuel update.

Tyre Control behavior notes for these bindings:
- Mode cycle order is fixed: `OFF -> DRY -> WET -> AUTO -> OFF`.
- `OFF` actively keeps tyre service OFF.
- `DRY` actively keeps tyre service ON and requests dry next tyres.
- `WET` actively keeps tyre service ON and requests wet next tyres.
- `AUTO` actively keeps tyre service ON and follows declared-wet authority (`Telemetry.WeatherDeclaredWet`) to keep requested next tyres DRY/WET.
- Tyre-service enforcement truth is authoritative from the four individual tyre-change flags: service ON only when all four tyre-change flags are selected; any partial/manual subset is treated as service OFF for control truth/enforcement.
- Tyre Control no longer uses toggle semantics internally (`Pit.ToggleTyresAll` remains available only as a direct user action). Engine command model is explicit raw commands only:
  - `OFF` enforcement sends `#cleartires$`
  - `DRY`/`WET`/`AUTO` enforcement sends `#t$` before compound targeting, then `#tc ...$` (`dry=0`, `wet=2`)
- Outside AUTO (`OFF`/`DRY`/`WET`), tyre mode is a bounded 2-way truth-sync contract with actual MFD truth:
  - manual mode selections are treated as requests,
  - if MFD truth confirms within the short confirmation window, mode stays selected,
  - if not confirmed, mode falls back to actual MFD truth (`service OFF -> OFF`, `service ON + dry-family requested compound -> DRY`, `service ON + wet-family requested compound -> WET`),
  - manual truth reconciliation runs before manual enforcement, so when external MFD truth has drifted the plugin follows truth first instead of re-applying stale `OFF`/`DRY`/`WET` intent,
  - `ResetToOff()` safety resets keep mode latched at `OFF` (no immediate `OFF -> DRY/WET` remap on the next telemetry tick).
  - ambiguous/unavailable truth is held fail-safe (no twitchy flip-flopping).
- In AUTO, tyre control remains plugin-owned authoritative mode only while MFD ownership is still plugin-owned:
  - bounded unconfirmed enforcement still publishes info-only `TYRE AUTO UNCONFIRMED` feedback/logging,
  - a bounded plugin-owned suppression window follows plugin tyre sends so immediate resulting MFD changes are treated as plugin-owned,
  - MFD tyre truth changes observed outside that suppression window are treated as external/manual ownership takeover: plugin publishes `TYRE AUTO CANCELLED`, exits AUTO, and remaps to current manual truth (`OFF`/`DRY`/`WET`).
- Tyre control mode resets to `OFF` on `Telemetry.IsOnTrackCar` edge transitions (`false->true` or `true->false`) via the existing pit-control reset seam.
- Compound command retries are bounded by cooldown + attempt limits even when a local send attempt fails, preventing per-tick resend hammering.

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

Transport mode is configurable in **Settings → Pit Commands**:
- `Auto (Direct message then fallback)` (default) tries direct window-message send first, then falls back to legacy foreground `SendInput`.
- `Legacy foreground SendInput only` uses only the focus-required `SendInput` path.
- `Direct message only` uses only the window-message path and does not fallback.

If both available transport options fail for the selected mode (for example no iRacing window is available for direct send, or legacy fallback is blocked because iRacing is not foreground), LalaLaunch publishes `Pit Cmd Fail` and logs a pit-command warning so the failure is visible.
For custom messages, raw commands, and stateless built-in pit actions, a successful direct send is transport-attempt only (queued/unverified), not authoritative proof that iRacing applied the command.
Stateful toggle actions (`ToggleFuel`, tyre/fix toggles, etc.) remain the only pit actions with authoritative before/after telemetry confirmation.

## 3. Pit Entry Assist

Pit Entry Assist helps you arrive at the pit entry line at the right speed. It uses saved marker context plus braking guidance to tell you whether you are:

- comfortably early,
- getting close,
- braking now,
- already late.

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
