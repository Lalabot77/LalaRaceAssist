# Repository status

Validated against commit: HEAD
Last updated: 2026-04-23
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- 2026-04-23 Tyre Control simplification follow-up landed (single-send confirmation, no `#t$`, no retries):
  - tyre control command model now uses `OFF => #cleartires$` and `DRY/WET/AUTO => #tc ...$` only (internal `#t$` sequencing removed);
  - tyre service/compound resend loops and retry/cooldown attempt budgets were removed for tyre control;
  - each manual action/AUTO target change now performs one send attempt and waits a short bounded confirmation window (~900 ms);
  - unconfirmed timeout (or immediate send failure) now publishes `PIT CMD FAIL`, then remaps mode to current authoritative MFD truth with no retry loop;
  - preserves existing mode cycle and AUTO external/manual takeover-cancel behaviour.
- 2026-04-23 PR review follow-up restored non-blocking post-toggle verification in `TryToggleFuelFillEnabled(...)`:
  - `_fuelToggleSender()` transport-attempt success is no longer treated as confirmed toggle by itself;
  - after successful send attempt, the engine now performs one immediate snapshot read and requires `dpFuelFill` to match expected ON/OFF before returning success;
  - no wait/re-poll loops were reintroduced; snapshot unavailable/mismatch returns failure so `ModeCycle` does not advance on unconfirmed toggle.
- Pit Fuel Control regression follow-up landed (`OFF -> MAN` ModeCycle freeze fix):
  - removed redundant blocking telemetry re-poll loop from `PitFuelControlEngine.TryToggleFuelFillEnabled(...)` after `Pit.ToggleFuel`;
  - `Pit.ToggleFuel` remains the authoritative state-confirmed toggle seam (before/after `dpFuelFill` check in `PitCommandEngine`);
  - preserves existing non-blocking behavior while keeping toggle-result reporting aligned to immediate telemetry truth during `OFF -> MAN`.
- Tyre Control regression follow-up landed (manual mode-change confirmation window restore):
  - restored `BeginOrClearManualConfirmation(mode)` arming in `PitTyreControlEngine.SetMode(...)`;
  - prevents immediate manual-truth reconciliation from remapping fresh manual mode selections to stale MFD truth before first enforcement send attempts;
  - restores expected tyre control-engine send attempts after `ModeCycle`/`SetDry`/`SetWet` manual actions while keeping existing bounded reconcile fallback behavior.
- Tyre Control PR review follow-up landed (complete AUTO intent match + unknown service-enforcement hold):
  - AUTO plugin-owned delayed convergence now requires full relevant pending-intent agreement (`service` and `compound` when both are pending/relevant), preventing single-dimension matches from masking external/manual takeover;
  - tyre-service enforcement retries are now held while service truth is unknown/unavailable (`HasTireServiceSelection=false`), so telemetry gaps do not burn bounded retry budget;
  - scope remained narrow: no transport, command-model, or mode-cycle redesign.
- Tyre Control follow-up bundle landed (build fix + authoritative service truth + AUTO cancel hardening):
  - fixed `PitTyreControlEngine.OnTelemetryTick()` local-variable collision build break (`desiredWet`);
  - tyre-service truth availability is now authoritative only when all four tyre-change flags are available (partial/missing flag telemetry is unknown/non-authoritative);
  - outside AUTO manual truth mapping now only consumes authoritative four-flag service truth (no partial/LF fallback authority);
  - AUTO external-ownership cancel no longer forces `OFF` on ambiguous/unavailable truth;
  - AUTO now includes delayed plugin-intent convergence protection so staged plugin-originated truth updates arriving after immediate suppression are not misclassified as external takeover;
  - genuine external/manual MFD takeover still cancels AUTO (`TYRE AUTO CANCELLED`) and remaps to concrete manual truth.
- 2026-04-23 PR follow-up fixed AUTO-exit OFF toggle guard:
  - `PitFuelControlEngine.ModeCycle()` AUTO-exit now checks live `dpFuelFill` truth before sending `Pit.ToggleFuel` OFF,
  - if fill is already OFF, AUTO exits to OFF state without sending a toggle (`Source=STBY`, AUTO cleared/disarmed),
  - OFF toggle send still runs only when fill is currently ON; bounded verification + `Pit Cmd Fail` mismatch feedback behavior remains unchanged.
- 2026-04-22 Pit Fuel Control V2 follow-up polish landed:
  - `ModeCycle` now explicitly drives effective mode loop `OFF -> MAN -> AUTO -> OFF` and actively toggles MFD fuel-fill truth on OFF/MAN transitions (`Pit.ToggleFuel` ON/OFF with bounded `dpFuelFill` validation),
  - toggle validation mismatches now publish `Pit Cmd Fail` and fall back to actual MFD truth without correction looping,
  - OFF is now a hard safety guard: source actions (`SourceCycle`, `SetPush`, `SetNorm`, `SetSave`) cannot send fuel commands while effective mode is OFF,
  - AUTO entry now sends immediately for `PUSH/NORM/SAVE` and arms only on successful send; PLAN entry is one-shot immediate send and then forced back to `Source=STBY` disarmed,
  - AUTO source cycle is now PLAN-isolated (`PUSH -> NORM -> SAVE -> PUSH` only); PLAN remains available in MAN cycle only,
  - AUTO cancel edge-trigger and lap-cross AUTO update cadence remain unchanged.
  - same-day follow-up: MAN->AUTO immediate-send failures now explicitly publish `Pit Cmd Fail` for both `PLAN` and `PUSH/NORM/SAVE` entry branches while preserving fallback-to-`STBY` + disarmed behavior.
- Tyre Control PR follow-up landed (explicit raw-command model + AUTO external ownership cancel/remap):
  - tyre control engine no longer uses internal toggle semantics for service enforcement (`Pit.ToggleTyresAll` remains a direct user action only);
  - explicit tyre commands are now authoritative in-engine (`OFF => #cleartires$`; `DRY/WET/AUTO => #t$` then dry/wet `#tc ...$`);
  - AUTO ownership now includes bounded plugin-owned suppression windows for post-send MFD changes;
  - MFD tyre changes outside suppression are treated as external takeover, publish `TYRE AUTO CANCELLED`, and remap mode out of AUTO to current manual truth (`OFF`/`DRY`/`WET`);
  - four-tyre service truth + tri-state ambiguous hold behavior remain intact (no collapse of unknown truth to OFF).
