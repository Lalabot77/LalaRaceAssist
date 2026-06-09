# SimHub Log Messages

**CANONICAL OBSERVABILITY MAP**

Validated against: HEAD
Last reviewed: 2026-04-24
Last updated: 2026-06-08
Branch: work

Scope: Info/Warn logs emitted via `SimHub.Logging.Current.Info(...)` and `SimHub.Logging.Current.Warn(...)`. Use the tag prefixes to filter in SimHub’s log view. Placeholder logs are noted; Debug Cleanup Phase A removed obsolete/debug-only messages and stale documentation as noted in Development_Changelog. Legacy/alternate copies of this list do not exist.

## Pit Debrief diagnostic lifecycle logs

- `[LalaPlugin:PitDebriefBoxDiag]` — one-shot Pit Debrief box-delta source trace emitted at lifecycle edges only: pit-entry/debrief start, first in-box edge, first post-box edge, `Pit.Box.LastDeltaSec` countdown finalization, debrief refresh from the completed countdown seam, and final review if a boxed stop still has pending/suppressed delta evidence. Fields include track identity, driver/player track percent, session/lap, pit phase and lane/stall/box booleans, countdown active state, target/elapsed/final elapsed, `Pit.Box.LastDeltaSec` validity, the debrief delta passed, the exported sign note (`target - actual`, positive quicker), and a reason string. These logs are intended to compare Daytona against tracks where box deltas already resolve and are not emitted per tick.
- `[LalaPlugin:PitDebriefFuelDiag]` — one-shot Pit Debrief fuel-target source trace emitted at pit entry, box entry, first active service refresh, first positive target observation, refuel deselect, box exit, and final debrief. Fields include `Fuel.Pit.WillAdd`, `Fuel.Pit.Box.WillAddLatched`, `Fuel.Pit.AddedSoFar`, `Fuel.Pit.Box.EntryFuel`, current fuel when available, `_isRefuelSelected`, `_isRefuelling`, whether a refuel-cancel clear was sent to Pit Debrief, whether an explicit in-box cancel was latched versus normal completion/reset, the `fuelTargetLitres` value passed, and the current debrief `ServiceFuelTargetLitres`. These logs are edge-triggered/source-trace diagnostics, not per-tick telemetry spam.

## How to read logs for debugging
Workflow routing note: `Docs/Internal/Property_Snapshot_Debug_Workflow.md` owns debugging workflow/triage guidance; this page remains the canonical log-message/tag contract.

- **Filter by tag prefix** (e.g., `[LalaPlugin:Fuel Burn]`, `[LalaPlugin:Pit Cycle]`, `[LalaPlugin:Finish]`) to isolate subsystems.
- **Lap-coupled logs** (PACE/FUEL/RACE PROJECTION) appear once per completed lap and include acceptance reasons; correlate them with lap numbers.
- **Pit-cycle logs** appear on pit entry/exit/out-lap completion. Use them with active Pit/PitLite core exports.
- **Session/identity logs** (`[LalaPlugin:Session]`, `[LalaPlugin:Finish]`) mark resets and finish detection; pair with `Reset.*` SimHub exports.
- **Message system logs** (`MSGV1`) surface missing evaluators and active stack debug—check when adding new messages.

