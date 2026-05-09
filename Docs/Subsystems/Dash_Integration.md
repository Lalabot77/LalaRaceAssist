# Dash Integration

Validated against commit: HEAD
Last updated: 2026-04-23
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
  - PreRace current-fuel basis uses live current fuel when valid/positive; setup fallback (`Fuel.Setup.FuelLevel`) is used only during pre-race/grid/formation phases, and active race-running stays live-fuel authoritative even when live fuel is `0`,
  - required one-stop path uses `(effective current fuel + pit fuel request) - total fuel needed`,
  - required no-stop/multi-stop paths use `effective current fuel - total fuel needed`.
- `LalaLaunch.PreRace.FuelSource` / `LapTimeSource` / `LiveFacingBasisText` contract:
  - `LiveFacingBasisText` format is `DATA <LIVE|PLAN> | LAP <LIVE|PLAN|PROFILE|SIM|DEFAULT> / BURN <LIVE|PLAN|PROFILE|DEFAULT>`,
  - only approved tokens are emitted by the DATA-governed resolver: `LIVE`, `PLAN`, `PROFILE`, `SIM`, `DEFAULT`,
  - DATA `LIVE` labels now reflect the true selected provenance (not just positive runtime values): burn/lap report `LIVE` only when selected stable source is truly live-accepted runtime data,
  - DATA `PLAN` never emits `LIVE` or `SIM`; it remains `PLAN -> PROFILE -> DEFAULT` for both burn and lap.
  - manual PreRace selections may still emit `live` when planner fuel value is active from live snapshot/runtime source labeling.
- PreRace status is scenario-first (`required strategy` vs `selected strategy`) and fully partitioned:
  - required `No Stop` => `SINGLE STINT OKAY` or `ADD START FUEL FOR SINGLE STINT`; selecting stop strategies shows `SINGLE STINT POSSIBLE`,
  - required `One Stop` => no-stop `SINGLE STINT NOT POSSIBLE`, one-stop feasibility (pit-stop refill-capacity gate, then orange advisory `2 STINT PLAN REQUIRES MORE FUEL` when under-fuelled-but-refuel-settable, `OVERFUELLED`, `CHECK NEXT STINT FUEL`, or `SINGLE STOP OKAY`), multi-stop `SINGLE STOP POSSIBLE`,
  - required `Multi Stop` => non-multi selections publish `MULTI STINTS REQUIRED`; multi-stop publishes `MAX FUEL SET FOR MULTI STOP` or max-fuel-required guidance.
- If a widget is meant to represent runtime truth, prefer stable `Fuel.*` / pace outputs over UI-only text from elsewhere.
- Race-end dash gating should consume plugin-owned finish-phase exports directly: `Race.EndPhase` / `Race.EndPhaseText` / `Race.EndPhaseConfidence` plus `Race.LastLapLikely`; dashboards must not infer leader finish from player white/checkered flags or track-disappearance heuristics.
- Finish leader-finished booleans for dashboards are `Race.OverallLeaderHasFinished` and `Race.ClassLeaderHasFinished`; legacy/duplicate derived `Race.LeaderHasFinished` is no longer part of the core dash export surface.
- `SessionState 4 -> 5` should be interpreted as overall race lifecycle/overall leader finish phase. Dashboards must not interpret that transition as player-class-leader finished in multiclass; class finish is plugin-resolved independently.
- For finish/post-race summary widgets, use frozen `RaceFinish.*` exports instead of live `ClassLeader.*`/`ClassBest.*` values. `RaceFinish` is split-stage: class snapshot and player snapshot, and resets when session leaves post-finish lifecycle.
- `RaceFinish.Active` means either stage is active (`ClassSnapshotActive || PlayerSnapshotActive`).
- Class stage captures class-winner identity when class winner finishes; player stage captures player fields (position/fuel/player best/class best) when player finishes.
- Before class snapshot, player-facing `RaceFinish` fields stay neutral defaults for finish widgets (`0` / `"-"`). While class snapshot is active and player snapshot is still pending, player-facing fields publish live values (position, fuel, best lap), then freeze once player snapshot activates.
- `RaceFinish.PlayerOverallFieldSize` / `RaceFinish.PlayerClassFieldSize` freeze at class snapshot (not player snapshot) so `Pxx / yy` denominators do not drift when drivers quit before player finish.
- `RaceFinish.PlayerFinishGapSec` (and compatibility mirror `ClassWinnerGapSec`) is a live timer after class snapshot and freezes at player snapshot.
- Player snapshot trigger hierarchy is plugin-owned: per-car finish-like flags and robust player-checkered seams (`GameData.Flag_Checkered` / `SessionFlagsDetails.IsCheckered*`) are used before `SessionState==6` safety fallback. Dashboards must consume exported snapshot flags and must not reimplement finish crossing heuristics.
- Strategy fuel guidance should consume plugin-owned tactical exports directly:
  - `Fuel.RequiredBurnToEnd*` for burn-to-end guidance/state/source,
  - `Fuel.Contingency.*` for active reserve display/debug,
  - `Fuel.Refuel.*` for race-running next-stop refuel guidance (`NextLitres`, `NextLitresCeil`, `NextText`, `Valid`, and basis context),
  - `Fuel.Delta.LitresCurrent/Plan/WillAdd` + Push/Save variants for tactical deltas.
