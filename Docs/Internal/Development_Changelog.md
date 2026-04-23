# Development Changelog

This file tracks internal development history between releases.
The public user-facing release history is maintained in the root `CHANGELOG.md`.

## Pre-v1 public development history

### Initial release foundations
- Established the first public plugin packaging path for SimHub + iRacing testing.
- Set the core split between runtime calculations and dashboard presentation.
- Landed early launch support, fuel tooling, and dashboard package foundations.

### v0.1
- Early user-facing launch workflow and launch dashboards.
- Basic fuel and race-support features for live use.
- Initial profile-based setup flow for repeatable car settings.

### v0.2
- Expanded profile-driven workflow and saved-data usage.
- Improved dashboard package structure for live driving support.
- Added stronger race-session support around planning and dash presentation.

### v0.3
- Added fuller quick-start and user-guide material for testers.
- Strengthened learning/store/lock workflow around fuel and track data.
- Expanded practical dash usage guidance for race sessions.

### v0.4
- Matured pit entry and pit-loss workflows, including learned markers and cleaner pit-cycle guidance.
- Improved dry/wet data separation and condition-aware storage.
- Expanded opponent and race-context tooling, including stronger same-class race support.
- Made track-scoped planning data more practical for venue-specific strategy setup.

## Post-v1.0 development

### 2026-04-23 — PR follow-up: expire satisfied Fuel Control owned-mirror expectations without requiring a change tick
- Classification: **internal-only** (ownership guard hardening; no new action/export surface).
- Updated `PitFuelControlEngine` owned-mirror tracking to immediately clear pending requested-fuel / fuel-fill expectations whenever current observed telemetry is already at the queued expected value, even when no same-tick telemetry delta occurs.
- This closes the stale-pending path where convergence during suppression-window or baseline-init could leave expectations armed and later manual MFD edits to the same value incorrectly attributed as plugin-owned.
- Preserved scope/invariants:
  - no redesign, retries, polling loops, or hidden recovery logic;
  - no new transport behavior and no `Pit.ToggleFuel`/toggle-semantics reintroduction.

### 2026-04-23 — PR follow-up: tighten Fuel Control MAN/AUTO mirror ownership and explicit OFF/MAN commands
- Classification: **both** (driver-visible Fuel Control command/mirror behavior correction + internal ownership hardening).
- Updated `PitFuelControlEngine` to address review follow-up requirements in one focused change:
  - `SetPlan` remains MAN-only and now cleanly no-ops in AUTO (no AUTO disarm/state mutation on blocked press).
  - `OFF -> MAN` now sends explicit MFD ON command (`#+fuel$`), and `AUTO -> OFF` send failure now leaves AUTO unchanged.
  - PLAN sends are now validity-gated at send time, so planner/live mismatch cannot fall through into unintended zero-litre sends.
  - Added owned-mirror expectation tracking for Fuel Control sends, so delayed MAN-owned telemetry echoes are consumed as owned updates instead of misclassified external takeovers (`FUEL CHANGED BY MFD` path).
- Preserved invariants:
  - no transport redesign/retries/poll loops or hidden recovery logic,
  - no `Pit.ToggleFuel` reintroduction inside Fuel Control,
  - subsystem scope remains limited to `PitFuelControlEngine` behavior corrections plus aligned docs.

### 2026-04-23 — Pit Fuel Control authoritative behavior-table alignment (OFF isolation, PLAN MAN-only, external mirror messaging)
- Classification: **both** (driver-visible Fuel Control behavior contract corrections + internal state-machine ownership cleanup).
- Aligned `PitFuelControlEngine` to the authoritative table contract:
  - `OFF -> MAN` is now selection-only `FUEL MAN STBY` with no send; OFF source/set actions are isolated to `OFF STBY` (no sends).
  - `MAN -> AUTO` now sends only for `PUSH/NORM/SAVE`; `STBY` and `PLAN` both enter `AUTO STBY` with no send.
  - `AUTO -> OFF` now always attempts explicit raw OFF command `#-fuel$`; failed send reverts to `AUTO STBY`, successful send exits AUTO.
  - Added MAN-only direct action `Pit.FuelControl.SetPlan`; PLAN is blocked in OFF/AUTO paths.
- Expanded external mirror behavior to explicit table text:
  - in AUTO, external MFD/fuel-request changes now publish `AUTO REFUEL CANCELLED BY MFD` and mirror to OFF/MAN STBY based on MFD truth;
  - in MAN/OFF, external MFD/fuel-request changes now publish `REFUEL SET OFF BY MFD`, `REFUEL SET ON BY MFD`, or `FUEL CHANGED BY MFD` (no plugin send).
- Kept invariants unchanged:
  - no `Pit.ToggleFuel`/toggle semantics added to Fuel Control,
  - no retries/poll loops/hidden recovery logic added,
  - AUTO remains the only plugin-owned mode.
### 2026-04-23 — Tyre Control PR review follow-up: keep service-ON intent tracking for AUTO/DRY/WET `#tc` sends
- Classification: **both** (driver-visible AUTO ownership/cancel stability fix + narrow internal intent-tracking correction).
- Updated `PitTyreControlEngine.EnsureCompound(...)` send path so successful DRY/WET/AUTO `#tc ...$` sends now record pending service-ON intent (`desiredSelected=true`) in addition to pending compound intent.
  - preserves the simplified single-command model (`OFF => #cleartires$`; `DRY/WET/AUTO => #tc ...$`) and does **not** reintroduce `#t$`;
  - keeps delayed OFF->ON service convergence from successful `#tc` sends attributable to plugin-owned intent even when compound-family confirmation already succeeded.
- While compound confirmation is pending, service pending intent is also opportunistically cleared as soon as authoritative service truth reports ON.

### 2026-04-23 — Tyre Control PR review follow-up: restore compound confirmation success path before timeout failure
- Classification: **both** (driver-visible false-failure/false-AUTO-collapse fix + narrow internal confirmation-path correction).
- Updated `PitTyreControlEngine.EnsureCompound(...)` pending-confirmation path to check for successful requested-compound family convergence before timeout handling.
  - while `_compoundConfirmationPending` is active, the engine now first confirms success when `snapshot.HasRequestedCompound` and requested compound family matches current dry/wet target;
  - on match, confirmation state is cleared immediately (pending flag + deadline) and pending compound-intent tracking is also cleared;
  - timeout fallback to `HandleUnconfirmedCommand(...)` now runs only when the confirmation window expires without a family match.
- Prevents false `PIT CMD FAIL` emissions (and unintended AUTO collapse to manual truth) after a successful single-send `#tc ...$` compound change.

### 2026-04-23 — Tyre Control PR review follow-up: always issue `#tc` on DRY/WET/AUTO intent (no already-correct send suppression)
- Classification: **both** (driver-visible DRY/WET/AUTO reliability fix + narrow internal enforcement correction).
- Updated `PitTyreControlEngine.EnsureCompound(...)` to remove the requested-compound-family short-circuit that previously treated "already correct" as a no-op.
  - DRY/WET/AUTO now always perform a single `#tc ...$` send when a mode transition or AUTO enforcement event requires compound intent handling.
  - This preserves the single-send + bounded confirmation window model (no retry loops, no `#t$` reintroduction).
- Fixes the service-off recovery hole where compound family matched but no command was emitted:
  - when tyre service is OFF and requested compound already matches DRY/WET family, the engine now still issues `#tc`, allowing iRacing to turn tyre service ON as part of compound request handling.

### 2026-04-23 — Tyre Control simplification follow-up: remove `#t$` sequencing + remove resend loops
- Classification: **both** (driver-visible tyre command behavior simplification + internal control-path reduction).
- Simplified `PitTyreControlEngine` to single-send, single-confirmation behaviour:
  - removed internal tyre service-on command sequencing (`#t$`) from DRY/WET/AUTO flows;
  - DRY/WET/AUTO now send only one `#tc ...$` attempt per target change, while OFF keeps `#cleartires$`;
  - removed retry/cooldown resend loops for tyre service/compound sends.
- Added short bounded command confirmation windows (~900 ms) with single-failure fallback:
  - on unconfirmed timeout (or immediate send failure), tyre control now publishes standard pit feedback `PIT CMD FAIL`;
  - mode then remaps to current MFD truth when authoritative truth is available (no stale intended mode hold);
  - no secondary retry mechanism or duplicate send loop is used.
- Preserved existing high-level contracts:
  - mode cycle remains `OFF -> DRY -> WET -> AUTO -> OFF`,
  - AUTO external/manual ownership-cancel behaviour remains in place,
  - no internal toggle-semantics reintroduction.
### 2026-04-23 — LapRef/PB seam reliability follow-up (validated-lap PB writes + immediate LapRef refresh)
- Classification: **both** (driver-visible SessionBest/PB timing reliability improvement + internal seam hardening).
- Moved PB write ownership to the accepted validated-lap seam in `UpdateLiveFuelCalcs`:
  - PB compare/write now uses the same authoritative lap-time candidate used by LapRef validated capture (freshness-guarded `CarIdxLastLapTime` handoff).
  - This removes fragile dependency on delayed native best-lap event timing for PB persistence.
- Added a bounded second `UpdateLapReferenceContext(...)` pass in the existing 500ms group immediately after accepted-lap processing so SessionBest/ProfileBest handoff visibility no longer waits an extra update cycle.
- Kept scope/invariants intact:
  - condition-specific wet/dry PB ownership remains unchanged,
  - cleared/non-positive PB values still behave as unavailable baseline,
  - sector persistence remains optional and only writes when real sectors exist,
  - no H2H delta semantics or dashboard JSON contracts changed.

### 2026-04-23 — PR follow-up: restore OFF->MAN progression for ModeCycle-only Fuel Control bindings
- Classification: **both** (driver-visible mode-cycle progression restore + narrow explicit-command ownership correction).
- Updated `PitFuelControlEngine.ModeCycle()` OFF branch so it no longer exits on selection-only state mutation.
  - `OFF -> MAN` now issues a real explicit fuel-amount send attempt via existing raw command ownership (`#fuel ...$`) using current selected source target.
  - This keeps toggle semantics out of Fuel Control while allowing MFD `dpFuelFill` truth to move to MAN for users who only bind `Pit.FuelControl.ModeCycle`.
- Preserved invariants:
  - non-AUTO mode truth remains telemetry-derived from `dpFuelFill`,
  - no `Pit.ToggleFuel` / `#!fuel` path was reintroduced into Fuel Control,
  - AUTO entry/exit behavior and existing OFF hard guard semantics outside this transition remain unchanged.

### 2026-04-23 — Pit Fuel Control mode ownership refactor: remove internal fuel-toggle semantics
- Classification: **both** (driver-visible Fuel Control mode behavior update + internal ownership simplification).
- Refactored `PitFuelControlEngine` so Fuel Control no longer depends on `_fuelToggleSender`, `TryToggleFuelFillEnabled(...)`, or `NotifyPluginFuelToggleAction()`; Fuel Control mode/source paths now rely only on explicit raw fuel command sends.
- Updated mode-cycle behavior to explicit-command model:
  - `OFF -> MAN` is now selection-only intent (`FUEL MODE MAN`) and sends no command,
  - `MAN -> AUTO` keeps existing immediate amount-send ownership semantics (`PUSH/NORM/SAVE` arm on successful send; `PLAN` remains one-shot then forced `STBY` disarmed; `STBY` stays disarmed),
  - `AUTO -> OFF` now uses explicit raw command `#-fuel$` with single-attempt transport semantics (`Pit Cmd Fail` on local transport failure; no retries/poll loops).
- Kept invariants unchanged:
  - OFF hard guard remains (source actions do not send while effective mode is OFF),
  - AUTO cancel edge detection, on-track reset behavior, and lap-cross AUTO cadence remain unchanged,
  - direct plugin action `Pit.ToggleFuel` remains available as a separate pit action outside Fuel Control ownership.

### 2026-04-23 — Pit Fuel Control regression follow-up: remove blocking OFF->MAN toggle re-poll loop
- Classification: **both** (driver-visible ModeCycle responsiveness fix + internal control-loop safety hardening).
- Updated `PitFuelControlEngine.TryToggleFuelFillEnabled(...)` to stop running a second blocking telemetry poll loop after `Pit.ToggleFuel`.
  - `Pit.ToggleFuel` already runs through `PitCommandEngine` stateful before/after confirmation for `dpFuelFill`.
  - The extra poll loop could falsely fail against stale snapshot cadence and made `ModeCycle` appear frozen on `OFF -> MAN`.
- New behavior is now non-blocking at this seam:
  - `TryToggleFuelFillEnabled(...)` now does one immediate post-send snapshot read with no wait loop and only returns success when telemetry `dpFuelFill` matches the expected toggle target;
  - snapshot unavailable or post-send mismatch now returns failure so ModeCycle does not proceed on transport-attempt-only success;
  - existing `Pit Cmd Fail` feedback semantics remain unchanged.

### 2026-04-23 — Tyre Control regression follow-up: restore manual confirmation window on mode changes
- Classification: **both** (driver-visible tyre command-send restoration + internal manual-truth ordering fix).
- Restored missing `BeginOrClearManualConfirmation(mode)` call in `PitTyreControlEngine.SetMode(...)`.
  - Without this arming call, manual truth reconciliation could run immediately on the next telemetry tick and remap fresh manual mode selections (`DRY`/`WET`) back to stale current MFD truth before enforcement sends ran.
  - Result was apparent control-engine no-op behavior: mode looked selected briefly but no tyre-service/compound send attempts landed.
- With confirmation-window arming restored, mode-change sends now get their intended bounded confirmation window before external-truth fallback/remap logic is allowed to reclaim ownership.

### 2026-04-23 — Tyre Control PR review follow-up: complete AUTO intent match + unknown service-enforcement hold
- Classification: **both** (driver-visible AUTO ownership correctness + internal enforcement gating hardening).
- Hardened `PitTyreControlEngine.IsObservedTruthConvergingToPluginIntent(...)` so plugin-owned convergence requires a complete match across the full relevant pending intent set:
  - when both service and compound plugin intents are pending/relevant, both must match before AUTO treats observed truth as plugin-owned convergence,
  - single-dimension matches no longer suppress external/manual AUTO cancel when the other pending dimension diverges.
- Hardened service-enforcement gating in `PitTyreControlEngine.EnsureTyreService(...)`:
  - tyre-service retries/attempt budget are now held when four-flag service truth is unavailable (`HasTireServiceSelection=false`),
  - unknown/unavailable service truth no longer burns bounded service retries during telemetry gaps.
- Kept scope tight: no command-model redesign, no transport changes, and no mode-cycle changes.

