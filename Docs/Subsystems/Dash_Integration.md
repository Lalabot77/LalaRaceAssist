# Dash Integration

Validated against commit: HEAD
Last updated: 2026-04-21
Branch: work

## Purpose
Define how Lala Race Assist Plugin exports should be consumed by dashboards.

This document is the canonical dash-facing contract layer. It does **not** redefine subsystem behavior; it explains:
- which plugin outputs are meant for dash consumption,
- how dashboards should gate and render them,
- where the plugin ends and dashboard presentation begins.

## Core principles
- **The plugin owns logic; dashboards own presentation.**
- **Prefer stable exports** when both raw/live and stable variants exist.
- **Use explicit visibility gates** instead of guessing from text/value presence.
- **Do not renormalize plugin units** unless the dash needs purely visual scaling.
- **Clear state aggressively on session transitions** so stale visuals do not linger.

## High-level dash ownership map
### Strategy / fuel / pace
- Use stable strategy/fuel exports for labels and decision widgets.
- Treat `LalaLaunch.PreRace.*` as a separate on-grid/pre-race info layer, not a replacement for live `Fuel.*` or Strategy planner ownership.
- Consume `LalaLaunch.PreRace.StatusText` together with `LalaLaunch.PreRace.StatusColour` (`green`/`orange`/`red`) for simple dash styling; do not recreate planner/live mismatch or multi-stop decision logic in dash scripts.
- `LalaLaunch.PreRace.FuelDelta` is a live on-grid seam and should be expected to move immediately with fuel changes:
  - required one-stop path uses `(current fuel + pit fuel request) - total fuel needed`,
  - required no-stop/multi-stop paths use `current fuel - total fuel needed`.
- `LalaLaunch.PreRace.FuelSource` / `LapTimeSource` contract:
  - Auto uses runtime ownership labels (`live`/`profile`/`fallback`) and does not publish `planner`,
  - manual PreRace selections (`No Stop`/`Single Stop`/`Multi Stop`) keep planner-owned labeling where applicable.
- PreRace status is scenario-first (`required strategy` vs `selected strategy`) and fully partitioned:
  - required `No Stop` => `NO STOP OKAY` or `ADD FUEL FOR NO STOP`; selecting stop strategies shows `NO STOP POSSIBLE`,
  - required `One Stop` => no-stop `SINGLE STINT NOT POSSIBLE`, one-stop feasibility (pit-stop refill-capacity gate, then `ONE STOP REQUIRES MORE FUEL` / `OVERFUELLED` / `SINGLE STOP OKAY`), multi-stop `SINGLE STOP POSSIBLE`,
  - required `Multi Stop` => no-stop `NO STOP NOT POSSIBLE`, single-stop `SINGLE STOP NOT POSSIBLE`, multi-stop `MAX FUEL IN / MULTI STOP CONFIRMED` or `MAX FUEL REQUIRED`.
- If a widget is meant to represent runtime truth, prefer stable `Fuel.*` / pace outputs over UI-only text from elsewhere.

### Launch
- Gate live launch widgets on `LaunchModeActive` and related launch-visible state.
- Keep **Launch Analysis** concepts separate from live launch widgets; dashboards can show results, but trace review belongs to the plugin review surface.