- Tyre Control PR follow-up landed (tri-state manual truth mapping for tyre-service telemetry gaps):
  - outside AUTO manual truth mapping now treats tyre-service state as tri-state (`ON` / `OFF` / `UNKNOWN`) instead of collapsing unavailable service telemetry to OFF;
  - unknown tyre-service truth now returns ambiguous/no-truth for manual reconciliation (no forced OFF remap during telemetry gaps; fail-safe hold behavior preserved);
  - confirmed OFF still maps to `OFF`, and service ON still maps to `DRY/WET` only when requested-compound family truth is valid.
- Tyre Control PR follow-up landed (outside-AUTO ownership ordering + OFF reset latch fix):
  - outside AUTO (`OFF`/`DRY`/`WET`), manual truth reconciliation now runs before manual enforcement so stale manual mode intent is not re-applied ahead of external MFD truth;
  - `ResetToOff()` safety resets now stay latched at `OFF` on the next telemetry tick (no immediate `OFF -> DRY/WET` truth-remap regression);
  - AUTO ownership behavior and AUTO unconfirmed info-feedback behavior remain unchanged.
- Tyre Control follow-up landed (manual 2-way MFD truth sync outside AUTO + AUTO info-only unconfirmed policy):
  - outside AUTO (`OFF`/`DRY`/`WET`), plugin mode now runs a bounded 2-way truth-sync contract against all-four tyre service truth + requested compound truth (`PitSvTireCompound`), including post-request confirmation fallback to actual MFD truth and bounded external-change remap;
  - manual truth mapping remains constrained to existing families (`service OFF => OFF`, `service ON + dry-family => DRY`, `service ON + wet-family => WET`) with fail-safe hold when truth is ambiguous;
  - AUTO remains authoritative ownership mode even when bounded enforcement attempts are unconfirmed;
  - unconfirmed AUTO enforcement now publishes visible info-level feedback/logging (`TYRE AUTO UNCONFIRMED`) without collapsing mode out of AUTO;
  - existing bounded retry/cooldown and transport ownership seams remain unchanged (no second transport path).
- 2026-04-22 docs sweep for v1.1 release prep completed:
  - refreshed root `CHANGELOG.md` unreleased `v1.1` notes to be concise and user-facing,
  - reviewed and refreshed `Docs/User_Guide.md` and `Docs/Quick_Start.md` pit/custom command guidance links,
  - added canonical subsystem contract doc `Docs/Subsystems/Pit_Commands_And_Fuel_Control.md` to capture combined pit command + fuel/tyre control ownership,
  - updated `Docs/Project_Index.md` subsystem map to include the new pit command subsystem doc.
- Pit Fuel Control v2 redesign landed (AUTO plugin-owned, OFF/MAN MFD-derived truth):
  - effective `Pit.FuelControl.Mode` now derives from iRacing MFD fuel-enable truth (`dpFuelFill`) whenever plugin AUTO is not active (`OFF` when unchecked, `MAN` when checked);
  - AUTO cancel no longer uses mismatch/baseline comparison and is now edge-triggered on external changes to either requested fuel (`PitSvFuel`) or fuel-enable (`dpFuelFill`);
  - external (non-plugin-owned) changes still cancel AUTO once and force safety recovery state (`Source=STBY`, `AutoArmed=false`) with short feedback `AUTO CANCELLED`;
  - plugin-owned changes are ignored by cancel detection only when they come from plugin raw `#fuel` sends or explicit plugin `Pit.ToggleFuel` actions;
  - `Telemetry.IsOnTrackCar` reset seam remains authoritative and now resets AUTO/STBY ownership state without forcing plugin-owned OFF/MAN mode values.
- Tyre Control v1 follow-up landed (service-state contract fix + retry hardening + Pit Commands UI tidy-up):
  - `PitTyreControlEngine` service-state truth now reuses the same all-four tyre-selection seam used by `Pit.ToggleTyresAll` confirmation (`PitCommandEngine.ReadTyresAllState`), replacing prior any-tyre truth for control enforcement;
  - tyre-control enforcement contract is now explicit: service ON only when all four tyre-change flags are selected; partial selections are treated as OFF;
  - compound send attempts now always consume retry/cooldown budget even on local raw-send failure, keeping retries bounded and non-spammy on failure paths;
  - Settings → Pit Commands removed preview auto-focus toggle and now shows only `Tyre Mode Cycle` for tyre control binding UI;
  - direct `Pit.TyreControl.SetOff/SetDry/SetWet/SetAuto` actions remain registered for SimHub Controls & Events / Dash Studio use.
- Plugin-owned tyre control v1 landed with focused helper ownership (`PitTyreControlEngine`) and existing pit transport seam reuse:
  - added persistent tyre mode contract (`OFF` / `DRY` / `WET` / `AUTO`) with mode-cycle order `OFF -> DRY -> WET -> AUTO -> OFF`;
  - mode reset now reuses existing `Telemetry.IsOnTrackCar` edge reset seam + session reset seam and forces `OFF` on each edge;
  - runtime target behavior now enforces: `OFF` -> tyre service OFF; `DRY` -> service ON + dry compound target; `WET` -> service ON + wet compound target; `AUTO` -> service ON + declared-wet authority targeting;
  - immediate requested-state verification seam uses `Telemetry.PitSvTireCompound` family checks (dry `{0,1}` / wet `{2,3}`), bounded by cooldown + attempt limits (no per-tick spam/infinite resend);
  - compound-change attempts now emit explicit observability with requested/fitted/weather seams and available GT3-first compound context (`DriverTires01/02.TireCompoundType`);
  - added plugin actions `Pit.TyreControl.ModeCycle/SetOff/SetDry/SetWet/SetAuto` and dash exports `Pit.TyreControl.Mode` + `Pit.TyreControl.ModeText` (settings UI now intentionally exposes only `Tyre Mode Cycle` row for tyre control).
- PR follow-up fixed PB condition-only readback fallback in 500ms best-lap refresh path:
  - when a best-lap event cannot be validated against the current accepted-lap handoff, condition-only PB readback now uses live `_isWetMode` instead of stale `_lastValidLapWasWet`;
  - validated events continue to use accepted-lap wet latch, preserving write/read consistency on accepted laps.