### 2026-04-23 — Tyre Control follow-up bundle: build fix + authoritative four-flag truth + AUTO ambiguity/external hardening
- Classification: **both** (driver-visible AUTO ownership safety correction + internal compile/truth-seam hardening).
- Fixed `PitTyreControlEngine.OnTelemetryTick()` compile break by removing duplicate local-name collision (`desiredWet`).
- Tightened tyre-service truth availability seam in `LalaLaunch.BuildPitTyreControlSnapshot()`:
  - service truth is now authoritative only when all four tyre-change flags are available,
  - partial/missing flag telemetry now yields unknown service truth (no LF-only/partial fallback authority for manual reconciliation or AUTO ownership detection).
- Hardened AUTO cancel semantics in `PitTyreControlEngine`:
  - AUTO cancel no longer forces `OFF` when manual truth mapping is ambiguous/unavailable,
  - AUTO now stays active until concrete `OFF`/`DRY`/`WET` remap truth exists.
- Added delayed plugin-result hardening for AUTO external-ownership detection:
  - plugin-owned protection now includes short-lived intent tracking for recent plugin-issued service/compound targets,
  - delayed truth convergence that matches recent plugin intent is treated as plugin-owned (no false external-takeover cancel),
  - genuine external/manual MFD takeover still cancels AUTO with `TYRE AUTO CANCELLED` and concrete remap.
### 2026-04-23 — PR follow-up: guard AUTO exit toggle on live fuel-fill truth
- Classification: **both** (driver-visible OFF transition correctness fix + internal state-machine safety hardening).
- Updated `PitFuelControlEngine.ModeCycle()` AUTO-exit branch so it no longer blindly sends `Pit.ToggleFuel` OFF.
  - AUTO exit now first checks live `dpFuelFill` truth from snapshot telemetry.
  - If fuel fill is already OFF, AUTO exits without sending a toggle and publishes OFF outcome (`Source=STBY`, `AutoArmed=false`, AUTO cleared).
  - OFF toggle send path remains active only when fill is currently ON; bounded verification/mismatch feedback semantics stay unchanged (`Pit Cmd Fail` on mismatch).
- Prevents the prior edge case where AUTO could exit while disarmed with fill already OFF and accidentally flip fill back ON via blind toggle.

### 2026-04-22 — Pit Fuel Control V2 follow-up polish (Mode/MFD alignment + OFF guard + AUTO entry send + PLAN isolation)
- Classification: **both** (driver-visible pit-fuel control behavior corrections + internal ownership/safety hardening).
- Updated `PitFuelControlEngine` mode-cycle contract to explicit effective-mode loop `OFF -> MAN -> AUTO -> OFF`:
  - `OFF -> MAN` now sends plugin `Pit.ToggleFuel` ON and validates `dpFuelFill`,
  - `AUTO -> OFF` now sends plugin `Pit.ToggleFuel` OFF, exits AUTO, and forces `Source=STBY` + `AutoArmed=false`,
  - toggle validation uses bounded short-delay checks; mismatches publish `Pit Cmd Fail` and fall back to MFD truth (no correction loop).
- Hardened OFF safety contract:
  - while effective mode is OFF, all fuel source actions (`SourceCycle`, `SetPush`, `SetNorm`, `SetSave`) are blocked from sending `#fuel`.
- AUTO behavior polish:
  - entering AUTO from `PUSH/NORM/SAVE` now attempts immediate send and arms AUTO only on success,
  - entering AUTO from `PLAN` performs one immediate send then always returns to `STBY` disarmed,
  - AUTO source-cycle is now `PUSH -> NORM -> SAVE -> PUSH` (PLAN removed from AUTO cycle; PLAN remains MAN-only cycle option).
- Kept existing invariants intact:
  - AUTO cancel remains edge-triggered,
  - lap-cross AUTO send cadence unchanged,
  - OFF/MAN remain MFD-derived truth and no parallel plugin OFF/MAN state was introduced.
- Follow-up fix (same day): AUTO-entry immediate-send failure now explicitly publishes `Pit Cmd Fail` in both MAN->AUTO send branches (`PLAN` one-shot branch and `PUSH/NORM/SAVE` branch) while preserving existing fallback behavior (`Source=STBY`, `AutoArmed=false`).

### 2026-04-22 — Tyre Control PR follow-up: explicit raw-command model + AUTO external ownership cancel/remap
- Classification: **both** (driver-visible tyre-control command/ownership behavior correction + bounded internal ownership detection).
- Updated `PitTyreControlEngine` to remove internal toggle-based tyre-service control (`Pit.ToggleTyresAll` no longer used by tyre control engine); engine now uses explicit raw commands only:
  - `OFF` enforcement => `#cleartires$`
  - `DRY` / `WET` / `AUTO` enforcement => `#t$` then compound `#tc ...$`.
- Kept tyre service truth authoritative from all four individual tyre-change flags and preserved tri-state fail-safe manual truth mapping behavior (`UNKNOWN`/ambiguous truth does not remap).
- Added bounded plugin-owned suppression windowing after plugin tyre sends; in AUTO, MFD truth changes outside that window are treated as external ownership takeover and now cancel AUTO with remap to manual truth (`OFF`/`DRY`/`WET`) plus visible feedback (`TYRE AUTO CANCELLED`).
- Kept scope tight: no pit-command transport redesign, no mode-cycle changes, and no unrelated subsystem behavior changes.

### 2026-04-22 — Tyre Control PR follow-up: tri-state manual truth mapping for tyre-service gaps
- Classification: **internal-only** (bug fix to align runtime with already-documented fail-safe hold semantics).
- Updated `PitTyreControlEngine` manual truth mapping to treat tyre-service telemetry as tri-state (`ON` / `OFF` / `UNKNOWN`) instead of collapsing missing service truth to confirmed OFF.
- `TryMapManualTruthMode(...)` now returns no truth when tyre-service state is unknown, so manual mode does not remap during telemetry gaps.
- Confirmed OFF mapping remains unchanged when service OFF is explicitly known; ON mapping still requires valid requested-compound family truth (`DRY`/`WET`), otherwise no remap.
- Kept scope tight: no AUTO behavior changes, no control-loop redesign, and no user-facing contract changes beyond fixing incorrect OFF remaps during missing-data windows.

### 2026-04-22 — Tyre Control PR follow-up: outside-AUTO pre-enforcement sync + OFF reset latch fix
- Classification: **both** (driver-visible tyre-control ownership behavior + narrow engine ordering/reset fix).
- Updated `PitTyreControlEngine.OnTelemetryTick()` manual-mode ordering so outside AUTO (`OFF`/`DRY`/`WET`) truth reconciliation runs before manual enforcement, preventing stale manual intent from being re-applied ahead of external MFD truth.
- Addressed PR review reset regression: `ResetToOff()` OFF safety resets no longer remap back to `DRY/WET` on the next telemetry tick via manual truth-sync.
- Kept scope tight: AUTO ownership and AUTO unconfirmed feedback behavior remain unchanged.
- Synced docs for pit behavior and dash/inventory contracts (`Docs/Pit_Assist.md`, `Docs/Subsystems/Dash_Integration.md`, `Docs/Internal/SimHubParameterInventory.md`, `Docs/RepoStatus.md`).

### 2026-04-22 — Tyre Control follow-up: manual 2-way truth sync + AUTO info-only unconfirmed policy
- Classification: **both** (driver-visible tyre-control behavior contract change + bounded internal reconciliation/feedback logic).
- Updated `PitTyreControlEngine` outside-AUTO behavior (`OFF`/`DRY`/`WET`) to use bounded manual truth sync against actual MFD truth:
  - manual selections are treated as requests with a short confirmation window,
  - unconfirmed manual requests now remap mode to actual truth (`service OFF => OFF`, `service ON + dry-family requested compound => DRY`, `service ON + wet-family requested compound => WET`),
  - external non-plugin MFD changes outside AUTO now flow back into plugin mode via bounded reconciliation (no per-tick twitching/spam).
- Preserved AUTO as ownership mode:
  - failed bounded AUTO enforcement attempts no longer imply mode collapse,
  - AUTO now publishes info-only unconfirmed feedback/logging (`TYRE AUTO UNCONFIRMED` + info logs) while retaining AUTO mode.
- Kept existing guardrails intact:
  - mode cycle unchanged (`OFF -> DRY -> WET -> AUTO -> OFF`),
  - all-four tyre service truth remains aligned with `Pit.ToggleTyresAll`,
  - command transport ownership remains inside existing `PitCommandEngine` seam.
### 2026-04-22 — v1.1 documentation sweep (release-prep, no code changes)
- Classification: **both** (public release-note/doc refresh + internal canonical-doc map alignment).
- Refreshed root `CHANGELOG.md` unreleased `v1.1` section so staged release notes are complete, concise, and quickly scannable for users.
- Reviewed and updated core user docs:
  - `Docs/User_Guide.md` pit/custom command section now points explicitly to the user-facing and subsystem-owning pit command docs.
  - `Docs/Quick_Start.md` pit setup section now includes direct links for full usage + technical ownership references.
- Added canonical subsystem documentation for the combined pit command stack:
  - new `Docs/Subsystems/Pit_Commands_And_Fuel_Control.md` covering built-in pit actions, custom messages, transport semantics, Pit Fuel Control ownership, and Tyre Control ownership.
- Updated index/status alignment:
  - added the new subsystem doc to `Docs/Project_Index.md`,
  - recorded the sweep in `Docs/RepoStatus.md`.

### 2026-04-22 — Pit Fuel Control v2 redesign (AUTO-owned + MFD-derived OFF/MAN + edge-trigger external cancel)
- Classification: **both** (driver-visible pit-fuel workflow contract change + internal ownership/cancel seam redesign).
- Reworked `PitFuelControlEngine` mode ownership contract:
  - AUTO remains plugin-owned.
  - OFF/MAN are no longer plugin-owned states and now derive from iRacing MFD fuel-enable telemetry (`dpFuelFill`) whenever AUTO is not active.
- Replaced prior mismatch/baseline-driven AUTO cancel with edge-trigger external-change detection:
  - monitors both requested fuel (`PitSvFuel`) and MFD fuel-enable (`dpFuelFill`) edges while AUTO is active/armed,
  - treats changes as plugin-owned only inside plugin suppression windows caused by plugin raw `#fuel` sends or explicit plugin `Pit.ToggleFuel` action sends,
  - all other edges are external and trigger one-shot AUTO cancel (`AUTO CANCELLED`) with `Source=STBY` + `AutoArmed=false`.
- Preserved existing source selection and AUTO send invariants:
  - lap-triggered AUTO send cadence unchanged,
  - `PLAN` remains blocked from active AUTO operation (AUTO + PLAN forces STBY/disarmed),
  - explicit `PUSH/NORM/SAVE` source selection in `AUTO + STBY` immediately re-arms AUTO and runs normal send path.
- Kept `Telemetry.IsOnTrackCar` edge reset seam and updated reset semantics:
  - reset still forces `Source=STBY` + `AutoArmed=false`,
  - reset now clears plugin-owned suppression/ignore windows and observed-edge baselines,
  - reset no longer forces synthetic plugin-owned OFF/MAN mode state.
- Updated pit toggle ownership seam:
  - `PitCommandEngine.Execute(...)` now returns send-attempt success so `LalaLaunch.PitToggleFuel()` can notify Pit Fuel Control of explicit plugin-owned toggle sends.
- Documentation sync updated for dash/user/internal contracts (`Pit_Assist`, Dash Integration subsystem notes, SimHub parameter inventory, repo status).

### 2026-04-22 — Tyre Control v1 follow-up (service-state truth + bounded retry + Pit Commands UI tidy-up)
- Classification: **both** (driver-visible pit-control contract correction + internal retry hardening + settings UI cleanup).
- Tyre service truth for `PitTyreControlEngine` now reuses the same all-four tyre-selection seam used by `Pit.ToggleTyresAll` confirmation (`PitCommandEngine.ReadTyresAllState(...)`) instead of `IsAnyTireChangeSelected(...)`.
- Tyre control ON/OFF contract is now explicit and aligned to `Pit.ToggleTyresAll`: ON means all four tyre-change flags are selected; OFF means not all four are selected.
- Compound retry/send suppression is now bounded even when local raw send fails: each attempt still consumes retry budget and starts cooldown, preventing per-tick hammering on failed local send paths.
- Settings → Pit Commands UI tidy-up:
  - removed preview-only `Auto-focus iRacing before pit/custom message send` toggle row,
  - tyre control section now exposes only `LalaLaunch.Pit.TyreControl.ModeCycle` in plugin settings.
- Direct tyre actions (`SetOff`/`SetDry`/`SetWet`/`SetAuto`) remain registered and usable via SimHub Controls & Events / Dash Studio; this follow-up is UI cleanup, not action removal.

### 2026-04-22 — Plugin-owned tyre control v1 (OFF / DRY / WET / AUTO)
- Classification: **both** (new user-facing pit-control feature with internal control/verification seams).
- Added focused `PitTyreControlEngine` ownership for persistent tyre mode state (`OFF`, `DRY`, `WET`, `AUTO`) with mode-cycle contract `OFF -> DRY -> WET -> AUTO -> OFF`.
- Tyre mode now reuses the existing `Telemetry.IsOnTrackCar` edge reset seam and session reset seams to force mode back to `OFF` on re-entry/transition.
- Runtime behavior contract: `OFF` enforces tyre service OFF; `DRY`/`WET` enforce service ON + GT3-first compound request targeting; `AUTO` enforces service ON and follows `Telemetry.WeatherDeclaredWet` for requested DRY/WET targeting.
- Command dispatch stays on the upgraded plugin-owned pit transport seam (`PitCommandEngine` raw/built-in paths only); no local/raw chat injection path was added in tyre control.
- Added minimal dash-facing exports `Pit.TyreControl.Mode` and `Pit.TyreControl.ModeText`, plus new plugin actions (`ModeCycle`, `SetOff`, `SetDry`, `SetWet`, `SetAuto`) and Settings → Pit Commands control surface rows.
- Added bounded observability log for compound-change attempts including `PitSvTireCompound`, `PlayerTireCompound`, `WeatherDeclaredWet`, and `DriverTires01/02.TireCompoundType` context.

### 2026-04-22 — PR follow-up: PB readback wet/dry fallback on unvalidated best-lap events
- Classification: **both** (driver-visible PB condition routing correction + internal gate hardening).
- In `LalaLaunch.cs` 500ms PB refresh path:
  - condition-only PB readback now uses current `_isWetMode` whenever the best-lap event does not validate against the current accepted lap handoff (`lapValidForPb == false`),
  - validated best-lap events continue using accepted-lap wet latch for write/read consistency.
- Prevents stale-latch dry/wet routing from publishing PB seconds from the wrong surface condition around tight timing gates and restore windows.