## Action, dash, and launch controls
- **`[LalaPlugin:Runtime] manual recovery reset triggered (reason: <reason>).`** — Unified transient runtime re-arm path fired (Overview reset button, `PrimaryDashMode` action, or session-transition reset path). Includes trigger reason in the log line.
- **`[LalaPlugin:Runtime] fuel health check queued (reason: <reason>).`** — Runtime-health trigger was detected (session/combo/car-active edge) and queued for bounded fuel live-cap health evaluation.
- **`[LalaPlugin:Runtime] fuel health check passed reason=... raw=... runtime=... src=... strategyMissing=...`** — Queued fuel health check evaluated healthy without recovery.
- **`[LalaPlugin:Runtime] planner-safe fuel recovery start/end ...`** — Planner-safe targeted fuel/live-snapshot recovery executed; end line reports health verdict and resolved cap source.
- Phase 1 MonitorSystem migration preserves all three runtime fuel-health log families above unchanged. The same existing outcomes now also publish `FUEL HEALTH OK`, `CHECK FUEL DATA`, `FUEL DATA RECOVERED`, or `FUEL DATA FAULT` through `LalaLaunch.MonitorSystem.*`.
- **`[LalaPlugin:MonitorSystem] pit trigger=<FuelControlModeChanged|FuelControlDataChanged|PredictiveTwoLapsFuelRemaining|PitRoadEntry|PitRoadSnapshotSeeded|PitBoxEntry|PitRoadExit> sessionTime=... sessionType=... sessionState=... phase=... monitorState=... monitorEnum=... monitorText=... onPitRoad=... inPitStall=... currentFuel=... mode=... data=... mfdRefuelEnabled=... mfdFuelRequest=... pluginRefuelValid=... pluginNextLitres=... pluginFuelOnExit=... warningText=<none|REFUEL OFF|MFD FUEL LOW|EXIT FUEL SHORT|BASELINE SHORT> warningEnum=<0|3|4> reason=...`** - Phase 2B pit-stop monitor evidence log. It is emitted only on trigger edges/one-shot threshold events, not per tick. Phase 2B warning publication is edge-only and monitor-enable gated; Fuel Control DATA changes remain log-only in this phase. `PitRoadSnapshotSeeded` is logged only when the framework primes while already on pit road; it seeds exit/refuel-complete evidence without publishing a priming warning. The framework freezes pit-entry evidence internally for exit/refuel-complete guards but does not send commands, add new SimHub exports, and Phase 2C adds an edge-only independent gross baseline warning (`BASELINE SHORT`) without changing the evidence fields, sending commands, or altering fuel/refuel math. Optional Monitor Event CSV rows may be written separately only when the debug setting is enabled and a non-normal MonitorSystem publication changes.
- **`[LalaPlugin:MonitorSystem] REFUEL OFF suppressed post-pit-exit until outlap complete sessionTime=... sessionType=... sessionState=... completedLaps=... exitCompletedLap=... severity=... mfdRefuelEnabled=... pluginNextLitres=... pluginFuelOnExit=...`** - Concise evidence that the post-pit-outlap inhibit suppressed only `REFUEL OFF` after a pit-road exit. Suppression lasts while current completed laps is less than or equal to the lap latched at exit, clears on completed-lap advance or session reset, does not publish a MonitorSystem warning, and does not create a Monitor Event CSV row.
- **`[LalaPlugin:MonitorSystem] fuel guidance suppressed race-ending text=<REFUEL OFF|MFD FUEL LOW|BASELINE SHORT> severity=<3|4> phase=<2|3|4> phaseText=<AfterZeroLeaderRunning|LeaderFinished|SessionComplete> sessionTime=... sessionType=... sessionState=... onPitRoad=... inPitStall=... currentFuel=... mfdRefuelEnabled=... mfdFuelRequest=... pluginRefuelValid=... pluginNextLitres=... pluginFuelOnExit=...`** - Concise evidence that the existing finish authority (`Race.EndPhase >= 2`) suppressed selected late-race actionable fuel guidance. `REFUEL OFF` and `MFD FUEL LOW` are suppressed in race-ending phase; `BASELINE SHORT` is suppressed only for predictive/off-pit-road CAUTION context. The suppression does not publish MonitorSystem output, does not clear active warnings, does not create a Monitor Event CSV row, and does not affect `EXIT FUEL SHORT`, pit/box/exit WARNING `BASELINE SHORT`, fuel-health, Car/Opp/H2H, Launch, or Rejoin stale-state messages.
- **`[LalaPlugin:MonitorSystem] car health check=<OppTarget|CarSASlot|H2HTarget> status=<fail|still-failing|recovered> text=<OPPONENT DATA UNRELIABLE|TRAFFIC DATA UNRELIABLE|H2H DATA UNRELIABLE> sessionTime=... sessionType=... playerCarIdx=... reason=...`** - Phase 3A Car/Opp/H2H impossible-state evidence log. Fail/recovery logs are edge-triggered; repeated still-failing logs are log-only and rate-limited. The checks observe existing published/internal Opponents, CarSA, and H2H outputs only and do not recalculate gaps, validate race order/checkpoint freshness, select fallbacks, reset subsystems, send commands, or add SimHub exports. Optional Monitor Event CSV rows may be written separately only when the debug setting is enabled and a non-normal MonitorSystem publication changes.
- **`[LalaPlugin:MonitorSystem] stale check=<LaunchActiveTooLong|RejoinActiveTooLong|FuelProjectionStale|FuelModelStale|FuelLearningStale> status=<fail|still-failing|recovered> text=<LAUNCH ACTIVE TOO LONG|REJOIN ACTIVE TOO LONG|FUEL PROJECTION STALE|FUEL MODEL STALE|FUEL LEARNING STALE> reason=...`** - Phase 4A/4B stale-state evidence log. Fail/recovery logs are edge-triggered; repeated still-failing logs are log-only and rate-limited to at most once per 60 seconds. These checks observe existing Launch, Rejoin, and plugin-owned Fuel Model/projection/learning state only, and stale WATCH publication yields to active fuel-health, pit, and Car/Opp/H2H reliability warnings; they do not reset subsystems, recalculate fuel/projection/strategy values, send commands, route through MSGV1, or add SimHub exports. Optional Monitor Event CSV rows may be written separately only when the debug setting is enabled and a non-normal MonitorSystem publication changes.
- **`[LalaPlugin:MonitorSystem] event CSV path='<path>'`** - One-time info log when the optional Monitor Event CSV writer first resolves `MonitorSystem_Events.csv` for this plugin run.
- **`[LalaPlugin:MonitorSystem] event CSV disabled after write failure: <message>`** - Optional Monitor Event CSV writer hit an I/O failure and disabled itself to avoid repeated write failures.
- **`[LalaPlugin:Dash] StrategyDashModeToggle action fired -> Advanced=<true|false>.`** — Strategy Dash mode binding pressed; toggles persisted Advanced/Simple presentation status (`true` = Advanced, `false` = Simple).
- **`[LalaPlugin:Dash] DeclutterMode action fired -> DeclutterMode=0/1/2.`** — Declutter control pressed; cycles the 0/1/2 export used for dash visibility bindings.【F:LalaLaunch.cs†L10-L50】
- **`[LalaPlugin:Dash] SecondaryDashMode action fired (legacy) -> DeclutterMode=0/1/2.`** — Legacy alias for the same declutter cycle to preserve old bindings.【F:LalaLaunch.cs†L10-L50】
- **`[LalaPlugin:Dash] Event marker action fired (pressed latched).`** — Event marker action pressed; pulses the event marker latch for CSV tracing.【F:LalaLaunch.cs†L10-L70】
- **`[LalaPlugin:Launch] LaunchMode pressed -> re-enabled launch mode.`** — User pressed Launch while feature was user-disabled; flag cleared.【F:LalaLaunch.cs†L17-L45】
- **`[LalaPlugin:Launch] LaunchMode blocked (inPits=..., seriousRejoin=...).`** — Launch button ignored due to pit/rejoin guard.【F:LalaLaunch.cs†L17-L45】
- **`[LalaPlugin:Launch] LaunchMode pressed -> ManualPrimed.`** — Launch primed manually after passing guards.【F:LalaLaunch.cs†L17-L45】
- **`[LalaPlugin:Launch] LaunchMode pressed -> aborting (state=...).`** — Launch button used as cancel; state reset and user-disabled latched.【F:LalaLaunch.cs†L17-L45】
- **`[LalaPlugin:PitScreen] Toggle pressed IN PITS -> dismissed=..., manual=...`** — Pit screen dismiss toggle used while on pit road.【F:LalaLaunch.cs†L47-L87】
- **`[LalaPlugin:PitScreen] Toggle pressed ON TRACK -> manual=...`** — Pit screen manual force toggle used on track.【F:LalaLaunch.cs†L47-L87】
- Legacy foreground `SendInput` transport logs were removed with issue #698 cleanup; pit/custom commands now use only direct `postmessage` transport in normal dispatch.
- **`[LalaPlugin:PitCommand] transport=postmessage direct-sequence chat-open-attempted=<bool> text-send-attempted=<bool> submit-attempted=<bool> chat-open-succeeded=<bool>`** — Per-command direct-path sequence audit showing which open/type/submit stages ran for a send attempt.
- **`[LalaPlugin:PitCommand] transport=postmessage abort=true reason=<reason> chat-open-attempted=<bool> text-send-attempted=<bool> submit-attempted=<bool>`** — Direct-path bounded abort record used for diagnosing unsafe/partial command attempts without per-tick spam.
- **`[LalaPlugin:PitCommand] transport=postmessage chat-open-attempted=false chat-open-suppressed=state-maybe-open`** — Info line when direct transport intentionally skips sending `T` because previous direct attempt left chat-open state uncertain.
- **`[LalaPlugin:PitCommand] action=<Action> transport=<postmessage|none> local-transport-issue reason=<reason> raw='<raw>' normalized='<normalized>'`** — Direct transport-stage issue before state confirmation; action publishes a specific `PIT CMD ... FAIL` warning.
- **`[LalaPlugin:PitCommand] action=<Action> expected-state-mismatch expected=<bool> before=<before> after=<after> transport=<postmessage>`** — Stateful command did not produce expected before/after toggle confirmation; action publishes `PIT CMD CONFIRM FAIL`.
- **`[LalaPlugin:PitCommand] action=<Action> expected-state-check skipped: before state unavailable.`** — Stateful command was sent but pre-send telemetry truth was unavailable for before/after confirmation; action publishes `PIT CMD CONFIRM FAIL`.
- **`[LalaPlugin:PitCommand] action=<Action> transport=<postmessage> attempted=true delivery=<verified|unverified> effect-confirmed=<true|false> before=<before> raw='<raw>' normalized='<normalized>'`** — Action fire audit line for pit commands. Stateful built-ins use before/after telemetry as authoritative effect confirmation (`effect-confirmed=true` only when confirmed); stateless built-ins remain transport-attempt only (`delivery=unverified`, `effect-confirmed=false`).
- **`[LalaPlugin:PitCommand] action=FuelSetMax ... raw='<raw>' normalized='<normalized>'`** — `Pit.FuelSetMax` alternates transport payload by press (`#fuel +150` then `#fuel 0.01` then repeat); audit line raw/normalized fields show which phase was sent. Tank-full short-circuit now applies only to MAX phase (ZERO phase still transports).
- **`[LalaPlugin:PitCommand] custom-message send blocked: message text is empty.`** — Custom-message action was triggered but the configured message text was blank; action publishes a specific `PIT CMD ... FAIL` warning.
- **`[LalaPlugin:PitCommand] custom-message transport=<postmessage|none> local-transport-issue reason=<reason> text='<text>'`** — Custom-message direct send hit a local transport-stage issue; action publishes a specific `PIT CMD ... FAIL` warning.
- **`[LalaPlugin:PitCommand] custom-message transport=<postmessage> attempted=true delivery=unverified effect-confirmed=false text='<text>'`** — Custom-message send audit line when transport was attempted; this is transport-attempt observability only (no authoritative in-sim effect confirmation seam).
- **`[LalaPlugin:PitCommand] raw-command send blocked: command text is empty after normalization raw='<raw>' normalized='<normalized>'`** — Raw pit-command request was empty/invalid after command normalization; action publishes a specific `PIT CMD ... FAIL` warning and now includes both raw/normalized payload diagnostics.
- **`[LalaPlugin:PitCommand] raw-command transport=<postmessage|none> local-transport-issue reason=<reason> raw='<raw>' normalized='<normalized>'`** — Raw pit-command direct send hit a local transport-stage issue; action publishes a specific `PIT CMD ... FAIL` warning.
  Failure-text mapping uses existing reasons only: process/window resolution reasons publish `PIT CMD WINDOW FAIL`; `postmessage-open-chat-failed` publishes `PIT CMD CHAT FAIL`; empty command, character send, and submit send failures publish `PIT CMD SEND FAIL`. Fuel/tyre callers preserve the raw command engine failure text instead of republishing a generic failure. No current generic failure-publish path emits `PIT CMD TIMEOUT FAIL`.