- Wet lap-time / wet PB persistence write-path audit follow-up:
  - accepted-lap wet/dry routing is now latched through downstream PB write/readback gating, preventing condition drift between lap validation and PB persistence;
  - wet tyre routing now treats positive native `PlayerTireCompound` values as wet (`>0`) to avoid false-dry routing on non-zero wet compound variants;
  - profile avg-lap persistence (`AvgLapTimeDry/Wet`) now follows pace-accepted laps directly (no longer blocked by fuel-accepted gate), restoring wet lap-time feed for Strategy/Profile consumers;
  - PB write gating now treats cleared non-positive baseline values as unavailable, allowing first valid wet PB relearn lap to persist after clear.
- Pit Fuel Control polish — feedback-only max-fill wording:
  - plugin-owned pit fuel sends keep original transport payload behavior (no outgoing litres clamp; max-override continues using additive overshoot payload),
  - max-fill style requests now use short user-facing feedback (`FUEL MAX`) when requested/sent litres exceed current tank space (or max-override path is active), using raw tank-space comparison so sub-1L overshoot still resolves as max-fill feedback,
  - normal non-max requests keep litres-based feedback text,
  - command ownership, transport mode behavior, and AUTO cancel behavior remain unchanged.
- PreRace follow-up clarified one-stop feasibility ownership in code:
  - introduced explicit helper `IsOneStopFeasibleForPreRace(...)` for one-stop gate evaluation,
  - helper evaluates one-stop against pit-stop refill capacity plus second-stint fuel demand (`total needed - start fuel`),
  - scenario-first decision ordering and all status outcomes remain unchanged.
- PR follow-up corrected PreRace one-stop feasibility physical gate:
  - one-stop feasibility no longer uses race-start free space (`tank - currentFuel`) as the stop-fill cap,
  - one-stop feasibility now uses pit-stop refill capacity (effective tank capacity at stop) before normal underfuel/overfuel checks,
  - scenario-first status ordering and all other status behavior remain unchanged.
- PreRace v2 scenario-first + grid live-delta/source-contract follow-up:
  - PreRace status logic now classifies required strategy first (`no-stop` / `one-stop` / `multi-stop`) and then evaluates selected strategy with mutually exclusive outcomes (eliminates prior no-stop/multi-stop fallthroughs),
  - one-stop feasibility now includes a tank-capacity gate (`fuelStillNeeded > maxFuelAddPossible` => red `ONE STOP NOT POSSIBLE`),
  - Auto now always follows required-strategy behavior and does not inherit manual-only `STRATEGY MISMATCH`,
  - PreRace one-stop fuel delta now consumes raw telemetry requested-fuel seam on grid so delta updates while dialing pit fuel request,
  - shared planner/live race-length matching tolerances relaxed to ±1 minute (timed) / ±1 lap (lap-limited),
  - Auto source labels remain runtime-owned (`live`/`profile`/`fallback`) and no longer expose planner ownership labels.
- Added plugin-owned `ClassBest.*` export family for the current player-class session-best lap holder:
  - new exports: `ClassBest.Valid`, `ClassBest.CarIdx`, `ClassBest.Name`, `ClassBest.AbbrevName`, `ClassBest.CarNumber`, `ClassBest.BestLapTimeSec`, `ClassBest.BestLapTime`, `ClassBest.GapToPlayerSec`.
  - holder resolution reuses the existing simplified trusted class-best seam (`TryResolveClassSessionBestLap`) already used by `H2H*.ClassSessionBestLapSec` / magenta session-best-in-class coloring.
  - identity resolution reuses existing session-info/native helper seams (`TryGetCarIdentityFromSessionInfo`, `TryGetCarDriverInfo`).
  - gap semantics reuse the existing class-leader live-gap seam (`TryGetCheckpointGapSec` when sane, otherwise progress/pace fallback via shared `ResolveClassGapToPlayerSec`).
  - fail-safe unresolved contract is explicit (`Valid=false`, `CarIdx=-1`, empty strings, `BestLapTimeSec=0`, `BestLapTime="-"`, `GapToPlayerSec=0`) and now clears alongside existing class-leader resets on non-eligible sessions.
- Direct pit/custom transport chat-state sequencing follow-up:
  - direct window-message path now logs staged attempt/abort telemetry (`chat-open`, `text-send`, `submit`) per command attempt,
  - direct path now tracks uncertain chat-open carryover and can suppress repeated chat-open keying (`chat-open-suppressed=state-maybe-open`) on next attempt,
  - `Auto` fallback is now intentionally suppressed after partial direct chat-state mutation (`fallback_suppressed=true reason=postmessage-partial-state-unsafe`) to prevent second-path chat corruption,
  - no autofocus/focus-steal work and no duplicate-send retry path were introduced; transport mode/action surfaces remain unchanged.
- Pit/custom transport follow-up truth-sync (semantics/observability only):
  - transport order remains unchanged (`Auto`: direct `postmessage` first, bounded legacy `sendinput` fallback only on direct-path failure),
  - no duplicate-send retry path was added after queued direct-message success,
  - pit-command success logs now separate transport attempt vs effect confirmation (`delivery=...`, `effect-confirmed=...`),
  - custom-message/raw-command/stateless built-ins now log transport-attempt success as unverified delivery; stateful built-ins keep before/after telemetry as authoritative confirmation.
- PR follow-up hardened pit/custom transport iRacing process authority to simulator-only matching:
  - `IsIracingProcessName(...)` now accepts only `iRacingSim64DX11` (case-insensitive),
  - both `IsIracingForeground()` and `TryResolveIracingMainWindow(...)` continue to share that helper,
  - transport/fallback seam behavior is otherwise unchanged (direct window-message first in `Auto`, bounded foreground `SendInput` fallback).
- Pit/custom command transport upgrade (bounded seam in `PitCommandEngine`):
  - new default transport mode is `Auto`: direct iRacing window-message send first (`postmessage`) with explicit fallback to legacy foreground `sendinput`;
  - added settings-level transport selector (`Auto`, `Legacy foreground SendInput only`, `Direct message only`) in `Settings -> Pit Commands`;
  - built-in pit actions, custom-message actions, raw pit-command seam, feedback exports, and stateful toggle confirmation ownership remain unchanged;
  - transport logs now explicitly report `transport=...`, fallback (`fallback_from=postmessage`), and bounded failure reasons (`no-iracing-process`, `no-iracing-window`, `not-foreground`, etc.).