### 2026-04-22 — Wet lap-time / wet PB persistence write-path audit follow-up
- Classification: **both** (driver-visible wet persistence correction + internal routing gate hardening).
- Hardened accepted-lap wet/dry routing consistency in `LalaLaunch.cs`:
  - latched accepted-lap wet mode and reused it for downstream PB write-path gating (`TryUpdatePBByCondition(...)`) and condition-only PB readback.
  - broadened wet tyre detection to treat positive `PlayerTireCompound` values as wet (`>0`) so wet-mode routing does not silently stay dry when telemetry uses non-zero wet compound variants.
- Fixed pace persistence coupling:
  - moved profile avg-lap (`AvgLapTimeDry/Wet`) persistence to the pace-accepted path instead of the fuel-accepted path, restoring wet lap-time persistence/feed when fuel delta validation rejects a lap.
- Fixed wet PB relearn-after-clear acceptance:
  - `TryUpdatePBByCondition(...)` now treats cleared/non-positive baseline PB values as unavailable so first valid wet PB candidate can persist after a clear.

### 2026-04-22 — Pit Fuel Control polish: feedback-only max-fill wording
- Classification: **both** (driver-visible pit-fuel feedback wording polish + internal transport-behavior safety correction).
- Kept command ownership, transport mode behavior, and AUTO cancel semantics unchanged.
- Reverted Pit Fuel Control transport clamping in `PitFuelControlEngine` so plugin-owned outgoing `#fuel` sends continue using original transport behavior (override path remains additive max-overshoot; non-override path sends requested target litres).
- Replaced transport clamping with feedback-only max-fill polish:
  - when the requested/sent litres exceed current tank space (or the existing max-override path is active), feedback now uses short `FUEL MAX`,
  - otherwise Pit Fuel Control keeps normal litres-based feedback strings.
- Refined feedback-only max-fill detection to compare requested litres against raw tank-space telemetry (not rounded-up space), so sub-1L overshoot cases still report `FUEL MAX` consistently.
- Preserved AUTO/manual send cadence, source/mode semantics, and existing command ownership contracts.
### 2026-04-22 — PreRace follow-up: explicit one-stop pit-refill feasibility helper
- Classification: **internal-only** (code-path clarity/refactor; one-stop status behavior unchanged).
- Added `IsOneStopFeasibleForPreRace(...)` in `LalaLaunch.cs` so one-stop feasibility is explicitly evaluated against pit-stop refill capacity (effective tank at stop) and second-stint fuel demand (`total needed - start fuel`).
- Kept scenario-first PreRace decision flow and all status text/colour outcomes unchanged.

### 2026-04-21 — PR follow-up: one-stop feasibility now uses pit-stop refill capacity
- Classification: **both** (driver-visible PreRace status correction + docs truth sync).
- Corrected the one-stop feasibility gate in `LalaLaunch.PreRace.StatusText` evaluation:
  - feasibility no longer uses race-start free tank space (`tank - currentFuel`) as the stop-fill limit,
  - feasibility now uses effective stop refill capacity (effective tank capacity at the stop) before underfuel/overfuel checks.
- Kept the existing scenario-first status structure and all other PreRace status behavior unchanged.

### 2026-04-21 — PreRace v2 scenario-first decision rewrite + live fuel-delta/source contract fixes
- Classification: **both** (driver-visible PreRace behavior corrections + internal match-tolerance update).
- Reworked PreRace status to a strict scenario-first model:
  - classify required strategy from stints (`<=1.0` no-stop, `<=2.0` one-stop, `>2.0` multi-stop),
  - evaluate selected strategy against that required strategy with mutually exclusive outcomes (no-stop/one-stop/multi-stop paths no longer fall through to incorrect green states).
- Corrected one-stop feasibility validation:
  - added tank-cap constrained feasibility gate (`fuel still needed > max fuel add possible` => `ONE STOP NOT POSSIBLE` red),
  - retained underfuel/overfuel handling with overfuel still gated at `> 2x` contingency.
- Updated Auto semantics so it always represents required strategy behavior and never emits manual-only mismatch caution.
- Fixed PreRace live fuel delta behavior on grid by feeding PreRace with raw telemetry pit-fuel request seam so one-stop deltas move immediately when the driver changes requested fuel.
- Tightened Auto source-label contract:
  - Auto source labels now remain runtime-owned (`live`/`profile`/`fallback`) and do not show planner labels,
  - manual modes keep planner-owned labels where applicable.
- Relaxed planner/live match tolerances used by shared session-match helper:
  - timed races from ±0.10 min to ±1.0 min,
  - lap races from ±0.01 lap to ±1.0 lap.
### 2026-04-21 — Plugin-owned `ClassBest.*` export family (player-class session-best holder)
- Classification: **both** (new driver-visible export family + internal seam reuse/contract sync).
- Added plugin-owned class session-best holder exports in `LalaLaunch.cs`:
  - `ClassBest.Valid`, `ClassBest.CarIdx`,
  - `ClassBest.Name`, `ClassBest.AbbrevName`, `ClassBest.CarNumber`,
  - `ClassBest.BestLapTimeSec`, `ClassBest.BestLapTime`,
  - `ClassBest.GapToPlayerSec`.
- Kept class-best holder resolution on the existing simplified trusted seam:
  - reused `TryResolveClassSessionBestLap(...)` (same seam feeding `H2H*.ClassSessionBestLapSec` and session-best-in-class color logic),
  - no new class-resolution system or metadata-heavy authority logic was introduced.
- Reused existing seams for identity and gap semantics:
  - identity uses existing session-info/native helpers (`TryGetCarIdentityFromSessionInfo`, `TryGetCarDriverInfo`),
  - gap uses the same preferred live-gap semantics as `ClassLeader.GapToPlayerSec` via shared `ResolveClassGapToPlayerSec(...)` (`TryGetCheckpointGapSec` sane window first, then progress/pace fallback).
- Added explicit fail-safe unresolved output contract and reset behavior:
  - `Valid=false`, `CarIdx=-1`, empty strings, `BestLapTimeSec=0`, `BestLapTime="-"`, `GapToPlayerSec=0`.

### 2026-04-21 — Direct pit/custom transport chat-state sequencing hardening
- Classification: **both** (driver-visible pit/custom transport behavior fix + internal observability/sequencing hardening).
- Kept transport surface and ownership unchanged (`Auto`, `Direct message only`, `Legacy foreground SendInput only`; plugin-owned actions unchanged).
- Hardened direct `postmessage` sequencing in `PitCommandEngine` for chat state uncertainty:
  - added explicit staged direct-path logs for open/type/submit attempts and abort reason context,
  - introduced bounded `state-maybe-open` guard that suppresses re-sending `T` when previous direct attempt may have already opened chat,
  - increased open->type delay to reduce first-press “chat-open only” sequencing races.
- Added partial-state safety guard in `Auto` mode:
  - when direct path has already mutated chat state and then fails, legacy `sendinput` fallback is suppressed (`fallback_suppressed=true reason=postmessage-partial-state-unsafe`) to prevent second-path chat corruption/duplicate-open behavior.
- Preserved existing non-goals:
  - no autofocus/focus-steal work added,
  - no generic duplicate-send retry path added,
  - stateful before/after telemetry confirmation ownership unchanged.

### 2026-04-21 — Pit/custom transport follow-up: truthful direct-message confirmation semantics
- Classification: **both** (observability/log wording and docs-truth sync; no transport-order or action-surface redesign).
- Kept transport order unchanged in `PitCommandEngine`:
  - `Auto` still tries direct `PostMessage` first,
  - legacy foreground `SendInput` fallback still runs only on bounded direct-path failure reasons,
  - no new retry/double-send path after queued direct-message success was added.
- Tightened pit-command audit semantics so logs distinguish transport attempt from confirmed effect:
  - stateful built-ins now log `delivery=<verified|unverified>` + `effect-confirmed=<true|false>` using existing before/after telemetry confirmation authority,
  - custom-message, raw-command, and stateless built-in success logs now explicitly report `delivery=unverified effect-confirmed=false` instead of implying proven in-sim execution.
- Updated pit/custom transport docs (`SimHubLogMessages`, `Dash_Integration`, `Pit_Assist`, `User_Guide`, `Quick_Start`, `README`, `RepoStatus`) to document the same truth model and explicitly note why no generic postmessage-success -> sendinput retry was added (duplicate-send risk).

### 2026-04-21 — PR follow-up: simulator-only iRacing process matching for pit/custom transport
- Classification: **internal-only** (transport target-authority hardening; no new settings, actions, or user workflow changes).
- Tightened `PitCommandEngine.IsIracingProcessName(...)` from broad `"iRacing"` substring matching to simulator-only executable matching (`iRacingSim64DX11`, case-insensitive).
- Kept transport architecture unchanged while hardening both existing callers through the shared helper: `IsIracingForeground()` and `TryResolveIracingMainWindow(...)` now reject launcher/UI companion processes and only treat simulator process identity as authoritative.

### 2026-04-21 — Pit/custom transport upgrade: direct window-message first with bounded legacy fallback
- Classification: **both** (driver-visible transport mode setting + bounded transport/observability behavior change).
- Added a bounded transport-selection seam inside `PitCommandEngine` (no action-surface ownership change):
  - new default mode `Auto` tries direct iRacing window-message transport first (`WM_KEYDOWN/UP T` -> `WM_CHAR` text -> `WM_KEYDOWN/UP Enter`),
  - on direct-path failure, `Auto` falls back once to legacy foreground `SendInput`,
  - added explicit mode controls: `Legacy foreground SendInput only` and `Direct message only`.
- Kept command ownership and behavior seams unchanged:
  - built-in pit actions, custom messages, and raw pit-command sends still route through existing plugin-owned entry points,
  - stateful toggle confirmation remains telemetry before/after authoritative,
  - short-lived `Pit.Command.*` feedback exports remain unchanged.
- Added transport observability without per-tick spam:
  - send attempts now log `transport=<postmessage|sendinput>`,
  - fallback path logs `fallback_from=postmessage` with explicit `reason=...`,
  - failure lines now include transport reason context (`no-iracing-process`, `no-iracing-window`, `not-foreground`, etc.).
### 2026-04-21 — PreRace PR follow-up (project compile include + Auto one-stop status path)
- Classification: **both** (compile-fix project file inclusion + dash-visible Auto status correctness).
- Added `PlannerLiveSessionMatchHelper.cs` to explicit `<Compile Include=...>` items in `LaunchPlugin.csproj` so non-SDK project builds include the new shared helper/snapshot types.
- Corrected PreRace Auto status branching so `> 1.0` and `<= 2.0` stints returns `SINGLE STOP OKAY` instead of falling through to `NO STOP OKAY`.
- Kept all other PreRace decision-tree behavior unchanged (`<= 1.0` no-stop path and `> 2.0` existing multi-stop handling remain as implemented).

### 2026-04-21 — PreRace PR follow-up (accessibility + mismatch comparable-input gate)
- Classification: **both** (compile-fix API accessibility correction + dash-visible PreRace caution gating refinement).
- Changed `FuelCalcs.GetPlannerSessionMatchSnapshot()` from `public` to `internal` so method/type accessibility is consistent with internal `PlannerLiveSessionMatchSnapshot` and `CS0050` is avoided.
- Tightened non-Auto `STRATEGY MISMATCH` emission to comparable-input mismatches only (`plannerMatchResult.HasComparableInputs && !plannerMatchResult.IsMatch`) so transient missing planner/live values do not mask more actionable fuel status outputs.
- Kept all other PreRace decision outputs and colour contract behavior unchanged.

### 2026-04-21 — PreRace authority refresh + shared planner/live validity seam
- Classification: **both** (runtime/dash-visible status behavior + internal seam cleanup).
- Extracted a focused shared planner/live session match helper (`PlannerLiveSessionMatchHelper`) covering car, track, basis (time/lap), and race-length tolerance checks.
- Switched `PitFuelControlEngine` PLAN validity to consume the shared helper (no private duplicate match logic).
- Added `FuelCalcs.GetPlannerSessionMatchSnapshot()` to provide planner-side identity/basis/race-length inputs; includes a bounded fallback to last loaded planner track key to reduce transient false mismatch windows when planner track rows refresh.
- Refreshed PreRace Auto authority:
  - race length now follows live session definition seams first (`CurrentSessionInfo._SessionTime` or `_SessionLaps`);
  - fuel/lap source now follows runtime stable seams first (`LiveFuelPerLap_Stable`, `ProjectionLapTime_Stable`) with fallback only when needed.
- Replaced coarse PreRace status with richer decision outputs + dash color contract:
  - `LalaLaunch.PreRace.StatusText` now emits specific decision strings (including mismatch/max-fuel/overfuel/underfuel states),
  - added `LalaLaunch.PreRace.StatusColour` (`green`/`orange`/`red`).
- Non-Auto PreRace now emits orange `STRATEGY MISMATCH` (never green) when planner/live combo+basis+race-length do not match, while still computing planner-intent values.
- Added conservative overfuel warning rule: `OVERFUELLED` triggers only when excess fuel exceeds `2x` configured contingency.
### 2026-04-21 — Pit Fuel Control external reset simplification (`IsOnTrackCar` edge-only)
- Classification: **both** (driver-visible pit-fuel lifecycle reset behavior change + internal reset-trigger seam simplification).
- Removed pit-fuel-specific external reset triggers in `LalaLaunch` that were tied to:
  - session type name change tracking,
  - SessionState transition tracking (`1 -> 2`),
  - associated cached previous-session-type / previous-session-state fields.
- Replaced the above with a single external lifecycle trigger:
  - `DataCorePlugin.GameRawData.Telemetry.IsOnTrackCar` boolean edge detection only.
  - On either edge (`false -> true` or `true -> false`), Pit Fuel Control is reset to inert `OFF + STBY` via existing `ResetToOffStby()`.
- Intentionally left internal guardrails unchanged (AUTO cancel behavior, OFF/STBY guardrails, suppression rules, and iRacing AutoFuel ownership behavior).
### 2026-04-21 — PR follow-up: bound Strategy live-cap fallback path
- Classification: **internal-only** (runtime authority-seam correctness; no UI/workflow change).
- Tightened `TryGetRuntimeLiveCapForStrategy(...)` so stale cached live-cap values cannot bypass fallback freshness gating:
  - removed unbounded `LiveCarMaxFuel` return path,
  - removed overlapping `EffectiveLiveMaxTank` return path,
  - Strategy live-cap authority is now a single bounded seam: `raw -> bounded fallback -> unavailable`.