- **`[LalaPlugin:PitCommand] raw-command transport=<postmessage> attempted=true delivery=unverified effect-confirmed=false raw='<raw>' normalized='<normalized>'`** — Raw pit-command send audit line for paths that intentionally reuse built-in pit-command normalization before injection; this does not prove in-sim execution.
- **`[LalaPlugin:PitFuelControl] PitFuelControlModeCycle action received`** — `LalaLaunch` action entry marker proving SimHub binding reached plugin method for ModeCycle (same pattern used for SourceCycle/SetPush/SetNorm/SetSave/SetDataLive/SetDataPlan/CycleData/SetPlan).
- **`[LalaPlugin:PitFuelControl] entry action=<ModeCycle|SourceCycle|SetPush|SetNorm|SetSave|SetDataLive|SetDataPlan|CycleData|SetPlan|OnLapCross> mode=<OFF|MAN|AUTO> source=<PUSH|NORM|SAVE|STBY> data=<LIVE|PLAN> autoArmed=<bool> isAutoModeActive=<bool> suppressFuelControl=<bool> suppressReason=<none|no-session|no-plugin-manager|unknown|...> iracingAutoFuelEnabled=<bool> telemetryFuelFillEnabled=<bool> targetLitres=<value|n/a> overrideActive=<bool> lastSentFuelLitres=<value|none>`** — `Enable Debug Logging`-gated Fuel Control engine entry snapshot for focused ownership diagnostics. Normal always-on action evidence remains the compact `PitFuelControl* action received` lines from `LalaLaunch`; `planValid` is no longer emitted because PLAN is DATA, not SOURCE.
- **`[LalaPlugin:PitFuelControl] blocked action=<...> reason=<snapshot-null|suppressed:<reason>|off-hard-guard|source-stby|target-invalid|send-failed|auto-not-armed|lap-cross-no-material-delta|iracing-autofuel-ownership|external-mirror-change|owned-mirror-consumed>`** — Explicit no-send/early-return diagnostics for Fuel Control action and ownership paths. Serious `snapshot-null`, `target-invalid`, and `send-failed` blocks remain always-on Info; benign ownership/mirror/suppression churn is `Enable Debug Logging`-gated and transition/rate-limited.
- **`[LalaPlugin:PitFuelControl] telemetry suppression-cleared previousReason=<reason>`** — Transition log emitted once when telemetry suppression state clears; `OnTelemetryTick suppressed:<reason>` logging is transition/throttled (not per tick).
- **`[LalaPlugin:PitTyreControl] Compound change attempt target=<DRY|WET> cmd='<#tc ...>' requested=<...> player=<...> weatherDeclaredWet=<true|false> available01='<...>' available02='<...>'`** — Tyre control v1 observability line emitted when the plugin attempts a compound change; includes requested/fitted seams and GT3-first available-compound context from `DriverTires01/02.TireCompoundType`.
- **`[LalaPlugin:PitTyreControl] Manual truth sync remap reason=<manual-confirmation-fallback|manual-external-truth-sync|auto-cancel-external-ownership> mode=<OFF|DRY|WET>`** — Tyre-control mode remap to actual MFD truth after bounded manual confirmation/reconciliation, or AUTO external-ownership cancel/remap.
- **`[LalaPlugin:PitTyreControl] AUTO enforcement unconfirmed: ...`** — Info-only AUTO ownership observability when bounded service/compound enforcement attempts are not confirmed; AUTO mode is retained and does not collapse to OFF/DRY/WET.
- **`[LalaPlugin:PitTyreControl] AUTO cancelled: external tyre ownership detected, remapped=<OFF|DRY|WET>.`** — AUTO ownership cancellation when tyre MFD truth changes outside plugin-owned protection and a concrete manual-truth remap exists; plugin exits AUTO and follows manual truth. Ambiguous/unavailable truth does not cancel AUTO.
- **`[LalaPlugin:PitTyreControl] mode=<OFF|DRY|WET|AUTO> reason=<driver-action|auto-correction> command='<#cleartires|#t tc 0|#t tc 2>' sent=<true|false>`** — Tyre command-send audit for one-shot sends (driver action or AUTO correction).
- **`[LalaPlugin:PitTyreControl] mode=<OFF|DRY|WET> reason=truth-mirror serviceKnown=<bool> serviceOn=<bool> requestedCompound=<value|NA>`** — Outside-AUTO truth-following remap to current MFD truth after settle hold (no corrective send).
- **`[LalaPlugin:PitTyreControl] mode=<OFF|DRY|WET> reason=auto-cancel serviceKnown=<bool> serviceOn=<bool> requestedCompound=<value|NA>`** — AUTO manual-takeover cancel/remap when MFD truth changes away from AUTO desired declared-wet target after settle.
- **`[LalaPlugin:MsgCx] MsgCx action fired (pressed latched + engines notified).`** — MsgCx action invoked; message engines receive cancel signal.【F:LalaLaunch.cs†L87-L118】
- **`[LalaPlugin:Launch] State change: <old> -> <new>.`** — Launch state machine transition (e.g., primed → logging).【F:LalaLaunch.cs†L2470-L2494】
- **`[LalaPlugin:Launch Trace] <reason> – cancelling to Idle.`** — Launch trace aborted to idle with the provided reason (debounced).【F:LalaLaunch.cs†L3048-L3074】
- **`[LalaPlugin:Launch] ManualPrimed timeout fired ...`** — Manual prime exceeded 30 s; launch cancelled and user-disabled latched.【F:LalaLaunch.cs†L4993-L5004】
- **`[LalaPlugin:Init] Actions registered: MsgCx, TogglePitScreen, Pit.ClearAll, Pit.ClearTires, Pit.ToggleFuel, Pit.FuelSetZero, Pit.FuelAdd1, Pit.FuelRemove1, Pit.FuelAdd10, Pit.FuelRemove10, Pit.FuelSetMax, Pit.ToggleTiresAll, Pit.ToggleFastRepair, Pit.ToggleAutoFuel, Pit.Windshield, Pit.FuelControl.SourceCycle, Pit.FuelControl.ModeCycle, Pit.FuelControl.SetPush, Pit.FuelControl.SetNorm, Pit.FuelControl.SetSave, Pit.FuelControl.SetDataLive, Pit.FuelControl.SetDataPlan, Pit.FuelControl.CycleData, Pit.FuelControl.SetPlan, CustomMessage01..CustomMessage10 (+ aliases Pit.FuelAdd/Pit.FuelRemove), PrimaryDashMode, StrategyDash.ModeToggle, DeclutterMode, ToggleDarkMode, SecondaryDashMode (legacy), EventMarker, LaunchMode, TrackMarkersLock, TrackMarkersUnlock`** — Init-time action registration summary for SimHub bindings. `SetPlan` is a compatibility alias for the DATA model; legacy `PushSaveModeCycle` is not registered.
- **`[LalaPlugin:DarkMode] ToggleDarkMode action fired -> Mode=<old>(<label>)-><new>(<label>).`** — Dark mode cycle action fired from Controls & Events.
- **`[LalaPlugin:DarkMode] Lovely availability changed -> available=<true|false>.`** — Runtime Lovely detection status changed (logged once per transition).
- **`[LalaPlugin:DarkMode] Auto Active transition -> active=..., Alt=..., Precip=..., S=..., W=..., F=..., on<2.0, off>4.0.`** — Auto hysteresis transition with brightness-factor inputs and thresholds.

## Fuel seeds, session change, and identity
- **`[LalaPlugin:Fuel Burn] Captured seed from session ... dry=X (n=a), wet=Y (n=b).`** — Saves rolling dry/wet fuel figures before session change for seeding Race.【F:LalaLaunch.cs†L790-L830】
- **`[LalaPlugin:Fuel Burn] Seeded race model from previous session ... conf=Z%.`** — Applies saved seeds on entering Race with matching car/track.【F:LalaLaunch.cs†L934-L956】
- **`[LalaPlugin:Fuel Burn] Car/track change detected – clearing seeds and confidence`** — Fuel model reset because car or track identity changed.【F:LalaLaunch.cs†L968-L983】
- **`[LalaPlugin:Session] token change old=... new=... type=...`** — Session identity changed (SessionID/SubSessionID); triggers subsystem resets and pit-save finalization.【F:LalaLaunch.cs†L3308-L3365】
- **`[LalaPlugin:Fuel Burn] live-max health source=... raw=... live=... lastValid=... effective=...`** — Debounced live-cap diagnostic snapshot for runtime-authoritative max tank seam and fallback state.
- **`[LalaPlugin:Fuel Burn] runtime burn basis selected source=... burn=... liveAccepted=... fallback=... profileFuel=... reason=...`** — Logged on no-live-lap runtime authority source transitions to prove PROFILE-before-SIMHUB ordering without per-tick jitter spam.
- **`[LalaPlugin:Surface] Mode flip Dry->Wet/Wet->Dry (tyres=..., PlayerTireCompound=..., ExtraProp=..., trackWetness=...)`** — Wet mode toggled based on tyre compound telemetry; includes track wetness context for the change.【F:LalaLaunch.cs†L1402-L1426】