### Rejoin / pit / messages
- Rejoin widgets should respect the explicit rejoin exports rather than infer active state from message text alone.
- Pit-screen / pit-entry widgets should combine pit visibility toggles with pit-specific active flags.
- Pit command buttons in strategy/pit widgets must bind to plugin-owned actions (`LalaLaunch.Pit.ClearAll`, `LalaLaunch.Pit.ClearTires`, `LalaLaunch.Pit.ToggleFuel`, `LalaLaunch.Pit.FuelSetZero`, `LalaLaunch.Pit.FuelAdd1`, `LalaLaunch.Pit.FuelRemove1`, `LalaLaunch.Pit.FuelAdd10`, `LalaLaunch.Pit.FuelRemove10`, `LalaLaunch.Pit.FuelSetMax`, `LalaLaunch.Pit.ToggleTiresAll`, `LalaLaunch.Pit.ToggleFastRepair`, `LalaLaunch.Pit.ToggleAutoFuel`, `LalaLaunch.Pit.Windshield`, `LalaLaunch.Pit.FuelControl.SourceCycle`, `LalaLaunch.Pit.FuelControl.ModeCycle`, `LalaLaunch.Pit.FuelControl.SetPush`, `LalaLaunch.Pit.FuelControl.SetNorm`, `LalaLaunch.Pit.FuelControl.SetSave`) rather than `IRacingExtraProperties` action ids.
- Built-in pit command rows are user-configured in plugin **Settings → Pit Commands**; dashboards should bind those action ids directly and not carry raw transport syntax.
- Custom chat buttons should bind to plugin-owned actions `LalaLaunch.CustomMessage01..LalaLaunch.CustomMessage10` (configured in Settings → Custom Messages) rather than embedding transport/raw chat syntax in dash logic.
- Message-dash widgets should consume the message engine outputs directly rather than rebuilding message priority logic in SimHub expressions.

### Pit command transport contract
- Dashboards trigger pit actions only; transport ownership is in-plugin.
- Transport is plugin-configurable via **Settings → Pit Commands → Pit command transport**:
  - `Auto (Direct message then fallback)` (default): tries direct iRacing window-message send first (`WM_KEYDOWN/UP T` → `WM_CHAR` text → `WM_KEYDOWN/UP Enter`), then falls back to legacy foreground `SendInput` if direct transport is unavailable.
  - `Legacy foreground SendInput only`: preserves previous foreground-only `SendInput` behavior.
  - `Direct message only`: direct window-message transport without legacy fallback.
- Auto fallback safety guard: if a direct attempt already mutated chat state but then aborts (partial open/type sequence risk), legacy fallback is intentionally suppressed for that press to avoid second-path corruption/duplicate-open behavior.
- Legacy fallback still requires iRacing foreground; direct-window path may still fail when no usable iRacing main window is available.
- Transport truth model: successful direct-message queueing is only `attempted=true` / `delivery=unverified` for custom messages, raw commands, and stateless built-ins (no post-send effect confirmation seam).
- Stateful built-ins keep authoritative before/after telemetry confirmation ownership (`effect-confirmed=true|false`).
- Dashboards can bind short-lived user feedback exports `LalaLaunch.Pit.Command.DisplayText` and `LalaLaunch.Pit.Command.Active` for command confirmations/failures.
- Dashboards can also bind `LalaLaunch.Pit.Command.LastAction`/`LastRaw` for diagnostics, plus `LalaLaunch.Pit.Command.FuelSetMaxToggleState` for real `Pit.FuelSetMax` MAX/ZERO toggle state (`false=last press sent ZERO / next press sends MAX`, `true=last press sent MAX / next press sends ZERO`). Tank-full short-circuit applies to MAX phase only; ZERO phase is still transported as `#fuel 0.01`.
- Dashboards can bind `LalaLaunch.Pit.FuelControl.*` exports (`Source/SourceText`, `Mode/ModeText`, `TargetLitres`, `OverrideActive`) for pit fuel control state display; dashboards do not own source/mode/plan validity logic.
- Pit Fuel Control ownership/reset contract for dash rendering: OFF/reset/suppressed states are `OFF + STBY`; AUTO cancellation on external requested-fuel movement (outside send suppression), iRacing AutoFuel ownership, Offline Testing suppression, and any `Telemetry.IsOnTrackCar` boolean edge (`false->true` or `true->false`) all end in `OFF + STBY`.
- The same transport seam also dispatches custom-message actions; dashboards should keep custom-message content authored in plugin Settings rather than hardcoding message text in dash scripts/buttons.

### H2H / traffic
- `H2HRace.*` and `H2HTrack.*` are already flattened for dashboard binding; dashboards should not try to recreate selector logic.
- CarSA / Opponents data that is not part of a published dash contract should stay a technical dependency, not a dashboard-owned truth source.

## Visibility and gating
### Dash visibility toggles
Use the exported visibility families as hard gates:
- `LalaDashShow*` = `DRIVER` column in Dash Control = Lala Race Dash visibility
- `MsgDashShow*` = `STRATEGY` column in Dash Control = Lala Strategy Dash visibility
- `OverlayDashShow*` = `OVERLAY` column in Dash Control = overlay visibility