### 2026-04-21 — Runtime health checks + planner-safe fuel recovery (session-transition stability)
- Classification: **both** (driver-visible Strategy live-cap/session-recovery stability + internal runtime seam hardening/observability).
- Added bounded fuel/live-snapshot runtime health checks in `LalaLaunch` with debounced trigger queueing from:
  - session token change,
  - session type change,
  - combo change,
  - car-active edges (ignition/engine start + active-driving edge).
- Added planner-safe targeted fuel recovery path:
  - re-reads runtime-authoritative live max tank seam,
  - refreshes Strategy live max display + snapshot,
  - avoids planner/manual/preset clobber on normal manual recovery.
- Manual recovery action path now attempts planner-safe targeted fuel recovery first before broad reset logic.
- Unified Strategy live-cap authority with runtime seam:
  - `FuelCalcs` now consumes plugin runtime live-cap authority (`raw -> bounded fallback`) instead of direct-only raw read,
  - added debounced strategy/runtime health logs for live-cap source/value transitions.
- Removed automatic planner manual-override reset from the fuel-model session reset path to preserve planner intent during runtime/session re-arm.
### 2026-04-21 — Class-resolution simplification: trusted-property authority + shared class leader/class-best seams
- Classification: **both** (driver-visible class leader / class-best / finish consistency improvement + internal cleanup removing failed metadata-heavy seams).
- Replaced overcomplicated class authority logic with one trusted runtime seam:
  - `DataCorePlugin.GameData.HasMultipleClassOpponents` is now the single-class vs multiclass switch.
  - Single-class path now bypasses class matching entirely and uses overall leader/field-best directly.
- Unified class leader resolution across all consumers with one helper seam:
  - single-class leader uses `CarIdxPosition == 1`,
  - multiclass leader uses `CarIdxClassPosition == 1` filtered to player class.
- Unified class-best/session-best-in-class seam:
  - single-class scans whole field,
  - multiclass scans only player-class cars.
- `Race.ClassLeaderHasFinished*` now consumes the same resolved class-leader seam as `ClassLeader.*` (no separate finish-only class resolver).
- Removed recent failed/duplicated class-resolution pieces from runtime paths:
  - removed class metadata cache-driven authority helpers (`RefreshClassMetadata`, `GetCachedClassShortName`, `IsEffectivelySingleClassSession`, `IsSameEffectiveClass`, enum authority tree),
  - removed class-count (`NumCarClasses`) authority dependencies from H2H/class-best/class-leader/finish flows.
- Kept class membership fallback intentionally minimal in multiclass only:
  - player class identity: `GameData.CarClass` (with bounded player row fallback),
  - candidate class identity: per-car `DriverInfo` class label lookup.

### 2026-04-21 — Wet-condition PB/session-best audit follow-up (condition-only PB reads + LapRef session-best cross-condition guard)
- Classification: **both** (driver-visible wet PB/session-best correctness + bounded LapRef authority seam hardening).
- Fixed remaining wet PB fallback leakage in planner/profile-facing PB reads:
  - strategy/profile PB display paths now use condition-only PB lookup (`wet -> wet only`, `dry -> dry only`) and no longer borrow dry PB when wet PB is absent.
- Kept wet PB persistence path condition-scoped and unchanged in ownership:
  - validated-lap PB updates continue to route `_isWetMode` into `TryUpdatePBByCondition(...)`,
  - wet PB updates persist only wet fields (`BestLapMsWet`, `BestLapSector1..6WetMs`), dry path remains separate.
- Hardened LapRef SessionBest authority sync against cross-condition repopulation:
  - trusted player best-lap seam remains enabled only after current-context capture arming,
  - tick-sync now applies only when authoritative value is near-equal to captured current-context session-best baseline (tight tolerance), preventing a dry global-best seam value from overwriting wet-mode SessionBest after wet reset/capture.

### 2026-04-21 — Final docs sweep: plugin-owned pit commands + custom messages + pit fuel control
- Classification: **both** (user-facing guidance alignment + subsystem/internal contract sync).
- Updated user-facing docs (`README`, `Quick Start`, `User Guide`, `Pit Assist`) to align around final plugin-owned pit/custom workflow:
  - built-in pit commands configured in **Settings → Pit Commands**,
  - custom chat actions configured in **Settings → Custom Messages**,
  - dash/button binding guidance points to plugin-owned action surfaces rather than raw chat strings or legacy `IRacingExtraProperties` pit actions,
  - explicit runtime caveat retained: iRacing focus is currently required for reliable send; auto-focus remains preview/not implemented.
- Updated dash/internal contract docs to keep action/export surfaces and persistence wording aligned with final state:
  - `Docs/Subsystems/Dash_Integration.md` pit/custom action guidance and settings ownership,
  - `Docs/Internal/SimHubParameterInventory.md` pit/custom action-surface note.
- No runtime behavior redesign was introduced; task was bounded to docs-truth sync.

### 2026-04-21 — Profile-track PB clear polish: immediate PB text refresh + warning colour + compact label
- Classification: **both** (profile editor UX polish + internal property-refresh correctness in the PB clear path).
- Updated `ClearBestLapAndSectorsForCondition(...)` to mirror relearn visual refresh behavior:
  - clears condition PB value + PB updated metadata,
  - clears the condition PB display-text cache,
  - raises `BestLapTimeDryText` / `BestLapTimeWetText` property changed immediately.
- Updated profile-track PB clear buttons to use compact label text: `Clear PB Data` in both dry and wet sections.
- Updated condition `No sector data` status text colour to an orange warning-style tone for clearer visibility in the editor.
### 2026-04-21 — PR #584 follow-up: custom-message load normalization preserves saved rows
- Classification: **internal-only** (settings JSON load/normalization correction; no action binding or runtime dispatch contract change).
- Removed eager default slot prepopulation from `LaunchPluginSettings.CustomMessages` so Json.NET deserialization no longer appends loaded rows after an already-populated default set.
- Kept `NormalizePitCommandSettings(...)` as the single default-slot authority:
  - if `CustomMessages` is missing/null/undersized, it now creates/fills exactly `CustomPitMessageSlotCount` slots,
  - if loaded slots exist, they remain in-place and are normalized (slot numbering/default field fill) without being displaced by preexisting defaults.
- Fix outcome: saved custom-message label/text rows survive restart, while fresh installs and missing settings still receive the expected 10 default slots.

### Pit Fuel Control follow-up: zero transport, AUTO ownership cancel redesign, OFF/STBY reset semantics, offline suppression
- Updated `PitCommandEngine` zero-fuel transport payloads to `#fuel 0.01$` for both:
  - dedicated `Pit.FuelSetZero`,
  - ZERO phase of `Pit.FuelSetMax` toggle.
- Reworked `PitFuelControlEngine` AUTO cancel trigger to movement-based ownership:
  - while AUTO is active/armed, requested-fuel movement is tracked from live telemetry (`PitSvFuel`),
  - movement outside the plugin send-suppression window is treated as external ownership and cancels AUTO once.
- Removed stale-baseline dependency for AUTO cancel:
  - cancellation no longer requires mismatch against prior `LastSentFuelLitres`.
- Updated AUTO cancel post-state to truthful inert ownership:
  - cancel/disengage now forces `OFF + STBY` with `AutoArmed=false`.
- Added iRacing AutoFuel ownership guard:
  - plugin AUTO does not coexist with native AutoFuel; active AUTO is cleanly cancelled to `OFF + STBY`.
- Added bounded reset triggers in `LalaLaunch` for pit fuel control state:
  - session type name change => `OFF + STBY`,
  - SessionState transition `1 -> 2` => `OFF + STBY`,
  - explicit non-trigger: `2 -> 3` does not reset.
- Added Offline Testing suppression:
  - pit fuel control is suppressed/inert and forced to `OFF + STBY` (no active control behavior).
- Classification: **both** (driver-visible pit-fuel command/cancel/reset behavior changes + internal ownership-contract hardening/docs alignment).
### 2026-04-21 — LapRef profile-track PB sector UI + H2H unresolved log spam follow-up
- Classification: **both** (profile-track editor UX visibility/control update + LapRef/H2H runtime behavior hardening).
- Added profile-track editor controls for condition-specific PB cleanup:
  - new dry action clears only dry PB lap + dry PB sectors (`S1..S6`),
  - new wet action clears only wet PB lap + wet PB sectors (`S1..S6`),
  - both actions are independent from existing `Zero and Relearn` condition resets.
- Added profile-track condition status indicator (`No sector data`) for legacy lap-time-only PB rows:
  - shown only when a condition PB lap exists but complete sector payload is missing.
- Stopped recurring H2H unresolved class-best info spam:
  - unresolved info log is now reason-transition latched (not cadence-repeated),
  - startup single-class metadata-not-ready path is suppressed to avoid noisy no-op diagnostics while keeping fail-safe output behavior.
- LapRef profile-best rematerialization is now condition-only for `LapRef.ProfileBest.*`:
  - wet mode no longer falls back to dry PB reference lookup.
  - session-best capture and reset ownership seams remain unchanged.
### 2026-04-21 — PR #585 review follow-up: unknown class authority no longer defaults to single-class
- Classification: **both** (driver-visible class-best/class-leader correctness during startup metadata windows + internal authority seam hardening).
- Tightened `ResolveSessionClassAuthority(...)` so unknown class-count no longer collapses to `SingleClass` when `HasMultipleClassOpponents` is unavailable/false.
- Updated authority contract:
  - `NumCarClasses == 1` => single-class,
  - `NumCarClasses > 1` => multiclass,
  - unknown class-count + positive `HasMultipleClassOpponents` => multiclass,
  - unknown class-count without positive multiclass hint => unresolved (`Unknown`) fail-safe.
- This prevents transient multiclass startup windows from being misclassified as single-class, so class-best and class-leader consumers avoid cross-class selection until authority is explicit.

### 2026-04-21 — Analysis-first cleanup: native class-resolution seam simplification across H2H/ClassLeader/finish
- Classification: **both** (driver-visible consistency/correctness in class-best/class-leader behavior + internal seam simplification).
- Added a single session class-state authority seam in `LalaLaunch` (`ResolveSessionClassAuthority`):
  - `WeekendInfo.NumCarClasses == 1` => single-class,
  - `WeekendInfo.NumCarClasses > 1` => multiclass,
  - unknown class-count => fallback to `GameData.HasMultipleClassOpponents`.
- Simplified effective same-class matching to one shared rule:
  - explicit single-class sessions always match regardless of class label blank/mismatch/source disagreement,
  - multiclass sessions require usable matching class identity and stay fail-safe on blank/unusable class values.
- Removed stored `_isMultiClassSession` state and aligned finish-path class leader resolution to the same effective same-class seam used by H2H/ClassLeader/session-best-in-class consumers.
- Preserved metadata source ordering and bounded backfill behavior:
  - `DriverInfo.Drivers##` remains preferred,
  - `DriverInfo.CompetingDrivers[*]` remains missing-entry-only backfill (no overwrite).

### PR #584 follow-up: debounce custom-message settings saves
- Kept the `Settings -> Custom Messages` persistence fix from PR #584 intact while changing save timing for slot text edits (`Name` / `MessageText`) to a bounded debounce window (500 ms).
- Custom-message text edits now schedule a delayed save that is flushed after typing settles, instead of writing the full settings file on every keystroke.
- Added explicit pending-save flush points on normal plugin tick and plugin shutdown so the latest settled text persists across SimHub restarts.
- Kept pit/custom command runtime behavior unchanged (`LalaLaunch.CustomMessage01..10` action names, binding surface, and transport/dispatch seams unchanged).
- Classification: **internal-only** (persistence-layer I/O timing polish; no runtime command behavior/UI contract change).

### Custom message settings persistence follow-up
- Fixed settings persistence for `Settings -> Custom Messages` slot edits by hooking `LaunchPluginSettings.CustomMessages` collection/item change events and saving plugin settings on `Name`/`MessageText` changes.
- Startup path now reattaches custom-message persistence hooks immediately after settings load so existing saved slot content is preserved and editable without losing bindings.
- Kept runtime dispatch/contracts unchanged: `LalaLaunch.CustomMessage01..10` action bindings and existing pit/custom command transport logic were not modified.
- Classification: **both** (driver-visible persistence fix + bounded settings-layer internal wiring).
### 2026-04-20 — PR #582 follow-up: native single-class authority precedence over cache diversity
- Classification: **internal-only** (multiclass state authority ordering correction for existing H2H/ClassLeader seams; no new exports/UI).
- Tightened `RefreshClassMetadata(...)` multiclass assignment ordering so explicit native single-class (`WeekendInfo.NumCarClasses == 1`) now wins outright and cannot be overridden by cache string divergence.
- Kept explicit native multiclass authority next (`NumCarClasses > 1`, or unknown class-count with positive `HasMultipleClassOpponents`).
- Restricted cache-proven class diversity (`>1` distinct non-blank class names) to unresolved/unknown class-count state only, so cache evidence assists authority gaps but cannot override explicit native class-count signals.

### PR #582 follow-up: class metadata fallback gating + finish multiclass authority hardening
- Fixed `RefreshClassMetadata(...)` fallback gating so `CompetingDrivers[*]` recovery now runs when `Drivers##` rows exist but still do not provide usable class metadata; this removes unresolved class-best windows caused by late/blank `Drivers##` class fields in live non-race sessions.
- Kept source precedence unchanged: `Drivers##` remains preferred, and fallback only fills missing class entries without overwriting already-resolved class identities.
- Decoupled `_isMultiClassSession` from unresolved class-count state by requiring an explicit multiclass signal (`NumCarClasses > 1` or unknown class-count + positive `HasMultipleClassOpponents`) instead of inferring multiclass from `!IsEffectivelySingleClassSession(...)`.
- This preserves fail-safe class-best matching semantics (unknown class-count is still non-single-class for blank-class matching) while avoiding finish-path multiclass side effects during metadata startup.
- Classification: **both** (driver-visible class-best availability and finish-path correctness + internal seam hardening).

### PR #582 follow-up: require explicit single-class authority before blank-class fallback
- Tightened `IsEffectivelySingleClassSession(...)` to require an explicit native single-class signal (`WeekendInfo.NumCarClasses == 1`) before allowing blank-class same-class fallback in H2H/ClassLeader class-best resolution.
- Unknown class-count state (`NumCarClasses <= 0`/unavailable) now stays fail-safe as unresolved/non-single-class even when `GameData.HasMultipleClassOpponents` has not populated yet, preventing transient whole-field class-best selection in multiclass sessions.
- Kept existing multiclass fail-safe behavior unchanged (`blank_class_identity_multiclass`/unresolved paths remain authoritative).
- Classification: **both** (driver-visible H2H/ClassLeader class-best safety correction + internal seam hardening).