## Lap detection and per-lap summaries
- **`[LalaPlugin:Lap Detector] Pending expired ...`** — `Enable Debug Logging`-gated lap detector diagnostic for an armed lap increment expiring without confirmation; includes target lap and pct. Normal accepted lap crossings do not emit always-on success spam.
- **`[LalaPlugin:Lap Detector] Pending rejected ...`** — `Enable Debug Logging`-gated lap detector diagnostic for an armed lap rejected at expiry with pct/speed context.
- **`[LalaPlugin:Lap Detector] Ignored reason=low speed ...`** — `Enable Debug Logging`-gated lap detector diagnostic for lap increments ignored because speed is below the crossing threshold.
- **`[LalaPlugin:Lap Detector] Pending armed ...`** — `Enable Debug Logging`-gated lap detector diagnostic for atypical pct crossings that require confirmation.
- **`[LalaPlugin:Lap Detector] lap_crossed source=CompletedLaps ...`** — `Enable Debug Logging`-gated diagnostic for atypical accepted crossings; normal success details are not always-on.
- **`[LalaPlugin:PACE] Lap N: ...`** — Per-lap pace summary with acceptance reason, lap time, stint/last5, leader lap/avg, sample count.【F:LalaLaunch.cs†L1238-L1259】
- **`[LalaPlugin:FUEL PER LAP] Lap N: ...`** — Fuel acceptance/rejection, mode, window counts, confidence, pit involvement.【F:LalaLaunch.cs†L1259-L1267】
- **`[LalaPlugin:FUEL DELTA] Lap N: ...`** — Current fuel, required liters, delta, stable burn/laps remaining.【F:LalaLaunch.cs†L1268-L1272】
- **`[LalaPlugin:RACE PROJECTION] Lap N: ...`** — After-zero source/value, timer0, session remain, projected laps, projection lap seconds, projected remaining seconds.【F:LalaLaunch.cs†L1273-L1281】
- **`[LalaPlugin:Leader Lap] clearing leader pace (feed dropped), lastAvg=...`** — Leader pace cache cleared when feed disappears.【F:LalaLaunch.cs†L1336-L1355】

## Projection, after-zero, and pit window
- **`[LalaPlugin:Drive Time Projection] After 0 Source Change from=... to=...`** — Switches between planner and live after-zero once timer zero is seen and live estimate is valid.【F:LalaLaunch.cs†L2005-L2019】
- **`[LalaPlugin:Pit Window] state=... label=... reqAdd=... tankSpace=... lap=... confStable=... reqStops=... closeLap=...`** — Pit window state transitions with context (requested add, tank space, confidence).【F:LalaLaunch.cs†L2145-L2335】
- **`[LalaPlugin:Drive Time Projection] tRemain=... after0Used=... lapsProj=... simLaps=... lapRef=... lapRefSrc=... after0Observed=... state=... mode=...`** — Per-lap race-only projection snapshot showing after-zero, lap reference, simulation comparison, and active projection phase (`grid-formation` vs `race-running`).【F:LalaLaunch.cs†L3674-L3686】
- **`[LalaPlugin:Pace] source=... lap=... stint=... last5=... profile=...`** — Projection lap source change (stint/last5/profile/fallback) with lap seconds.【F:LalaLaunch.cs†L4378-L4391】
- **`[LalaPlugin:Drive Time Projection] projection=drive_time ...`** — Logged when projected laps differ notably from sim laps; shows delta laps, lap ref, after-zero source, remaining seconds.【F:LalaLaunch.cs†L4498-L4516】
- **`[LalaPlugin:After0Result] driver=... leader=... pred=... lapsPred=...`** — After-zero outcome logged once when session ends or checkered seen.【F:LalaLaunch.cs†L4534-L4560】

## Opponents and pit-exit prediction
- **`[LalaPlugin:Opponents] Opponents subsystem active (eligible live session).`** — Opponents activation gate opened for the current eligible live session; native same-class neighbor outputs are now live.
- **`[LalaPlugin:Opponents] Native data unavailable -> outputs invalid (<reason>).`** — Native prerequisites are incomplete (for example missing player row/identity or telemetry arrays), so `Opp.*` and `PitExit.*` stay invalid/empty until data recovers; cadence-limited.
- **`[LalaPlugin:Opponents] Player Drivers row is pace-car flagged (source=<source>); preserving player row and filtering only non-player pace-car rows.`** — One-time anomaly warning when the current player `DriverInfo.Drivers##` row matches the Pace Car metadata helper; Opponents keeps the player row to avoid unsafe removal and filters only non-player Pace Car rows.
- **`[LalaPlugin:CarSA] CarSA enabled (source=CarIdxTruth, slots=5/5)`** — CarSA subsystem became valid for the session (CarIdx truth, 5/5 slots).【F:CarSAEngine.cs†L387-L390】
- **`[LalaPlugin:CarSA] Class rank map built using <source> (<count> classes)`** — Class-rank lookup initialized from session info (CarClassRelSpeed preferred, else CarClassEstLapTime). Drives Faster/Slower class StatusE labeling in multiclass sessions.【F:LalaLaunch.cs†L4930-L4987】
- **`[CarSA] BestLap fallback now using DriverInfo CarClassEstLapTime until best lap available.`** — Best-lap estimates switched to class-estimate fallback until per-car best laps arrive (once per session).【F:LalaLaunch.cs†L6655-L6660】
- **`[LalaPlugin:PitExit] Predictor valid -> true (pitLoss=X.Xs)`** — Pit-exit predictor became valid with native player/class progress context.
- **`[LalaPlugin:PitExit] Predicted class position changed -> P# (ahead=N)`** — Predicted post-stop class position changed while valid (debug-gated with cadence filters).
- **`[LalaPlugin:PitExit] Predictor valid -> false`** — Pit-exit predictor lost validity (native prerequisites unavailable).
- **`[LalaPlugin:PitExit] Pit-in snapshot: lap=... t=... posClass=... posOverall=... gapLdr=... pitLoss=... predPosClass=... carsAhead=... srcPitLoss=... laneRef=... boxRef=... directRef=... entryGapLdr=... gapLdrLive=... gapLdrUsed=... pitLossLive=... pitLossUsed=... predGapAfterPit=... lock=...`** — One-time pit entry snapshot logging player positions, pit loss inputs, locked pit-trip gap/pit loss, and prediction summary (see trailing lock/used/live fields).【F:LalaLaunch.cs†L4712-L4747】
- **`[LalaPlugin:PitExit] Math audit: native progress model active (...)`** — Pit-in audit line confirming native progress/pit-loss-lock model context.

## H2H and messaging native-authority diagnostics
- **`[LalaPlugin:H2H] Native class-best unresolved reason=<reason> playerCarIdx=<idx> hasMultipleClassOpponents=<bool>`** — Transition-latched info log for class-best resolution misses (logs once per unresolved-reason transition). Single-class `no_valid_best_laps` startup is suppressed to avoid no-op startup spam. Obsolete legacy ExtraProperties fallback-removal warnings are no longer emitted.
- **`[LalaPlugin:MSGV1] Session active (routine details suppressed; enable Debug Logging for message diagnostics).`** — `Enable Debug Logging`-gated MSGV1 session marker for message-development diagnostics. Obsolete per-signal legacy ExtraProperties fallback-removal warnings are no longer emitted.