- `Fuel.Refuel.*` is runtime tactical guidance and should be preferred over `StrategyDash.NextRefuel*` during race-running usage; StrategyDash next-refuel helpers remain pre-green/planning oriented (not obsolete).
- Export cleanup caution: no fuel-facing export should be removed until both checks are complete: dashboard JSON usage audit and internal C# reference/consumer audit.
- Do not rebuild burn-to-end with dash-side NCALC formula chains from raw fuel/time/pace properties; use plugin-owned `Fuel.RequiredBurnToEnd`.
- Tactical delta exports are contingency-aware on the required-to-finish side only; `Fuel.Pit.WillAdd` remains clamp mirror and should not be treated as reserve-augmented request.
- Runtime pit-space exports are live-cap authoritative when available: `Fuel.Pit.TankSpaceAvailable` reflects live remaining capacity and `Fuel.Pit.WillAdd`/`Fuel.Pit.FuelOnExit` consume that runtime cap seam (not Strategy/Profile max-fuel override when live cap exists).
- Setup fallback seam for pre-grid/pre-race: dashboards may consume `Fuel.Setup.FuelLevel`/`Fuel.Setup.FuelLevelValid`/`Fuel.Setup.FuelLevelSource` when live tank telemetry is zero/unavailable. This setup seam is read-only and does not replace runtime `Fuel.*` telemetry once live fuel is available.

### Launch
- Gate live launch widgets on `LaunchModeActive` and related launch-visible state.
- Keep **Launch Analysis** concepts separate from live launch widgets; dashboards can show results, but trace review belongs to the plugin review surface.