### H2H/ClassLeader class-best seam hardening across Practice/Quali/Race + single-class blank-class fallback
- Restored native class-best/session-best-in-class resolution in live opponent-eligible sessions (Practice, Open Qualify, Lone Qualify, Qualifying, Race) by refreshing class metadata before class-best consumers run in the tick path.
- Broadened class metadata cache population to prefer `DriverInfo.Drivers##` with `CompetingDrivers[*]` fallback, removing race-like timing assumptions from the class map seam.
- Added a narrow effective same-class resolver used by `ComputeH2HClassSessionBestLapSec(...)`, `TryResolveClassSessionBestLap(...)`, and `IsNewSessionBestInClass(...)`:
  - primary single/multiclass authority: `SessionData.WeekendInfo.NumCarClasses`,
  - secondary supporting hint: `GameData.HasMultipleClassOpponents`,
  - blank class identity allowed only in defensibly single-class sessions,
  - multiclass + blank class identity remains fail-safe (no class collapse).
- Kept H2H magenta class-best coloring and `ClassLeader.*` exports on the same restored native seam (no ExtraProperties dependency introduced).
- Added bounded H2H info logging for class-best resolution failure reasons (missing/late metadata, blank identity in multiclass, no valid best laps yet) without per-tick spam.
- Classification: **both** (driver-visible class-best/class-leader behavior restoration + internal seam hardening/observability).

### Class leader export family: plugin-owned `ClassLeader.*` for all live sessions
- Added a new plugin-owned `ClassLeader.*` export family in `LalaLaunch`:
  - `ClassLeader.Valid`, `ClassLeader.CarIdx`
  - `ClassLeader.Name`, `ClassLeader.AbbrevName`, `ClassLeader.CarNumber`
  - `ClassLeader.BestLapTimeSec`, `ClassLeader.BestLapTime`
  - `ClassLeader.GapToPlayerSec`
- Kept class-leader authority native-only and session-best-only by reusing/extending the existing class-best seam from `_carSaBestLapTimeSecByIdx`:
  - shared resolver now returns both class-best lap time and the winning `carIdx`,
  - H2H class session-best lap-time path continues to consume the same class-best authority.
- Kept identity resolution on the existing native/session-info seam already used elsewhere (`TryGetCarIdentityFromSessionInfo` + `TryGetCarDriverInfo`).
- Mirrored existing repo live-gap semantics for `GapToPlayerSec`:
  - prefer CarSA checkpoint gap (`TryGetCheckpointGapSec`, sane `abs <= 30s`),
  - fallback to native progress/pace gap (`|Δlaps| * paceRef`) consistent with Opponents track-gap style.
- Session eligibility follows existing live opponent session policy (Practice, Open Qualify, Lone Qualify, Qualifying, Race) and is explicitly not race-only.
- Added fail-safe publication contract when unresolved/no valid class-best lap exists (`Valid=false`, `CarIdx=-1`, identity empty, best lap `0`/`"-"`, gap `0`).
- Classification: **both** (new driver-visible export family + internal seam extension/docs alignment).

### PR review follow-up: gate LapRef SessionBest authority by active context
- Added LapRef session-best authority re-arm gating in `LapReferenceEngine` so trusted `playerBestLapTimeSec` synchronization only applies after a current-context session-best baseline has been captured.
- On LapRef context reset (session/token/type/car/track/wet-dry changes), session-best authority sync is now disarmed to prevent stale prior-session best-lap carry-over from suppressing first valid in-session SessionBest sector capture.
- First valid in-context `CaptureValidatedLap(...)` session-best capture re-arms authority, preserving the intended split:
  - trusted seam continues to drive `LapRef.SessionBest.LapTimeSec` when context-valid,
  - LapRef retains ownership of session-best sector snapshot payload (`S1..S6*`).
- Classification: **both** (driver-visible new-session SessionBest capture correctness + internal LapRef authority-seam hardening/docs alignment).

### LapRef follow-up: trusted SessionBest authority + rollover parity cleanup
- Fixed remaining LapRef SessionBest timing lag by decoupling SessionBest lap-time authority from LapRef-local validated-lap latching:
  - `LapRef.SessionBest.LapTimeSec` is now synchronized each tick from the same trusted player best-lap seam used by H2H/core (best-lap authority path already resolved in `LalaLaunch`),
  - LapRef still owns SessionBest snapshot sectors (`S1..S6*`) from validated-lap captures.
- Reduced LapRef-local rollover lifecycle state by removing lap-ref-advance latch dependency from current-lap compare re-arm, keeping rollover re-arm driven by live active-segment wrap behavior.
- Preserved invariants:
  - H2H behavior unchanged,
  - CarSA remains sole fixed-sector owner,
  - compare/cumulative outputs remain current-lap-only truth,
  - PB persistence ownership and legacy lap-time-only PB compatibility unchanged.
- Classification: **both** (dashboard-visible SessionBest timing/rollover parity correction + internal seam simplification/docs alignment).

### LapRef refactor: remove local player display lifecycle and reuse H2H/core player seams
- Refactored `LapReferenceEngine` player-side behavior to reuse the same trusted player seams already proven in H2H/core instead of maintaining a competing LapRef-local player display lifecycle.
- `LapRef.Player.LapTimeSec` now publishes from the same trusted player last-lap seam used by H2H/core (CarIdx last-lap authority path passed through `LalaLaunch`), removing prior dependency on LapRef-validated snapshot latching for player-row lap-time display.
- Player sector row publication now consumes live CarSA fixed-sector cache directly each tick (H2H-style cache consumption) rather than a bespoke LapRef-local display/rollover snapshot model.
- Removed redundant LapRef-local player-display machinery:
  - removed the dedicated live player display snapshot state,
  - removed redundant validated-player snapshot usage from player-row publication.
- Kept only minimal LapRef-owned current-lap comparable state for truthful compare/cumulative outputs, with rollover/lap-advance re-arm preserved.
- Kept LapRef ownership boundaries intact:
  - SessionBest/ProfileBest remain static LapRef-owned reference rows,
  - compare/cumulative outputs remain current-lap truthful,
  - PB persistence seam ownership remains unchanged.
- Classification: **both** (player-row behavior parity correction + internal architecture/docs alignment).

### PR #577 review follow-up: clear AUTO baseline on AUTO -> OFF
- Updated `PitFuelControlEngine.ModeCycle` AUTO disable branch to clear stale AUTO baseline state:
  - `AUTO -> OFF` now sets `LastSentFuelLitres=-1` and `Source=STBY` in addition to `Mode=OFF` and `AutoArmed=false`.
- Kept existing behavior unchanged:
  - cycle order remains `OFF -> MAN -> AUTO -> OFF`,
  - forced-STBY MAN->AUTO guardrails remain intact,
  - feedback remains `FUEL MODE OFF` with no extra message.
- Intent preserved: disabling AUTO now leaves an inert baseline so re-entry cannot compare against stale prior AUTO sent liters.
- Classification: **both** (driver-visible AUTO disable/re-entry correctness + internal contract/docs alignment).

### Pit Fuel Control ModeCycle follow-up: restore AUTO -> OFF
- Corrected `PitFuelControlEngine.ModeCycle` cycle order so mode can be disabled again:
  - `OFF -> MAN`
  - `MAN -> AUTO`
  - `AUTO -> OFF`
- Kept existing forced-STBY AUTO guardrail semantics unchanged:
  - `MAN -> AUTO` when source is `PLAN` still forces `Source=STBY`, keeps `AutoArmed=false`, and publishes `FUEL AUTO STBY`.
- `AUTO -> OFF` now clears `AutoArmed` and publishes `FUEL MODE OFF` without sending a pit command.
- Classification: **both** (driver-visible control-cycle fix + internal contract/docs alignment).

### PR #576 follow-up: FuelSetMax ZERO-phase tank-full bypass + forced-STBY feedback refinement
- Corrected `PitFuelControlEngine.ModeCycle` PLAN-forced-STBY AUTO transition arming:
  - `MAN -> AUTO` when source is `PLAN` now sets `Mode=AUTO`, `Source=STBY`, and `AutoArmed=false` (prevents immediate `AUTO CANCELLED` checks before source reselection).
- Corrected `PitFuelControlEngine.ModeCycle` STBY-to-AUTO arming:
  - `MAN -> AUTO` when source is already `STBY` now keeps `AutoArmed=false` and publishes `FUEL AUTO STBY` (prevents immediate self-cancel checks while waiting for a live source reselection).
- Fixed `PitCommandEngine` tank-full short-circuit gating for `Pit.FuelSetMax` so it is phase-aware:
  - MAX phase (`#fuel +150`) still uses the existing fuel-add short-circuit,
  - ZERO phase (`#fuel 0`) now always transports even when tank space is near zero/full.
- Kept accepted semantics unchanged:
  - `Pit.Command.FuelSetMaxToggleState` still flips on every press (including later transport failure),
  - `LastAction` / `LastRaw` / normal pit-command transport seam remain unchanged.
- Refined forced-STBY `ModeCycle` feedback text in `PitFuelControlEngine` to preserve mode context in one message:
  - `AUTO -> MAN` forced STBY now publishes `FUEL MAN STBY`,
  - `MAN -> AUTO` from `PLAN` forced STBY now publishes `FUEL AUTO STBY`.
- Normal mode-cycle feedback remains unchanged:
  - `OFF -> MAN` => `FUEL MODE MAN`
  - `MAN -> AUTO` (live source: `PUSH`/`NORM`/`SAVE`) => `FUEL MODE AUTO`
- Classification: **both** (driver-visible pit-command/feedback correction + internal contract/docs alignment).

### Pit Fuel Control control-model follow-up (real max toggle + STBY guardrails on mode changes)
- Corrected `Pit.FuelSetMax` to a real transport toggle in `PitCommandEngine`:
  - press 1 sends MAX (`#fuel +150`),
  - press 2 sends ZERO (`#fuel 0`),
  - then alternates MAX/ZERO on every press.
- Kept `Pit.Command.FuelSetMaxToggleState` as the plugin-owned phase export and retained the accepted behavior that phase flips on every press even if transport later fails.
- Updated `PitFuelControlEngine.ModeCycle` guardrails:
  - `AUTO -> MAN` now forces `Source=STBY`,
  - `MAN -> AUTO` while `Source=PLAN` is now allowed, but forces `Source=STBY` instead of hard-blocking/skipping AUTO.
- Kept selection feedback/identity on the existing pit-command seam (`Pit.Command.DisplayText`, `Pit.Command.LastAction`, active message timing), with `ModeCycle` publishing `FUEL SRC STBY` when a forced STBY transition occurs.
- Classification: **both** (driver-visible control-model behavior correction + internal contract/docs alignment).

### PR follow-up: guard LapRef CarIdx authority with freshness check
- Tightened `ResolveLapRefAuthoritativeLapTimeSec(...)` to avoid overriding the validated-gate lap with stale `CarIdxLastLapTime` during one-tick rollover lag.
- When both values are valid and differ beyond a tight freshness tolerance, LapRef now prefers the validated gate candidate for that capture tick; otherwise it keeps CarIdx as authority.
- This keeps LapRef/session-best capture and `_lastValidLapMs` PB handoff aligned to the just-finished lap when rollover telemetry briefly lags.
- Classification: **internal-only** (correctness hardening of existing LapRef authority seam).

### LapRef timing-source alignment + PB trigger/rollover parity fix
- Aligned LapRef validated lap-time capture to the same authoritative player seam trusted by H2H/core (`CarIdxLastLapTime` for player `CarIdx`), with guarded fallback to the validated-gate candidate only when that array seam is unavailable.
- Updated the validated-lap → PB-trigger handoff so `_lastValidLapMs` now latches from that same authoritative captured lap value, eliminating first-beat misses caused by stale/non-authoritative lap-time capture at S/F.
- Kept PB persistence invariants unchanged:
  - PB lap-time update still occurs through existing `TryUpdatePBByCondition(...)` seam.
  - sector fields persist only when real fixed-sector values exist.
  - legacy lap-time-only PB rows (no sectors) remain valid and supported.
- Tightened LapRef player-row rollover feel toward proven H2H behavior by sourcing display refresh directly from live CarSA fixed-sector cache presence (while compare/cumulative truth remains current-lap re-armed only).
- Classification: **both** (runtime LapRef/PB correctness behavior + canonical LapRef/export docs update).
### PR #575 follow-up: preserve pit-command failure feedback on source changes
- Classification: **both** (driver-visible pit command feedback correctness + internal contract alignment).
- Updated `PitFuelControlEngine` source-change paths (`SourceCycle` and direct `SetPush/SetNorm/SetSave`) so selection feedback is only published when no send was attempted (`Mode=OFF`).
- In `Mode=MAN/AUTO`, source-change actions now preserve transport failure feedback (`Pit Cmd Fail`) from the pit-command send path instead of overwriting it with `FUEL SRC ...`.

### Pit Fuel Control action/feedback follow-up (source/mode feedback + direct-select + zero + max-toggle state)
- Updated `PitFuelControlEngine` so source/mode actions now publish through the existing pit-command feedback seam even when no fuel send occurs:
  - `SourceCycle` now always surfaces `FUEL SRC <PUSH|NORM|SAVE|PLAN|STBY>` feedback when selection-only,
  - `ModeCycle` now always surfaces `FUEL MODE <OFF|MAN|AUTO>`,
  - both paths now stamp `Pit.Command.LastAction` with the actual action id (`Pit.FuelControl.SourceCycle` / `Pit.FuelControl.ModeCycle`) on selection-only presses.
- Added direct source-select actions:
  - `Pit.FuelControl.SetPush`
  - `Pit.FuelControl.SetNorm`
  - `Pit.FuelControl.SetSave`
  - In `MAN/AUTO`, these send immediately using the normal raw pit-command path while preserving action identity in `Pit.Command.LastAction`; in `OFF`, they update selection-only feedback.
- Added new plugin-owned pit action `Pit.FuelSetZero` mapped through the existing built-in pit-command transport/feedback seam with short feedback text `FUEL ZERO`.
- Added plugin-owned max-fuel toggle concept state in `PitCommandEngine`:
  - pressing `Pit.FuelSetMax` now flips persistent state `Pit.Command.FuelSetMaxToggleState`,
  - transport/feedback still routes through the normal pit-command seam.
- Classification: **both** (new driver-visible pit action/feedback behavior and internal pit-command/pit-fuel control seam extension).

### PR follow-up: apply Pit Fuel Control contingency before clamp
- Corrected live Pit Fuel Control target composition in `LalaLaunch.BuildPitFuelControlSnapshot` so `NORM/PUSH/SAVE` now apply contingency before the non-negative clamp:
  - `target = max(0, requirement + contingency - currentFuel)` (equivalently `max(0, shortfall + contingency)`),
  - replaces the prior pattern that clamped shortfall first and then added contingency, which could over-command fuel when already above base requirement.