## Pit, refuel, and PitLite
- **`[LalaPlugin:Pit Cycle] Saved PitLaneLoss = Xs (src).`** — Persisted pit lane loss from PitLite/DTL (debounced).【F:LalaLaunch.cs†L2950-L3004】
- **`[LalaPlugin:Pit Cycle] Persist decision: action=SKIP reason=...`** — Pit-loss candidate rejected (NaN/invalid or non-positive) before any write attempt.【F:LalaLaunch.cs†L3251-L3332】
- **`[LalaPlugin:Pit Cycle] PitLoss locked, blocked candidate Xs source=...`** — Pit lane loss candidate was blocked due to a locked profile value; candidate details saved for UI display.【F:LalaLaunch.cs†L3201-L3212】
- **`[LalaPlugin:Pit Cycle] Pit Lite Data used for DTL.`** — Consumed PitLite out-lap candidate to save pit loss.【F:LalaLaunch.cs†L3004-L3035】
- **`[LalaPlugin:Refuel Rate] Learned refuel rate ... Cooldown until ...`** — Refuel EMA learning completed and was applied/saved (unlocked, or locked first-fill when no usable stored rate existed). 【F:LalaLaunch.cs†L3488-L3507】
- **`[LalaPlugin:Refuel Rate] Locked; blocked learned overwrite for '...' (candidate ... L/s).`** — Verbose-debug-only line emitted when a valid learned refuel-rate candidate is intentionally ignored because the active profile has `RefuelRateLocked=true`.
- **`[LalaPlugin:Tyre Learn] candidate armed (all four tyres selected).`** — Tyre-time learner armed only when per-wheel tyre MFD flags (`dpLFTireChange/dpRFTireChange/dpLRTireChange/dpRRTireChange`) indicate all four selected.
- **`[LalaPlugin:Tyre Learn] service start confirmed.`** — Tyre-time learner confirmed in-box service start in valid pit-stall service context.
- **`[LalaPlugin:Tyre Learn] sample raw=... correctedFixed=... correctedDerived=... jackDropAllowance=... savedNow=... wheelOrder=... tLF=... tRF=... tLR=... tRR=... d1=... d2=... d3=... avgInterval=... medianInterval=... perTyreEst=... corrected4TyreEst=... tailDerived=... start=... firstClear=... lastClear=... offsets=... pitEntry=... pitExit=... stallExit=... pitSvStatus=... pitSvFlags=... pitStopElapsed=...`** — One compact per-candidate diagnostic line for four-tyre samples; captures wheel clear order/timestamps/interval statistics and correction inputs used by the persisted learner path.
- **`[LalaPlugin:Tyre Learn] accepted/persisted corrected tyre change time ... (raw ..., method=derived,...jackDrop=1.0s|fixed_tail+jack_drop(1.0s)).`** — Valid corrected all-four candidate persisted (subject to lock/bounds) with method-tagged correction path logging.
- **`[LalaPlugin:Tyre Learn] rejected: ...`** — Candidate rejected for bounded reasons (partial selection, out-of-bounds time, telemetry gap, no profile, or leaving pit before completion).
- **`[LalaPlugin:Tyre Learn] locked overwrite suppressed ...`** / **`locked first-fill allowed ...`** — Lock guard outcome for `TireChangeTimeLocked` using refuel-like semantics.
- **`[LalaPlugin:Pit Lite] ...`** — See PitCycleLite section below for entry/exit/out-lap/publish logs.
- **`[LalaPlugin:Pit Cycle] ...`** — See PitEngine section below for DTL/direct computations and pit-lap captures.

## Profiles, PBs, and fuel seeds
- **`[LalaPlugin:Profiles] Changes to '<profile>' saved.`** — Save action from Profiles tab command.【F:LalaLaunch.cs†L560-L580】
- **`[LalaPlugin:Profiles] Applied profile to live and refreshed Fuel.`** — Profile applied via view-model callback during init.【F:LalaLaunch.cs†L2585-L2633】
- **`[LalaPlugin:Profile] Session start snapshot: Car='...'  Track='...'`** — Live snapshot cleared and session identity pushed to Fuel tab on session change.【F:LalaLaunch.cs†L3308-L3365】
- **`[LalaPlugin:Profile] New live combo detected. Auto-selecting profile for Car='...' Track='...'`** — Auto-selection fired once per new car/track combo in DataUpdate.【F:LalaLaunch.cs†L3598-L3625】
- **`[LalaPlugin:Pace] PB Updated: car @ track -> lap`** — PB update accepted via ProfilesManagerViewModel.【F:ProfilesManagerViewModel.cs†L66-L88】
- **`[LalaPlugin:Pace] validated-lap PB gate candidate=... wet=... car='...' trackKey='...' -> accepted|rejected`** — Accepted-lap PB write gate decision from `UpdateLiveFuelCalcs`; emitted on every validated-lap PB evaluation (Info on acceptance, verbose-debug on rejection).
- **`[LalaPlugin:LapRef] Session best updated (dry|wet): <seconds>s`** — LapRef captured a new in-session validated best snapshot for the active condition.
- **`[LalaPlugin:LapRef] Profile PB sectors persisted (dry|wet) for <car> @ '<track>'.`** — Profile PB update included real fixed-sector data and wrote condition-specific PB sector fields.
- **`[LalaPlugin:Profiles] Track resolved: key='...'`** — Track resolution during profile operations.【F:ProfilesManagerViewModel.cs†L160-L182】
- **`[LalaPlugin:Profiles] Default Settings profile not found, creating baseline profile.`** — Baseline profile auto-created on load miss.【F:ProfilesManagerViewModel.cs†L551-L570】
- **`[LalaPlugin:Profile] PitLoss lock set to ... for '...'`** — Pit lane loss lock toggled for the selected track in the Profiles UI.【F:ProfilesManagerViewModel.cs†L405-L421】
- **`[LalaPlugin:Profile/Pace] PB updated for track '...' (...) ...`** — CarProfiles PB change (live or manual).【F:CarProfiles.cs†L230-L251】
- **`[LalaPlugin:Profile/Pace] AvgDry updated ...`** — Dry average lap time edited for a track.【F:CarProfiles.cs†L526-L547】
- **`[LalaPlugin:Profile / Pace] AvgWet updated ...`** — Wet average lap time edited for a track.【F:CarProfiles.cs†L718-L740】
- **`[LalaPlugin:Profile/Pace] Persisted AvgLapTimeDry for ...`** — Live dry average lap time persisted once sample threshold is met and dry condition is unlocked.【F:LalaLaunch.cs†L1944-L2007】
- **`[LalaPlugin:Profile/Pace] Persisted AvgLapTimeWet for ...`** — Live wet average lap time persisted once sample threshold is met and wet condition is unlocked.【F:LalaLaunch.cs†L1944-L2007】
- **`[LalaPlugin:Profiles] Relearn Pit Data for ...`** — Profiles UI command cleared pit-loss data and reset track markers for the selected track.【F:ProfilesManagerViewModel.cs†L1078-L1099】
- **`[LalaPlugin:Profiles] Relearn Dry Conditions for ...`** — Profiles UI command cleared dry fuel/pace/PB fields and unlocked dry condition for the selected track.【F:ProfilesManagerViewModel.cs†L1101-L1113】
- **`[LalaPlugin:Profiles] Relearn Wet Conditions for ...`** — Profiles UI command cleared wet fuel/pace/PB fields and unlocked wet condition for the selected track.【F:ProfilesManagerViewModel.cs†L1115-L1127】

## Dashboard and pit screen automation
- **`[LalaPlugin:Dash] Ignition off detected – auto dash re-armed.`** — Auto dash will re-run on next ignition-on/engine-start.【F:LalaLaunch.cs†L3690-L3710】
- **`[LalaPlugin:Dash] Auto dash executed for session '...' – mode=auto, page='...'`** — Auto dash switched page on ignition-on/engine-start.【F:LalaLaunch.cs†L3711-L3724】
- **`[LalaPlugin:Dash] Auto dash timer expired – mode set to 'manual'.`** — Auto dash reverted to manual after delay.【F:LalaLaunch.cs†L3718-L3729】
- **`[LalaPlugin:PitScreen] Active -> <bool> (onPitRoad=..., dismissed=..., manual=...)`** — Pit screen visibility changed due to pit state or manual toggle.【F:LalaLaunch.cs†L3734-L3763】
- **`[LalaPlugin:PitScreen] Mode -> <auto|manual> (onPitRoad=..., dismissed=..., manual=...)`** — Pit screen mode changed (manual toggle or automatic pit-road logic).【F:LalaLaunch.cs†L3875-L3878】
- **`[LalaPlugin:PitScreen] Reset to auto (session-change|combo-change) -> mode=..., manual=..., dismissed=...`** — Manual pit screen was cleared due to session token or car/track combo changes.【F:LalaLaunch.cs†L3820-L3834】【F:LalaLaunch.cs†L4068-L4435】

## Finish timing and after-zero observation
- **`[LalaPlugin:Finish] end_phase phase=...`** — End-phase transition log from SessionState authority resolver (`Unknown/Running/AfterZeroLeaderRunning/LeaderFinished/SessionComplete`) with confidence and remaining-time context.
- **`[LalaPlugin:Finish] overall_finish trigger=state ...`** — Overall leader-finished latch from SessionState authority (`>=5`).
- **`[LalaPlugin:Finish] class_finish trigger=single_class_mirror source=overall`** — Single-class class-leader-finished mirror latch from overall leader state.
- **`[LalaPlugin:Finish] last_lap_likely=...`** — Last-lap-likely dash gate transition log (`SessionState==5` any race type, or timed-race state 4 with zero remaining).
- **`[LalaPlugin:Finish] reset trigger=non_race_sustained ticks=3`** — Finish latches reset only after sustained non-race transition guard (anti-blip).
- **`[LalaPlugin:Finish] checkered_flag trigger=flag ...`** — Finish detection driven by session flag data; includes leader/class validity and multiclass flag.【F:LalaLaunch.cs†L4566-L4715】
- **`[LalaPlugin:Finish] leader_finish trigger=derived source=...`** — Derived leader finish (class/overall) once timer zero seen and heuristics trip.【F:LalaLaunch.cs†L4716-L4740】
- **`[LalaPlugin:Finish] finish_latch trigger=driver_checkered ...`** — Driver checkered lap detected; logs timer0, leader/driver checkered times, after-zero measurements.【F:LalaLaunch.cs†L4729-L4780】