- PreRace system refresh + shared planner/live validity seam:
  - extracted shared planner/live session match helper and moved Pit Fuel Control PLAN validity to it;
  - added `PlannerLiveSessionMatchHelper.cs` to explicit `LaunchPlugin.csproj` compile items for non-SDK project build inclusion;
  - PreRace Auto now uses live race-definition authority first (`_SessionTime` timed / `_SessionLaps` lap-limited) plus runtime stable fuel/lap source seams;
  - PreRace Auto status thresholds now map stints as: `<= 1.0` => `NO STOP OKAY`; `> 1.0` and `<= 2.0` => `SINGLE STOP OKAY`; `> 2.0` => existing multi-stop handling;
  - non-Auto PreRace now shows orange `STRATEGY MISMATCH` only when planner/live inputs are comparable and actually mismatch (`HasComparableInputs && !IsMatch`), so transient missing values do not raise mismatch;
  - replaced coarse PreRace status band with richer text + color output (`LalaLaunch.PreRace.StatusColour`: `green`/`orange`/`red`);
  - overfuel warning now requires excess > `2x` configured contingency.
  - PR follow-up fixed planner snapshot API accessibility mismatch by making `FuelCalcs.GetPlannerSessionMatchSnapshot()` internal to match internal snapshot type visibility.
- PR follow-up fix for Strategy live-cap freshness-window bypass:
  - tightened `TryGetRuntimeLiveCapForStrategy(...)` to remove unbounded cached-cap return paths (`LiveCarMaxFuel` / `EffectiveLiveMaxTank`);
  - Strategy live-cap authority now resolves through one bounded fallback seam only (`raw -> bounded fallback -> unavailable`), so stale cached caps cannot outlive fallback freshness gating.
- PR follow-up fix for runtime fuel health/recovery merge blockers:
  - moved pit-road telemetry read before active-driving runtime health edge logic so `isOnPitRoad` is defined before use;
  - tightened `ManualRecoveryReset(...)` planner-safe short-circuit so early return is allowed only during active live session when planner-safe recovery succeeds;
  - preserved planner-safe behavior for live runtime recovery while keeping non-live manual reset on the existing broad reset path.
- Runtime health/recovery stabilization sweep (fuel/live snapshot seam):
  - added bounded runtime health checks for live max tank seam (session token/type, combo, and car-active edges) with debounced recovery;
  - unified Strategy live-cap authority to plugin runtime live-cap seam (`raw -> bounded last-valid fallback`);
  - added planner-safe targeted manual recovery path for fuel/live snapshot refresh;
  - removed automatic planner manual-override reset from fuel-model session reset path to preserve planner intent during runtime re-arm;
  - synced Fuel Model / Pace & Projection / Fuel Planner docs and internal inventory/log/changelog notes to reflect the new bounded health/recovery behavior.
- Analysis-first class-resolution simplification landed with one trusted-property authority model and shared seams:
  - class-state authority for runtime consumers now uses `GameData.HasMultipleClassOpponents` directly;
  - single-class path now bypasses class matching entirely and uses overall leader / whole-field best directly;
  - multiclass path now uses one shared player-class seam for both class leader and class-best resolution;
  - `Race.ClassLeaderHasFinished*` now consumes the same class-leader seam used by `ClassLeader.*` (no separate finish-only resolver);
  - removed metadata-cache authority helpers and duplicate class authority trees from active class-leader/class-best/finish paths.
- Wet-condition PB/session-best audit follow-up (bounded to profile/PB/planner/LapRef wet-dry behavior):
  - planner/profile PB reads now use condition-only lookup, so wet mode no longer borrows dry PB when wet PB is absent;
  - validated wet PB persistence remains condition-scoped on existing telemetry gate path (`_isWetMode` -> `TryUpdatePBByCondition(...)` -> wet PB fields only);
  - LapRef session-best authority sync remains trusted-seam based but now only applies when near-equal to the captured current-context baseline, preventing dry global-best seam values from repopulating wet SessionBest after wet reset.
- Final documentation sweep aligned user-facing + dash-facing + internal docs with the implemented combined pit command stack:
  - plugin-owned pit command actions and plugin-owned custom-message actions are now the documented default workflow;
  - built-in pit command configuration location is documented as `Settings -> Pit Commands`;
  - custom-message configuration location is documented as `Settings -> Custom Messages`;
  - pit fuel control usage notes are aligned to the implemented plugin-owned source/mode/action/export behavior;
  - focus caveat remains explicit and truthful (`iRacing` foreground required for reliable pit/custom sends; auto-focus not implemented).
- Profile-track PB clear polish follow-up (bounded to profile-track editor clear path + UI text styling):
  - condition PB clear now immediately clears visible PB text in-place by mirroring relearn refresh semantics (clear PB display cache + raise property changed for condition PB text);
  - dry/wet clear button labels are now compact and identical: `Clear PB Data`;
  - condition `No sector data` status text now uses an orange warning-style colour for better visibility;
  - PB cleared-state semantics remain unchanged (`0`/non-positive still unavailable, no valid zero-time PB reference path).
- PR #584 follow-up fixed custom-message restart persistence at settings load:
  - removed eager default prepopulation of `LaunchPluginSettings.CustomMessages` before JSON deserialize;
  - `NormalizePitCommandSettings(...)` remains the default-slot authority and now supplies default rows only when collection data is missing/null/undersized;
  - saved custom-message rows now load intact across restart, while new/missing settings still normalize to the expected 10 slots.
- Pit Fuel Control follow-up (bounded to pit fuel-control + pit-command seam):
  - `Pit.FuelSetZero` and `Pit.FuelSetMax` ZERO phase now transport `#fuel 0.01$` (MAX phase unchanged).
  - AUTO cancel now uses live requested-fuel movement ownership detection outside plugin send suppression (no stale baseline mismatch dependence); cancellation is one-shot to `OFF + STBY` with `AUTO CANCELLED`.
  - Plugin AUTO now disengages when iRacing AutoFuel is enabled (no AUTO co-ownership).
  - External lifecycle reset now uses only `Telemetry.IsOnTrackCar` edge detection (`false -> true` and `true -> false`) and forces `OFF + STBY` on each edge.
  - Offline Testing now suppresses Pit Fuel Control to inert `OFF + STBY`.