### Rejoin / pit / messages
- Rejoin widgets should respect the explicit rejoin exports rather than infer active state from message text alone.
- Pit-screen / pit-entry widgets should combine pit visibility toggles with pit-specific active flags.
- Pit command buttons in strategy/pit widgets must bind to plugin-owned actions (`LalaLaunch.Pit.ClearAll`, `LalaLaunch.Pit.ClearTires`, `LalaLaunch.Pit.ToggleFuel`, `LalaLaunch.Pit.FuelSetZero`, `LalaLaunch.Pit.FuelAdd1`, `LalaLaunch.Pit.FuelRemove1`, `LalaLaunch.Pit.FuelAdd10`, `LalaLaunch.Pit.FuelRemove10`, `LalaLaunch.Pit.FuelSetMax`, `LalaLaunch.Pit.ToggleTiresAll`, `LalaLaunch.Pit.ToggleFastRepair`, `LalaLaunch.Pit.ToggleAutoFuel`, `LalaLaunch.Pit.Windshield`, `LalaLaunch.Pit.FuelControl.SourceCycle`, `LalaLaunch.Pit.FuelControl.ModeCycle`, `LalaLaunch.Pit.FuelControl.SetPush`, `LalaLaunch.Pit.FuelControl.SetNorm`, `LalaLaunch.Pit.FuelControl.SetSave`, `LalaLaunch.Pit.FuelControl.SetDataLive`, `LalaLaunch.Pit.FuelControl.SetDataPlan`, `LalaLaunch.Pit.FuelControl.CycleData`, `LalaLaunch.Pit.FuelControl.SetPlan`, `LalaLaunch.Pit.FuelControl.PushSaveModeCycle`, `LalaLaunch.Pit.TyreControl.ModeCycle`, `LalaLaunch.Pit.TyreControl.SetOff`, `LalaLaunch.Pit.TyreControl.SetDry`, `LalaLaunch.Pit.TyreControl.SetWet`, `LalaLaunch.Pit.TyreControl.SetAuto`) rather than `IRacingExtraProperties` action ids.
- Built-in pit command rows are user-configured in plugin **Settings → Pit Commands**; dashboards should bind those action ids directly and not carry raw transport syntax.
- Custom chat buttons should bind to plugin-owned actions `LalaLaunch.CustomMessage01..LalaLaunch.CustomMessage10` (configured in Settings → Custom Messages) rather than embedding transport/raw chat syntax in dash logic.
- Message-dash widgets should consume the message engine outputs directly rather than rebuilding message priority logic in SimHub expressions.
- Offline Testing Track Learning lock/review pages should consume the `TrackLearning.*` export family and actions (`TrackLearning.PitLoss.ToggleLock`, `TrackLearning.Markers.ToggleLock`, `TrackLearning.Condition.ToggleLock`) as a pure consumer surface; dashboards must not reimplement profile lock routing or persistence behavior.

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
- `LalaLaunch.Pit.Command.Active` is a restartable pulse/hold gate (re-published identical text retriggers the hold window).
- Dashboards should style pit feedback from `LalaLaunch.Pit.Command.Severity` / `LalaLaunch.Pit.Command.SeverityText` and should not parse `DisplayText` for priority.
- Publish-priority rule: while an active pit feedback message is live, lower-severity incoming feedback is suppressed; equal/higher severity replaces immediately and restarts the hold.
- Dash visual mapping contract:
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
- Dashboards can also bind `LalaLaunch.Pit.Command.LastAction`/`LastRaw` for diagnostics, plus `LalaLaunch.Pit.Command.FuelSetMaxToggleState` for real `Pit.FuelSetMax` MAX/ZERO toggle state (`false=last press sent ZERO / next press sends MAX`, `true=last press sent MAX / next press sends ZERO`). Tank-full short-circuit applies to MAX phase only; ZERO phase is still transported as `#fuel 0.01`.
- Dashboards can bind `LalaLaunch.Pit.FuelControl.*` exports (`Data/DataText`, `Source/SourceText`, `Mode/ModeText`, `TargetLitres`, `OverrideActive`, and compatibility `PushSaveMode/PushSaveModeText`) for pit fuel control state display; dashboards do not own data/source/mode logic.
- Dashboards can bind `LalaLaunch.Pit.TyreControl.Mode` / `ModeText` for plugin-owned tyre control display (`OFF`/`DRY`/`WET`/`AUTO`); dashboards must not implement tyre decision logic.
- Tyre control display contract: outside AUTO (`OFF`/`DRY`/`WET`) those exports are bounded truth-synced with MFD state (all-four tyre-change flags + `PitSvTireCompound` dry/wet family mapping) **before** manual enforcement (so stale manual intent does not fight external MFD truth); `ResetToOff()` safety resets remain latched at `OFF` and do not immediately remap back to `DRY/WET` on the next tick.
- Tyre control ON/OFF service truth is four-flag authoritative: ON/OFF is authoritative only when all four tyre flags are available; partial/missing tyre-flag telemetry is unknown/unavailable and service enforcement is held (no additional resend attempts while unknown).
- Tyre control command model is explicit raw command ownership in-plugin (no internal toggle semantics): `OFF => #cleartires$`; `DRY/WET/AUTO => #tc ...$` only (no internal `#t$` pre-send).
- Tyre control confirmation model is single-send/single-window: each manual action or AUTO target change attempts one send, waits a short confirmation window (~0.9 s), and if unconfirmed publishes `PIT CMD FAIL` then remaps mode to current MFD truth (no retry loop, no stale intended mode hold).
- AUTO ownership contract includes plugin-owned send-observation protection: MFD tyre changes immediately after plugin sends are ignored as plugin-owned; delayed convergence is treated as plugin-owned only when observed truth matches the full relevant pending plugin intent (service and compound when both are pending/relevant). Only concrete external/manual truth outside plugin-owned protection cancels AUTO (publish `TYRE AUTO CANCELLED` and remap mode out of AUTO to `OFF`/`DRY`/`WET`). Ambiguous/unavailable truth does not cancel AUTO and does not force `OFF`.
- In plugin Settings → Pit Commands, tyre-control binding UI intentionally exposes only `Tyre Mode Cycle`; direct `SetOff/SetDry/SetWet/SetAuto` actions remain available for direct binding in SimHub Controls & Events / Dash Studio.
- Pit Fuel Control ownership/reset contract for dash rendering: only AUTO is plugin-owned; outside AUTO, mode is always MFD truth from `dpFuelFill` (`OFF` unchecked, `MAN` checked). DATA is `LIVE` or `PLAN` and defaults to `LIVE` on session/control reset. SOURCE is now only `STBY`/`NORM`/`PUSH`/`SAVE`; `SOURCE=PLAN` is retired. `CycleData`/`SetDataLive`/`SetDataPlan` force `SOURCE=STBY` and send no fuel command. Legacy `SetPlan` is kept for one release as `DATA=PLAN` + `SOURCE=STBY` with `FUEL DATA PLAN` feedback. `ModeCycle` remains `OFF -> MAN -> AUTO -> OFF` with explicit raw commands only (no internal `Pit.ToggleFuel`). `OFF -> MAN` explicitly sends `#+fuel$`, then mirrors `MAN STBY`; OFF source/set actions stay isolated (`OFF STBY`, no send). `SourceCycle` order is `STBY -> NORM -> PUSH -> SAVE -> STBY`. `NORM` always uses runtime/live burn; `PUSH`/`SAVE` use DATA (`LIVE` for live push/save, `PLAN` for planner/profile memory push/save). External MFD OFF/ON/fuel-change actions are monitor+mirror only (no plugin send): AUTO publishes `AUTO REFUEL CANCELLED BY MFD` and mirrors to `OFF STBY`/`MAN STBY`; MAN/OFF publish `REFUEL SET OFF BY MFD`, `REFUEL SET ON BY MFD`, or `FUEL CHANGED BY MFD` and mirror to `OFF STBY`/`MAN STBY`. MAN-owned plugin sends are consumed as owned mirror echoes so delayed telemetry is not misclassified as external takeover. iRacing AutoFuel ownership, Offline Testing suppression, and `Telemetry.IsOnTrackCar` edges still force inert disarmed STBY safety.
- Pit Fuel Control migration note for dashboards:
  - replace any `Pit.FuelControl.Source == 3` / `SourceText == "PLAN"` checks with `Pit.FuelControl.Data == 1` / `DataText == "PLAN"`;
  - replace buttons that used `Pit.FuelControl.SetPlan` as a direct planner refuel send with `SetDataPlan` followed by an explicit source button (`SetPush`, `SetSave`, or `SetNorm`) when a command should be sent;
  - keep existing `SetPlan` bindings only as a short-term compatibility path: it now selects DATA PLAN, parks SOURCE at STBY, and does not send fuel.