## Property snapshot debug exports
- **`[LalaPlugin:Debug] Property snapshot rolling automation START (runtime-only)...`** — Runtime rolling capture started after Soft Debug, Property Snapshot, and rolling-combined-CSV gates pass.
- **`[LalaPlugin:Debug] Property snapshot rolling automation STOP.`** — Runtime rolling capture stopped.
- **`[LalaPlugin:Debug] Property snapshot rolling START ignored: <gate>.`** — Debug UI start request was ignored because Soft Debug, Property Snapshot, or rolling combined CSV is off.
- **`[LalaPlugin:Debug] Property snapshot rolling CSV reset completed.` / `reset failed: <message>`** — Rolling CSV reset action outcome.
- **`[LalaPlugin:Debug] Property snapshot paths primary='...' fallback='...' one-shot subfolder='PropertySnapshots'.`** — One-time path discovery log when snapshot writing first runs.
- **`[LalaPlugin:Debug] Property snapshot summary reason=...`** — Manual/detailed capture summary emitted only through the effective `Enable Debug Logging` gate. Routine auto heartbeat, one-shot write-success, and rolling append-success logs are intentionally suppressed because the CSV output itself proves successful writes.
- **`[LalaPlugin:Debug] Property snapshot export/write/append failed ...`** — Write/export failure logs remain Warn and always visible while the debug feature is enabled.
- **`[LalaPlugin:Debug] Property snapshot rolling CSV schema reset: <reason>. Existing file will be rewritten using current wide schema.`** — One-shot rolling snapshot compatibility guard log emitted when an existing rolling CSV is legacy (`SnapshotUtc`), missing/blank, or unknown schema; old contents are not parsed as wide rows and the file is safely rewritten in current wide format (`SimHubProperty` column-0 authority).

## Car Tracking Probe CSV debug exports
- **`[LalaPlugin:CarTrackingProbe] START ignored: Soft Debug is disabled.`** — Debug UI start request was ignored because master soft debug is off.
- **`[LalaPlugin:CarTrackingProbe] START ignored: Enable Car Tracking Probe is off.`** — Debug UI start request was ignored because the Car Tracking Probe enable toggle is off.
- **`[LalaPlugin:CarTrackingProbe] CSV capture START.`** — Runtime-only Car Tracking Probe CSV capture started; rows are written only while master soft debug and the enable toggle remain on.
- **`[LalaPlugin:CarTrackingProbe] CSV capture STOP.`** — Runtime-only Car Tracking Probe CSV capture stopped and pending rows were flushed.
- **`[LalaPlugin:CarTrackingProbe] CSV reset completed.`** — Current runtime Car Tracking Probe CSV path was removed/reset and capture was stopped.
- **`[LalaPlugin:CarTrackingProbe] CSV reset failed: <message>`** — Reset attempted but file deletion/cleanup failed.
- **`[LalaPlugin:CarTrackingProbe] CSV disabled after write failure: <message>`** — Writer hit an IO/format failure and disabled runtime capture to avoid repeated write failures.

## Leader lap selection
- **`[LalaPlugin:Leader Lap] candidate source=overall_p1_last_lap|overall_p1_best_lap_low_conf ...`** — `Enable Debug Logging`-gated diagnostic for routine overall-P1 candidate sampling after identity resolution.
- **`[LalaPlugin:Leader Lap] using overall leader lap from overall_p1_last_lap ... = Xs`** — `Enable Debug Logging`-gated diagnostic for accepted current overall P1 last-lap samples used by the 3-sample overall race-leading pace window.
- **`[LalaPlugin:Leader Lap] using low-confidence overall leader best lap fallback ... = Xs`** — `Enable Debug Logging`-gated diagnostic when the rolling window is empty and current overall P1 `CarIdxBestLapTime` seeds the published leader pace without being ingested into the rolling window.
- **`[LalaPlugin:Leader Lap] reject source=overall_p1_last_lap|overall_p1_best_lap_low_conf ... reason=...`** — `Enable Debug Logging`-gated candidate rejection detail.
- **`[LalaPlugin:Leader Lap] hold started reason=... completed_player_lap=... avg_s=...`** — Always-on transition/rate-limited authority log when temporary missing/unsampleable overall-P1 feed starts using the bounded held rolling average for projection continuity.
- **`[LalaPlugin:Leader Lap] hold expired reason=... current_player_lap=... last_valid_player_lap=... – returning 0`** — Always-on transition/rate-limited authority log when the one-completed-player-lap hold expires without fresh overall-P1 authority and leader lap fails closed.
- **`[LalaPlugin:Leader Lap] authority recovered source=... carIdx=... lap=... seconds=...`** — Always-on transition log when valid overall leader authority returns after a fail-closed/hold state.
- **`[LalaPlugin:Leader Lap] no valid overall leader ... – returning 0`** — Always-on transition/rate-limited authority-loss/fail-closed log when no bounded hold/fallback is available.
- **`[LalaPlugin:Leader Lap] ResetSnapshotDisplays: cleared live snapshot including leader delta.`** — Live strategy snapshot cleared on session end/reset from FuelCalcs.【F:FuelCalcs.cs†L2985-L3023】
- **`[LalaPlugin:Leader Lap] CalculateStrategy: estLap=..., leaderDelta=..., leaderLap=...`** — Strategy lap calculation when leader delta or estimate changes meaningfully.【F:FuelCalcs.cs†L3839-L3879】

## PitEngine (DTL/direct lane timing)
- **`[LalaPlugin:Pit Cycle] Direct lane travel computed -> lane=Xs, stop=Ys, direct=Zs`** — Valid direct lane time captured; includes lane and stop timing.【F:PitEngine.cs†L90-L175】
- **`[LalaPlugin:Pit Cycle] Pit exit detected – lane=Xs, stop=Ys, direct=Zs. Awaiting pit-lap completion.`** — Pit exit edge latched with timers; arms pit lap/out-lap tracking.【F:PitEngine.cs†L122-L175】
- **`[LalaPlugin:Pit Cycle] Pit-lap invalid – aborting pit-cycle evaluation.`** — Pit lap failed validation; cycle cleared.【F:PitEngine.cs†L175-L218】
- **`[LalaPlugin:Pit Cycle] Pit-lap captured = Xs – awaiting out-lap completion.`** — Valid pit lap latched; waiting for out-lap.【F:PitEngine.cs†L189-L207】
- **`[LalaPlugin:Pit Cycle] Out-lap invalid – aborting pit-cycle evaluation.`** — Out-lap rejected; clears state.【F:PitEngine.cs†L207-L218】
- **`[LalaPlugin:Pit Cycle] DTL computed (formula): Total=Xs, NetMinusStop=Ys (avg=As, pitLap=Bs, outLap=Cs, stop=Ds)`** — Final DTL computation with contributing terms.【F:PitEngine.cs†L218-L239】

## PitCycleLite (pit-lite surface)
- **`[LalaPlugin:Pit Lite] Entry detected. Arming cycle and clearing previous pit figures.`** — Pit entry edge; resets latched values.【F:PitCycleLite.cs†L122-L147】
- **`[LalaPlugin:Pit Lite] Exit detected. Latching lane and box timers from PitEngine.`** — Pit exit edge seen; pulls timers from PitEngine.【F:PitCycleLite.cs†L147-L163】
- **`[LalaPlugin:Pit Lite] Exit latched. Lane=..., Box=..., Direct=..., Status=Status.`** — Immediate exit latch with timers and status (drive-through vs stop).【F:PitCycleLite.cs†L147-L163】
- **`[LalaPlugin:Pit Lite] Out-lap complete. Out=..., In=..., Lane=..., Box=..., Saved=... (source=...).`** — Out-lap completed; publishes loss candidate and lap stats.【F:PitCycleLite.cs†L170-L208】
- **`[LalaPlugin:Pit Lite] In-lap latched. In=...`** — In-lap duration latched when validated.【F:PitCycleLite.cs†L183-L190】
- **`[LalaPlugin:Pit Lite] Publishing loss. Source=..., DTL=..., Direct=..., Avg=...`** — Publishes preferred loss with baseline pace.【F:PitCycleLite.cs†L194-L208】
- **`[LalaPlugin:Pit Lite] Publishing direct loss (avg pace missing). Lane=..., Box=..., Direct=...`** — Fallback publication when pace unavailable.【F:PitCycleLite.cs†L205-L213】