- LapRef/H2H/profile-track follow-up (bounded scope):
  - profile-track editor now has condition-specific PB cleanup actions:
    - `Clear Dry PB + Sectors` (dry PB lap + dry S1..S6 only),
    - `Clear Wet PB + Sectors` (wet PB lap + wet S1..S6 only);
  - each condition block now shows `No sector data` only when that condition has a PB lap but incomplete/missing PB sector payload;
  - H2H unresolved class-best info log is now reason-transition latched (no cadence spam), with startup suppression for single-class metadata-not-ready state;
  - LapRef profile-best rematerialization is now condition-only for `LapRef.ProfileBest.*` (wet profile-best no longer falls back to dry PB).
- PR #585 follow-up fixed a high-priority startup authority regression in native class-state resolution:
  - unknown `WeekendInfo.NumCarClasses` no longer defaults to single-class when `HasMultipleClassOpponents` is unavailable/false;
  - authority now remains unresolved/fail-safe unless native single-class is explicit (`NumCarClasses == 1`) or a positive multiclass signal exists (`NumCarClasses > 1`, or unknown class-count with `HasMultipleClassOpponents == true`);
  - this preserves multiclass safety for class-best and class-leader consumers during metadata-lag windows and avoids transient cross-class leaders/best laps.
- Analysis-first class-resolution cleanup aligned native class authority and same-class matching across H2H, ClassLeader, session-best-in-class labeling, and finish-path class leader resolution:
  - introduced a single native authority seam (`NumCarClasses` primary, `HasMultipleClassOpponents` fallback only when class-count is unknown) and removed stored `_isMultiClassSession` state from finish-path decisions;
  - simplified effective same-class matching to one shared rule: explicit single-class sessions always match (blank/mismatch tolerated), multiclass sessions require usable matching class identity;
  - aligned finish-path class-leader selection to the same helper used by class-best/class-leader consumers, eliminating stricter ad hoc class-string checks in one path;
  - retained source-ordered class metadata population (`Drivers##` preferred, `CompetingDrivers[*]` bounded missing-entry backfill) and multiclass fail-safe behavior.
- PR #584 follow-up debounced `Settings -> Custom Messages` text-edit persistence to avoid full settings writes on each keystroke:
  - custom slot `Name` / `MessageText` edits now use a 500 ms settle window before save;
  - pending debounced saves are flushed during normal plugin tick and plugin shutdown to keep latest settled text persisted across SimHub restarts;
  - custom-message action names/binding surfaces and pit/custom command runtime behavior were intentionally unchanged.
- Fixed Settings -> Custom Messages persistence regression: custom slot label/message edits now save on change via settings-layer collection/item hooks, so values persist across SimHub restarts without altering action binding names or pit/custom command execution seams.
- PR #582 follow-up addressed final multiclass authority ordering review feedback:
  - `_isMultiClassSession` now applies native authority in strict order: explicit native single-class (`NumCarClasses == 1`) wins outright, explicit native multiclass stays next (`NumCarClasses > 1`, or unknown + `HasMultipleClassOpponents`), and cache diversity is only used when class-count authority is unresolved/unknown;
  - this prevents cache divergence (`CarClassShortName` vs `CarClassName`) from overriding an explicit native single-class session signal.
- PR #582 merged-follow-up addressed remaining review findings with tight class-metadata/cache scope:
  - class metadata cache still prefers `DriverInfo.Drivers##`, but `CompetingDrivers[*]` now always runs as a per-car missing-entry backfill pass (never overwriting already resolved class rows);
  - `_isMultiClassSession` still honors explicit native multiclass signals as primary, and now accepts already-built cache diversity (`>1` distinct non-blank class names) only when native class-count authority is unresolved/unknown;
  - unknown class-count startup safety remains intact (no multiclass inference from unresolved/blank state alone).
- PR #582 follow-up addressed two additional review findings without widening subsystem scope:
  - class metadata fallback now keys off actual metadata recovery (not only `Drivers##.CarIdx` row presence), so `CompetingDrivers[*]` is used when `Drivers##` exists but class names are still blank/late;
  - `_isMultiClassSession` now requires an explicit multiclass signal (`NumCarClasses > 1`, or unknown class-count with positive `HasMultipleClassOpponents`) instead of deriving from unresolved single-class checks, preventing finish-path multiclass side effects during startup.
- PR #582 follow-up fixed a single-class inference safety gap in class-best resolution:
  - `IsEffectivelySingleClassSession(...)` now treats unknown `WeekendInfo.NumCarClasses` state as unresolved/non-single-class instead of inferring single-class from an unset `HasMultipleClassOpponents` hint.
  - blank-class same-class fallback now requires explicit native single-class authority (`NumCarClasses == 1`), preventing transient whole-field class-best picks in multiclass metadata-startup windows.
- Restored native class-best resolution seam for H2H/ClassLeader in all live opponent-eligible sessions (Practice/Open Qualify/Lone Qualify/Qualifying/Race):
  - class metadata refresh now runs before class-best consumers in tick order (cache update -> class-best calculations -> H2H/ClassLeader publication paths),
  - metadata cache population now prefers `DriverInfo.Drivers##` with `CompetingDrivers[*]` fallback.
- Added conservative effective same-class fallback behavior for single-class sessions with blank class identity:
  - single-class authority uses native `WeekendInfo.NumCarClasses` (with `HasMultipleClassOpponents` as supporting hint),
  - blank class identity is accepted only for defensibly single-class matching,
  - multiclass sessions still fail safe when class identity is unusable (no cross-class collapse).
- Added bounded H2H info observability for class-best resolution miss reasons (`missing_or_late_class_metadata`, `blank_class_identity_multiclass`, `no_valid_best_laps`) while keeping existing one-time fallback-removal warning behavior.
- Added plugin-owned live-session `ClassLeader.*` export family for player-class session-best leader context:
  - new exports: validity/carIdx, identity (`Name`/`AbbrevName`/`CarNumber`), class-best lap (`BestLapTimeSec`/`BestLapTime`), and `GapToPlayerSec`.
  - class-best seam now resolves both best-lap seconds and winning car index from existing native `CarIdxBestLapTime` cache authority.
  - identity resolution reuses existing native/session-info helpers (`Drivers##` + `CompetingDrivers[*]` fallback seams).
  - gap semantics mirror existing live-gap behavior (prefer CarSA checkpoint gap when sane, otherwise native progress/pace fallback).
  - eligibility is live opponent sessions (Practice/Open Qualify/Lone Qualify/Qualifying/Race), with fail-safe invalid outputs when unresolved.