- `NORM` now follows the same direct requirement-minus-current seam as `PUSH/SAVE` via `-Fuel_Delta_LitresCurrent*`, keeping live-source authority consistent and independent from clamp-driven add seams.
- Classification: **both** (runtime pit-fuel target correction + internal contract/docs update).

### Fuel projection phase seam fix (grid/formation) + Pit Fuel Control authority remap
- Corrected runtime fuel projection authority for SessionState `2/3` (grid/formation) in `LalaLaunch.UpdateLiveFuelCalcs`:
  - timed-race lookahead now uses session-definition authority `DataCorePlugin.GameRawData.CurrentSessionInfo._SessionTime` with elapsed `Telemetry.SessionTime` (`remaining = max(0, _SessionTime - SessionTime)`),
  - SessionState `4` keeps normal race-running projection behavior.
- Kept the existing public `Fuel.*` output families unchanged while fixing baseline behavior at the shared projection seam consumed by pit-need and litre-delta paths.
- Remapped Pit Fuel Control live source authority away from `WillAdd` coupling:
  - `NORM` remains non-clamped runtime need (`Fuel.Pit.NeedToAdd` equivalent),
  - `PUSH/SAVE` now use direct non-clamped requirement-minus-current seams (`-Fuel_Delta_LitresCurrentPush/Save`) rather than `WillAdd`-derived expressions.
- Added contingency alignment for Pit Fuel Control live modes using profile track contingency (`FuelCalcs.ContingencyValue` + `IsContingencyInLaps`) with explicit-zero respected (no hidden reserve).
- Verified/retained PLAN contingency seam: `PlannerNextAddLitres` continues to inherit planner contingency from `CalculateSingleStrategy` total/stop planning path.
- Classification: **both** (runtime fuel behavior correction for existing outputs + internal contract/docs updates).

### PR 572 follow-up: Pit Fuel Control correctness fixes
- Tightened PLAN validity in `PitFuelControlEngine` so PLAN now requires all of:
  - planner car identity == live car identity,
  - planner track/layout key == live track/layout key,
  - planner race basis == live race basis (time-limited vs lap-limited),
  - planner race length == live race length (with small tolerance for time-limited comparisons).
- PLAN race-length matching now compares actual strategy fields (`FuelCalcs.RaceMinutes` / `RaceLaps`) against live session seams (`CurrentSessionInfo._SessionTime` / `_SessionLaps`) rather than basis-only gating.
- Replaced implicit MAX magic value usage with explicit overshoot constant on raw command path (`MaxFuelOvershootLitres`), keeping clamp-safe iRacing semantics while avoiding dependency on potentially stale plugin-side max state.
- Added dedicated raw pit-command transport path in `PitCommandEngine` (`ExecuteRawPitCommand`) and moved Pit Fuel Control sends to that path.
- Raw pit-command path now reuses built-in pit-command normalization (`NormalizeChatCommand`) so trailing `$` is handled consistently with existing built-in pit actions before chat injection.
- Classification: **internal-only** (bounded correctness and transport-normalization fixes for existing PR 572 feature surface).

### Pit Fuel Control integration into plugin-owned Pit Command seam
- Added focused `PitFuelControlEngine` to own pit-fuel source/mode state and behavior (`PUSH/NORM/SAVE/PLAN/STBY` + `OFF/MAN/AUTO`) without widening central runtime responsibilities in `LalaLaunch`.
- Added new plugin-owned actions:
  - `Pit.FuelControl.SourceCycle`
  - `Pit.FuelControl.ModeCycle`
- Added new exports:
  - `Pit.FuelControl.Source`
  - `Pit.FuelControl.SourceText`
  - `Pit.FuelControl.Mode`
  - `Pit.FuelControl.ModeText`
  - `Pit.FuelControl.TargetLitres`
  - `Pit.FuelControl.OverrideActive`
- Reused existing pit-command transport/feedback seam via `PitCommandEngine` custom-message dispatch for fuel set sends and user-facing feedback messages.
- Implemented locked behavior contract:
  - immediate send in MAN/AUTO on source change,
  - AUTO lap-cross updates via existing canonical lap-cross seam,
  - PLAN validity gating (live/planner car+track+basis match),
  - live-source MAX override hysteresis (`>1.10` arm, `<1.05` disarm),
  - manual driver pit-fuel override detection while AUTO is active (transitions to `MAN + STBY`),
  - short post-send suppression window to avoid telemetry-lag false positives.
- Classification: **both** (new runtime pit-fuel command behavior and new plugin action/export surface for dashboards).

### LapRef rollover seam fix for transient zero-segment boundary samples
- Tightened `LapReferenceEngine` rollover detection so current-lap compare/cumulative eligibility is re-armed on either:
  - normal wrap (`current > 0 && previous > 0 && current < previous`), or
  - boundary transition into segment `0` from a late-lap segment (`previous > 1 && current == 0`).
- This closes the `6 -> 0 -> 1` transient boundary path where lap-pct mapping can briefly emit `0`, ensuring current-lap comparable state is cleared at lap start before any new-lap sector completion.
- Kept player-row sector-box persistence unchanged (display continuity still survives rollover), while compare/cumulative truth now reliably re-arms to empty at new-lap start.
- Classification: **internal-only** (rollover correctness hardening inside existing LapRef export contract).

### LapRef cumulative-delta rollover truth fix (post-persistence follow-up)
- Finalized LapRef live-current compare semantics by separating **display persistence** from **compare eligibility** in `LapReferenceEngine`.
- Kept player-row sector-box continuity across rollover, but moved `LapRef.Compare.*` and top-level cumulative deltas to consume current-lap comparable state only.
- On normal lap rollover, current-lap comparable state now re-arms immediately:
  - `LapRef.DeltaToSessionBestValid = false`
  - `LapRef.DeltaToProfileBestValid = false`
  - both cumulative delta values publish `0` until at least one new-lap comparable sector pair exists.
- Removed dead rollover scaffolding from the prior patch (`_isLivePlayerLapRolloverArmed`) while retaining only needed state (`_livePlayerCurrentLapSnapshot`, `_lastLivePlayerActiveSegment`).
- Preserved boundaries and invariants: CarSA sector ownership unchanged, H2H/Opponents unchanged, profile-best persistence/fallback semantics unchanged (including sectorless legacy PB rows).
- Classification: **both** (runtime compare correctness fix + canonical LapRef/export docs updated).

### LapRef live sector rollover persistence fix
- Updated `LapReferenceEngine` live player comparison snapshot handling so completed sector boxes are no longer hard-cleared every update tick.
- Added a narrow lap-rollover seam for the live player row:
  - detects segment wrap (`current < previous`) as lap rollover context
  - rearms local current-lap capture state without forcing a visible full-row clear
  - allows progressive slot overwrite as each new-lap sector completion arrives
- Preserved reset boundaries: full live-sector clear still happens on true LapRef reset conditions (session token/type, car, track, wet/dry, explicit engine reset).
- Left profile-best fallback semantics unchanged (lap-time-only PB rows with missing sectors remain valid/possible).
- Classification: **internal-only** (presentation continuity fix; no export contract expansion).
### PR 570 follow-up cleanup: custom-message diagnostics + inventory wording tidy
- Updated custom-message dispatch plumbing so `Pit.Command.LastAction` and `Pit.Command.LastRaw` are now refreshed on every custom-message trigger (`CustomMessage01..10` + normalized sent text), preventing stale pit-action diagnostics after custom sends.
- Kept change bounded: no transport redesign, no UI/dash expansion, no feedback-model rewrite.
- Cleaned `SimHubParameterInventory` wording so settings persistence fields are no longer listed as normal runtime export rows; moved to an explicit non-export settings note.
- Classification: **internal-only** (diagnostic correctness + documentation contract clarity).

### Pit command polish + Settings UI expansion (Pit Commands + Custom Messages)
- Polished pit command user feedback wording:
  - `Clear All` feedback now reads `Pit Clear All`.
  - Fuel-add commands now publish `Fuel MAX` when the add reaches effective tank clamp/max (including +1/+10 edge cases), and the old `Tank Full` wording was removed.
- Added a new Settings → `Pit Commands` expander with:
  - driver-facing guidance text,
  - iRacing focus reliability warning,
  - preview-only `Auto-focus iRacing before pit/custom message send` settings surface (forward-looking; no focus-steal behavior implemented in this task),
  - built-in binding rows for all plugin-owned pit actions.
- Added a new Settings → `Custom Messages` expander with ten editable custom-message slots (friendly label + message text) and per-slot Controls & Events binding rows.
- Added plugin-owned custom-message actions `LalaLaunch.CustomMessage01..10` that dispatch via the existing direct chat-injection transport seam and reuse short-lived `Pit.Command.*` feedback exports.
- Classification: **both** (user-facing Settings UI + action surface expansion, plus bounded runtime pit/custom-message dispatch polish).

### LapRef live current-lap comparison correction + output-prune follow-up
- Corrected LapRef comparison/delta source so live comparison now uses current-lap progress instead of replaying the last validated snapshot sector-by-sector.
- Added a dedicated live player comparison seam in `LapReferenceEngine`:
  - current active segment remains lap-pct driven (`1..6`, else `0`)
  - only completed sectors behind the active segment are eligible for compare/delta
  - current in-progress sector and future sectors remain excluded (no synthetic partial timing)
- Kept session-best/profile-best as static reference snapshots (capture/persistence ownership unchanged).
- Pruned redundant/misleading per-reference active-segment exports:
  - kept `LapRef.ActiveSegment` and `LapRef.Player.ActiveSegment`
  - removed `LapRef.SessionBest.ActiveSegment` and `LapRef.ProfileBest.ActiveSegment`
- Kept top-level cumulative deltas because they remain dash-useful once sourced from completed current-lap sectors only:
  - `LapRef.DeltaToSessionBestSec` / `LapRef.DeltaToSessionBestValid`
  - `LapRef.DeltaToProfileBestSec` / `LapRef.DeltaToProfileBestValid`
- Classification: **both** (runtime comparison behavior correction + export contract tightening).

### LapRef active-segment mirroring + cumulative delta exports
- Extended `LapReferenceEngine` so active segment is now explicitly live-player-driven for all LapRef rows:
  - `LapRef.ActiveSegment`
  - `LapRef.Player.ActiveSegment`
  - `LapRef.SessionBest.ActiveSegment`
  - `LapRef.ProfileBest.ActiveSegment`
- Added new top-level cumulative delta exports for dash-friendly moving comparison:
  - `LapRef.DeltaToSessionBestSec`
  - `LapRef.DeltaToSessionBestValid`
  - `LapRef.DeltaToProfileBestSec`
  - `LapRef.DeltaToProfileBestValid`
- Cumulative deltas now sum sectors `S1..S(active)` using only real sector pairs where both player and reference are valid; no synthetic sector data is introduced.
- Kept existing per-sector compare outputs (`LapRef.Compare.*.S1..S6State/DeltaSec`) unchanged and additive.
- Preserved subsystem boundaries: CarSA remains fixed-sector owner, H2H/Opponents behavior unchanged, and profile PB persistence rules unchanged.
- Classification: **both** (runtime export behavior and dashboard-visible LapRef contract expansion).

### Pit command follow-up (PR 568): direct chat injection + confirmed feedback exports
- Reworked `PitCommandEngine` transport from macro-hotkey binding to direct iRacing chat command injection (`open chat` → `type command` → `send`), keeping plugin-owned `LalaLaunch.Pit.*` actions as the dashboard command surface.
- Expanded pit action set to include `Pit.ClearTires`, `Pit.FuelAdd1`, `Pit.FuelRemove1`, `Pit.FuelAdd10`, `Pit.FuelRemove10`, `Pit.FuelSetMax`, `Pit.ToggleAutoFuel`, and `Pit.Windshield` (while keeping `Pit.FuelAdd` / `Pit.FuelRemove` aliases for compatibility).
- Added short-lived pit command feedback exports and diagnostics:
  - `Pit.Command.DisplayText`
  - `Pit.Command.Active`
  - `Pit.Command.LastAction`
  - `Pit.Command.LastRaw`
- Added before/after confirmation for stateful toggle commands where telemetry authority is available; mismatch paths now publish `Pit Cmd Fail` and emit bounded warning logs with action + expected/before/after context.
- Tightened transport-result semantics: transport-stage logs are now explicitly best-effort, while authoritative success/failure for stateful commands remains the before/after confirmation result.
- Added explicit `Tank Full` user-facing case for fuel-add commands using existing `Pit_TankSpaceAvailable` authority.
- Classification: **both** (runtime pit-command transport/feedback behavior + user-facing pit-command workflow guidance).

### Plugin-owned pit command actions (Strategy Dash/PitPopUp) with explicit SDK-first fallback contract
- Superseded by the later **direct chat injection** follow-up entry above; retained as historical record of PR 568 baseline.
- Added plugin-owned pit command actions in `LalaLaunch` Controls & Events registration: `Pit.ClearAll`, `Pit.FuelAdd`, `Pit.FuelRemove`, `Pit.ToggleFuel`, `Pit.ToggleTiresAll`, and `Pit.ToggleFastRepair`.
- Added focused `PitCommandEngine` transport helper so `LalaLaunch` remains action-registration surface while transport/mapping/logging remain subsystem-local.
- Implemented explicit transport contract:
  - `PitCommandTransportMode=macro_hotkey` (default) uses configured macro key taps (`PitMacroKey*`, default `F13..F18`).
  - `PitCommandTransportMode=sdk` logs a one-time unavailability warning and falls back to macro hotkeys because no writable iRacing SDK pit-command seam is available in current plugin references.
- Added pit command observability:
  - per-fire action/transport/result log lines,
  - one-time invalid/missing binding warnings,
  - `Pit.CommandTransportMode` export for dashboard troubleshooting.
- Classification: **both** (runtime action ownership/transport + user-facing pit-button binding/setup guidance).

### Pit-loss baseline standardization (drive-through) + fixed pit-exit transition allowance
- Standardized pit-loss semantics and guidance so learned/stored pit-lane loss is explicitly a **drive-through baseline** (clean limiter-speed lane travel, no box stop).
- Added fixed `PitExitTransitionAllowanceSec = 2.75` in `LalaLaunch.cs` at the shared boxed-stop prediction seam (`CalculateTotalStopLossSeconds`), yielding:
  - `TotalStopLoss = pit-lane baseline + boxed service model + 2.75s transition allowance`.
- Kept pure lane-travel outputs unchanged (`Fuel.LastPitLaneTravelTime`, `PitExit.TimeS` remain travel-only).
- Kept ownership boundaries intact: Pit timing remains pit-loss owner, Opponents continues consuming the shared stop-loss seam for race-scoped pit-exit countdown prediction.
- Classification: **both** (runtime prediction semantics + user-facing learning guidance/docs).