## FuelCalcs (planner and strategy)
- **`[LalaPlugin:Fuel Burn] Strategy reset – defaults applied.`** — Planner reset to defaults (throttled to 1 s).【F:FuelCalcs.cs†L2038-L2057】
- **`[LalaPlugin:Leader Lap] ResetSnapshotDisplays: cleared live snapshot including leader delta.`** — Live snapshot cleared after session end/reset (mirrors leader delta wipe).【F:FuelCalcs.cs†L2985-L3023】
- **`[LalaPlugin:Leader Lap] CalculateStrategy: estLap=..., leaderDelta=..., leaderLap=...`** — Strategy leader lap calculation log (only when values change meaningfully).【F:FuelCalcs.cs†L3839-L3879】
- **`[LalaPlugin:Strategy] live-cap authority available=... source=... litres=...`** — Strategy live-cap resolver state from runtime-authoritative seam.
- **`[LalaPlugin:Strategy] UpdateLiveDisplay: live max tank refresh ...`** — Strategy live snapshot max-tank display refresh event.
- **`[LalaPlugin:Strategy] RefreshLiveSnapshot requested.`** — Explicit strategy-side live-snapshot refresh action invoked.
- **`[LalaPlugin:Strategy] Live Detect changed: source=... session=... isRace=... isLimitedSessionLaps=... sessionLaps=... isLimitedTime=... sessionTime=... basis=... value=... reason=...`** — Emitted only when Live Detect result-signature changes, with source/session/limit fields included for bounded declared-race diagnostics.

## Message system v1
- **`[LalaPlugin:MSGV1] <message>`** — General MSGV1 engine logs (e.g., stack outputs).【F:Messaging/MessageEngine.cs†L478-L560】
- **`[LalaPlugin:MSGV1] Registered placeholder evaluators: ...`** — Fired when message definitions reference missing evaluators; lists evaluator→message mapping.【F:Messaging/MessageEngine.cs†L499-L560】

## File and trace housekeeping
- **`[LaunchTrace] Deleted trace file: <path>`** — Launch trace file deletion via UI command.【F:LaunchAnalysisControl.xaml.cs†L55-L70】
- **`[LalaPlugin:Launch Trace] Skip discard for finalized trace: <path>`** — Verbose-debug guard rail: discard was requested but the current trace had already been finalized/kept, so no deletion occurred.【F:LalaLaunch.cs†L14647-L14653】
- **`[LalaPlugin:Launch Trace] Removed empty launch trace on shutdown: <path>`** — Verbose-debug housekeeping removed an obviously empty launch trace (no telemetry rows and no usable summary) during plugin end-service closeout.【F:LalaLaunch.cs†L15109-L15118】
- **`[LalaPlugin:Launch Trace] Removed empty launch trace file: <path>`** — Verbose-debug housekeeping removed an obviously empty historical launch trace during Launch Analysis file-list discovery so it is not shown as selectable.【F:LalaLaunch.cs†L15159-L15169】
- **`[LalaPlugin:SessionSummary] AppendSummaryRow called green=... checkered=...`** — Session summary CSV writer invoked; row is only appended when green and checkered are both seen.【F:SessionSummaryLogger.cs†L50-L74】

## Pit Entry Assist
- **`[LalaPlugin:PitEntryAssist] Stored pit-entry marker unavailable for track='...'. Legacy iRacingExtraProperties pit-entry fallbacks are disabled (DistanceToPitEntry, PitEntryTrkPct).`** — Warns once while stored marker authority is unavailable; assist outputs are reset/off until stored marker inputs become valid again (warning can fire again after stored inputs recover and later drop).【F:PitEngine.cs†L336-L348】
- **`[LalaPlugin:PitEntryAssist] Session pit-speed authority unavailable. Legacy IRacingExtraProperties pit-speed fallback is removed; Pit Entry Assist outputs remain reset/off until session pit limit is valid.`** — Warns once while session pit-speed authority is unavailable; assist outputs remain reset/off until native session pit limit recovers.
- **`[LalaPlugin:PitEntryAssist] ACTIVATE dToLineRaw=... dToLineGuided=... dReq=... margin=... spdΔ=... decel=... buffer=... cue=...`** — Edge-triggered when the assist arms (EnteringPits, limiter overspeed, or pit-screen manual arming). Captures raw/guided distance, constant-decel requirement, margin, speed delta, profiled decel, buffer, and cue at arming time.【F:PitEngine.cs†L428-L446】
- **`[LalaPlugin:PitEntryAssist] ENTRY LINE SAFE/NORMAL: Speed Δ at Line ...kph, Below Limiter at ...m, Time Loss: +...s`** — Edge-triggered on the pit-lane entry transition when below the limiter. Includes the first compliant distance and computed time loss vs the limiter.【F:PitEngine.cs†L460-L495】
- **`[LalaPlugin:PitEntryAssist] ENTRY LINE BAD: Speed Δ at Line ...kph, Braked ...m too late`** — Edge-triggered on the pit-lane entry transition when still above the limiter; time loss is omitted/zero.【F:PitEngine.cs†L496-L507】
- **`[LalaPlugin:PitEntryAssist] END`** — Edge-triggered when the assist disarms (pit entry handled, invalid inputs, distance ≥500 m, or arming removed).【F:PitEngine.cs†L376-L398】

**Example pit entry lines:**
- `[LalaPlugin:PitEntryAssist] ACTIVATE dToLineRaw=185.3m dToLineGuided=170.3m dReq=142.7m margin=27.6m spdΔ=35.2kph decel=14.0 buffer=15.0 cue=2`
- `[LalaPlugin:PitEntryAssist] ENTRY LINE SAFE: Speed Δ at Line -2.1kph, Below Limiter at 58.4m, Time Loss: +0.62s`
- `[LalaPlugin:PitEntryAssist] END`

## Pit markers and track length
- **`[LalaPlugin:PitMarkers] MSGV1 fire: captured track=<trackKey> entryPct=<pct> exitPct=<pct> locked=<bool>`** — Fired once per session per track when the first valid pit entry and exit markers are captured; emits an MSGV1 informational message and latches suppression by track key.【F:PitEngine.cs†L738-L767】【F:Messaging/MessageEvaluators.cs†L141-L173】
- **`[LalaPlugin:PitMarkers] MSGV1 fire: track_length_delta track=<trackKey> start_m=<m> now_m=<m> delta_m=<m>`** — Fired once per session when a track length delta above threshold is detected; publishes an MSGV1 info message advising markers may be off.【F:PitEngine.cs†L654-L687】【F:Messaging/MessageEvaluators.cs†L175-L198】
- **`[LalaPlugin:PitMarkers] MSGV1 fire: locked_mismatch track=<trackKey> storedEntryPct=<pct> candEntryPct=<pct> storedExitPct=<pct> candExitPct=<pct> tolPct=<pct>`** — Fired once per session per track when stored locked markers differ from a live detection beyond tolerance; sends an MSGV1 medium-priority info message.【F:PitEngine.cs†L682-L723】【F:Messaging/MessageEvaluators.cs†L200-L223】

## Rejoin assist
- **`[LalaPlugin:Rejoin Assist] MsgCx override triggered.`** — Message context override fired inside rejoin assist engine.【F:RejoinAssistEngine.cs†L601-L622】