- PR review follow-up gated LapRef SessionBest authority sync by active context:
  - LapRef now disarms authoritative `playerBestLapTimeSec` sync on context reset (session/type/car/track/wet-dry), preventing stale prior-session best-lap carry-over from pre-seeding new-session SessionBest lap-time state.
  - first valid in-context `CaptureValidatedLap(...)` session-best capture re-arms authoritative sync, so trusted authority resumes driving `LapRef.SessionBest.LapTimeSec` only when context-valid.
  - this preserves LapRef ownership of session-best sector payload (`S1..S6*`) while restoring first-lap SessionBest sector capture behavior in new sessions.
- LapRef analysis-first follow-up fixed remaining parity gaps against trusted H2H/core seams:
  - `LapRef.SessionBest.LapTimeSec` no longer depends on LapRef-local validated-lap latch timing; it now synchronizes each tick from the same trusted player best-lap authority path used by H2H/core.
  - LapRef keeps session-best sector payload ownership (`S1..S6*`) from validated-lap capture, decoupled from session-best lap-time authority.
  - removed remaining LapRef-local lap-ref advance rollover latch dependency in current-lap compare re-arm, keeping rollover behavior aligned to live active-segment wrap seam.
  - `LapRef.Player.LapTimeSec` and player-row CarSA-cache-driven sector display behavior remain on the previously corrected trusted seams.
- PR #577 review follow-up cleared AUTO baseline state on disable:
  - `ModeCycle` still cycles `OFF -> MAN -> AUTO -> OFF`.
  - `AUTO -> OFF` now sets `Mode=OFF`, `AutoArmed=false`, `LastSentFuelLitres=-1`, and `Source=STBY`, with feedback `FUEL MODE OFF` and no send.
  - Existing MAN->AUTO forced-STBY guardrail behavior remains unchanged (`PLAN/STBY -> AUTO` keeps `AutoArmed=false` and feedback `FUEL AUTO STBY`).
- PR #576 follow-up fixed bounded Pit Fuel Control behavior regressions:
  - `Pit.FuelSetMax` tank-full short-circuit is now phase-aware: MAX phase can short-circuit, ZERO phase always transports.
  - forced-STBY mode transitions now preserve mode context in one feedback string:
    - `AUTO -> MAN` => `FUEL MAN STBY`
    - `MAN -> AUTO` from `PLAN` => `FUEL AUTO STBY`
  - `MAN -> AUTO` from `PLAN` now leaves `AutoArmed=false` while forcing `Source=STBY` (AUTO mode selected, but not armed until explicit live-source reselection/send).
  - `MAN -> AUTO` from `STBY` now also leaves `AutoArmed=false` and keeps feedback explicit as `FUEL AUTO STBY` (prevents immediate self-cancel before reselection/send).
- Pit Fuel Control control-model follow-up corrected action semantics:
  - `Pit.FuelSetMax` is now a real MAX/ZERO behavioral toggle on transport (`MAX -> ZERO -> MAX -> ZERO`), while `Pit.Command.FuelSetMaxToggleState` still flips on every press.
  - `ModeCycle` no longer hard-blocks `MAN -> AUTO` when `Source=PLAN`; it allows AUTO and immediately forces `Source=STBY` so the driver must reselect a live source.
  - Latest follow-up keeps that MAN->AUTO forced-STBY guardrail but restores disable flow by cycling `AUTO -> OFF` (no AUTO->MAN forced-STBY branch in current behavior).
  - `ModeCycle` selection feedback remains plugin-owned and explicit (`FUEL AUTO STBY` on forced-STBY MAN->AUTO, `FUEL MODE OFF` on AUTO->OFF).
- PR follow-up hardened LapRef authoritative-lap freshness at rollover:
  - `ResolveLapRefAuthoritativeLapTimeSec(...)` now validates `CarIdxLastLapTime` freshness against the validated-gate lap candidate before overriding.
  - when both are valid but diverge beyond a tight tolerance, capture/PB handoff now stays on the validated-gate candidate for that tick (prevents one-tick stale previous-lap overrides).
- LapRef timing-source and PB-trigger alignment fix landed:
  - LapRef validated-lap capture now resolves player lap time from authoritative `CarIdxLastLapTime` seam (same trusted player lap-time seam used by H2H/core), with guarded fallback only when unavailable.
  - `_lastValidLapMs` (PB trigger handoff) now latches from that same authoritative captured lap value, removing first-expected-beat misses caused by stale capture values.
  - `LapRef.Player.LapTimeSec` and `LapRef.SessionBest.LapTimeSec` now inherit the corrected capture seam through LapRef snapshot/session-best ownership.
- LapRef rollover presentation parity tightened to proven H2H feel:
  - player-row sector box refresh now consumes live CarSA fixed-sector cache presence directly (H2H-like display continuity),
  - compare/cumulative truth remains current-lap-only and re-arms on rollover as before.
- PB persistence invariants preserved:
  - PB lap-time update stays on existing `TryUpdatePBByCondition(...)` seam,
  - sector persistence remains conditional on real sector data only,
  - legacy lap-time-only PB rows remain valid.
- PR #575 follow-up fixed Pit Fuel Control source-change failure feedback masking:
  - `SourceCycle` and direct source-select actions now publish `FUEL SRC ...` selection feedback only when no send is attempted (`Mode=OFF`).
  - In `Mode=MAN/AUTO`, failed send attempts now keep existing pit-command failure feedback (`Pit Cmd Fail`) visible instead of being overwritten by selection text.
- PR follow-up corrected Pit Fuel Control live-target contingency ordering:
  - `NORM/PUSH/SAVE` now apply contingency before clamp (`max(0, shortfall + contingency)`)
  - avoids contingency-only overfuel commands when current fuel is already above the base live requirement
  - `NORM` now aligns with the same direct requirement-minus-current seam family used by `PUSH/SAVE` (`-Fuel_Delta_LitresCurrent*`)