### Player track-percent export + blended pit-exit time-to-exit export
- Added `Car.Player.TrackPct` in `LalaLaunch.cs` as a plugin-owned player lap-distance percent (`CarIdxLapDistPct`) normalized to `0..1`, publishing `0` when unavailable/invalid.
- Added `PitExit.TimeToExitSec` in `LalaLaunch.cs` as an additive blended dash export that uses `PitExit.RemainingCountdownSec` at low speed and converges toward `PitExit.TimeS` near pit-limiter speed.
- Limiter-speed authority chain for blending: `DataCorePlugin.GameData.PitLimiterSpeed` (primary) with fallback parse of `DataCorePlugin.GameRawData.SessionData.WeekendInfo.TrackPitSpeedLimit`; invalid limiter/input paths clamp safely and keep existing exports unchanged.
- Follow-up tightened blend input validity: `PitExit.RemainingCountdownSec <= 0` is now treated as unavailable, preventing inactive-cycle zero countdowns from collapsing `PitExit.TimeToExitSec` when kinematic `PitExit.TimeS` is still positive.

### Pit box modeled service overhead (+1.0s fixed stationary allowance)
- Added a fixed `+1.0s` boxed-service overhead at the canonical modeled boxed-service seam in `LalaLaunch.cs` (`CalculatePitBoxModeledTargetSeconds`).
- Modeled boxed target is now `max(fuelTime, tireTime) + 1.0`, preserving existing refuel-rate, tire-time, and fuel-add semantics.
- Kept ownership aligned by applying this once upstream so downstream consumers (`Pit.Box.TargetSec`, `Pit.Box.RemainingSec`, and `Fuel.Live.TotalStopLoss`) inherit the same boxed-service correction without lane-travel changes or double-counting.

### Pit.Box.LastDeltaSec stop-end sampling fix (final elapsed authority)
- Fixed `LalaLaunch.cs` pit-box transition handling so `Pit.Box.LastDeltaSec` is computed from the current stop-end elapsed authority (`_pit.PitStopElapsedSec`) when the boxed countdown becomes inactive.
- Added a guarded fallback to the cached countdown elapsed only if the stop-end authority value is invalid, preventing off-by-one-tick positive bias near target stops.
- Preserved the existing `Pit.Box.LastDeltaSec` contract (`latched target - final elapsed`, positive=quicker, negative=slower) and existing 5-second visibility window behavior.

### Pit box latch follow-up: freeze effective target (repairs included)
- Fixed the PR 561 pit-box latch basis in `LalaLaunch.cs` so settle-phase latching now uses the live effective target `max(modeledTargetSec, repairRemainingSec)` instead of modeled-only target.
- This preserves non-repair stop behavior while ensuring repair-involved stops freeze a repair-aware `Pit.Box.TargetSec`.
- `Pit.Box.RemainingSec` and `Pit.Box.LastDeltaSec` now align with the same frozen effective target semantics for repair stops.

### Pit box countdown target latch + short-lived post-stop delta export
- Updated boxed countdown behavior in `LalaLaunch.cs` so `Pit.Box.TargetSec` now latches/freeze after a short in-box settle window (1.0s elapsed), preventing late-stop target drift from moving the countdown.
- Updated `Pit.Box.RemainingSec` to count down from the latched target (still repair-aware through native repair-left authority).
- Added `Pit.Box.LastDeltaSec`, computed at stop end as `(latched target - final elapsed)`, where positive means quicker than target and negative means slower; export auto-resets to `0` after a 5-second visibility window.
- Ensured stale post-stop deltas do not leak into later stops by clearing `Pit.Box.LastDeltaSec` when a new boxed stop becomes active.

### Plugin-owned pit-box distance/time exports for dash use
- Added plugin-owned `Pit.Box.DistanceM` and `Pit.Box.TimeS` exports in `LalaLaunch.cs` for in-lane pit-box guidance, with fail-safe zero publication outside pit lane or when authority inputs are invalid.
- Added `PitEngine.PlayerPitBoxTrackPct` as the native/session authority seam for player pit-box track percent (`DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverPitTrkPct`) and reused it in pit-phase logic paths.
- Kept pit-box time estimation conservative by deriving it from current speed only when speed is sane; otherwise `Pit.Box.TimeS` publishes `0`.

### Brake previous-peak detector hysteresis + re-arm lock refinement
- Refined `Brake.PreviousPeakPct` event detection in `LalaLaunch.cs` to use start/end hysteresis thresholds: start at `brake > 0.05 && throttle < 0.20`, end at `brake <= 0.02 || throttle >= 0.20`.
- Added explicit latch/re-arm behavior after publish so a completed event cannot immediately retrigger; re-arm now requires three consecutive release ticks where either `brake <= 0.02` or `throttle >= 0.20`.
- Preserved existing external contract (`Brake.PreviousPeakPct` updates only on event end, normalized `0..1` inputs, no speed gate) and reset paths now clear active/latch/counter state together.

### Brake previous-peak event-based detector replacement
- Replaced the prior Dahl-style `Brake.PreviousPeakPct` 40-sample window capture with an event-based peak detector in `LalaLaunch.cs`.
- Braking event contract is now: start when `brake > 0.05` and `throttle < 0.20`; while active track `peak = max(peak, brake)`; end when `brake <= 0.05` or `throttle >= 0.20`; on end, latch `Brake.PreviousPeakPct = peak` and reset internal event state.
- Kept internal/runtime brake values normalized on a `0..1` basis (no in-plugin ×100 scaling), preserved manual/session reset behavior, and intentionally kept stationary testing valid by not adding a speed guard.

### Pit Entry Assist aggressive fallback removal (stored-marker authority only)
- Removed pit-entry distance fallback branches in `PitEngine.UpdatePitEntryAssist` that previously read `IRacingExtraProperties.iRacing_DistanceToPitEntry` and `IRacingExtraProperties.iRacing_PitEntryTrkPct`.
- Pit Entry Assist distance authority is now stored/plugin-owned track markers only; when stored marker inputs are unavailable/invalid, assist is reset/off for that tick.
- Added a one-time warning log while unavailable: pit-entry legacy Extra Properties fallbacks are disabled, so missing stored markers now explicitly surface in SimHub logs instead of silently using fallback data.

### CarSA dead debug comparison scaffold prune
- Removed dead internal CarSA debug comparison scaffolding in `LalaLaunch.cs` that sampled legacy Dahl/iRacing relative-gap properties without feeding active debug CSV output, SimHub exports, or user-facing behavior.
- Removed the now-orphaned comparison-gap reset helper used only by that dead scaffolding.
- Kept active CarSA debug CSV schema/cadence behavior unchanged and left Opponents/Pit/H2H/Fuel runtime behavior untouched.

### Brake previous-peak manual/session reset hardening
- Added a dedicated brake capture runtime reset (`ResetBrakeCaptureState`) and invoked it from `ManualRecoveryReset`, which is used by session transitions and `PrimaryDashMode` manual recovery.
- Manual/session recovery now clears active capture progress and resets `Brake.PreviousPeakPct` to `0.0`, preventing stale peaks from previous runs/sessions from being published as fresh captures.

### Player PositionInClass live-context alignment (Car + H2H player rows)
- Aligned player-facing `PositionInClass` publication to the existing Opponents effective/live race-order seam so player rows use the same race-context truth as H2H/Opp target rows.
- Added/updated player-facing exports for consistent dash consumption: `Car.Player.PositionInClass`, `H2HRace.Player.PositionInClass`, and `H2HTrack.Player.PositionInClass`.
- Kept ownership boundaries intact: Opponents remains race-order owner, CarSA remains session-agnostic spatial owner, and H2H remains consumer/publisher only.

### Opponents 5-slot export expansion + live PositionInClass alignment
- Expanded Opponents race-order publication from 2 to 5 slots each side under the existing flat `Opp.*` namespace (`Opp.Ahead1..5.*`, `Opp.Behind1..5.*`) while preserving backward compatibility for existing `Ahead1/2` and `Behind1/2` bindings.
- Added richer race-order-safe per-slot metadata (identity/cosmetic, validity/pit state, effective `PositionInClass`, lap-context fields, and gap/fight metrics) without importing CarSA-local traffic/status semantics.
- Kept top-level `Opponents_SummaryAhead/Behind` readability stable (first-two-slot shape) and added per-slot summaries through slot 5.
- Aligned published race-context `PositionInClass` semantics to effective/live RaceProgress-first ordering across Opponents and H2H outputs, while preserving ownership boundaries (Opponents = race order owner, CarSA = track selector owner).

### Opponents follow-up: pit-lap cache lifecycle + optional metadata
- Fixed Opponents `LapsSincePit` state lifetime by splitting NativeRaceModel per-tick transient reset from full reset, so pit-lap history is preserved within-session but cleared on subsystem/session reset.
- Added stale carIdx pruning for cached pit-lap state against currently visible rows to avoid stale/reused-carIdx leakage.
- Added optional Opp slot metadata exports (`IRating`, `SafetyRating`, `Licence`, `LicLevel`, `UserID`, `TeamID`, `IsFriend`, `IsTeammate`, `IsBad`) using existing DriverInfo fields and existing friend/tag sets.

### Opponents final tidy: slot lookup cleanup + explicit gap semantics
- Cleaned Opp export attachment lambdas in `LalaLaunch.cs` by using local Opp slot lookup delegates and removing repeated direct `GetAheadSlot/GetBehindSlot` chains in the friend/tag flag paths.
- Made Opp gap semantics explicit in code and docs: `Gap.TrackSec` = native progress estimate, `Gap.RelativeSec` = preferred relative gap (checkpoint seam when valid else track fallback), and `GapToPlayerSec` = legacy mirror of preferred relative.

### Session-change stall recovery hardening
- Fixed `LalaLaunch.DataUpdate` starvation by removing the refuel-cooldown full-tick early-return; cooldown now gates only refuel-learning internals so downstream runtime updates continue.
- Added a bounded transient runtime recovery seam (`ManualRecoveryReset`) and wired both Overview reset button and `PrimaryDashMode` action to the same path for manual re-arm.
- Unified session token/type transition runtime reset execution through the same recovery seam while preserving existing fuel-model transition handling.

### Dashboard packaging
- Updated dash control visibility labels to `DRIVER`, `STRATEGY`, and `OVERLAY` while preserving clear tooltips.
- Added a release-facing warning below the debug-mode master toggle to reduce accidental always-on troubleshooting.

### Documentation
- Continued v1 documentation alignment between public guides and subsystem ownership docs.
- Kept Strategy-first wording and live-vs-committed plan distinctions explicit in user-facing docs.

### Release preparation
- Hardened Strategy `Presets...` modal open flow against null preset entries and popup open failures.
- Fixed Preset Manager `PreRace Mode` dropdown behavior by reusing the validated ComboBox interaction pattern.
- Embedded shipped preset defaults and expanded track-marker defaults in owning code paths for predictable first-run packaging.

### Plugin UI
- Added a new top-level `OVERVIEW` tab as the plugin landing page/front door.
- Added quick-link buttons, lightweight runtime status text, and a fail-soft GitHub latest-release check (`Check Now` + `Open Releases`).
- Added dashboard preview placeholders that document the exact drop-in paths/names for future images (`Assets/Overview/*.png`) without shipping binaries in this branch.

### Shift Assist learning
- Refined Learning v2 adjacent-gear crossover solve with a small internal relative early-bias tolerance (`a_next >= a_curr * (1 - pct)`) so learned shift RPMs land modestly earlier without flat RPM subtraction, hard cap shaping, or reintroducing the prior absolute accel-margin bias.
- Preserved all existing Learning v2 invariants: speed-domain curves, ratio-based RPM conversion, stability buffer before publish, gear-1 exclusion, no-fallback publication, and safe clamp (`redline - 200`).

### Brake previous-peak reset threshold follow-up
- Updated Dahl-style `Brake.PreviousPeakPct` capture reset threshold from `brake01 <= 0.0` to `brake01 <= 0.02` so completed captures can re-arm during slight trail-brake residuals.
- Removed unused brake capture clock state from `LalaLaunch.cs` (`_brakeClock`) with no behavior change beyond the reset-threshold update.

### Brake previous-peak export (Dahl-style)
- Added `Brake.PreviousPeakPct` export in the runtime update path to mirror Dahl `BrakeCurvePeak` behavior.
- Capture now starts when brake rises above zero, tracks max brake for a 40-sample window, latches at sample 40, and only resets/arms for a new capture after completion plus brake return-to-zero.

### Opponents / H2H session eligibility
- Expanded Opponents leaderboard-neighbor publication scope from race-only to live opponent sessions (Practice, Qualifying/Open Qualify, Lone Qualify, Race), while keeping Offline Testing out of scope.
- Preserved the existing H2HRace selector seam (`Opp.Ahead1` / `Opp.Behind1`) so H2HRace now lights up naturally in practice/qualifying sessions when leaderboard identity is available.
- Kept race-specific pit-exit prediction bounded to Race sessions and reset pit-exit outputs outside Race.

### Opponents activation log wording cleanup
- Updated the Opponents activation log text to match current behavior: `Opponents subsystem active (eligible live session).`
- Synced affected docs to remove stale race-only/lap-gate wording where it referenced Opponents activation.

### Opponents pit-exit hot-loop reset cleanup
- Changed race-scoped pit-exit reset handling to clear once on Race → non-Race transition instead of resetting every non-race telemetry tick.
- Added a small OpponentsEngine race-active latch that is also cleared by `OpponentsEngine.Reset()`.

### Opponents native-only cutover
- Removed Opponents runtime dependency on `IRacingExtraProperties` player, class leaderboard, and class-ahead/behind feeds.
- Replaced Opponents identity, same-class ordering, and lap enrichment with native session-driver + telemetry arrays (`PlayerCarIdx`, `CarIdxLap`, `CarIdxLapDistPct`, `CarIdxClassPosition`, `CarIdxBestLapTime`, `CarIdxLastLapTime`, `CarIdxOnPitRoad`, `CarIdxTrackSurface`).
- Replaced Opponents gap and pit-exit math with a native progress/pace model (lap+distance based) and retained explicit invalid outputs when prerequisites are missing.
- Added bounded Opponents invalid-state logging for native prerequisite failures; no hidden fallback path to Extra Properties remains in Opponents.
- Kept H2H selector seam and CarSA ownership boundaries unchanged.

### Opponents native follow-up correctness fixes
- Prevented unknown/non-positive `PositionInClass` rows from sorting ahead of valid class positions in the class-position ordering path.
- Cleared the temporary pit-out bypass latch after it serves the final-120s suppression bypass, restoring normal late-race suppression.
- Reset pit-exit predictor state on invalid native snapshot so stale pit-exit snapshot/audit data cannot leak.