These toggles are the contract between plugin settings and dash layout visibility. The short UI labels are only there to fit the matrix width; dashboards should still treat the exported families above as the canonical technical meaning. Dash JSON should not fight them.

### Session-reset expectations
Hide or clear visuals when:
- session identity changes,
- a subsystem’s explicit active flag goes false,
- the relevant dash visibility toggle goes false,
- a widget’s source data is clearly invalid / unavailable.

If runtime data appears stalled after a session boundary, the plugin now exposes a manual recovery re-arm path via the Overview reset button and the `PrimaryDashMode` action binding; both invoke the same transient runtime reset orchestration in-plugin.

## Pit Entry Assist binding
### Primary properties
- `Pit.EntryAssistActive`
- `Pit.EntryDistanceToLine_m`
- `Pit.EntryRequiredDistance_m`
- `Pit.EntryMargin_m`
- `Pit.EntryCue`
- `Pit.EntryCueText`
- `Pit.EntrySpeedDelta_kph`
- `Pit.EntryDecelProfile_mps2`
- `Pit.EntryBuffer_m`

### Consumption guidance
- Use `Pit.EntryAssistActive` as the hard visibility gate.
- Use `Pit.EntryMargin_m` as the primary continuous signal.
- Treat cue text/value as secondary state, not the main visualization input.
- A fixed-scale marker is preferred over re-scaling around the current cue.

### Recommended visualization
- Fixed ±150 m vertical or horizontal marker range.
- Center line = ideal braking point.
- Up/positive = early; down/negative = late.
- Keep cue colors simple so small margin changes remain visible.

## Pit screen mode
### Properties
- `PitScreenActive`
- `PitScreenMode`

### Contract
- `auto` = plugin-driven pit-screen activation.
- `manual` = user-forced pit screen while on track.

Dashboards can style these differently, but they should not reinterpret the lifecycle. Manual pit-screen state is reset by the plugin on major session/combo resets.

## Message engine consumption
For dashboards that use the v1 message engine:
- bind directly to `MSGV1.*` exports for active text, colors, counts, and stack state,
- keep `MsgCx` / clear behavior in the plugin action path,
- avoid recreating priority resolution in dash expressions.

The subsystem docs own message-system behavior; dashboards should remain consumers only.

## Dark Mode integration
### Stable exports for dashboards
- `LalaLaunch.Dash.DarkMode.Mode`
- `LalaLaunch.Dash.DarkMode.Active`
- `LalaLaunch.Dash.DarkMode.BrightnessPct`
- `LalaLaunch.Dash.DarkMode.LovelyAvailable`
- `LalaLaunch.Dash.DarkMode.OpacityPct`
- `LalaLaunch.Dash.DarkMode.ModeText`

### Consumption rules
- Bind dashboards to the `LalaLaunch.Dash.DarkMode.*` exports, not Lovely internals.
- `BrightnessPct` remains `0..100`.
- `Mode` contract is:
  - `0` = Off
  - `1` = Manual
  - `2` = Auto
- When Lovely override is enabled and available, dashboards should still trust the plugin export rather than probing Lovely directly.

## Expression hygiene
- Force floating-point math in SimHub expressions where scaling matters.
- Prefer simple thresholding over deeply nested expression trees.
- Avoid dash-side smoothing that masks the plugin’s own stable-vs-live contract.

## Logging alignment
Dash developers/support work can cross-check live visuals against plugin logs for:
- launch state changes,
- pit-entry assist activation / line / end events,
- message engine behavior,
- projection source changes,
- rejoin state transitions.

## Non-goals
Dashboards should **not**:
- recreate strategy math,
- own H2H selector logic,
- own launch or rejoin logic,
- bypass plugin visibility toggles,
- depend on undocumented internal state when a canonical export already exists.

## v1 documentation note
The v1 GitHub docs now present dashboards as the presentation layer across all systems. This page is the canonical technical companion to the user-facing `Docs/Dashboards.md` page.