- Corrected baseline fuel projection authority across race phases without adding new public property families:
  - SessionState `2/3` (grid/formation) now drives timed-race lookahead from `CurrentSessionInfo._SessionTime` with elapsed `Telemetry.SessionTime`
  - SessionState `4` keeps normal live running projection behavior
  - existing `Fuel.Delta.*` / `Fuel.Pit.*` outputs inherit the fix from the shared projection seam
- Remapped Pit Fuel Control source authority to non-clamped live seams:
  - `PLAN` stays planner-owned (`PlannerNextAddLitres`)
  - `NORM` stays non-clamped runtime need seam
  - `PUSH/SAVE` now use direct requirement-minus-current seams (no `WillAdd` coupling)
- Added live-source contingency alignment for Pit Fuel Control:
  - `PUSH/NORM/SAVE` now include profile contingency (`ContingencyValue` + `IsContingencyInLaps`)
  - explicit contingency zero remains zero (no hidden reserve)
- Hardened LapRef rollover seam for transient zero-segment boundary samples:
- LapRef player-side refactor now reuses H2H/core trusted seams directly:
  - `LapRef.Player.LapTimeSec` now publishes from the same trusted player last-lap seam used by H2H/core.
  - player sector row display now consumes live CarSA fixed-sector cache directly each tick (H2H-style) instead of a LapRef-local display/rollover snapshot lifecycle.
  - removed redundant LapRef-local player display state while retaining only minimal current-lap comparable state for truthful compare/cumulative outputs.
  - SessionBest/ProfileBest static reference ownership and PB persistence invariants remain unchanged.
  - current-lap compare/cumulative eligibility now re-arms on normal wrap (`current > 0 && previous > 0 && current < previous`) and on boundary transition into segment `0` from late-lap state (`previous > 1 && current == 0`)
  - closes the `6 -> 0 -> 1` mapping path so stale prior-lap compare/cumulative validity does not leak into new-lap start
  - kept player-row sector-box persistence behavior unchanged
- Finalized LapRef live cumulative delta rollover semantics after the earlier sector-box persistence patch:
  - kept player sector-box visual persistence across lap rollover
  - separated current-lap compare eligibility from display persistence
  - `LapRef.Compare.*` and top-level cumulative deltas now use current-lap comparable state only
  - at lap start/rollover, cumulative valid flags now re-arm false and cumulative values publish `0` until new-lap comparable sectors exist
- Removed dead rollover state from `LapReferenceEngine` (`_isLivePlayerLapRolloverArmed`) while retaining required rollover tracking (`_lastLivePlayerActiveSegment`) and current-lap comparable snapshot state.
- Kept profile-best fallback semantics unchanged, including legacy lap-time PB rows with missing sectors.
- Fixed LapRef live player sector presentation across lap rollover:
  - removed per-tick hard clear behavior from the live player comparison snapshot path
  - completed sector boxes now persist through start/finish and are replaced progressively as new-lap sectors complete
  - live-sector full clear remains tied to true LapRef reset conditions only (session/car/track/type/wet-dry/explicit reset)
- Added a narrow local lap-rollover rearm seam in `LapReferenceEngine` to keep internal current-lap capture clean without causing visible zero/empty flash at new-lap start.
- Kept profile-best fallback semantics unchanged (lap-time PB may still exist without sector payload).
- Corrected LapRef live-comparison semantics: per-sector compare and top-level cumulative deltas now use player **current-lap completed sectors** from live CarSA cache context, not `_playerSnapshot` last-validated-lap sectors.
- Kept references static: session-best and profile-best remain validated/reference snapshots with unchanged capture/persistence ownership.
- Pruned redundant LapRef per-reference active-segment exports:
  - kept `LapRef.ActiveSegment` and `LapRef.Player.ActiveSegment`
  - removed `LapRef.SessionBest.ActiveSegment` and `LapRef.ProfileBest.ActiveSegment`
- Active-segment publication is now intentionally lean: `LapRef.ActiveSegment` (family-level) and `LapRef.Player.ActiveSegment` (player row) provide the live highlight source.
- Added additive top-level cumulative LapRef delta exports with explicit validity guards:
  - `LapRef.DeltaToSessionBestSec`, `LapRef.DeltaToSessionBestValid`
  - `LapRef.DeltaToProfileBestSec`, `LapRef.DeltaToProfileBestValid`
- Kept existing LapRef per-sector compare outputs (`LapRef.Compare.*.S1..S6State/DeltaSec`) unchanged.
- Preserved boundaries: CarSA fixed-sector ownership unchanged, H2H/Opponents behavior unchanged, and profile PB persistence rules unchanged.
- Updated LapRef subsystem and parameter inventory docs to reflect the active-segment mirroring and cumulative-delta contract.
- Reworked pit command transport to direct chat-command injection while keeping plugin-owned pit action endpoints for Strategy Dash / PitPopUp (`LalaLaunch.Pit.*`).
- Expanded plugin-owned pit action set (`ClearTires`, ±1/±10 fuel steps, `FuelSetMax`, `ToggleAutoFuel`, `Windshield`) with compatibility aliases retained for `Pit.FuelAdd` and `Pit.FuelRemove`.
- Added short-lived pit command feedback exports (`Pit.Command.DisplayText`, `Pit.Command.Active`) plus low-cost diagnostics (`Pit.Command.LastAction`, `Pit.Command.LastRaw`).
- Added explicit failure handling:
  - chat injection unavailability/failure warnings,
  - before/after mismatch warnings for stateful toggles,
  - user-facing `Pit Cmd Fail` fallback text.
- Tightened pit-command logging semantics so transport-stage warnings are explicitly best-effort; state-confirmation mismatch remains the authoritative failure signal for stateful actions.
- Fuel-add feedback now uses explicit `Fuel MAX` wording for max/clamp cases using existing pit tank-space authority.
- Kept pit read-side authority unchanged: pit service status/selection remains telemetry-based (`dpFuelFill`, tyre selectors, `PlayerCarPitSvStatus` and related seams).
- Updated subsystem/user/internal docs and development changelog to match final direct-chat transport and feedback/failure contract.
- Added focused plugin-owned Pit Fuel Control state machine (`PitFuelControlEngine`) integrated into the existing Pit Command transport/feedback seam.
- Added plugin-owned Pit Fuel Control actions:
  - `Pit.FuelControl.SourceCycle`
  - `Pit.FuelControl.ModeCycle`