### Opponents CarSA true-gap seam (Ahead1/Behind1)
- Added a narrow CarSA checkpoint-time seam (`TryGetCheckpointGapSec`) and wired Opponents to prefer it for `Opp.Ahead1.GapToPlayerSec` and `Opp.Behind1.GapToPlayerSec`.
- Kept existing progress/pace gap math as fallback and left neighbor selection ownership unchanged in Opponents.

### Opponents class-position warm-up fallback guard
- Prevented class-position ordering from activating while the player class position is unset/non-positive, so neighbor selection falls back to lap-progress ordering until player class position is valid.

### Opponents class-color normalization parse-order fix
- Fixed class-color normalization to parse plain numeric strings as decimal by default, preserving correct `ClassColor:CarNumber` identity keys when native `CarClassColor` values arrive as decimal text.

### PR534 rerun: Opponents class-color normalization
- Re-applied the PR534 decimal-first class-color normalization change on `work` so it can be submitted again as a fresh PR after the original PR was closed unmerged.

### Opponents RaceProgress-first live ordering fix
- Changed native Opponents same-class neighbor ordering to always use live race progress (`CarIdxLap` + `CarIdxLapDistPct`) for Ahead/Behind target selection so overtakes switch immediately.
- Kept native `CarIdxClassPosition` as anchor/validation context only (and fallback positional context when present), preventing delayed official class-position updates from blocking live target swaps.
- Preserved existing `Opp.Ahead1/2` + `Opp.Behind1/2` output contracts and H2HRace selector seam behavior.

### Refuel-rate lock (car profile)
- Added persisted car-level `RefuelRateLocked` so refuel-rate learning follows the existing learn → validate → lock trust model used by other learned values.
- Added a `Locked` checkbox beside the existing Refuel Rate control in Profiles → CAR, reusing the plugin’s existing lock-control UX pattern.
- Gated `SaveRefuelRateToActiveProfile(...)` so locked profiles keep their stored refuel rate; runtime learning math remains unchanged and now emits a concise verbose-debug line when a locked overwrite attempt is blocked.

### Refuel-rate lock follow-up (runtime + first-fill)
- Fixed locked runtime behavior so blocked locked overwrite attempts now keep runtime/planner on the stored locked refuel rate rather than showing/applying the learned candidate.
- Added locked first-fill fail-safe: if a profile is locked but the stored refuel rate is unusable, the first valid learned candidate is allowed to populate once; further locked overwrites are blocked.
- Updated the Profiles refuel slider tooltip text to explicitly say live refuel events update the value while unlocked.

### Launch trace shutdown + empty-trace housekeeping hardening
- Fixed launch plugin shutdown behavior so end-service now performs close/flush without unconditional deletion of the current launch trace pointer, preventing valid completed trace loss on SimHub exit.
- Added explicit launch-trace lifecycle state to separate discard-eligible aborted/invalid current runs from finalized completed traces that must be kept.
- Added conservative empty-trace housekeeping: launch traces with no telemetry rows and no usable summary are removed during shutdown/list discovery and excluded from Launch Analysis file selection.

### LapRef offline reference comparison (Issue #540)
- Added a new standalone `LapReferenceEngine` and `LapRef.*` export family for player-only offline reference comparison (player row, session-best row, profile-best row, and per-sector comparison rows).
- Kept H2H/Opponents/CarSA responsibilities unchanged: LapRef reads CarSA fixed-sector cache as a read-only seam and does not modify H2H contracts.
- Wired LapRef capture to the existing validated-lap acceptance path and existing wet/dry routing path (`_isWetMode`) to avoid duplicate validation or condition-detection logic.
- Extended `TrackStats` with compatibility-safe optional condition-specific PB sector fields (`BestLapSector1..6Dry/WetMs`) and persisted them only when real sectors exist on a new PB.
- Synced subsystem/docs contracts (`H2H`, `CarSA`, `Profiles_And_PB`, `SimHubParameterInventory`, `SimHubLogMessages`, `RepoStatus`) and added `Docs/Subsystems/LapRef.md`.

## 2026-04-20 — PR #582 post-merge class-metadata completion + multiclass cache-proof follow-up
- Classification: **internal-only** (class-metadata/cache decision correctness for existing H2H/ClassLeader seams; no new exports/UI).
- Kept `DriverInfo.Drivers##` as the preferred class metadata source, then always ran `DriverInfo.CompetingDrivers[*]` as a per-car backfill pass for missing class entries only (no overwrite of already-resolved cache rows).
- Updated multiclass session inference so `_isMultiClassSession` is now true when either:
  - explicit native multiclass signal is present (`NumCarClasses > 1`, or unknown class-count with positive `HasMultipleClassOpponents`), or
  - the freshly built class cache itself proves diversity (`>1` distinct non-blank class names).
- Preserved prior startup safety behavior: unknown class-count state alone does not infer multiclass, and blank/unresolved class states are not treated as class-diversity evidence.


## 2026-04-21 — PR follow-up: runtime fuel health-check ordering + manual-reset live-session gate
- Classification: **internal-only** (compile/order fix + runtime reset guardrail correction with unchanged user workflow).
- Moved pit-road telemetry read earlier in `DataUpdate` so the active-driving runtime fuel-health edge check uses an in-scope `isOnPitRoad` value before evaluation.
- Tightened `ManualRecoveryReset(...)` short-circuit semantics: planner-safe early return now requires an active live session in addition to successful planner-safe recovery.
- Preserved planner-safe runtime behavior for active live session recovery while ensuring manual reset outside active live session still executes the broad reset path.

## 2026-04-10 — Opponents Pit Exit dash export follow-up
- Classification: **both** (user-facing dash exports + internal docs/contract alignment).
- Exported active pit-cycle countdown as `PitExit.RemainingCountdownSec` from the existing Opponents active-cycle remaining-time predictor (`>0` while running, `0` when inactive/unavailable).
- Exported active pit-cycle state as `PitExit.ActivePitCycle` (`true` only during active pit-cycle prediction phase).
- Wired both exports in `LalaLaunch.cs` under the existing `PitExit.*` attach path without changing pit-exit prediction logic.
- Updated Opponents subsystem docs, SimHub parameter inventory, and RepoStatus for canonical contract alignment.

## 2026-04-13 — Pit.Box in-box service countdown contract
- Classification: **both** (new driver-facing exports + internal docs/contract alignment).
- Added `Pit.Box.Active`, `Pit.Box.ElapsedSec`, `Pit.Box.RemainingSec`, and `Pit.Box.TargetSec` exports in `LalaLaunch.cs`.
- Countdown `ElapsedSec` now explicitly reuses the existing `PitEngine.PitStopElapsedSec` timer; no second box timer was introduced.
- Countdown target is the existing modeled service target only: `max(fuelTime, tireTime)` where `fuelTime = Pit_WillAdd / EffectiveRefuelRateLps` and tyre time uses existing tyre-change selection/time.
- Countdown is explicitly in-box service phase only (`in pit lane && in pit stall && PitPhase.InBox`) and hard-zeros when inactive/unavailable (including drive-through/missed-box states).

## 2026-04-13 — Pit.Box repair-aware target + optional-repair setting
- Classification: **both** (user-facing setting + runtime pit timing/prediction behavior alignment + docs updates).
- Extended the shared pit-box service target seam to include repairs concurrently while boxed using native repair-left authority: `max(fuelTime, tireTime, mandatoryRepairTime[, optionalRepairTime])`.
- Mandatory repair-left time is now included by default when valid in boxed service state; optional repair-left time is included only when the new global setting is enabled.
- Added global setting `PitBoxIncludeOptionalRepairs` (default `false`) and exposed it in Settings UI (`GlobalSettingsView`) so drivers can opt in optional repairs.
- Kept ownership and timing invariants intact: no second timer, no PitExit logic rewrite, and downstream pit-exit prediction continues consuming the same shared total-stop-loss seam.

## 2026-04-13 — Pit.Box contract tidy-up (settings export mismatch)
- Classification: **internal-only** (docs contract correction).
- Removed the mistaken `Settings.PitBoxIncludeOptionalRepairs` row from `SimHubParameterInventory` because no matching `AttachCore("Settings.*")` export exists in runtime code.
- Kept runtime behavior unchanged: setting remains UI/persisted JSON + runtime seam input only.

## 2026-04-13 — Pit.Box repair-left countdown semantic hotfix
- Classification: **both** (driver-facing countdown correctness + internal contract alignment).
- Fixed in-box countdown semantics so live repair-left telemetry is no longer treated as a total target and reduced again by elapsed time.
- Kept `Pit.Box.TargetSec` honest as modeled fixed-duration service time only (`max(fuelTime, tireTime)`), while `Pit.Box.RemainingSec` now resolves as `max(modeledRemainingSec, repairRemainingSec)` during valid boxed service state.
- Preserved pit-exit/shared stop-loss seam behavior by keeping repair-aware box authority in `CalculateTotalStopLossSeconds` via `max(modeledTargetSec, repairRemainingSec)`.

## 2026-04-13 — Fuel pit-box refuel gauge seam + WillAdd twitch analysis
- Classification: **both** (new dash-facing exports + internal contract clarification).
- Added boxed-refuel gauge exports in `LalaLaunch.cs`: `Fuel.Pit.Box.EntryFuel`, `Fuel.Pit.AddedSoFar`, and `Fuel.Pit.WillAddRemaining`.
- `EntryFuel` now latches once when valid boxed refuel context becomes active; `AddedSoFar` and `WillAddRemaining` update live during the stop and all three values reset to `0` when boxed refuel context is inactive.
- Investigated end-of-stop `Fuel.Pit.WillAdd` twitch: root cause is expected clamp behavior (`WillAdd = min(requestedAdd, maxTank-currentFuel)`) as tank space tightens near completion, not a planner/runtime bug.
- Left `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`, and `Fuel.Delta.LitresWillAdd` semantics unchanged; documented the clamp behavior and directed UI countdown usage to `Fuel.Pit.WillAddRemaining`.

## 2026-04-13 — Pit refuel gauge follow-up (compile + latch hardening)
- Classification: **internal-only** (bug fix + seam hardening with no runtime fuel-math contract change).
- Fixed CS0841 compile blocker by passing an in-scope authoritative fuel sample (`data.NewData?.Fuel`) to `UpdatePitRefuelGaugeValues(...)` from `DataUpdate`.
- Replaced `Pit_WillAdd`-driven refuel-active detection with lifecycle-based boxed latch semantics:
  - latch trigger requires boxed service + refuel selected + flow signal (`fuel rise` or `_isRefuelling` seam),
  - once latched, phase remains active for the rest of boxed service,
  - reset occurs only when boxed service ends.
- Kept all existing runtime fuel outputs/semantics unchanged: `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`, and `Fuel.Delta.LitresWillAdd`.

## 2026-04-13 — Boxed refuel stable target export (`Fuel.Pit.Box.WillAddLatched`)
- Classification: **both** (new dash-facing export + internal contract/docs alignment).
- Added `Fuel.Pit.Box.WillAddLatched` in `LalaLaunch.cs` as a boxed-latched UI target captured once from live `Fuel.Pit.WillAdd` when boxed refuel latch first activates.
- Kept `Fuel.Pit.WillAdd` runtime semantics unchanged (`min(requestedAdd, maxTank-currentFuel)` live clamp behavior remains authoritative for runtime math).
- Updated boxed countdown semantics to use the latched target (`Fuel.Pit.WillAddRemaining = max(0, Fuel.Pit.Box.WillAddLatched - Fuel.Pit.AddedSoFar)`) so in-stop tank-space clamp shrink does not move the purple target.

## 2026-04-13 — Boxed refuel latch timing tidy + in-box cancel clear
- Classification: **both** (dash-facing boxed refuel behavior fix + canonical docs alignment).
- Updated `UpdatePitRefuelGaugeValues(...)` so `Fuel.Pit.Box.WillAddLatched` now latches immediately when valid boxed service is active and refuel is selected, instead of waiting for refuel-flow detection.
- Kept split latch semantics: `Fuel.Pit.Box.EntryFuel` still waits for first flow signal (`fuel rise` or `_isRefuelling`) so `Fuel.Pit.AddedSoFar` remains grounded to actual fill start.
- Added explicit in-box cancel behavior: when refuel is deselected while still boxed, boxed refuel exports clear immediately (`EntryFuel`, `WillAddLatched`, `AddedSoFar`, `WillAddRemaining`) so stale pending-fuel countdown does not persist.
- Preserved runtime fuel-math semantics (`Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`, `Fuel.Delta.LitresWillAdd`) unchanged.

## 2026-04-10 — Repo-wide iRacingExtraProperties runtime fallback removal sweep
- Classification: **both** (runtime behavior cleanup + internal docs/contract alignment).
- Removed remaining runtime `IRacingExtraProperties` reads from `LalaLaunch.cs`, `PitEngine.cs`, `MessagingSystem.cs`, and `Messaging/SignalProvider.cs`.
- H2H class session-best is now native-only; when native class-best authority is unavailable, `H2H*.ClassSessionBestLapSec` stays `0` and a bounded warning is logged.
- Removed leader-lap ExtraProperties candidates and retained native `DataCorePlugin` leader sources only.
- Removed wet-tyre ExtraProperties fallback (`iRacing_Player_TireCompound`); wet mode now uses native `PlayerTireCompound` only.
- Removed car/track display fallback reads and class-color fallback reads from ExtraProperties in `LalaLaunch.cs`.
- Removed Pit Entry Assist pit-speed fallback (`iRacing_PitSpeedLimitKph`); assist now uses native session pit limit only and logs a bounded warning when unavailable.
- Removed legacy ExtraProperties signal accessors in MSGV1 `SignalProvider`; affected signals now intentionally remain unavailable with one-time-per-signal warnings.
- Removed legacy ExtraProperties traffic fast-path in `MessagingSystem`; native/session context path remains.
- Removed `IRacingExtraProperties.` prefix bypass from `ProfilesManagerViewModel` property helper to eliminate compatibility alias treatment.

## 2026-04-12 — Launch trace mixed-file parser summary-header hotfix
- Classification: **internal-only** (housekeeping parser correctness + log-noise reduction; no user workflow or format changes).
- Fixed launch trace housekeeping analysis to explicitly skip the summary CSV header row that follows `[LaunchSummaryHeader]`.
- Prevented valid mixed trace files from routing `TimestampUtc,...` through telemetry row parsing, eliminating the false telemetry DateTime parse error during Launch Analysis file scans.
- Kept launch trace naming/CSV format/summary schema unchanged; empty/header-only cleanup and completed trace retention rules remain intact.