## Shift Assist
- **`[LalaPlugin:ShiftAssist] Enabled=true/false`** — Shift assist runtime evaluation toggled on/off (startup + live toggle + action toggle).【F:LalaLaunch.cs†L3616-L3616】【F:LalaLaunch.cs†L5905-L5913】
- **`[LalaPlugin:ShiftAssist] Toggle action -> Enabled=...`** — Action binding flipped shift assist on/off from the button map.【F:LalaLaunch.cs†L3635-L3643】
- **`[LalaPlugin:ShiftAssist] Debug CSV toggle action -> Enabled=...`** — Action binding toggled the Shift Assist debug CSV writer on/off for the current settings profile.【F:LalaLaunch.cs†L3651-L3659】
- **`[LalaPlugin:ShiftAssist] Learning samples reset for stack '...'.`** — Action binding cleared retained learning samples for the active stack and re-armed learning state where applicable.【F:LalaLaunch.cs†L3834-L3845】
- **`[LalaPlugin:ShiftAssist] ShiftAssist_ResetTargets_ActiveStack stack='...' changed=true/false`** — Action binding reset active-stack targets to defaults (without clearing learning samples); reports whether any profile row changed.【F:LalaLaunch.cs†L1029-L1062】【F:LalaLaunch.cs†L3847-L3848】
- **`[LalaPlugin:ShiftAssist] ShiftAssist_ResetTargets_ActiveStack_AndSamples stack='...' changed=true/false`** — Action binding reset active-stack targets and then cleared learning samples for that stack.【F:LalaLaunch.cs†L1029-L1062】【F:LalaLaunch.cs†L3849-L3858】
- **`[LalaPlugin:ShiftAssist] ShiftAssist_ApplyLearnedToTargets_ActiveStack_OverrideLocks stack='...' changed=true/false`** — Action binding writes learned RPM values into active-stack targets (ignoring lock state) and reports whether profile rows changed.
- **`[LalaPlugin:ShiftAssist] ShiftAssist_Lock_G1..G8/ShiftAssist_Unlock_G1..G8/ShiftAssist_ToggleLock_G1..G8 stack='...' gear=G# locked=true/false`** — Lock actions force or toggle per-gear learning lock state on the active gear stack profile row.【F:LalaLaunch.cs†L997-L1015】【F:LalaLaunch.cs†L3859-L3882】
- **`[LalaPlugin:ShiftAssist] Test beep triggered (duration=...ms)`** — UI test button fired a manual confirmation beep and latch window.【F:LalaLaunch.cs†L5844-L5871】
- **`[LalaPlugin:ShiftAssist] Beep type=primary/urgent gear=... rawGear=... maxForwardGears=... target=... redline=... effectiveTarget=... rpm=... rpmRate=... leadMs=... throttle=... suppressDown=... suppressUp=...`** — Normal runtime shift cue fired and includes full trigger context for tuning lead-time, gear interpretation, and suppression behavior.【F:LalaLaunch.cs†L6072-L6110】
- **`[LalaPlugin:ShiftAssist] Delay sample captured gear=... delayMs=... avgMs=...`** — Beep-to-upshift timing sample accepted into the rolling per-gear delay averages.【F:LalaLaunch.cs†L5957-L5975】
- **`[LalaPlugin:ShiftAssist] AudioDelayMs=... backend=SoundPlayer`** — Optional telemetry log (when debug CSV mode is enabled) recording trigger-to-audio-issue latency.【F:LalaLaunch.cs†L6082-L6089】
- **`[LalaPlugin:ShiftAssist] Debug CSV disabled for session after write failure path='...' error='...'`** — Debug CSV writer hit an IO/write error and self-disabled for the remainder of the session.【F:LalaLaunch.cs†L6306-L6310】
- **Shift Assist debug CSV now includes urgent observability columns** — each row records urgent eligibility, suppression reason, attempt/play outcome, timing anchors (`MsSincePrimary*`, `MsSinceUrgentPlayed`), and context (`RedlineRpm`, `OverRedline`, `BeepType`) so urgent reminder behavior can be diagnosed from CSV alone; with the 1000ms urgent delay now enforced inside `ShiftAssistEngine`, `OFF_WAITING_GAP` suppression should no longer appear during normal operation.
- **`[LalaPlugin:ShiftAssist] Delay stats reset.`** — Manual reset of all runtime delay statistics from the action binding/UI control.【F:LalaLaunch.cs†L3624-L3632】
- **`[LalaPlugin:ShiftAssist] Sound=Custom path='...'`** — Beep playback currently resolved to a valid custom WAV file path.【F:ShiftAssistAudio.cs†L261-L264】
- **`[LalaPlugin:ShiftAssist] Sound=EmbeddedDefault path='...'`** — Beep playback resolved to the extracted embedded default WAV.【F:ShiftAssistAudio.cs†L265-L268】
- **`[LalaPlugin:ShiftAssist] WARNING custom wav missing/invalid, falling back to embedded default`** — Custom WAV was enabled but missing/invalid; warning is emitted once per session and playback falls back to embedded default sound.【F:ShiftAssistAudio.cs†L233-L241】
- **`[LalaPlugin:ShiftAssist] Embedded default beep resource stream missing.`** / **`[LalaPlugin:ShiftAssist] Embedded default beep resource missing.`** — Embedded WAV resource was unavailable in the assembly; default extraction cannot proceed.
- **`[LalaPlugin:ShiftAssist] Failed to extract embedded beep: ...`** — IO/extraction error while writing the default WAV to disk.【F:ShiftAssistAudio.cs†L72-L85】
- **`[LalaPlugin:ShiftAssist] Failed to play sound '...': ...`** — Sound playback failed for selected path; shift cue remains logically triggered but audio output failed.【F:ShiftAssistAudio.cs†L175-L182】
- **`[LalaPlugin:ShiftAssist] HardStop failed: ...`** — Audio stop attempt failed while disabling/muting beeps; logged as warning.【F:ShiftAssistAudio.cs†L192-L200】


- **Shift Assist urgent cue volume policy** — Urgent beep audio uses derived 50% of the main beep slider at runtime and has no separate urgent volume control.


## 2026-06-08 debug logging gate clarification
- Extra/detail logs use `Enable Debug Logging` only while master `Enable debugging mode` is also on (`SoftDebugEnabled && EnableDebugLogging`); master debug alone must not enable extra SimHub Info diagnostic verbosity.
- PitExit math-audit/detail logging is folded under `Enable Debug Logging`; the old visible `PitExit Verbose Logging` UI toggle is no longer an active behavior gate. Normal PitExit INFO/WARN operational logs remain unchanged.
- Lap Detector diagnostics, Leader Lap routine candidate/source/accept/reject details, PitFuelControl benign diagnostic churn, MSGV1 optional session marker, Property Snapshot manual/detailed summaries, and Shift Assist audio-delay telemetry logs are treated as extra debug/detail logs and use the same master-debug + `Enable Debug Logging` gate; Shift Assist Debug CSV rows remain controlled by master debug plus `Shift Assist Debug CSV`.
## `[LalaPlugin:PitDebrief]`

Emitted once when a pit debrief finalizes after the existing out-lap/final pit-loss seam. This is a structured final readout for the latest completed stop and must not repeat while `Pit.Debrief.Valid` remains true. `actualLossSec` is total-stop-equivalent: current DTL/direct lane-equivalent final loss plus latched stationary box duration for boxed stops. Unavailable numeric fields use `na`; exported numeric fields publish safe `0` defaults. Exit prediction evidence remains in the structured fields; the quoted `summary` intentionally omits exit wording and uses progressive-summary wording latched at finalization. The log includes `rawBoxDeltaSec` and `boxDeltaSuppressed` so a suppressed `BOX ... (Δ PENDING)` can be audited without per-tick logging; box delta uses Pit Debrief sign (`actual elapsed - predicted target`, positive slower), which is the inverse of the dashboard-facing `Pit.Box.LastDeltaSec` sign. `entryLossSec` is printed with two decimals for debug review, while SimHub numeric exports remain raw doubles.

Contract:

```text
[LalaPlugin:PitDebrief] final stop=<int> entry=<token> entryLossSec=<sec|na> decelQuality=<token> limiter=<token> box=<token> missed=<token> boxDeltaSec=<num|na> rawBoxDeltaSec=<num|na> boxDeltaSuppressed=<true|false> fuelTargetL=<num|na> fuelAddedL=<num|na> refuelSec=<num|na> refuelRateLps=<num|na> tyres=<int|na> serviceSec=<num|na> predTotalLossSec=<num|na> actualLossSec=<num|na> lossDeltaSec=<num|na> lossSource=<dtl|direct|unavailable> exitPredPos=<int|na> exitActualPos=<int|na> exitPosDelta=<int|na> exitAccuracy=<token> summary="<summary text>"
```

Example:

```text
[LalaPlugin:PitDebrief] final stop=1 entry=POOR entryLossSec=1.20 decelQuality=UNKNOWN limiter=POOR box=OVERSHOT missed=long boxDeltaSec=5.8 rawBoxDeltaSec=5.8 boxDeltaSuppressed=false fuelTargetL=42.0 fuelAddedL=40.8 refuelSec=15.1 refuelRateLps=2.70 tyres=4 serviceSec=27.4 predTotalLossSec=46.0 actualLossSec=51.8 lossDeltaSec=5.8 lossSource=dtl exitPredPos=5 exitActualPos=8 exitPosDelta=3 exitAccuracy=MISS summary="ENTRY POOR (Δ +1.2s) | BOX OVERSHOT (Δ +5.8s) | SVC 40.8L & 4Ts | STRAT Δ +5.8s"
```