- Added plugin-owned Pit Fuel Control exports:
  - `Pit.FuelControl.Source` / `SourceText`
  - `Pit.FuelControl.Mode` / `ModeText`
  - `Pit.FuelControl.TargetLitres`
  - `Pit.FuelControl.OverrideActive`
- Implemented locked behavior contract: PLAN validity gating, MAN/AUTO immediate sends, AUTO lap-cross updates on the existing lap-cross detector seam, MAX override hysteresis, and AUTO cancel-to-`MAN+STBY` on manual driver fuel override.
- Kept dashboards as consumers only (no dashboard JSON/UI edits).
- PR 572 follow-up tightened PLAN validity further: car + track/layout key + race basis + race length now all must match between planner and live session before PLAN is considered valid.
- PR 572 follow-up moved pit fuel-control sends to a dedicated raw pit-command path that intentionally reuses built-in pit-command normalization before chat injection (trailing `$` consistency fix).
- PR 572 follow-up made MAX override command explicit with a named clamp-safe overshoot constant rather than implicit magic literals.
- Pit Fuel Control follow-up expanded action/feedback coverage:
  - `SourceCycle` and `ModeCycle` now update `Pit.Command.DisplayText` + `Pit.Command.LastAction` even on selection-only presses (no fuel send).
  - Added direct source-select actions: `Pit.FuelControl.SetPush`, `Pit.FuelControl.SetNorm`, `Pit.FuelControl.SetSave`.
  - Added plugin-owned zero-fuel action `Pit.FuelSetZero` on the normal pit-command transport/feedback seam.
  - `Pit.FuelSetMax` now flips a plugin-owned toggle concept state exported as `Pit.Command.FuelSetMaxToggleState` for dash label logic.

## Reviewed documentation set
### Changed in final docs sweep: plugin-owned pit commands + custom messages + pit fuel control
- `README.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in pit/custom command transport upgrade (direct window-message + bounded fallback)
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `GlobalSettingsView.xaml`
- `README.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in ClassLeader.* live-session export task
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef rollover seam transient-zero follow-up
- `LapReferenceEngine.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef timing-source + PB-trigger + rollover parity task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR follow-up: LapRef CarIdx freshness guard
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef active-segment + cumulative delta task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef cumulative-delta rollover truth follow-up
- `LapReferenceEngine.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef live rollover persistence task
- `LapReferenceEngine.cs`
- `Docs/Subsystems/LapRef.md`
### Changed in pit command polish + Settings expansion task
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `GlobalSettingsView.xaml`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/RepoStatus.md`

### Changed in PR 570 cleanup follow-up task
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control integration task
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `LaunchPlugin.csproj`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in PR 572 follow-up correctness fixes
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control action/feedback follow-up
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control control-model follow-up (real max toggle + STBY mode-guardrails)
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in PR #576 follow-up (FuelSetMax ZERO-phase bypass + forced-STBY feedback refinement)
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in fuel projection phase seam + Pit Fuel Control authority alignment task
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Pace_And_Projection.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR follow-up contingency-before-clamp correction
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef live-current comparison correction task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR #584 follow-up (custom-message load normalization persistence fix)
- `LalaLaunch.cs`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Previously changed in plugin-owned pit command actions task
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `LaunchPlugin.csproj`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `README.md`
- `CHANGELOG.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`


### Changed in Pit Fuel Control ModeCycle AUTO->OFF follow-up
- `PitFuelControlEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR #577 review follow-up (AUTO->OFF baseline clear)
- `PitFuelControlEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control follow-up (zero transport + AUTO ownership cancel + OFF/STBY reset + offline suppression)
- `PitCommandEngine.cs`
- `PitFuelControlEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control polish (feedback-only max-fill wording)
- `PitFuelControlEngine.cs`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control v2 redesign (AUTO-owned + MFD-derived OFF/MAN + edge-trigger cancel)
- `PitFuelControlEngine.cs`
- `LalaLaunch.cs`
- `PitCommandEngine.cs`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in class-best/class-leader live-session seam restore + single-class fallback task
- `LalaLaunch.cs`
- `Docs/Subsystems/H2H.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR #582 review follow-up (explicit single-class authority requirement)
- `LalaLaunch.cs`
- `Docs/Subsystems/H2H.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef profile-track UI integration + H2H unresolved log spam + wet/dry profile-best routing fix
- `ProfilesManagerView.xaml`
- `ProfilesManagerViewModel.cs`
- `CarProfiles.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef player-side seam reuse refactor
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef analysis-first SessionBest authority + rollover parity follow-up
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR follow-up: PB readback wet/dry fallback on unvalidated best-lap events
- `LalaLaunch.cs`
- `Docs/Subsystems/Profiles_And_PB.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

## Delivery status highlights
- Kept changes bounded to existing runtime seams and exports: corrected projection behavior at the shared timed-race authority seam rather than introducing parallel dash property families.
- Aligned Pit Fuel Control source targets with non-clamped requirements for live modes and retained planner-owned PLAN validity/authority.
- Kept ownership boundaries intact: dashboards remain presentation/control surfaces while plugin-owned actions own pit command dispatch.
- Preserved focused helper ownership (`PitCommandEngine`) for transport/mapping/feedback/failure logic instead of widening central runtime loops.
- Added bounded observability and short-lived user feedback exports so command success/failure is visible on dash and in SimHub logs.
- Extended focused-helper ownership with `PitFuelControlEngine` for pit fuel source/mode state and decision logic while keeping `LalaLaunch` as action/export wiring.
- Kept scope bounded to pit action surface/feedback seam extensions (no dashboard JSON/UI expansion): direct source-select + zero-fuel actions, selection-only source/mode feedback publication, and max-fuel toggle-state export.

## Validation note
- Validation recorded against `HEAD` (`Class-best blank-class fallback now requires explicit native single-class authority; unknown class-count state remains fail-safe non-single-class to prevent multiclass cross-class session-best collapse during metadata startup.`).

### Changed in plugin-owned tyre control v1 task
- `PitTyreControlEngine.cs`
- `LalaLaunch.cs`
- `LaunchPlugin.csproj`
- `GlobalSettingsView.xaml`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`