- The same transport seam also dispatches custom-message actions; dashboards should keep custom-message content authored in plugin Settings rather than hardcoding message text in dash scripts/buttons.

### H2H / traffic
- `H2HRace.*` and `H2HTrack.*` are already flattened for dashboard binding; dashboards should not try to recreate selector logic.
- League Class presentation/cohort contract (including `H2HRace.*`, `H2HTrack.*`, CarSA/Opp/H2H class-facing exports, and effective `PositionInClass` semantics) is canonicalized in `Docs/Subsystems/League_Class_System.md`; dashboards should consume plugin exports directly and not recreate resolver logic.
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

- Added additive `StrategyDash.*` V2 seam for pre-green dashboards (planning + grid/formation).
- StrategyDash phase contract is `0=IDLE`, `1=PRE GRID`, `2=GRIDDING`, `3=START READY`, `5=RACE`.
- `StrategyDash.*` remains publish-safe in race-running phase but is not the primary runtime contract; keep race-running widgets on existing `Fuel.*`, `Fuel.Pit.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Pit.FuelControl.*`, and boxed refuel latch seams.
- `StrategyDash.StartFuelAdviceText/StartFuelStatus` are owned by a dedicated start-fuel check (live fuel, then setup fallback, else unknown) against `StrategyDash.StartFuelRequiredLitres` with a `1.0 L` tolerance; they are intentionally not mapped from `LalaLaunch.PreRace.StatusText`.
- StrategyDash pre-green helpers:
  - `StrategyDash.BurnPlanText` is a concise no-stop/grid helper (`BURN PLAN: NORM/SAVE/PUSH`, optional `/ LIVE` or `/ MEMORY`) so dashes can show useful burn intent when next-refuel is not applicable; `NORM` is runtime/live, while `PUSH`/`SAVE` suffixes follow DATA when the basis is clear,
  - `StrategyDash.NextRefuelDeltaLitres` is `requested refuel - StrategyDash.NextRefuelTargetLitres`, using the exact same burn/source/DATA basis as the target,
  - `StrategyDash.NextRefuelStatus` aligns to absolute delta (`<=0.5 OK`, `<=2.0 CHECK`, else ACTION).
