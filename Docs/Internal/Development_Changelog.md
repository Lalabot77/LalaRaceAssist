## 2026-06-08 — MonitorSystem Phase 3A review follow-up
- Classification: **both** (driver-visible MonitorSystem warning selection correctness and internal false-positive reduction; no new exports, property names, settings, dashboard JSON, subsystem target/gap logic, pit/fuel/strategy/planner/command behavior, or Property Snapshot grouping changes).
- Active Car/Opp/H2H health publication now republishes the selected current active check whenever its text differs from the displayed MonitorSystem text, while still refusing to overwrite unresolved fuel-health alerts or active pit warnings.
- Deferred `CARSA GAP CHECK` because no clean non-debug CarSA output currently proves the runtime gap scale/lap-time basis is valid; MonitorSystem now keeps only Opponent target, CarSA slot `CarIdx`, and H2H target impossible-state checks instead of guessing about gap-scale readiness.
- Restored the main-branch `EXIT FUEL SHORT` tolerance to 1.5 L after the Phase 3A merge/rebase. Property Snapshot list reviewed: yes; existing `MonitorSystem.*` exports remain mapped to FuelStrategy and no export/property add, remove, rename, or regroup is required.

## 2026-06-08 — MonitorSystem Phase 3A Car/Opp/H2H impossible-state checks
- Classification: **both** (driver-visible MonitorSystem warning texts plus internal edge-gated health logs; no new exports, property names, settings, dashboard JSON, subsystem target/gap logic, pit/fuel/strategy/planner/command behavior, or Property Snapshot grouping changes).
- Added report-by-exception MonitorSystem checks for already-published impossible states only: valid Opponents targets with invalid/player `CarIdx`, valid CarSA slots with invalid `CarIdx`, and valid H2H race/track targets with invalid/player `CarIdx`. `CARSA GAP CHECK` is deferred until a clean non-debug CarSA gap-scale readiness signal exists.
- Checks are gated on eligible live sessions, valid player `CarIdx`, and a short 3 s session warmup; valid-false/blink-held rows are ignored, failures/recoveries are edge-logged, repeated still-failing evidence is log-only at 60 s, and publication does not override unresolved fuel-health alerts or active pit warnings.
- Preserved CarSA selection/gap/checkpoint logic, Opponents race-order/gap logic, H2H selector/sector/gap logic, PitExit, fuel model, planner/strategy, dash JSON, settings UI, property names, exports, fallback paths, self-healing, and telemetry-known wrappers.
- Property Snapshot list reviewed: yes; `ResolvePropertySnapshotGroup(...)` still maps existing `MonitorSystem.*` exports to FuelStrategy, and no export/property add, remove, rename, or regroup is required.

## 2026-06-08 — MonitorSystem Phase 2C baseline warning review follow-up
- Classification: **internal-only correctness follow-up** (baseline-warning numeric guard and same-tick trigger ordering; no export names, dashboard JSON, fuel/refuel math, planner, Pit Fuel Control, Pit Command, or message text changes).
- `BASELINE SHORT` now requires positive current fuel before evaluating, so a zero current-fuel reading fails silently instead of producing a baseline warning. This is a simple numeric sanity guard, not telemetry fallback/known-state logic.
- `UpdateMonitorPitStopFramework(...)` now tracks a local `pitRoadExitThisTick` flag and skips the predictive warning block for that same update when a pit-road-exit edge fired, preserving a just-published pit-exit WARNING from same-tick predictive CAUTION downgrade.
- Preserved predictive checks on later ticks, trusted SimHub telemetry reads, no `MfdFuelRequestKnown`, no new warnings, no CSV, no per-tick checks, no `Fuel.Refuel.*` / Pit Fuel Control / Pit Command / Strategy planner changes, and no Property Snapshot group changes.
- Property Snapshot list reviewed: yes; no export/property add, remove, rename, behavior-contract change, or group change because `MonitorSystem.*` remains in the existing Fuel/Strategy group.

## 2026-06-08 — MonitorSystem Phase 2C pit-exit MFD context fix
- Classification: **internal-only correctness follow-up** (baseline-warning edge context fix; no export names, dashboard JSON, fuel/refuel math, planner, Pit Fuel Control, Pit Command, or message text changes).
- Parameterized `IsMonitorBaselineFuelShort(...)` with `includeSelectedMfdFuel`, keeping selected `PitSvFuel` included for predictive, pit-road entry, and pit-box entry checks while excluding it on `PitRoadExit`.
- Pit-road exit `BASELINE SHORT` now uses actual exit fuel only, so a still-selected MFD fuel request after leaving pit road cannot hide a real post-stop shortfall.
- Preserved trusted SimHub telemetry reads, no telemetry fallback/known-state layer, no `MfdFuelRequestKnown`, no new warnings, no CSV, no per-tick checks, no `Fuel.Refuel.*` / Pit Fuel Control / Pit Command / Strategy planner changes, and no Property Snapshot group changes.
- Property Snapshot list reviewed: yes; no export/property add, remove, rename, behavior-contract change, or group change because `MonitorSystem.*` remains in the existing Fuel/Strategy group.

## 2026-06-08 — MonitorSystem trusted SimHub telemetry simplification
- Classification: **internal-only correctness/scope cleanup** (Phase 2B monitor implementation and internal docs; no export, message text/enum, fuel/refuel calculation, planner, Pit Fuel Control, or Pit Command behavior change).
- Removed the `MfdRefuelKnown` snapshot field and `TryReadMonitorPitBool` helper so Phase 2B service checks again read trusted SimHub/iRacing `dpFuelFill` through the existing simple bool-read path.
- Kept `PitSvFuel` as direct trusted telemetry and intentionally did not add `MfdFuelRequestKnown`, PitSvFuel availability checks, or other telemetry-validity/fallback layers; hypothetical missing SimHub telemetry is outside MonitorSystem scope unless project logs prove a real issue.
- Reverted the pit evidence log wording/format to `mfdRefuelEnabled` without `mfdRefuelKnown`.
- Preserved Phase 2B tolerances, edge-only behavior, Fuel Control DATA log-only behavior, message texts/enums, independent `Fuel.Refuel.NextLitres` basis, fuel/refuel calculations, Strategy/planner math, Pit Fuel Control behavior, Pit Command behavior, CSV behavior, and export names. No `BASELINE SHORT`, `FUEL MODEL CHECK`, independent gross SimHub baseline maths, new exports, or Property Snapshot grouping changes were added.
- Property Snapshot list reviewed: yes; no group change required because no SimHub exports/properties were added, removed, renamed, or regrouped.

## 2026-06-08 — MonitorSystem Phase 2C baseline gross fuel check
- Classification: **internal-only** (new MonitorSystem warning text using existing MonitorSystem exports; no new export names or dashboard JSON changed).
- Added `BASELINE SHORT`, a deliberately crude race-risk sanity check that trusts SimHub/iRacing telemetry directly: current fuel comes from `DataCorePlugin.GameData.NewData.Fuel`, fuel-per-lap from `DataCorePlugin.Computed.Fuel_LitersPerLap`, remaining laps from `DataCorePlugin.GameData.LapsRemaining`, MFD refuel state/request from `DataCorePlugin.GameRawData.Telemetry.dpFuelFill` / `PitSvFuel`, and pre-race available fuel can use the existing `Fuel.Setup.FuelLevel` seam.
- Formula is intentionally simple: pre-race/grid `baselineAvailable = setup fuel when valid, otherwise current fuel`; race/pit context `baselineAvailable = current fuel + PitSvFuel when dpFuelFill is enabled`; `baselineRequired = fuelPerLap * remainingLaps`; publish only when shortfall is greater than `MonitorBaselineFuelToleranceLaps = 1.0` lap of fuel.
- Evaluation is only on existing MonitorSystem trigger edges (`PredictiveTwoLapsFuelRemaining`, `PitRoadEntry`, `PitBoxEntry`, optional `PitRoadExit`); Fuel Control mode/data changes remain log/service-warning paths and do not introduce baseline checks.
- Preserved fuel-health priority (`FUEL DATA CHECK` / `FUEL DATA FAULT` block pit/baseline warning publication), `Fuel.Refuel.*`, Pit Fuel Control, Pit Command, Strategy planner, fallback systems, CSV writers, dashboard JSON, exports, and per-tick cadence.
- Property Snapshot list reviewed: yes; no export/property add, remove, rename, behavior-contract change, or group change because `MonitorSystem.*` remains in the existing Fuel/Strategy group.

## 2026-06-07 — MonitorSystem Phase 2B latest review follow-up
- Classification: **both** (driver-facing warning priority/correctness plus internal Phase 2B evidence-log wording; no export, fuel/refuel calculation, planner, Pit Fuel Control, or Pit Command behavior change).
- Narrowed fuel-health blocking to unresolved `FUEL DATA CHECK` / `FUEL DATA FAULT` only, so auto-recoverable `FUEL DATA RECOVERED` no longer suppresses urgent Phase 2B pit warnings.
- Added known/unknown MFD refuel telemetry handling: `dpFuelFill` is read without defaulting missing values to `false`, snapshots carry `MfdRefuelKnown`, service warnings fail closed when refuel telemetry is unknown, and pit evidence logs include `mfdRefuelKnown`.
- Off-pit-road Fuel Control mode changes now re-check CAUTION-level predictive risk while still inside the two-laps-fuel window, even after the one-shot predictive trigger previously fired clean, allowing later MFD/refuel changes to publish or clear predictive pit warnings before pit entry without per-tick scans or off-road WARNINGs.
- Preserved Phase 2B tolerances, Fuel Control DATA log-only behavior, message texts/enums, independent `Fuel.Refuel.NextLitres` basis, fuel/refuel calculations, Strategy/planner math, Pit Fuel Control behavior, Pit Command behavior, CSV behavior, and export names. No `BASELINE SHORT`, `FUEL MODEL CHECK`, independent gross SimHub baseline maths, new exports, or Property Snapshot grouping changes were added.
- Property Snapshot list reviewed: yes; no group change required because no SimHub exports/properties were added, removed, renamed, or regrouped.

## 2026-06-07 — MonitorSystem Phase 2B active review follow-up
- Classification: **both** (driver-facing warning priority/correctness plus internal Phase 2B documentation; no export, fuel/refuel calculation, planner, Pit Fuel Control, or Pit Command behavior change).
- Added a narrow fuel-health priority guard so Phase 2B pit warnings neither publish over nor clear `FUEL DATA CHECK`, `FUEL DATA FAULT`, or `FUEL DATA RECOVERED`; clean pit edges still clear only active Phase 2B pit-warning texts back to `MONITOR READY`.
- Gated `EXIT FUEL SHORT` on a meaningful pit-entry required add (`PluginNextLitres > 0.5L` via the existing fuel-still-required helper), preventing no-refuel/no-required-add pit cycles from warning solely because pit-lane driving burned fuel below entry fuel.
- Off-pit-road Fuel Control mode changes now evaluate CAUTION-level predictive risk when an active Phase 2B pit warning exists, allowing corrected predictive `REFUEL OFF` / `MFD FUEL LOW` warnings to clear before pit entry without publishing off-road service WARNINGs.
- Preserved Phase 2B tolerances, edge-only behavior, Fuel Control DATA log-only behavior, message texts/enums, independent `Fuel.Refuel.NextLitres` basis, fuel/refuel calculations, Strategy/planner math, Pit Fuel Control behavior, Pit Command behavior, CSV behavior, and export names. No `BASELINE SHORT`, `FUEL MODEL CHECK`, independent gross SimHub baseline maths, new exports, or Property Snapshot grouping changes were added.
- Property Snapshot list reviewed: yes; no group change required because no SimHub exports/properties were added, removed, renamed, or regrouped.

## 2026-06-07 — MonitorSystem Phase 2B PR #792 review follow-up
- Classification: **both** (driver-facing warning correctness and internal Phase 2B evidence-log documentation; no export, fuel/refuel calculation, planner, Pit Fuel Control, or Pit Command behavior change).
- Rebased Phase 2B pit-stop warning checks on the independent existing runtime recommendation seam `Fuel.Refuel.NextLitres` / `Fuel.Refuel.Valid` instead of `Pit_WillAdd` / current MFD-selected add mirrors. This allows `REFUEL OFF` to remain eligible when refuel is disabled and makes `MFD FUEL LOW` compare the MFD request against an independent recommendation. Invalid/missing recommendations fail closed.
- Added narrow MonitorSystem ownership detection for active Phase 2B pit-warning texts; relevant clean evaluation edges now clear only `REFUEL OFF`, `MFD FUEL LOW`, or `EXIT FUEL SHORT` back to `MONITOR READY`, leaving `FUEL DATA CHECK`, `FUEL DATA FAULT`, and `FUEL DATA RECOVERED` untouched.
- First-tick priming while already on pit road now seeds the pit-entry snapshot and logs `PitRoadSnapshotSeeded` without warning publication, so later pit-road exit can still compare actual fuel against the seeded expected fuel-on-exit.
- Preserved Phase 2B tolerances/guard style, edge-only warning publication, Fuel Control DATA log-only behavior, message texts/enums, fuel/refuel calculations, Strategy/planner math, Pit Fuel Control behavior, Pit Command behavior, CSV behavior, and export names. No `BASELINE SHORT`, `FUEL MODEL CHECK`, independent gross SimHub baseline maths, new exports, or Property Snapshot grouping changes were added.
- Property Snapshot list reviewed: yes; no group change required because no SimHub exports/properties were added, removed, renamed, or regrouped.

## 2026-06-07 — MonitorSystem Phase 2B pit-stop warning checks
- Classification: **both** (driver-facing MonitorSystem pit-stop warning text plus internal Phase 2 trigger/check documentation).
- Added edge-triggered `REFUEL OFF`, `MFD FUEL LOW`, and `EXIT FUEL SHORT` MonitorSystem messages using only Phase 2A pit-stop snapshots/evidence and existing plugin refuel recommendation seams (`PluginNextLitres`, `PluginFuelOnExit`).
- Trigger mapping: predictive two-laps-fuel remaining checks publish `REFUEL OFF`/`MFD FUEL LOW` as CAUTION; pit-road entry, pit-box entry, and on-pit-road/boxed Fuel Control mode changes publish those service checks as WARNING; Fuel Control DATA changes stay log-only; pit-road exit checks `EXIT FUEL SHORT` as WARNING before clearing the pit-entry snapshot.
- `REFUEL OFF` is guarded by effective refuel completion (`CurrentFuel + 0.75L >= pit-entry PluginFuelOnExit` or `PluginNextLitres <= 0.5L`) to avoid warning when iRacing/fuel-control naturally switches OFF after fuel has already been added.
- Updated the MonitorSystem message catalogue and Phase 2B edge log wording with `warningText`/`warningEnum`.
- Preserved fuel/refuel calculations, Strategy/planner math, Pit Fuel Control behavior, pit command behavior, dashboard export names, CSV behavior, and Phase 1 fuel-health messages. No `BASELINE SHORT`, `FUEL MODEL CHECK`, independent gross SimHub baseline maths, new exports, or Property Snapshot grouping changes were added.
- Property Snapshot list reviewed: yes; no group change required because no SimHub exports/properties were added, removed, renamed, or regrouped.

## 2026-06-07 — CarSA target-after-player checkpoint freshness guard
- Classification: **both** (driver-facing CarSA slot-01 precision/relative-gap correctness fix; no export names, dashboard JSON, or CSV schema changes).
- Added a target-after-player checkpoint truth freshness guard requiring the stored player gate timestamp to be finite, non-future, and no older than `min(15 s, half the active lap-time scale)` before `UpdateGateGapTruthForCar(...)` can accept the forward match.
- Rejects stale prior-lap player gate pairings such as the Imola ~103 s raw-gap case before `NormalizeGateGapSec(...)` can wrap them into fake close values, while preserving valid behind-car target-after-player matches where the player crossed the gate shortly before the target.
- Preserved checkpoint count/indexing, reverse matching, normalization, precision sign/tolerance/freshness handling, `Gap.TrackSec`, `Gap.RelativeSec` mapping/filtering except through cleaner truth input, H2H, Opponents, PitExit, dashboards, public exports, and Property Snapshot grouping.
- Property Snapshot list reviewed: yes; no group change because no SimHub exports/properties were added, removed, renamed, or regrouped.

## 2026-06-07 — PR #791 capture-window diagnostic reliability follow-up
- Classification: **internal-only** (Car Tracking Probe diagnostic reliability only; no runtime selection, public export, dashboard JSON, or user workflow change).
- Gated checkpoint-truth diagnostic collection on active Car Tracking Probe capture, with explicit START/STOP/RESET clearing, so pre-START or stopped-window events cannot appear in the first subsequent CSV row.
- Reset `LastCheckpointChangeTimeSec` on invalid checkpoint/LapDistPct and checkpoint re-anchor paths, preventing stale elapsed intervals on the first valid crossing after telemetry gaps.
- Reset latest-event same-tick fields on every truth update while preserving aggregate `*SinceSnapshot` counters for historical evidence, and changed snapshot consumption to keep an `existing/unknown` active-truth baseline when runtime checkpoint truth remains valid.
- Property Snapshot list reviewed: yes; unchanged because this refines Car Tracking Probe CSV diagnostics only and no SimHub exports/properties are added, removed, renamed, or regrouped.

## 2026-06-07 — PR #791 checkpoint diagnostics aggregate-counter follow-up
- Classification: **internal-only** (Car Tracking Probe diagnostic completeness only; no runtime selection, public export, dashboard JSON, or user workflow change).
- Added bounded `*SinceSnapshot` aggregate counters for truth updates, forward/reverse truth source counts, same-tick multiple/different-gate overwrites, prior-truth overwrites, RelativeSec mismatch clears, checkpoint crossings, total skipped checkpoints, and maximum skipped checkpoints.
- Latest-event fields continue to show the most recent event, while aggregate counters preserve useful evidence when multiple checkpoint/truth events occur between lower-frequency Car Tracking Probe rows; counters clear only with the existing post-row diagnostic consume.
- `TruthOverwroteExistingThisTick` now only reflects same-tick overwrite status for the latest truth event, with `TruthOverwroteExistingSinceSnapshot` carrying the latched window-level overwrite flag.
- Property Snapshot list reviewed: yes; unchanged because this adds Car Tracking Probe CSV diagnostics only and no SimHub exports/properties are added, removed, renamed, or regrouped.

## 2026-06-07 — PR #791 checkpoint diagnostics review follow-up
- Classification: **internal-only** (Car Tracking Probe diagnostic reliability only; no runtime selection, public export, dashboard JSON, or user workflow change).
- Latched checkpoint progression, truth overwrite, and RelativeSec mismatch evidence across telemetry ticks until the next successful Car Tracking Probe row snapshots all four target groups; the bounded diagnostic window is cleared only after that row is appended to the existing buffer.
- Race-start latch reset now clears all new progression/timing fields including `LastCheckpointChangeTimeSec`; Soft Debug disabled ticks clear pending truth provenance so re-enable cannot expose stale events.
- Preserved `TruthSource` as forward/reverse event provenance and left the existing precision `CandidateSource` responsible for identifying `checkpoint_truth` versus `checkpoint_filtered`.
- Property Snapshot list reviewed: yes; unchanged because this refines diagnostics only in Car Tracking Probe CSV and no SimHub exports/properties are added, removed, renamed, or regrouped.

## 2026-06-07 — CarSA checkpoint truth event diagnostics
- Classification: **internal-only** (bounded Car Tracking Probe CSV provenance expansion; no runtime selection, public export, dashboard JSON, or user workflow change).
- Added read-only checkpoint progression and truth-event diagnostic state for the selected Ahead01P/Behind01P targets and configured Probe A/B identities, including skipped-index evidence, forward/reverse source, gate/timestamp/lap provenance, raw/normalized/directional values, TrackSec correlation, same-tick overwrite metadata, prior truth, RelativeSec mismatch clearing, and precision candidate difference/error ratio.
- Extended only the existing buffered `CarTrackingProbe_<Track>_<Timestamp>.csv` schema; checkpoint crossing detection, 60-checkpoint count, truth generation/overwrite ordering, filtering, RelativeSec, precision, TrackSec, Opponents, H2H, PitExit, dashboard JSON, and public exports remain unchanged.
- Property Snapshot list reviewed: yes; unchanged because this adds raw/checkpoint diagnostics only to Car Tracking Probe CSV and no SimHub exports/properties are added, removed, renamed, or regrouped.
## 2026-06-07 - MonitorSystem Phase 2A pit-stop trigger framework
- Classification: **both** (internal pit-stop evidence framework and driver-visible settings placement correction; no new driver warnings, exports, command behavior, fuel math, or refuel math).
- Added a private MonitorSystem Phase 2A observer in `LalaLaunch.cs` that records edge-only trigger logs for `FuelControlModeChanged`, `FuelControlDataChanged`, `PredictiveTwoLapsFuelRemaining`, `PitRoadEntry`, `PitBoxEntry`, and `PitRoadExit`.
- Pit-road entry now freezes a lightweight internal evidence snapshot with UTC, session time/type/state, current fuel, raw MFD refuel enabled/request values, Pit Fuel Control mode/DATA, plugin refuel validity/next litres/fuel-on-exit, and pit-road/box state. The snapshot is not used for warnings in this phase.
- Moved `Enable Monitor System` out of Launch UI / Bindings and into `Dash Control -> Global Dash Functions -> General`; persistence and ON/OFF output behavior are unchanged.
- No Phase 2B race-risk checks were added: no `REFUEL OFF`, `MFD FUEL LOW`, `BASELINE SHORT`, or `EXIT FUEL SHORT` checks, no gross SimHub baseline check, no pit commands, no recovery, no new monitor text, no CSV writer, and no per-tick logs.
- Property Snapshot list reviewed: yes; no group change because no SimHub exports/properties were added, removed, renamed, or regrouped. Message catalogue reviewed: unchanged.

## 2026-06-07 — MonitorSystem pre-Phase 2 enable control + colour cleanup
- Classification: **both** (persisted driver setting and dash-facing state/colour behavior; no fuel-health logic, calculations, pit monitoring, or command behavior change).
- Added persisted `MonitorSystemEnabled` (default `true`) and `Settings -> Launch Settings -> Enable Monitor System`; changes apply immediately and save through the existing settings path.
- Simplified active state to `ON`: enabled/reset output is `ON` / `MONITOR READY` / enum `1`; disabled output is `OFF` / `MONITOR OFF` / enum `0`; active runtime no longer emits `AUTO`. Existing fuel-health publications are ignored while disabled and resume from ready after re-enable.
- WARNING/FAULT presentation now uses red background (`#FF0000`) with yellow text (`#FFFF00`). Export names, enum values, message texts, fuel-health predicates/recovery, fuel/refuel/planner math, Pit Fuel Control, and Pit Command remain unchanged.
- Property Snapshot list reviewed: yes; no group change because existing `MonitorSystem.*` exports remain in Fuel/Strategy.

## 2026-06-07 — PR #790 deferred fuel-health retry pending-state follow-up
- Classification: **internal-only correctness follow-up** (existing MonitorSystem messages/exports; no fuel, planner, pit-control, or command behavior change).
- Automatic planner-safe recovery deferral now retains `_fuelRuntimeHealthCheckPending`, its reason, and the current unhealthy streak so a transition-gap-only check remains eligible for the next bounded retry; attempted recovery still clears pending state, and the existing valid healthy pass still clears pending state while publishing `FUEL HEALTH OK`.
- Property Snapshot list reviewed: yes; no group change because no exports were added, removed, renamed, or regrouped.

## 2026-06-07 — PR #790 MonitorSystem fuel-health outcome follow-up
- Classification: **both** (dash-facing fuel-health status correctness + internal recovery-result distinction; no export names, message texts/enums, fuel logic, or command behavior changes).
- A prior `FUEL DATA CHECK` WATCH now clears to `FUEL HEALTH OK` on the next evaluation that satisfies the existing healthy pass basis, even when the queued-check flag was already cleared; the existing passed log remains queued-check-only.
- Planner-safe recovery now reports whether an attempt actually ran. Existing throttle/missing-manager deferrals retain WATCH instead of publishing FAULT; only an attempted unhealthy recovery publishes `FUEL DATA FAULT`. Recovery timing, side effects, pending-state clearing, and unhealthy-streak behavior remain unchanged.
- Active-live-session manual targeted recovery now publishes the same RECOVERED/FAULT/WATCH monitor outcome as automatic recovery while preserving the existing successful early return and unsuccessful/deferred broad-reset fall-through.
- Property Snapshot list reviewed: yes; no group change because the existing `MonitorSystem.*` exports remain in Fuel/Strategy. Message catalogue reviewed: unchanged because no emitted text or enum changed.

## 2026-06-07 — MonitorSystem Phase 1 framework + fuel-health visibility migration
- Classification: **both** (new dash-facing monitoring exports + internal framework/catalogue/docs; no fuel, planner, pit-control, or command behavior change).
- Added independent `MonitorSystem.cs` presentation state with Phase 1 enum/colour/text contract and identical-output deduplication; it has no dependency on fuel calculators, Strategy, pit engines, Opponents, CarSA, or H2H.
- Added `LalaLaunch.MonitorSystem.State`, `.Text`, `.BackgroundColour`, `.TextColour`, and `.Enum`; initial state is automatic `MONITOR READY`.
- Mirrored existing fuel-health outcomes only: unhealthy observation -> `FUEL DATA CHECK`, unchanged pass -> `FUEL HEALTH OK`, unchanged recovery success -> `FUEL DATA RECOVERED`, unchanged recovery failure -> `FUEL DATA FAULT`. Existing trigger conditions, health checks, logs, 450 ms evaluation throttle, two-observation streak, two-second recovery throttle, recovery state mutations, fuel math, Strategy/PreRace, `Fuel.Refuel.*`, `Pit.FuelControl.*`, and `Pit.Command.*` remain unchanged.
- Added `Docs/Internal/MonitorSystem_Messages.csv`; no debug CSV writer was added in Phase 1. Pit-stop monitoring and independent gross SimHub baseline fuel checks remain future work.
- Property Snapshot list reviewed: yes; `MonitorSystem.*` is assigned to the existing Fuel/Strategy group because Phase 1 surfaces the runtime fuel-health seam.

## 2026-06-06 — CarSA pit/rejoin slot-retention CSV diagnostics
- Classification: **internal-only** (bounded diagnostics schema expansion; no runtime selection, public export, dashboard JSON, or user workflow change).
- Added per-slot `CarSA_Debug_*.csv` evidence for all five ahead and five behind slots: assignment mode, current bounded-candidate presence/rank, hysteresis retention, blink hold, existing pre-assignment retention eligibility/reason, final on-track state, and LapDistPct validity. Existing distance, TrackSec, pit-road, and raw-surface columns remain available for correlation.
- Instrumentation observes the existing `ApplySlots(...)` branches after their unchanged decisions; candidate acquisition, cursor/duplicate handling, 10% hysteresis, blink hold, slot ordering, TrackSec, RelativeSec, precision, checkpoint matching, H2H, Opponents, PitExit, dashboards, and exports are unchanged.
- Property Snapshot list reviewed: yes; unchanged because this adds debug CSV diagnostics only, not SimHub exports/properties.

## 2026-06-06 — PR #788 reverse checkpoint lap-delta guard follow-up
- Classification: **both** (ahead precision safety hardening + internal documentation alignment; no export names or dashboard JSON changes).
- Tightened only the player-crossing reverse-match guard from `|target lap - player lap| <= 3` to `<= 2`, rejecting three-lap timestamp pairs that the unchanged downstream normalization/mapping chain cannot safely represent.
- Preserved the existing target-after-player gate-truth path, `NormalizeGateGapSec`, `MapToAhead` / `MapToBehind`, slot selection, `Gap.TrackSec`, `Gap.RelativeSec`, precision sign/tolerance/freshness, direct checkpoint seam, H2H, Opponents, PitExit, dashboards, and exports.
- Property Snapshot list reviewed: yes; no group change because no exports are added, removed, renamed, or regrouped.

## 2026-06-06 — CarSA reverse checkpoint matching for ahead precision availability
- Classification: **both** (driver-facing slot-01 ahead precision availability correction + internal checkpoint-cache ordering fix; no export names or dashboard JSON changes).
- Player checkpoint crossings now reverse-match eligible non-player timestamps already recorded for the same gate, allowing cars that crossed before the player (normally ahead cars) to feed the existing CarSA gate-truth/filter pipeline once the player reaches that gate.
- Reverse matching preserves the stored `target time - player time` convention, reverse-only `|lap delta| <= 2` guard and existing normalization path, and `UpdateGateGapTruthForCar(...)` ownership. It rejects future/stale timestamps using a bound of `min(15 s, half the active lap-time scale)`, requires current direct-checkpoint eligibility, and skips same-update target crossings so the existing target-after-player path is not duplicated or rate-distorted.
- Preserved CarSA slot selection, `Gap.TrackSec`, `Gap.RelativeSec` mapping/sticky/fallback/source semantics, precision sign normalization/tolerance/freshness, direct checkpoint seam behavior, H2HTrack, H2HRace, Opponents, PitExit, dashboard JSON, and all public export names.
- Property Snapshot list reviewed: yes; no group change because no exports are added, removed, renamed, or regrouped.
## 2026-06-06 — Mandatory Codex single-fence final-response policy
- Classification: **internal-only** (Codex governance/template policy; no runtime, UI, dashboard, export, profile, telemetry, or user-workflow behavior changes).
- Changed final-response presentation guidance from optional copy/paste formatting to a mandatory contract requiring the entire response inside exactly one fenced `text` block, with no content before or after it.
- Kept `Docs/Internal/CODEX_CONTRACT.txt` and `Docs/Internal/CODEX_TASK_TEMPLATE.txt` aligned on the exact opening fence, closing fence, and required result sections inside the single block.
- Root `CHANGELOG.md` and `Docs/RepoStatus.md` remain unchanged because this is internal process policy rather than user-facing release content or validated runtime state.
- Property Snapshot list reviewed: yes; no SimHub exports or properties are involved.

## 2026-06-06 — CarSA precision local checkpoint sign normalisation
- Classification: **both** (driver-facing slot-01 precision acceptance correction + internal diagnostics/docs clarification; no export names or dashboard JSON changes).
- After selecting the existing fresh checkpoint truth or freshness-bounded filtered state, `ComputeSlotPrecisionGap(...)` now locally negates the stored proximity candidate (`target checkpoint time - player checkpoint time`) into TrackSec convention before the unchanged current/±lap reconciliation.
- Close valid cases now retain the public sign contract (`Ahead01P` positive, `Behind01P` negative) and can publish checkpoint precision; long-gap short-path candidates still fail the existing same-sign/nearest-branch/tolerance checks and fall back to TrackSec.
- Preserved checkpoint cache storage, recording, filtered prediction, freshness gates, tolerance, `NormalizeGateGapSec`, `MapToAhead/MapToBehind`, `Gap.TrackSec`, `Gap.RelativeSec`, CarSA slot selection, blink-held/invalid `NaN`, H2HTrack, H2HRace, Opponents, PitExit, dashboard JSON, and all public export names.
- Car Tracking Probe CSV schema is unchanged: `RawCandidateSec` remains the selected stored/proximity candidate before sign normalization, while `ReconciledCandidateSec` is the best direction-normalized lap-adjusted candidate.
- Property Snapshot list reviewed: yes; no group change because no exports were added, removed, renamed, or regrouped.

## 2026-06-06 — CarSA directional checkpoint gap reconciliation
- Classification: **both** (driver-facing slot-01 precision correction + internal probe diagnostics/docs; no new public export names or dashboard JSON changes).
- `Car.Ahead01P.Gap.Sec` / `Car.Behind01P.Gap.Sec` now use finite selected-slot `Gap.TrackSec` as the directional-loop and lap-branch authority. The selected fresh checkpoint candidate is tested at its current value and ± one lap time, retaining only same-sign candidates; the closest candidate is accepted only within `min(6.0 s, max(1.5 s, 8% of |TrackSec|))`.
- Missing, wrong-direction, or out-of-tolerance checkpoint state falls back directly to TrackSec instead of passing through half-lap `MapToAhead/MapToBehind`, preventing short-way-around publication such as a roughly -17 s precision value when the correct behind loop is roughly -90 s. Invalid slot/TrackSec and blink-hold behavior remains `NaN`.
- Added diagnostics-only Car Tracking Probe CSV columns for each precision side: TrackSec authority, raw checkpoint candidate, best reconciled candidate, tolerance, candidate source, chosen source, and reject reason. No SimHub properties were added.
- Preserved `Gap.TrackSec`, visible `Gap.RelativeSec` semantics, CarSA slot selection, checkpoint recording, H2HTrack's current TrackSec override, H2HRace, Opponents, PitExit, dashboard JSON, CompetingDrivers removal, and all existing public export names.
- Property Snapshot list reviewed: yes; no public export/property add, remove, rename, behavior regroup, or snapshot-group change.

## 2026-06-06 — H2HTrack LiveGapSec TrackSec alignment
- Classification: **both** (dashboard-facing H2HTrack live-gap semantics align with Track SA; no export names or dashboard JSON changed).
- H2HTrack selectors now carry the selected CarSA slot directional `Gap.TrackSec` into H2H; `H2HTrack.Ahead/Behind.LiveGapSec` publishes `abs(Gap.TrackSec)` to preserve the existing positive display convention while matching Track SA directional-loop authority.
- If the selected CarSA TrackSec is invalid/non-finite, H2HTrack publishes the existing safe zero live-gap value and does not fall back to the prior independent full race-progress formula.
- Preserved H2HTrack target identity, validity rules, active segment, lap summaries, CarSA fixed-sector outputs, H2HRace race-gap behavior, Opponents outputs, CarSA gap math/slot selection, checkpoint/gate logic, `Gap.RelativeSec`, `Car.Ahead01P/Behind01P`, export names, dashboard JSON, CompetingDrivers removal, and Pace Car/self-target safety filters.
- Property Snapshot list reviewed: yes; no group change required because no exports/properties were added, removed, renamed, or regrouped.

## 2026-06-05 — Car tracking safety filters
- Classification: **both** (driver-facing target contamination/self-target prevention plus internal safety-filter documentation).
- Opponents now filters non-player `DriverInfo.Drivers##` Pace Car rows before building native race-order rows; filtered rows cannot enter `NativeRaceModel.Rows`, `Opp.Ahead1` / `Opp.Behind1`, H2HRace selector inputs, or PitExit prediction. The filter uses `IsPaceCar`, `CarIsPaceCar`, exact Pace Car identity labels, and conservative `CarPath` matching; it does not filter by `CarIdx`, preserving `PlayerCarIdx == 0`, and skipped Pace Car `CarIdx`/identity evidence clears matching blink-continuity holds before reinsertion.
- If the current player row is pace-car flagged, Opponents preserves the player row and logs a one-time anomaly warning rather than removing the player.
- H2H target publication now fails closed when a resolved ahead/behind target `CarIdx` equals `PlayerCarIdx`, so H2HTrack/H2HRace cannot publish the player as their own target or retain stale self-gap data.
- Preserved CarSA gap math, CarSA selection/checkpoint logic, Opponents ordering except Pace Car row exclusion, Opponents gap fallback math, H2H gap formula, export names, dashboard JSON, and Property Snapshot grouping. Property Snapshot list reviewed: yes; no export/property add, remove, rename, or regroup.

- 2026-06-05: Car Tracking Probe CSV flush failure guard follow-up landed.
  - Classification: **internal-only** (Car Tracking Probe CSV robustness only; no UI/schema/export changes).
  - Centralized probe CSV buffered flushes through a protected flush path so STOP, RESET, soft-debug/enable-off flush, session/file rotation, periodic flush, and plugin shutdown cleanup all disable capture and emit the existing one-shot probe failure log instead of propagating `File.AppendAllText` exceptions.
  - Preserved runtime invariants: no CSV schema changes, no UI changes, no Property Snapshot changes, no CarSA/Opp/H2H behavior changes, and no dashboard export changes.

- 2026-06-05: Car Tracking Probe CSV debug system landed.
  - Classification: **internal-only** (Debug UI diagnostics and CSV documentation only; no dashboard exports/properties added).
  - Added a Debug UI controlled `CarTrackingProbe_<Track>_<Timestamp>.csv` writer for player + Probe A/B CarIdx rows, bounded by enable/start/stop/reset controls and a normalized 1–20 Hz capture frequency.
  - CSV rows include `DriverInfo.Drivers##` identity/class fields only, raw CarIdx telemetry/flag arrays, player-vs-probe raw/derived progress deltas, read-only CarSA checkpoint gap diagnostics when available, and CarSA/Opp/H2H correlation fields.
  - Preserved runtime invariants: no CarSA gap math changes, no Opponents ordering changes, no H2H selector changes, no dashboard contract/export changes, no Property Snapshot changes, and no `DriverInfo.CompetingDrivers` normal-driver fallback for the probe.
  - Property Snapshot list reviewed: yes; unchanged because Car Tracking Probe is a separate debug CSV system and no exported property capture contract changed.

# Development Changelog

## 2026-06-08 — Debug Cleanup Phase A
- Classification: **internal-only** (debug/export cleanup and documentation alignment; no driver-facing runtime math, telemetry gating, learning, or dashboard-consumed core exports changed).
- Removed Rejoin ThreatDebug internals, OffTrack Debug CSV settings/UI/writer/probe export, and verbose-only Pit/PitLite SimHub exports. Active Pit/PitLite core exports, CarSA debug CSV/event CSV, Shift Assist debug CSV, DecelCapture, Property Snapshot, Event Marker, Trace Logging, and launch trace/analysis systems remain.
- Removed approved inert/commented debug clutter: parked MsgCx helper block, commented PitExit pit-out snapshot logger/call, commented `MSG.PitPhaseDebug`, commented `Pit.Debug.LastTimeOnPitRoad`, stale launch-trace skip comment, stale obsolete Rejoin debug comment, and stale Session Summary scaffolding wording.
- Updated docs for the Property Snapshot 2 Hz frequency cap and CarSA debug CSV `Tick`/`MiniSector`/`EventOnly` cadence support.
- Property Snapshot list reviewed: yes; cleanup removed exports/settings but did not add/rename exports or change snapshot behavior.

## 2026-06-05 — CompetingDrivers runtime tracking fallback removal
- Classification: **both** (driver-visible correctness hardening for Opp/H2HRace/PitExit/message target identity; no export names, dashboard JSON, settings, or UI controls changed).
- Removed the Opponents `DriverInfo.CompetingDrivers[*]` second-pass row creation so `NativeRaceModel.Rows`, `Opp.Ahead1`/`Opp.Behind1`, H2HRace selectors, and PitExit prediction can only receive live normal-driver rows built from `DriverInfo.Drivers##.*` plus CarIdx telemetry.
- Replaced the message-system class lookup fallback with a `DriverInfo.Drivers##.*` lookup only; when trusted driver rows are unavailable, message classification fails closed instead of using suspect CompetingDrivers data.
- Preserved CarSA gap math, CarSA physical slot selection, checkpoint timing, H2H selector behavior, PitExit math, export names, and Property Snapshot grouping.
- Property Snapshot list reviewed: yes, no group change required because no exports/properties were added, removed, renamed, or regrouped; this only removes suspect runtime data sourcing behind existing exports.
## 2026-06-05 — Pit Fuel Control failure feedback preservation follow-up
- Classification: **both** (driver/dashboard-facing failure feedback preservation plus documentation correction).
- Removed Pit Fuel Control's immediate generic `PIT CMD SEND FAIL` republish after failed raw sends so the more specific `PitCommandEngine` failure text (`PIT CMD WINDOW FAIL`, `PIT CMD CHAT FAIL`, or `PIT CMD SEND FAIL`) remains visible.
- Preserved existing fuel-control fallback behavior and diagnostics: send-failure paths still force `Source=STBY` where they already did, disarm AUTO where they already did, and keep existing `send-failed` logs.
- Confirmed Pit Tyre Control does not republish generic failure feedback after `_rawCommandSender` returns false; it already preserves command-engine failure feedback.
- Corrected Pit Commands subsystem output wording to remove stale Pit Fuel Control PushSaveMode compatibility-alias wording.
- Property Snapshot list reviewed: yes; no SimHub export/property names, groups, or dashboard JSON changed.

## 2026-06-05 — Pit command failure message specificity
- Classification: **both** (driver/dashboard-facing `Pit.Command.DisplayText` warning text changes plus internal log/doc inventory alignment).
- Replaced generic pit command failure publishes with specific warning texts where existing code paths already distinguish the failure source: `PIT CMD WINDOW FAIL`, `PIT CMD CHAT FAIL`, `PIT CMD SEND FAIL`, and `PIT CMD CONFIRM FAIL`.
- `PIT CMD TIMEOUT FAIL` remains reserved/documented but not currently emitted because the generic failure-publish sites do not have a true timeout/expiry distinction; Pit Fuel Control stale-owned-request expiry remains a mirror/ownership expiry path and does not publish command-failure feedback.
- Raw-command send failure paths preserve the `PitCommandEngine` failure text; no transport retry, confirmation window, payload, dashboard JSON, fuel math, or tyre mode behavior changed.
- Property Snapshot list reviewed: yes; no SimHub export/property names, groups, or dashboard JSON changed because only existing `Pit.Command.DisplayText` warning text values changed.

## 2026-06-04 — Pit command direct transport fixed path cleanup
- Classification: **both** (user-facing Settings UI simplification plus internal transport cleanup; no action names, Pit.Command exports, or fuel/strategy math changed).
- Removed the Settings -> Pit Commands transport-mode ComboBox; users no longer choose Auto / Legacy foreground SendInput / Direct message only.
- Pit/custom command dispatch is now fixed to the plugin-owned direct iRacing window-message path. Persisted `PitCommandTransportMode` values are retained as ignored legacy settings data so old settings load safely, but they cannot re-enable Auto fallback or legacy foreground `SendInput`.
- Removed the live legacy foreground `SendInput` fallback path from `PitCommandEngine`; direct-window failure now fails closed through the pit command feedback/log contract (now `PIT CMD WINDOW FAIL` for no usable window).
- Issue #698 legacy cleanup reviewed: `Pit.FuelControl.PushSaveMode`, `Pit.FuelControl.PushSaveModeText`, and legacy `PushSaveModeCycle` code exports/actions were already absent; stale docs/log inventory references were corrected. Canonical Pit Fuel Control DATA/SOURCE/MODE behavior and `CycleData` remain unchanged.
- Property Snapshot list reviewed: yes; no live SimHub export/property was added, removed, or renamed because the requested PushSaveMode exports were already absent and existing Pit/PitExit group coverage remains correct.

## 2026-06-04 — SESSION burn-target stationary no-burn credit clarification
- Classification: **internal-only** (helper naming/comment and documentation clarification only; no export names, dashboard JSON, or runtime formula/guard behavior changed).
- Renamed the SESSION pit-fuel-credit helper to `ComputeSessionBurnTargetStationaryNoBurnCreditLitres()` to clarify that the SESSION allowance is a stationary no-burn pit-box service-time fuel credit.
- Documented the invariant that the credit is not a timed-race distance or required-laps reduction: timed projections and lap-limited fallback projections preserve the existing `Fuel.LiveLapsRemainingInRace_Stable` remaining-lap denominator while applying the stationary no-burn box-time credit when the existing inputs are valid.
- Preserved the existing SESSION formula and guards: remaining stops from `ceil(validated Fuel.Live.RemainingStints)`, selected burn from `Fuel.Refuel.SelectedBurnPerLap`, lap seconds from the selected runtime refuel projection lap seam, and zero credit for invalid/non-positive credit inputs.
- Preserved STINT, END, INVALID, raw MFD request gating, `Fuel.Refuel.*`, pit/refuel math, planner logic, dashboard JSON, XAML, and SimHub export names.
- Property Snapshot list reviewed: yes; no export additions/removals/renames or snapshot-group behavior changes.

## 2026-06-03 — Fuel burn target SESSION pit-stop fuel credit
- Classification: **both** (existing dashboard-facing `Fuel.Burn.Target` SESSION value changes; no export names or UI surfaces changed).
- Refined only the `Fuel.Burn.Target` SESSION branch to add a fixed first-order pit-stop fuel-burn credit before calculating the session target: `ceil(validated Fuel.Live.RemainingStints) * 40.0s * (Fuel.Refuel.SelectedBurnPerLap / selected runtime refuel projection lap seconds)`.
- Documentation follow-up: clarified that selector phase thresholds use `effectiveTargetStints = Fuel.Live.RemainingStints + 1.0` because `Fuel.Live.RemainingStints` excludes the current stint (`>2.0` STINT, `>1.0` SESSION, `<=1.0` END, subject to validity and END reserve guard).
- Remaining stop-count source confirmed as existing plugin-owned validated `Fuel.Live.RemainingStints`, which is already computed from runtime refuel burn/projection context and normalized by runtime max-tank authority; it is rounded up conservatively because the source is exact tank-load/stint requirement.
- Invalid/non-positive selected burn, projection lap seconds, or remaining stop count produce zero credit and preserve the previous SESSION calculation rather than invalidating SESSION.
- Preserved STINT, END, INVALID, `Fuel.Burn.TargetText`, `Fuel.StintBurnTarget`, `Fuel.RequiredBurnToEnd`, `Fuel.Refuel.*`, `Fuel.Live.RemainingStints`, pit/refuel math, pit-loss learning, pit-window logic, Strategy planner, dashboard JSON, and XAML.
- Property Snapshot list reviewed: yes; no group change required because existing `Fuel.Burn.*` exports remain covered by the `Fuel.*` Fuel/Strategy prefix.

## 2026-06-03 — Blink-continuity deep stale-data review follow-up
- Classification: **internal-only** (stale-data/candidate-preservation follow-up; no export names or dashboard JSON changed).
- CarSA slot assignment now keeps a candidate cursor separate from held-slot identity preservation, so a blink-held A1/B1 does not consume or drop the nearest valid live candidate; following slots can still publish the next eligible live cars without duplicate `CarIdx` slots.
- Opponents telemetry-stale targets now explicitly publish gap exports as NaN, and row continuity pre-scans for live usable canonical identities so live reconnect/DriverInfo duplicates win over older held rows.
- Deep review confirmed H2HTrack remains fail-closed through `IsOnTrack` selector gating, H2HRace remains fail-closed through telemetry-stale `IsValid=false`/`CarIdx=-1` propagation, and pit-exit/checkpoint/lap-time ingestion continue to exclude telemetry-stale rows.
- Property Snapshot list reviewed: yes; no exports/properties were added, removed, renamed, or regrouped.

## 2026-06-03 — H2HTrack blink-held CarSA selector fail-closed follow-up
- Classification: **internal-only** (stale live-target suppression; no export names or dashboard JSON changed).
- `BuildH2HTrackSelector(...)` now requires CarSA slots to be both valid and on-track, so blink-held slots (`IsOnTrack=false`) can preserve identity/cosmetics in CarSA exports without becoming live-valid H2HTrack timing targets.
- Preserved CarSA blink-hold identity continuity, normal valid H2HTrack selector behavior, Opponents row hold behavior, H2HRace handling, export names, dashboard JSON, and fuel/pit/strategy/race-finish systems.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or regrouped.

## 2026-06-03 — Blink-hold stale-data leak follow-up
- Classification: **internal-only** (stale-data suppression follow-up; no export names or dashboard JSON changed).
- CarSA now evaluates blink-hold eligibility before the same-candidate slot fast path, so invalid-`LapDistPct` starts the hold immediately and normal/precision gap fields publish invalid values from the first affected tick.
- Opponents now removes/marks seen a held identity when the current snapshot reports the same canonical identity at a different `CarIdx`, preventing the old held row from being resurrected during reconnect/DriverInfo churn.
- Telemetry-stale Opponents targets now preserve identity/effective-position continuity but publish as not live-valid (`IsValid=false`, `CarIdx=-1`, no live gap truth); H2HRace selector propagation keeps stale identity with `CarIdx=-1` so H2H live timing/gap validity falls false instead of resolving stale live timing.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or regrouped.

## 2026-06-03 — Blink-hold precision suppression follow-up
- Classification: **internal-only** (behavior precision fix; no export names or dashboard JSON changed).
- Suppressed `Car.Ahead01P.Gap.Sec` / `Car.Behind01P.Gap.Sec` while the corresponding slot-01 blink hold is active, preventing recent gate truth from publishing as precision gap truth during a held telemetry gap.
- Added a dedicated CarSA blink-hold eligibility timestamp so invalid-`LapDistPct` holds can last the intended 1.0 s window without being shortened by the existing 0.5 s LapPct delta/closing-cache grace.
- Preserved existing LapPct cache grace, NotInWorld latch grace, valid live slot behavior, Opponents row hold behavior, export names, and dashboard JSON.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or regrouped.

## 2026-06-03 — CarSA/Opponents bounded opponent blink continuity
- Classification: **internal-only** (runtime stability/observability behavior; no export names or dashboard JSON changed).
- Added a 1.0 s CarSA physical-slot blink hold for existing ahead/behind `CarIdx` identities that briefly become NotInWorld or lose valid `LapDistPct`; identity/cosmetics are retained for target continuity while live gap truth is invalidated (`Gap.*` NaN/source 0, no checkpoint truth) until valid telemetry returns or the hold expires.
- Added a 2.0 s Opponents row hold for known non-player race-order rows with the same canonical identity/`CarIdx`; held rows keep their last RaceProgress anchor, publish as not on track, skip the CarSA checkpoint preferred gap, and are excluded from pit-exit prediction rows to avoid expanding pit-exit behavior.
- Message debounce intentionally deferred; the first fix is upstream data continuity, with MSGV1 debounce reserved for future captures showing sustained class-position oscillation after the row hold.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or regrouped.

## 2026-06-03 — Fuel burn target selector redesign
- Classification: **both** (new dashboard-facing fuel target exports plus internal Fuel Model selector contract).
- Phase 1 seam result: the clean raw driver-selected MFD fuel request seam exists at `DataCorePlugin.GameRawData.Telemetry.PitSvFuel`, with `dpFuelFill` indicating whether refuel is selected; `PitSvFuel` is upstream of tank-space clamp and is mirrored by Pit Fuel Control for both external/manual MFD changes and plugin-owned `#fuel` sends.
- Added `Fuel.Burn.Target`, `Fuel.Burn.TargetText`, and `Fuel.Burn.TargetValid` as a plugin-owned dashboard selector for `STINT` / `SESSION` / `END` / `INVALID`.
- SESSION uses the selected MFD request before tank-space clamp and intentionally does not use `Fuel.Refuel.NextLitresCeil`, `Fuel.Pit.WillAdd`, plugin-recommended refuel values, tank-space-clamped add values, or planner fuel values.
- Added an internal validity bit for `Fuel.Live.RemainingStints` so reset/default `0` does not publish `END`.
- Added a reserve-protected END guard: if the active guard burn/projection plus active contingency still exceeds current fuel plus one runtime max tank, the selector publishes `INVALID` rather than `END`.
- Preserved `Fuel.StintBurnTarget`, `Fuel.RequiredBurnToEnd`, `Fuel.Refuel.*`, `Fuel.Pit.*`, Pit Fuel Control command/refuel math, dashboard JSON, and XAML.
- Property Snapshot list reviewed: yes; new `Fuel.Burn.*` exports route through the existing `Fuel.*` prefix into the Fuel/Strategy group.

### Review follow-up: do not gate STINT/END on raw MFD availability
- Fixed selector invocation so missing/unreadable `PitSvFuel` invalidates only the SESSION branch. STINT and END now continue to publish from validated `Fuel.Live.RemainingStints`, `Fuel.StintBurnTarget`, and `Fuel.RequiredBurnToEnd` basis when raw MFD request telemetry is unavailable.
- Preserved SESSION semantics: it still requires valid raw MFD-selected fuel request, still gates the value by `dpFuelFill`, and still does not use `Fuel.Refuel.NextLitresCeil`, `Fuel.Pit.WillAdd`, planner values, plugin recommendations, or tank-space-clamped add values.
## 2026-06-03 — Pit Fuel Control stale request-fault expiry
- Classification: **both** (driver-facing recovery behavior + internal fault-lifecycle hardening).
- Added a bounded pending-owned requested-fuel confirmation expiry in `PitFuelControlEngine`: after the existing 900 ms post-send suppression and short confirmation allowance, a still-mismatched valid `PitSvFuel` is treated as external/manual MFD takeover, clearing stale pending ownership and `Pit.FuelControl.Fault` for that tick.
- Narrowed the MsgCx no-command recovery hook so it acts only when the pending request is already stale/expired by the same confirmation-expiry rules, then routes through the same external/manual takeover handling as telemetry expiry; it does not clear merely because `Pit.FuelControl.Fault` is non-zero, does not send pit commands, and does not change DATA selection.
- Preserved `Pit.FuelControl.Fault` value contract (`0/1/2/3`), DATA/SOURCE/MODE semantics, fuel/refuel math, pit-command transport, Strategy planner math, dashboard JSON, and export names.
- Property Snapshot list reviewed: yes, no grouping update needed because no exports changed and `Pit.FuelControl.*` remains under the existing `Pit.*` Pit/PitExit group.

## 2026-06-02 — Fuel burn analysis Avg10, minimum burn, and remaining-laps range
- Classification: **both** (additive dashboard-facing analysis exports/action plus internal Fuel Model contract documentation).
- Added `Fuel.Burn.Analysis.Avg10` from the existing fresh accepted-lap observer. Its backing rolling list now retains the latest 10 samples, while Avg3/Avg5 continue to select only their latest 3/5 and retain partial-window behavior.
- Added independent fresh accepted-lap `Fuel.Burn.Analysis.MinObserved` tracking and `BurnAnalysisResetMinObserved`; existing max reset continues to clear only `MaxObserved`. Lifecycle resets clear both extrema and the rolling Avg10 state.
- Added synchronized range exports: conservative `Fuel.Burn.Analysis.RemainingLapsMin = current runtime fuel / MaxObserved` and optimistic `Fuel.Burn.Analysis.RemainingLapsMax = current runtime fuel / MinObserved`, with safe `0.0` fallback for invalid, empty, non-finite, or non-positive inputs.
- Preserved accepted-lap gating, seeded-value exclusion, scoped count/reset behavior, `Fuel.LiveFuelPerLap*`, `Fuel.FuelBurnPredictor*`, Strategy planner, pit/refuel math, dashboard JSON, and XAML.
- Property Snapshot list reviewed: yes; the new `Fuel.Burn.Analysis.*` properties route through the existing `Fuel.*` prefix into the Fuel/Strategy group.

## 2026-06-01 — PR #770 follow-up: current-tick checkpoint gating + precision filtered expiry
- Classification: **internal-only** (CarSA checkpoint/precision correctness follow-up; no export name, UI, dashboard, or user-workflow contract changes).
- Added `CarSAEngine.RefreshDirectCheckpointEligibility(...)` and invoked it immediately before both Opponents refresh paths so `TryGetCheckpointGapSec(...)` consumes current-tick `CarIdxTrackSurface` / `CarIdxOnPitRoad` eligibility instead of prior-tick `_carStates`; the normal CarSA update synchronizes the same fail-closed cache.
- Preserved direct-cache hygiene: ineligible cars still clear narrow direct timestamp/lap arrays and cannot record direct timestamps until eligible, removing the one-tick pit-entry/off-track seam window without changing Opponents fallback or ordering.
- Bounded slot-01 precision filtered fallback by the existing `GateGapTruthMaxAgeSec` truth-observation freshness window, preserving `fresh truth -> recently defensible filtered -> track fallback -> invalid` without adding sticky hold or mirroring `Gap.RelativeSec`.
- Preserved eligible-car 15-second direct lookup rule, same-lap/adjacent-lap formula, CarSA physical slots, `Gap.RelativeSec`, Opponents/H2H behavior, dashboards, JSON, and export names.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.

## 2026-06-01 — CarSA direct checkpoint seam pit-state gating
- Classification: **internal-only** (CarSA checkpoint-seam eligibility correction; no export name, UI, dashboard, or user-workflow contract changes).
- `TryGetCheckpointGapSec(...)` now fails closed unless both current car states are on-track, not on pit road, and explicitly report `TrackSurfaceRaw == OnTrack`; pit lane, pit stall/tow, off-track, NotInWorld, unknown, and invalid states fall back downstream.
- Added narrow per-car direct-checkpoint cache hygiene: ineligible cars clear `_carGateTimeSecByCarGate` / `_carGateLapByCarGate`, and new direct timestamps are not recorded while ineligible, preventing old pre-entry samples from becoming eligible immediately after pit exit.
- Preserved the existing 15-second rule for eligible cars, same-lap/adjacent-lap correction math, CarSA slot ordering, `Gap.RelativeSec`, slot-01 precision gaps, Opponents native fallback/race ordering, H2H, dashboards, and export names.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.

## 2026-06-01 — CarSA slot-01 precision-gap freshness fix
- Classification: **internal-only** (existing CarSA precision export correctness refinement; no export name, UI, dashboard, or user-workflow contract changes).
- `Car.Ahead01P.Gap.Sec` / `Car.Behind01P.Gap.Sec` no longer publish stale raw gate truth indefinitely: precision publication now requires truth age within the existing `GateGapTruthMaxAgeSec` limit.
- Precision gaps retain their sharper textual/number-display intent with source order `fresh truth -> defensible filtered -> track fallback -> invalid`; they do not inherit `Gap.RelativeSec` smoothing priority or sticky-hold semantics.
- Reused the existing filtered-cache defensibility rules (`valid lap-time map`, `filtered cache valid`, and `rate valid || truth fresh`) without changing `Gap.RelativeSec`, slot selection, Opponents, H2H, dashboards, or export names.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.

## 2026-06-01 — CarSA checkpoint adjacent-lap runtime-scale fix
- Classification: **internal-only** (CarSA checkpoint seam runtime dependency correction; no export, UI, dashboard, or user-workflow contract changes).
- `TryGetCheckpointGapSec(...)` now uses a CarSA runtime-owned lap-time scale for adjacent-lap correction instead of reading `_outputs.Debug.LapTimeUsedSec`.
- The runtime scale is assigned from the existing `SelectLapTimeUsed(...)` result regardless of System Debug state and is cleared with the existing gate-gap caches.
- Preserved `SelectLapTimeUsed(...)` order, adjacent-lap formula/sign and `abs(lapDeltaAtGate) <= 1` guard, same-lap behavior, invalid-scale return-false behavior, native downstream fallback, Opponents/H2H/League Class boundaries, and all export names.
- Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.
## 2026-06-01 — Fuel burn analysis sample-count split
- Classification: **both** (additive dashboard-facing readiness exports plus internal Fuel Model contract documentation).
- Added `Fuel.Burn.Analysis.AvgSampleCount`, `StintSampleCount`, and `SessionSampleCount` from the existing synchronized burn-analysis rolling list, current-stint count, and session count. Existing `Fuel.Burn.Analysis.SampleCount` remains present as a compatibility alias of `SessionSampleCount`.
- Dashboard guidance now maps Avg3/Avg5 readiness to `AvgSampleCount`, `CurrentStint` readiness to `StintSampleCount`, and `SessionAvg` readiness to `SessionSampleCount`.
- Preserved accepted-lap gating, all burn-analysis value calculations, existing reset actions, Fuel Model lifecycle resets, `Fuel.LiveFuelPerLap*`, `Fuel.FuelBurnPredictor*`, Strategy planner, pit/refuel math, dashboard JSON, and XAML.
- Property Snapshot list reviewed: yes; the new `Fuel.Burn.Analysis.*` properties route through the existing `Fuel.*` prefix into the Fuel/Strategy group.

## 2026-06-01 — Fuel burn analysis popup docs + rolling-list synchronization follow-up
- Classification: **both** (public dashboard binding discoverability plus internal list-read safety hardening).
- Added short public guidance for `LalaLaunch.Fuel.Burn.DisplayAnalysis`, `LalaLaunch.BurnDisplayToggle`, the optional scoped reset actions, and direct `LalaLaunch.Fuel.Burn.Analysis.*` dashboard consumption.
- Protected all `Fuel.Burn.Analysis.*` backing state with one dedicated lock across accepted-sample recording, scoped resets, lifecycle reset, and property evaluation. This keeps `CurrentStint` sum/count, `SessionAvg` sum/count plus `SampleCount`, `MaxObserved`, `LastLap`, and Avg3/Avg5 reads consistent during concurrent dashboard reads and reset actions.
- Preserved available-sample averaging, accepted-lap gating, all other reset semantics, `Fuel.LiveFuelPerLap*`, `Fuel.FuelBurnPredictor*`, Strategy planner, pit/refuel math, dashboard JSON, and XAML.

## 2026-06-01 — Fuel burn analysis popup plugin exports and actions
- Classification: **both** (new dashboard-facing plugin export/action contract plus internal Fuel Model observer state).
- Added presentation-only `Fuel.Burn.DisplayAnalysis` with `LalaLaunch.BurnDisplayToggle`, plus `Fuel.Burn.Analysis.LastLap`, `Avg3`, `Avg5`, `CurrentStint`, `SessionAvg`, `MaxObserved`, and `SampleCount`.
- New analysis state observes the existing accepted-fuel-lap insertion seam only: one combined chronological fresh accepted stream across wet/dry, excluding seeded model values and all laps already rejected by existing Fuel Model gating.
- Added independently scoped reset actions: `LalaLaunch.BurnAnalysisResetAverages`, `LalaLaunch.BurnAnalysisResetCurrentStint`, `LalaLaunch.BurnAnalysisResetSessionAverage`, and `LalaLaunch.BurnAnalysisResetMaxObserved`. Current-stint analysis also clears on the existing confirmed pit-exit edge; normal Fuel Model lifecycle resets clear the full analysis state.
- Existing accepted-lap logic, `Fuel.LiveFuelPerLap*`, `Fuel.FuelBurnPredictor*`, Strategy planner, pit/refuel math, dashboard JSON, and XAML remain unchanged.
- Property Snapshot list reviewed: yes; new `Fuel.Burn.*` properties route into the existing Fuel/Strategy group through the existing `Fuel.*` prefix rule.

## 2026-05-31 — Opponents CarSA checkpoint seam same-tick overwrite fix

- Fixed the ordinary `UpdateLiveProperties(...)` Opponents refresh so it passes the existing CarSA `TryGetCheckpointGapSec` delegate when CarSA is available instead of overwriting a same-tick preferred `Opp.Ahead1` / `Opp.Behind1` checkpoint gap with native progress fallback.
- Preserved the existing duplicate refresh timing, Opponents RaceProgress-first target selection, native fallback rules, pit-exit behavior, League Class matching, H2H selector ownership, and all export names.
- Changelog impact: internal runtime correctness fix; no public release-note update in this task.
- Property Snapshot list reviewed: yes, no group change required because existing `Opp.*` exports remain in the same `CarOppH2H` group and no property names changed.

## 2026-05-31 — PR #767 PreRace planner-authority review follow-up
- Classification: **both** (dashboard-facing PreRace authority correctness plus internal seam documentation).
- Manual Dry/Wet planner authority now rejects unknown live condition as well as known conflicts; automatic condition mode remains allowed.
- Added a PreRace-specific strict `0.01`-minute timed-race tolerance alongside the existing strict `0.001`-lap check without changing the shared coarse planner/live match helper.
- Reused the existing resolved Live Detect race-definition seam for PreRace authority matching (`IsLimitedSessionLaps` / `IsLimitedTime`, compatibility fields, and `SessionsXX` fallback), preventing raw positive `_SessionTime` from misclassifying lap-limited authority.
- Existing fallback calculation, formation allowance/burn-down, runtime pit/refuel families, dashboard JSON, and export list remain unchanged. Property Snapshot list reviewed: yes; existing Fuel/Strategy groups remain correct.

## 2026-05-30 — PreRace total-fuel planner authority gate
- Classification: **both** (dashboard-facing PreRace correction plus internal planner/PreRace seam documentation).
- Added a narrow PreRace total authority selector: matching planner total is used only when planner total, finite formation values, normalized car identity, canonical track key, race basis, race length, and manually forced wet/dry gates pass. Lap-limited authority adds a strict `0.001`-lap tolerance on top of the existing planner/live comparison seam.
- Matching path publishes `max(0, FuelCalculator.TotalFuelNeeded - FormationFuelPlanned + FormationFuelRemaining)`; rejected authority retains the existing live/session fallback (`base race fuel + active contingency + FormationFuelRemaining`).
- Preserved formation allowance and burn-down behavior. `Fuel.Pit.TotalNeededToEnd`, `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Pit.FuelControl.*`, runtime pit/refuel behavior, and dashboard JSON files remain unchanged. Existing `StrategyDash.*` start-fuel helpers inherit the corrected PreRace total indirectly through their existing adapter path.
- Property Snapshot list reviewed: yes; behavior changes remain inside existing `PreRace.*` and indirectly affected `StrategyDash.*` Fuel/Strategy groups, with no export list update required.

## 2026-05-29 — PreRace formation fuel exports and total-fuel refinement
- Classification: **both** (dashboard-facing PreRace export contract additions plus internal fuel/PreRace seam documentation).
- Added `LalaLaunch.PreRace.FormationFuelPlanned` and `LalaLaunch.PreRace.FormationFuelRemaining`; planned mirrors `FuelCalculator.FormationLapFuelLiters`, while remaining is clamped from planned before formation, burn-down during formation when live fuel is valid, and `0` once race-running starts.
- Refined `LalaLaunch.PreRace.TotalFuelNeeded` to use `base race fuel + active contingency + FormationFuelRemaining`; timed PreRace projection uses the runtime after-zero seam when available and falls back to planner after-zero when the runtime projection has not populated yet.
- No runtime pit/refuel behavior changed: `Fuel.Pit.TotalNeededToEnd`, `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, and pit command/send paths remain formation-excluded and untouched.
- Property Snapshot list reviewed: yes; new `PreRace.*` exports resolve to the existing Fuel/Strategy snapshot group.

## 2026-05-29 — Double-prefix LalaLaunch export registration cleanup
- Classification: **both** (dashboard-facing export naming fix + internal Property Snapshot grouping alignment).
- Changed the PreRace/Friends `AttachCore` registrations from literal `LalaLaunch.*` names to unqualified internal names (`PreRace.*`, `Friends.Count`), preventing accidental public `LalaLaunch.LalaLaunch.*` properties.
- Intended dashboard-facing names remain single-prefixed (`LalaLaunch.PreRace.*`, `LalaLaunch.Friends.Count`); dashboard package JSON files were not edited.
- Property Snapshot list reviewed: yes; `PreRace.*` now resolves to Fuel/Strategy and `Friends.*` to Car/Opp/H2H.

## 2026-05-29 — iRacingExtraProperties dependency audit
- Classification: **internal-only** (audit/report documentation; no runtime code, dashboard package, export, or behavior changes).
- Added `Docs/Internal/iRacingExtraProperties_Dependency_Audit.md` as the detailed audit record for remaining code/docs/dashboard references.
- Confirmed no active C# runtime reads or fallback dependencies on optional `IRacingExtraProperties` remain; remaining C# hits are warning strings/comments documenting removed fallback behavior.
- Dashboard `.simhubdash` packages were searched and reported for manual Dash Studio fixes only; no dashboard JSON/layout files were changed.
- Property Snapshot list reviewed: yes (no SimHub export/property add/remove/rename/behavior change).

- 2026-05-28: CarSA SOF League-aware cohort alignment landed.
  - `Car.iRatingSOF` now respects League Class effective cohort semantics when League Class is enabled and the player effective class resolves valid: SOF is averaged from current-session `Drivers##` rows whose resolved effective class matches the player effective class.
  - Fallbacks preserved: when League Class is disabled, player effective class is unresolved, or cohort rows are not resolvable, SOF remains the prior full-field positive-iRating average.
  - Player cohort authority now uses the live-player resolver path (same seam as other League player exports), so manual player override is honored for SOF cohort selection.
  - Per-row League SOF resolution now mirrors existing Drivers identity fallbacks (`UserID` -> `CustomerId`/`CustomerID`, `UserName` -> `AbbrevName`) and special-cases live player rows through the player resolver seam so the player iRating remains included under manual override cohorts.
  - CarSA slot selection/order/hysteresis and other League subsystem ownership boundaries unchanged.


## 2026-05-28 — Strategy Dash Advanced/Simple mode binding
- Classification: **both** (new user-facing binding/status exports plus internal contract/docs alignment).
- Added `StrategyDash.ModeToggle` as a plugin-owned SimHub action exposed in Dash Control → Bindings.
- Added persisted status exports `LalaLaunch.StrategyDash.AdvancedMode` (`true` = Advanced, `false` = Simple) and `LalaLaunch.StrategyDash.ModeText` (`ADVANCED`/`SIMPLE`) so dashboards can switch presentation without owning strategy logic.
- Behavior change is additive only: no StrategyDash advice math, fuel calculations, PreRace workflow, telemetry polling, or navigation behavior changed.
- Property Snapshot list reviewed: yes; the new `StrategyDash.*` exports resolve to the existing Fuel/Strategy snapshot group.

## 2026-05-28 — Fuel Revamp Phase 3B temporal semantics/lifecycle classification (documentation-only)

- 2026-05-28: PR #759 review-comment follow-up (documentation-only naming correction).
  - Corrected Phase 3B temporal-semantics section export names to use actual SimHub properties:
    - `Fuel.ProjectionLapTime_Stable` / `Fuel.ProjectionLapTime_StableSource` (instead of unqualified projection names),
    - `LalaLaunch.PreRace.*` (instead of `PreRace.*`),
    - `Fuel.Live.DriveTimeAfterZero` / `Fuel.Live.ProjectedDriveSecondsRemaining` (instead of unqualified names).
  - No runtime code/behavior changes; documentation contract accuracy fix only.

- Classification: **internal-only** (analysis + documentation alignment only).
- Added explicit temporal semantics/lifecycle classification matrix for fuel/pit/prerace/strategydash/projection/finish export families in `Docs/Subsystems/Fuel_Model.md`.
- Added direct answers for stable-vs-smoothed export usage (`LiveLapsRemainingInRace*`, `_S`, `_Stable_S`) and dashboard seam preferences.
- Added explicit future rationalisation candidate list and do-not-touch-yet export families.
- Confirmed no runtime code, export names, calculations, or dashboard contracts changed; documentation clarifies current behavior only.
- Property Snapshot list reviewed: yes (no export/property contract changes in this task).

- 2026-05-27: Pit-window contingency-aware feasibility update completed.
  - Updated pit-window OPEN/status gating to include contingency on required side for PUSH/STD/ECO checks (`needAdd = lapsRemaining*burn + contingencyForMode - currentFuel`), preserving priority order `CLEAR PUSH > RACE PACE > FUEL SAVE > TANK SPACE`.
  - Litre contingency uses shared litres across checks; lap-based contingency resolves litres per burn basis (push/stable/save), aligned with existing contingency resolver semantics.
  - Protected domains unchanged: `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.Pit.NeedToAdd`, `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`, `Fuel.Pit.Box.*`, pit-box display/countdown logic, pit command/control, and runtime refuel target/send behavior.
  - PR #758 follow-up: pit-window no-stop gate now checks reserve-protected shortfall before returning `N/A`; when base-distance no-stop is true but reserve is short, pit-window proceeds to contingency-aware feasibility states instead of exiting early.

## 2026-05-27 — PreRace status catalogue phase cleanup (next-stint advisory split)
- Classification: **both** (pre-race status wording/semantics + docs/catalogue alignment).
- Updated one-stop selected pre-race status decision so next-stint refuel setup advisories no longer dominate main pre-race strategy status in pre-grid/gridding:
  - Phase-routed split: in `PRE GRID`/`GRIDDING`, `2 STINT PLAN REQUIRES MORE FUEL` and `CHECK NEXT STINT FUEL` branches publish `SINGLE STOP POSSIBLE` (orange) as main status.
  - In `START READY`/`RACE`, those same branches retain advisory text (`2 STINT PLAN REQUIRES MORE FUEL` / `CHECK NEXT STINT FUEL`).
- Preserved unchanged behavior/invariants:
  - `OVERFUELLED` pre-race path unchanged (still actionable in main status when threshold is exceeded),
  - one-stop infeasible hard-stop `2 STINT PLAN NOT POSSIBLE` unchanged,
  - no changes to StrategyDash next-refuel target/delta math or runtime tactical families (`Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.Pit.*`, `Pit.FuelControl.*`, `Fuel.RequiredBurnToEnd*`),
  - no export additions/removals/renames.

- 2026-05-20: Documentation sweep (release-readiness) completed; documentation-only.
  - Added `Docs/Internal/Property_Snapshot_Debug_Workflow.md` as canonical internal workflow for Property Snapshot manual/rolling/replay usage, troubleshooting matrix, and escalation flow.
  - Updated `Docs/Project_Index.md` with topic owner matrix, terminology matrix, and release-history ownership rubric.
  - Clarified user-doc boundaries for Strategy vs runtime fuel authority and PreRace info-layer semantics.
  - Added compact Race End / Finish Lifecycle clarification in existing subsystem docs; no standalone RaceFinish subsystem doc introduced.
  - No runtime code/XAML/dashboard/settings/export/log-string changes.

- 2026-05-20: Strategy Save All target-ownership fix validated (planner-selected profile/track).
  - Fixed `FuelCalcs.SavePlannerDataToProfile()` to always persist Save All into the Strategy planner-selected target (`SelectedCarProfile` + `SelectedTrackStats/SelectedTrack`) instead of switching to live-session `ActiveProfile`/`CurrentTrackKey` when live telemetry is active.
  - Confirmation dialog now reliably reports the actual planner-selected profile/track saved by this path.
  - Preserved existing save scope split and fields (car-level + track-level planner persistence) and kept `Apply to Live Session` ownership unchanged.
- 2026-05-20: Early pre-grid restricted max-tank authority seam validated.
  - Runtime max-tank detection now prefers `DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverCarFuelMaxLtr * DriverCarMaxFuelPct` when both are valid, so restricted caps can publish before `GameData.MaxFuel`/`CarSettings_MaxFUEL` hydrate.
  - Added defensive normalization for `DriverCarMaxFuelPct` to support both fractional (`0..1`) and percent (`0..100`) inputs.
  - `Fuel.Setup.FuelLevel` semantics remain unchanged and are not used as max-tank authority.
- 2026-05-20: PR #752 follow-up validated (planner canonical combo + wet dry-basis + pending-context timing).
  - `ApplyLiveSession` now sets pending live context before `SelectedCarProfile` mutations, then upgrades pending track identity to canonical resolved key before `SelectedTrackStats` selection so both load phases can use guarded SimHub fallback when identities match.
  - Canonical track identity is now used consistently for combo-change detection and active context (`resolved TrackStats.Key` precedence), preventing repeated false new-combo detection from display-name/key mismatch.
  - SimHub/Default no-profile fuel fallback now seeds `_baseDryFuelPerLap` as a dry-equivalent basis (`dry: value`, `wet: value / factor` with safe invalid-factor fallback), preventing wet-factor double-application drift.

- 2026-05-20: PR #748 post-merge follow-up #3 validated (planner fallback edge-case hardening).
  - Strategy `ApplyLiveSession` now stores pending/active live track identity using canonical resolved `TrackStats.Key` when available (then `CurrentTrackKey`, then display name fallback), so planner live-context guard comparisons match selected track identity across key/display-name differences.
  - No-profile planner fuel fallback now seeds `_baseDryFuelPerLap` from the active fallback basis (`SimHub` or `Default`) when profile fuel is unavailable, preventing later wet-factor/condition refresh paths from collapsing fallback fuel to `0`.
  - Planner pit-lane loss now loads independently from dry-fuel profile availability and clears to safe `0` when unavailable; track-scoped reset also clears pit-lane loss, preventing stale previous-track carry-over on no-profile loads.

- 2026-05-19: PR #750 follow-up (Option B) validated — live tyre prediction decoupled from Strategy slider.
  - `Fuel.Live.TireChangeTime_S` full-4 basis now reads active profile `TireChangeTime` (sanitized non-negative) instead of planner slider value.
  - Strategy slider remains planner what-if only (still seeded/refreshed from profile), and no longer mutates live boxed tyre-time prediction basis.

- 2026-05-19: PR #745/#746 follow-up fixes validated (tyre timing refresh, boxed tyre-count latch, preset checkbox state).
  - Strategy planner tyre timing now seeds from active profile `TireChangeTime` on profile load/switch and live-updates when active profile tyre time changes; no manual slider touch required.
  - Boxed pit prediction now latches selected tyre count on in-box activation and uses the latched count for boxed target modeling, preventing during-service per-wheel clear-down (`4→0`) from reducing active boxed target.
  - Preset Manager `Tyres Expected` editor checkbox is now explicitly two-state (`IsThreeState=False`), and preset edit clone/save paths normalize `TyreStopExpected` from resolved compatibility intent (no indeterminate/null editor state).

## 2026-05-19 — PreRace/StrategyDash formation-fuel adapter parity
- Classification: **both** (pre-green dash/export guidance behavior correction + docs alignment).
- `LalaLaunch.UpdatePreRaceOutputs(...)` now adds planner formation fuel (`FuelCalculator.FormationLapFuelLiters`, non-negative clamp) exactly once into pre-race total fuel need when `SessionState < 3` (before formation starts).
- Affected pre-green exports now inherit formation-aware totals: `LalaLaunch.PreRace.TotalFuelNeeded`, `LalaLaunch.PreRace.Stints`, `LalaLaunch.PreRace.FuelDelta`, pre-race status text/colour, and `StrategyDash.StartFuelRequiredLitres`/advice/status, `StrategyDash.NextRefuel*`, `StrategyDash.RequiredStopsPreGreen`.
- Protected runtime families intentionally unchanged: `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, `Pit.FuelControl.*` (no formation double-count after green).


- 2026-05-19: Property Snapshot Fuel/Strategy capture follow-up for live pit-tyre prediction seams.
  - Added explicit Fuel/Strategy external snapshot rows for `LalaLaunch.Fuel.Live.TireChangeCount`, `LalaLaunch.Fuel.Live.TireChangeTime_S`, `LalaLaunch.Fuel.Live.TotalStopLoss`, `LalaLaunch.Pit.Box.TargetSec`, `LalaLaunch.Pit.Box.RemainingSec`, and `LalaLaunch.Pit.Box.ElapsedSec`.
  - This is a snapshot observability-only update (no runtime pit/strategy formula changes).

- 2026-05-19: Phase 3B runtime selected-tyre live pit prediction implemented.
  - Added new runtime export `Fuel.Live.TireChangeCount` (`0..4`) from DP per-wheel tyre flags (`dpLFTireChange/dpRFTireChange/dpLRTireChange/dpRRTireChange`) with conservative fail-open fallback to `4` when flags are unavailable or partial.
  - Updated `Fuel.Live.TireChangeTime_S` semantics from all-or-nothing gate to selected-count estimate: `count<=0 => 0`, `count>=4 => full4`, else `shared=1.0 + scaled variable` from learned full-4 tyre time.
  - `Fuel.Live.TotalStopLoss` and boxed target contracts remain unchanged, but now consume the improved live tyre estimate; Strategy `TyresExpected`/preset intent ownership remains planner-only and untouched.

- 2026-05-19: PR #745 review follow-up fixed preset editor intent hydration for legacy presets.
  - Preset clone/copy paths now persist explicit edit/save intent via `source.ResolvedTyreStopExpected` instead of nullable `source.TyreStopExpected`.
  - Preset Manager checkbox now opens legacy presets with resolved checked state (`>0` true, `==0` false, missing/invalid true) and saves explicit `TyreStopExpected`.
  - Strategy math and live pit prediction/export seams unchanged.


- 2026-05-19: Strategy preset tyre intent separation implemented.
  - Added preset field `TyreStopExpected` with compatibility resolution from legacy `TireChangeTimeSec` (`>0 => true`, `==0 => false`, missing/invalid => `true`).
  - Preset apply no longer overwrites Strategy `TireChangeTime` slider seconds; apply now sets tyre intent only.
  - Strategy stop calculations now gate tyre time through `EffectiveStrategyTyreTimeSeconds` (`TyresExpected ? TireChangeTime : 0`).
  - Main Strategy UI adds `TYRES EXPECTED` toggle; Preset Manager now edits tyre intent instead of preset tyre seconds.
  - Live pit prediction seams (`Fuel.Live.TireChangeTime_S`, `Fuel.Live.TotalStopLoss`, boxed-service model) intentionally unchanged.

- 2026-05-18: Property Snapshot rolling recording indicator visual polish landed.
  - Debug UI now shows rolling status in a bordered state pill beside rolling controls using existing resolver output (`OFF`/`READY`/`RECORDING`) with explicit visual emphasis (`OFF` neutral grey, `READY` cyan/blue standby, `RECORDING` bright green active).
  - Added status-driven button enablement polish in-plugin only: `START` disabled during `RECORDING`; `STOP` disabled when inactive.
  - No rolling automation logic, resolver semantics, exports, snapshot/group capture behavior, DataCore fuel capture, or rolling CSV schema changes.

- 2026-05-18: Property Snapshot rolling status stale-refresh follow-up (PR #740 review).
  - Wired status refresh callback to `Enable debugging mode` (`Settings.EnableSoftDebug`) and `Enable Property Snapshot` master toggle so `ROLLING CSV: OFF/READY/RECORDING` updates immediately when status gates change.
  - Existing refresh hooks for rolling CSV toggle, mode selector, START/STOP/RESET, group toggle interactions, and view init remain unchanged.

- 2026-05-18: Property Snapshot rolling status exports + DataCore fuel capture rows added (debug observability only).
  - Added core exports `Debug.PropertySnapshot.RollingStatusText` (`OFF`/`READY`/`RECORDING`) and `Debug.PropertySnapshot.RollingModeText` (`MANUAL`/`FREQUENCY`/`PER LAP`) from existing snapshot rolling gates/runtime state.
  - Added Property Snapshot external capture rows (Fuel/Strategy group only) via direct `PluginManager.GetPropertyValue(...)` reads for `DataCorePlugin.Computed.Fuel_LitersPerLap`, `Fuel_LastLapConsumption`, `Fuel_CurrentLapConsumption`, plus optional `Fuel_CurrentLapValidForTracking` and `Fuel_RemainingLaps`; missing values remain blank-safe.
  - Debug UI now shows `ROLLING CSV: <status>` beside rolling controls and refreshes on relevant control actions; no rolling schema/layout changes and no fuel authority logic changes.
- 2026-05-18: Tyre learn corrected persistence jack/drop allowance tuning landed.
  - Added `TyreLearnJackDropAllowanceSeconds=1.0` to corrected tyre learner outputs so persisted full-service estimates include observed post-service jack/drop release transition.
  - Derived path now computes `(firstClear-start) + 4*medianInterval + 1.0`; fixed-tail fallback now computes `raw + 6.0 + 1.0` when derived validation fails.
  - Existing bounds (>5s and <=60s), lock semantics, candidate gating, and non-tyre learning subsystems remain unchanged.

- 2026-05-18: Tyre learn Phase 2 corrected persistence landed.
  - Replaced diagnostic-only all-four tyre learner completion path with corrected persistence to `TireChangeTime` using lock-aware `SaveTireChangeTimeToActiveProfile(...)`.
  - Preferred correction uses per-wheel clear ordered intervals (`d1/d2/d3`) with median-per-tyre derived model: `(firstClear-start) + 4*medianInterval`; fallback uses fixed-tail `raw+6.0s` only when derived validation fails.
  - Keeps conservative all-four-only gating, bounded rejection logs, and existing lock semantics (`locked suppress overwrite`, `locked first-fill allowed when no usable stored value`).

- 2026-05-18: PR #737 review bugfix follow-up (hydration holds correctness).
  - ClassLeader/ClassBest identity hold now applies only when the resolved leader/best `CarIdx` is unchanged; car changes clear identity fields to prevent mismatched car-vs-name exports.
  - CarSA class-rank and driver-info cache holds now clear on session-token change (no cross-session hold leakage); same-session hydration holds remain.
  - ClassBest identity hold logging is now transition-latched (single enter/clear diagnostics, no per-tick spam).

- 2026-05-18: Drivers## metadata hydration hold/readiness follow-up landed.
  - Added Drivers-only hydration hold behavior for ClassLeader/ClassBest identity fields, CarSA iRating/class-est-lap cache, and CarSA class-rank map so short Drivers table gaps no longer clear metadata to blank/0/NaN.
  - `IsCarSaIdentitySourceReady()` now scans Drivers01..Drivers64 for any usable row identity/class metadata instead of relying only on `Drivers01.CarIdx` presence.
  - Added bounded one-time transition diagnostics for hold/recovery states; no `CompetingDrivers` fallback restored and no denominator ownership/count paths changed.
- 2026-05-18: PR #733 same-tick authority/refuel SIM provenance alignment fix landed.
  - Active DATA authority classification now shares the same current-tick fallback provenance used by runtime refuel basis resolution.
  - prevents contradictory same-tick exports (`Fuel.Refuel.BurnSource=SIM` with `Pit.FuelControl.DataText=DFALT`); genuine DataCore fallback now aligns to `SIM`/`SIMH`, synthetic/plugin fallback aligns to `DEFAULT`/`DFALT`.
  - no authority-order, pit-send, or refuel-formula behavior change.

- 2026-05-18: PR #733 runtime refuel SIM provenance propagation fix landed.
  - Runtime refuel path now propagates fallback provenance into `ResolveRuntimeRefuelBasis(...)`/`ResolveDataGovernedBurnAndPaceBasis(...)` so genuine DataCore computed fallback can emit `SIM`/`SIMH`.
  - Data-authority classification path that uses plugin-held stable fallback keeps `fallbackFuelIsSimHub=false`, so synthetic/default fallback still emits `DEFAULT`/`DFALT`.
  - no authority-order, pit-send, or refuel-formula behavior change.

- 2026-05-18: PR #733 follow-up SIM provenance guard fix landed.
  - DATA-governed burn resolver now emits burn source `SIM` only when fallback provenance is genuine `DataCorePlugin.Computed.Fuel_LitersPerLap`; plugin/synthetic fallback inputs now emit `DEFAULT`.
  - prevents false `Fuel.Refuel.BurnSource=SIM` / `Pit.FuelControl.DataText=SIMH` on synthetic/default fallback values (for example startup floor values).
  - no authority-chain order changes and no pit-send/refuel-math behavior changes.

- 2026-05-18: DATA authority label cleanup before PR #733 merge.
  - Active DATA transitional authority text renamed from `BUILD` to `PEND` (`Pit.FuelControl.DataText` / docs/UI contract) for clearer driver-facing meaning (live authority pending while fallback is active).
  - Numeric authority code remains unchanged (`Pit.FuelControl.Data == 1`), with no authority-chain logic change and no pit send/refuel math behavior change.

- 2026-05-18: DATA LIVE BUILD fallback authority/provenance fix landed.
  - `ResolveDataGovernedBurnAndPaceBasis(...)` no longer takes Strategy Planner `FuelCalculator.FuelPerLap` as DATA LIVE burn authority during BUILD fallback; LIVE now falls back burn authority in order: LIVE stable -> PROFILE stable/profile baseline -> SIMH (`DataCorePlugin.Computed.Fuel_LitersPerLap`) -> DEFAULT.
  - DATA LIVE BUILD now reports truthful burn provenance (`Fuel.Refuel.BurnSource=PROFILE` when profile burn is used), keeps lap PROFILE precedence in BUILD, and no longer requires Strategy-tab live/profile toggles to refresh in-use runtime burn authority.
  - SIMH fallback is now reachable and surfaced as runtime authority (`BurnSource=SIM`, `Pit.FuelControl.DataText=SIMH`) when profile burn is unavailable but SimHub computed burn is valid.
- 2026-05-18: Tyre learning correction instrumentation pass landed (diagnostic-only).
  - Added bounded one-line `[LalaPlugin:Tyre Learn] sample ...` diagnostics on clean all-four tyre candidates, including service start, per-wheel clear timestamps/offsets, first/last clear, pit service status/flags snapshots, pit-stop elapsed sample, and corrected-estimate comparisons (`+6.0s` fixed tail and derived-tail `+1.0s` jack allowance when derivable).
  - Added per-wheel clear timestamp capture within the tyre learner state machine for LF/RF/LR/RR clear events.
  - Follow-up expanded sample payload with wheel clear order (`LF/RF/LR/RR`), explicit clear timestamps (`tLF/tRF/tLR/tRR`), derived intervals (`d1/d2/d3`), avg/median interval metrics, per-tyre estimate, corrected 4-tyre estimate, retained current saved tyre time, and pit entry/exit timestamps.
  - Ordering fix: in `ServiceStarted`, per-wheel `1->0` clear transitions and first/last clear timestamps are now captured before evaluating `allFourCleared`, ensuring the final-wheel tick sample includes complete wheel order/interval metrics.
  - Optional tidy: sample offset fields now print `NA` when a wheel timestamp is unavailable instead of negative offset artifacts.
  - Diagnostic context fix: pit-entry edge now clears prior stop `pitExit` sample context; `savedNow` now reads direct runtime/profile stored tyre time (no tyre-selection-gated resolver), preventing stale-exit carry-over and false `savedNow=0` on all-four-clear samples.
  - Safety follow-up for PR #734: raw all-four-clear candidate path is now diagnostic-only and no longer persists `candidateSec` to `TireChangeTime`; sample logging remains active and now emits `diagnostic-only: raw candidate not persisted.` on valid candidate path.
  - `savedNow` diagnostic source preference now uses persisted profile value first (`ActiveProfile.TireChangeTime` when valid), then runtime fallback (`FuelCalculator.TireChangeTime`), else `0.0`.
- 2026-05-18: Property Snapshot rolling automation hardening follow-up (PR #736 review).
  - rolling START/STOP state is now runtime-only; persisted `PropertySnapshotRollingActive` is forcibly cleared false on first runtime tick so automation cannot silently resume after restart/reload.
  - START now hard-guards on `Soft Debug`, `Enable Property Snapshot`, and `Write rolling combined CSV`; when any gate is off, START is ignored with bounded warning.
  - reduced FREQUENCY cap from 5 Hz to 2 Hz because rolling wide writer rewrites/parses the whole CSV each capture; START log now explicitly documents this IO cost/cap rationale.
  - auto-capture logging is now throttled/heartbeat-only; full snapshot summary + rolling success logs remain on manual marker captures.
  - Property Snapshot rolling mode ComboBox binding switched to `SelectedIndex` for robust int binding semantics (`MANUAL/FREQUENCY/PER LAP` => `0/1/2`).

- 2026-05-18: Property Snapshot rolling automation modes (Part 2) validated.
  - added rolling capture modes via debug settings: `MANUAL` (0), `FREQUENCY` (1), `PER LAP` (2), with `START`/`STOP` state control and guarded frequency setting (`default 1 Hz`, capped `max 2 Hz`).
  - Event Marker behavior remains manual capture trigger; manual captures still write one-shot snapshot files and optional rolling snapshots per existing toggle.
  - automatic rolling captures now run only when rolling output is enabled and START is active: FREQUENCY uses capped interval timing; PER LAP reuses existing lap-cross seam and de-duplicates by completed lap number.
  - added `RESET ROLLING CSV` UI/action path that deletes only `PropertySnapshot_Rolling.csv` (primary + fallback path) with bounded info/warn logging.
  - preserved existing group filters, select-all sync, rolling wide schema, changed-vs-previous behavior, and one-shot folder semantics.

- 2026-05-18 final tidy: aligned active docs/contracts with Drivers-only identity and renamed Drivers-row counter helpers for clarity.
  - Active ClassLeader/ClassBest docs now describe `DriverInfo.Drivers##` identity seams only (no CompetingDrivers fallback contract).
  - Renamed denominator support helpers to `CountValidDriversRowsExcludingPaceCar` / `CountPlayerClassDriversRowsExcludingPaceCar` to match current `Drivers##` data source usage.

- 2026-05-18 PR follow-up: removed remaining `DriverInfo.CompetingDrivers` runtime reliance and hardened native class-match identity for denominator fallback.
  - Race and League denominator support paths now consume `DriverInfo.Drivers##` only (including roster count, CarSA class-rank map source, identity and driver-info resolution helpers, and class-short resolution fallback).
  - `GetNativePlayerClassDriverCount()` now matches by native class ID (`PlayerCarClassID`/`DriverCarClassID` + row `CarClassID`) with class-name fallback, reducing short-name/id mismatch zero-count risk.

- 2026-05-18: League race class denominator source corrected to strict `DriverInfo.Drivers##` subclass cohort for League ON.
  - `GetLeagueClassPlayerDriverCount()` and `ResolveCanonicalPlayerClassRaceDenominator()` support paths now use strict current-session `DriverInfo.Drivers##` row scanning/resolution for League subclass counts (pace-car excluded), with no CSV membership fallback as race denominator authority.
  - `Race.PlayerClassFieldSize` / `RaceFinish.PlayerClassFieldSize` canonical seam now uses strict League subclass count when available, else native/session fallback only.

- 2026-05-17: Fixed live `Race.PlayerClassFieldSize` attach path to call canonical race denominator helper directly.
  - Removed the remaining indirection method for live export so League-enabled live publish cannot take a stale/legacy override path; `RaceFinish.PlayerClassFieldSize` freeze path and denominator helper logic unchanged.

- 2026-05-18: Property Snapshot group audit + Codex contract guardrail.
  - audited Property Snapshot grouping against current `AttachCore`/`AttachVerbose` export surface and inventory/changelog references;
  - updated snapshot grouping coverage so `Race.*`, `RaceFinish.*`, `ClassBest.*`, and `ClassLeader.*` map into `Car/Opp/H2H` (instead of defaulting to `Raw Debug`);
  - grouped `Pace.*` and `Surface.*` into `Fuel/Strategy` to keep race-planning fuel/pace observability aligned with existing snapshot semantics;
  - no export names changed and no runtime subsystem calculations were modified;
  - added mandatory `CODEX_CONTRACT` rule requiring same-task Property Snapshot review whenever SimHub exports/properties are added/removed/renamed/behavior-changed, with required verification line: `Property Snapshot list reviewed: yes/no, with reason.`

- 2026-05-17: Strategy Planner profile fuel preview stale-label fix + Property Snapshot include alignment.
  - fixed track/profile clear/reload UI refresh so Profile-mode AVG/ECO/MAX preview labels immediately clear to neutral (`-`) when selected profile/track fuel data is missing/cleared, without requiring a Live Snapshot -> Profile toggle.
  - root cause: `ResetTrackScopedProfileData()` reset display fields but did not raise `PropertyChanged` for `ProfileAvgFuelDisplay`/related AVG row bindings, allowing stale text to remain until a later mode-driven refresh.
  - Property Snapshot group/include docs now explicitly include new Pit Fuel Control export surface from PR #727 (`Pit.FuelControl.Data`, `DataText`, `DataColor`, `Source`, `SourceText`, `TargetLitres`, `TargetText`).

- 2026-05-17: PR #730 follow-up fixed tyre-time value/lock copy parity in profile clone paths.
  - `NewProfile()` default-seed clone now copies `TireChangeTime` together with `TireChangeTimeLocked`.
  - `CopyProfileProperties(...)` now copies `TireChangeTime` together with `TireChangeTimeLocked`.
  - removed unused `_tyreLearnLastAllFourSelected` field/assignments from `LalaLaunch` (no behavior change to learner state machine).

- 2026-05-16 RaceFinish player finish-time baseline latch follow-up landed.
  - `TryCaptureRaceFinishPlayerSnapshot(...)` now latches the first observed player-finish tick session timestamp before class-position retry gating.
  - If class position is temporarily unresolved (`<=0`) and capture retries on a later tick, `RaceFinish.PlayerFinishGapSec` now computes from the latched first-finish timestamp (no retry-path drift).
  - Latch clears when finish condition is no longer active or when player snapshot capture completes.

- 2026-05-16 RaceFinish deferred-identity apply-current-observed follow-up landed.
  - Deferred-reset apply path now uses pending identity only when it still matches the current observed session identity; otherwise pending is discarded and current observed identity is applied, preventing stale `B` apply when observed identity is `C`.
  - `A→B→A` stale-pending clear now also resets `_finishTimingSessionChangeDeferredInActiveLifecycle` so future defers log once again.

- 2026-05-16 PitExit League cohort authority follow-up landed.
  - `BuildRaceContextLeagueClassMatchDelegate()` now locks cohort authority to the live player effective class whenever League Class is enabled and the live player resolves valid, instead of allowing per-row player-resolution fallback to keep native-equivalent cohorts in some PitExit update states.
  - Candidate rows must resolve a valid effective class to join that effective cohort; unresolved candidates are excluded in League-active effective mode.
  - When League Class is enabled but live player effective class is unresolved, fallback remains native class-colour matching only when both rows have valid native class colours; otherwise `false` (no match-all).
  - Added narrow League settings/definition change invalidation for Opponents/PitExit (`_opponentsEngine.Reset()` on next tick) to avoid stale colour-only carry-over after mode/definition toggles.
  - PitExit timing/gap/countdown/loss/distance math unchanged; Opp race-slot ordering/gap math unchanged; CarSA/H2H/fuel unchanged.

- 2026-05-15 RaceFinish deferred-identity stale-clear follow-up landed.
  - `UpdateFinishTiming(...)` now clears pending deferred session identity when observed identity reverts to current active identity before lifecycle exit (`A→B→A` transient churn), preventing stale deferred identity apply on later genuine session changes.

- 2026-05-15 RaceFinish deferred session-reset detectability follow-up landed.
  - `UpdateFinishTiming(...)` now stores pending session identity while defer guard is active and does not overwrite current finish-session identity until reset can safely execute after lifecycle exit.
  - Preserves no-spam defer behavior during active post-finish lifecycle, then performs one clean reset + pending identity apply + existing LiveDetect fuel recalc path.

- 2026-05-15 RaceFinish reset/capture loop guard + class-position freeze guard landed.
  - `UpdateFinishTiming(...)` now defers `ResetFinishTimingState()` when session identity churn is observed during an already-active post-finish lifecycle (`SessionState>=5` with active RaceFinish snapshot), preventing per-tick reset/re-capture log spam in replay/finish phases.
  - Added bounded diagnostic log: `[LalaPlugin:Finish] session_change deferred in_active_finish_lifecycle state=...`.
  - `TryCaptureRaceFinishPlayerSnapshot(...)` now requires a positive live class position before freezing player snapshot (except `SessionState==6` safety fallback), preventing `RaceFinish.PlayerClassPosition=0` freeze when valid class position resolves a tick later.


- 2026-05-18: PR #736 review follow-up hardened Property Snapshot PER LAP write path with local exception guard.
  - wrapped `MaybeWritePropertySnapshotPerLap(...)` rolling write in `try/catch` and downgraded write failures to bounded warning logging, preserving lap-tick processing continuity and existing manual/frequency error-handling behavior.

## 2026-05-15 — PR #722 follow-up: unresolved-player native fallback now requires valid native class colours
- Classification: **internal-only** (cohort fallback correctness hardening; no export/format changes).
- In `BuildRaceContextLeagueClassMatchDelegate()`, unresolved-player fallback now performs native class-color compare only when both `playerRow.ClassColor` and `candidateRow.ClassColor` are non-empty; missing colour now returns `false` (prevents accidental match-all cohorting).
- `TryBuildRaceContextNativeCarRow(...)` now carries `ClassColor` into `NativeCarRow` so native fallback can still work when native class-color data is available.
- Protected domains unchanged: League enabled+valid effective-class behavior, disabled behavior, PitExit timing/gap/countdown/loss/distance math, Opp race-slot math, CarSA, and H2H.

## 2026-05-15 — PitExit League-class cohort selection fix (effective-class matcher delegate reliability)
- Classification: **both** (PitExit cohort target correctness + docs alignment).
- Fixed `BuildRaceContextLeagueClassMatchDelegate()` so League race-context matching no longer returns `null` solely because the preview player resolver is transiently unresolved.
- The delegate is now created whenever League Class is enabled, and resolves player effective class from the current race-model player row (`UserID`/name) at match time.
- When enabled but player effective class is unresolved at match time, class matching falls back to native class-color matching (existing fallback preserved).
- Result: `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, `PitExit.Ahead/Behind.Name|CarNumber|GapSec`, and `PitExit.Summary` now reliably switch to effective-class cohort selection when enabled+resolved (instead of remaining native-only with color-only League presentation).
- Protected domains unchanged: PitExit timing/gap/countdown/loss/distance math, Opp race slot selection, CarSA, and H2H logic.
## 2026-05-14 — Planner live fuel-session validity + pit delta lifecycle correction
- Classification: **both** (strategy planner source correctness + pit delta lifecycle correction + docs alignment).
- Strategy Planner (`FuelCalcs`) live-snapshot fuel path now enforces current-session validity:
  - live fuel cache is cleared on new car/track live-session combination,
  - LiveSnapshot fuel auto-fill falls back to profile condition fuel when live fuel is unavailable for the current session,
  - `FuelPerLapSourceInfo` now correctly reports Profile source on that fallback path (no stale `Live` carry-over label).
- Reverted the prior pit-entry clear behavior for `Pit.LastPaceDeltaNetLoss`; lifecycle returns to prior behavior (latched on completed pit-cycle calculation and cleared by existing reset paths only).
- `Pit.Box.LastDeltaSec` now stays latched after stop for review, clears at next boxed-stop activation, and clears on reset/session-reset paths (no short auto-expire window).
- PR review follow-up: Live Snapshot fuel source labels now recover to `Live avg` even when the returning live value is within fuel deadband, preventing stale profile-source labeling during live-valid periods.

## 2026-05-14 — StrategyDash start-fuel setup fallback now respects pre-race phase gate
- Classification: **both** (dash-facing start-fuel advice correctness + docs alignment).
- `StrategyDash.StartFuelAdviceText` / `StrategyDash.StartFuelStatus` now use setup-fuel fallback only when pre-race/grid/formation fallback is allowed (`SessionState < 4`), while keeping live-fuel-first precedence and unknown fallback behavior unchanged.
- Active race-running (`SessionState == 4`) no longer allows setup fuel to mask live start-fuel evaluation on StrategyDash.
- `1.0 L` start-fuel tolerance and advice text/status contract remain unchanged (`START FUEL OK/MAX`, `CHECK START FUEL`, `ADD START FUEL`; `0/1/2`).

## 2026-05-14 — Fuel dash support exports + capped fuel-control target
- Classification: **both** (new dash-facing fuel exports + pit fuel-control display/target cap behavior alignment).
- Added new exports: `Fuel.Live.RemainingStints` (1dp runtime stints projection on selected refuel burn basis), `Fuel.MaxTank` (runtime effective max-tank authority seam), `Fuel.PitStopsRequiredByFuelExact` (1dp non-ceiled companion), and `Pit.FuelControl.TargetText` (`"{N}L"` / `"{N}L MAX"`).
- Kept `Fuel.PitStopsRequiredByFuel` unchanged as rounded-up integer behavior; new exact export is additive only.
- Capped `Pit.FuelControl.TargetLitres` to valid runtime max tank while preserving numeric contract and non-negative clamp; invalid max tank keeps existing fallback behavior.
- Updated `Pit.LastPaceDeltaNetLoss` lifecycle to persist after stop and clear on next pit entry/session reset only.

- 2026-05-13 Pit Fuel Control legacy PushSave surface removal landed:
  - removed legacy action `Pit.FuelControl.PushSaveModeCycle`; canonical DATA actions remain (`SetDataLive`, `SetDataPlan`, `CycleData`);
  - removed legacy compatibility exports `Pit.FuelControl.PushSaveMode` / `Pit.FuelControl.PushSaveModeText`;
  - removed legacy PushSave compatibility settings/property wiring in favor of canonical settings-backed DATA mode (`PitFuelControlDataMode` / `PitFuelControlDataPlanModeEnabled`);
  - kept DATA SAVED guard behavior unchanged and renamed UI wording to `DATA SAVED Push/Save Guard (%)`.

## 2026-05-13 — After-stop delta DATA-governance + selected export
- Classification: **both** (runtime fuel delta behavior adjustment for existing exports + new dash-facing export).
- Updated `Fuel.Delta.LitresPlan`, `Fuel.Delta.LitresPlanPush`, and `Fuel.Delta.LitresPlanSave` to keep their existing meaning (after planned stop/add) while making burn/lap basis follow Pit Fuel Control DATA (`LIVE` uses live/stable basis, `SAVED` uses planner/profile basis only).
- Added `Fuel.Delta.AfterStop.Selected` export selecting from `LitresPlan*` by Pit Fuel Control SOURCE (`PUSH`, `SAVE`, `NORM`, `STBY->NORM advisory`).
- Protected invariants: no export renames/removals, no pit-command send-path changes, and no changes to `Fuel.Refuel.*`/current-stint delta families.
- PR review follow-up fixes: DATA SAVED after-stop delta path no longer allows runtime fallback fuel authority (`fallback=0` when DATA SAVED), and invalid after-stop DATA-governed basis now clears `Fuel.Delta.LitresPlan*` plus `Fuel.Delta.AfterStop.Selected` to prevent stale selected export values.
- Late PR review follow-up: `ResolveDataGovernedBurnAndPaceBasis(...)` now guards missing `GameData/NewData` so startup DATA SAVED snapshot paths fail cleanly instead of null-dereferencing (`stableLap/simLap` read only when game data is present).
- P0 build-fix follow-up: replaced missing after-stop helper callsites with existing canonical seams (`TryResolveDataGovernedProjectedLapsRemaining(...)` + `ResolveActiveContingency(...)` context) and added a narrow local contingency conversion helper to preserve DATA-governed after-stop semantics without touching `Fuel.Refuel.*` ownership.
- Reviewability tidy-up: re-indented/braced the inserted after-stop DATA block for clearer PR-side diff readability; behavior unchanged outside compile restoration.
- 2026-05-13 Pit Fuel Control DATA alignment follow-up landed:
  - `BuildPitFuelControlSnapshot()` now applies DATA basis selection to `NORM` as well as `PUSH/SAVE`;
  - `DATA LIVE + NORM` remains runtime/live stable normal target behavior;
  - `DATA SAVED + NORM` now uses planner/profile normal-basis target calculation (and rejects LIVE/SIM authority on this path);
  - `PUSH/SAVE`, DATA reset/toggle behavior, source/mode cycling, AUTO arming, and command-send behavior remain unchanged.
  - review follow-up: fixed `ResolveDataGovernedBurnAndPaceBasis(...)` call-site argument names in `TryResolvePlanNormNeed(...)` to match actual signature and avoid build failure.
  - review follow-up: removed stale Dash Integration wording that still implied `NORM` is always runtime/live.
  - review follow-up: threaded live `GameData` through `TryResolvePlanNormNeed(...)` into `ResolveDataGovernedBurnAndPaceBasis(...)` to prevent null `data` dereference risk on PLAN + NORM snapshot path.
- 2026-05-13 PitExit League Class color resolver identity fix landed:
  - PitExit ahead/behind class-color publication now passes selected target `UserID` into the existing race-context League resolver seam (instead of null identity), restoring CSV-only League Class color resolution for `PitExit.Ahead.ClassColor` / `PitExit.Behind.ClassColor`;
  - suffix-only and CSV-then-suffix behavior remain unchanged; native fallback and `0xRRGGBB` format are preserved;
  - no PitExit target-selection, predicted-position, gap/timing/countdown/loss/distance math changes.

## 2026-05-13 — League Class authority alignment for remaining player + PitExit class-facing exports
- Classification: **both** (dash-visible class presentation/cohort semantics + docs alignment).
- `Car.Player.PositionInClass` now publishes through the existing effective-position seam (`GetEffectivePositionInClassForPublishedContext`), so enabled+resolved League Class uses effective cohort position and disabled/unresolved stays native fallback.
- `Car.Player.ClassName` / `ClassColor` / `ClassColorHex` behavior remains League-aware with native fallback unchanged.
- `PitExit.Ahead.ClassColor` and `PitExit.Behind.ClassColor` now publish through the existing League race-context presentation seam while preserving the existing `0xRRGGBB` family contract and native fallback semantics.
- `PitExit.PredictedPositionInClass`, pit-exit target selection, and pit-exit timing/gap/countdown/loss/distance calculations remain unchanged.

- 2026-05-11 RaceFinish class denominator follow-up fix landed:
  - `ResolveRaceFinishLiveClassFieldSize(...)` source ordering now prefers existing effective class driver-count seam first, then native `Telemetry.OpponentsInClassCount + 1`, then SimHub baseline `GameData.NewData.OpponentsInClassCount + 1`; returns `0` only when all are unavailable/invalid;
  - preserves valid solo-class semantics (`0` opponents -> field size `1`) and keeps class snapshot/player snapshot/finish timing semantics unchanged.

## 2026-05-11 — Fuel burn runtime authority fix (PROFILE over SimHub fallback)
- Classification: **both** (runtime fuel authority-chain correctness + docs/log contract alignment).
- In `LalaLaunch`, no-accepted-lap runtime assignment now resolves `LiveFuelPerLap` to active-condition profile fuel when available, only falling back to `DataCorePlugin.Computed.Fuel_LitersPerLap` as last resort.
- Added bounded fuel-burn authority log on no-live-lap source/value transition (`[LalaPlugin:Fuel Burn] runtime burn basis selected ...`).
- Follow-up: narrowed transition-log signature to selected authority source only, preventing log churn from unused fallback/profile input jitter when authority is unchanged.

- 2026-05-11 property snapshot final polish pass:
  - UI cleanup: `Select All` now drives all Property Snapshot group checkboxes, and individual group toggles now back-sync `Select All`;
  - rolling hardening: snapshot value text now normalizes CR/LF to literal `\n` before CSV write to keep wide rolling reload line-safe.

- 2026-05-10 rolling CSV parser correctness fix:
  - rolling wide-file reload now parses CSV with quote-aware logic (no naive `Split(',')`), preventing comma/quote-containing values from corrupting column alignment over subsequent captures.

- 2026-05-10 rolling snapshot layout + fallback path bugfix:
  - rolling `PropertySnapshot_Rolling.csv` now writes in wide format (`SimHubProperty` rows, one new timestamp column per capture) for easier left-to-right comparison;
  - one-shot fallback writes now preserve relative subfolder paths (including `PropertySnapshots`) instead of flattening to fallback root.

- 2026-05-10 property snapshot changed-values observability tweak:
  - added per-capture summary log with `includeChanged`, row count, and `changed1/changed0/changedNA` totals to make changed-comparison behavior explicit during testing.

- 2026-05-10 property snapshot one-shot file organization tweak:
  - one-shot `PropertySnapshot_<...>.csv` files now write under `.../Logs/LalaPluginData/PropertySnapshots/` for cleaner folder organization;
  - rolling `PropertySnapshot_Rolling.csv` remains at `.../Logs/LalaPluginData/`.

- 2026-05-09 property snapshot path parity + gating diagnostics fix:
  - aligned snapshot primary CSV path with existing plugin CSV writers (`<SimHub install>/Logs/LalaPluginData`) and kept Documents fallback on write/append failure;
  - added one-time warning when Property Snapshot is enabled while Soft Debug is off, plus one-time info log of resolved primary/fallback snapshot paths.

- 2026-05-09 property snapshot per-press failure handling fix:
  - failed marker snapshot attempts now stamp the processed press count in the failure path to prevent repeated retries/log spam from a single press; next snapshots still require a new marker press.

- 2026-05-09 property snapshot write-path hardening follow-up:
  - snapshot one-shot and rolling writes now retry to Documents fallback when Program Files primary path fails during file write/append (including ACL-denied existing-folder cases);
  - snapshot export no longer permanently disables after a single primary IO failure;
  - added action-bounded diagnostics for marker press registration, snapshot write success, rolling append success, and fallback-path usage.

## 2026-05-09 — Finish semantics correction: SessionState 5 is overall lifecycle, class finish remains independently resolved

- Corrected finish model: `SessionState 4->5` is treated as overall race lifecycle / overall-leader finish phase, not an unconditional player-class-leader finish signal in multiclass.
- `Race.ClassLeaderHasFinished` remains primarily driven by resolved class-leader per-car finish-like flags, with multiclass lifecycle-equivalence fallback only when class/overall identities are provably equivalent (`same car`) or class leader is already on a higher lap than overall leader once session enters `state>=5`.
- `RaceFinish.ClassSnapshotActive` continues to key from `Race.ClassLeaderHasFinished` (plus `SessionState==6` safety fallback), preventing blind SessionState-only class snapshot capture in multiclass.
- `RaceFinish.PlayerSnapshotActive` remains player-perspective: per-car finish-like flags and robust player-checkered seams (`GameData.Flag_Checkered` / `SessionFlagsDetails.IsCheckered*`) before `SessionState==6` safety fallback.
- Preserved split-stage snapshot architecture and Race.EndPhase semantics; no dashboard ownership changes.

- 2026-05-09 follow-up: corrected multiclass class-finish lifecycle fallback reference model.
  - removed invalid `class leader lap > overall leader lap` assumption,
  - now captures dynamic finish reference pct from overall leader at `SessionState 4->5`,
  - class-finish fallback now uses class-leader own crossing/wrap evidence against that dynamic reference,
  - includes guarded already-crossed-on-transition path requiring credible prior sampled class-leader pct evidence.
- 2026-05-09 pre-merge follow-up: restored valid solo-class denominator behavior (`OpponentsInClassCount>=0 => +1`) and tightened dynamic finish-reference latch to require true prior->current crossing evidence (wrap/already-crossed guards unchanged).
- 2026-05-09 follow-up: anchored class-leader post-lifecycle crossing/wrap detection to post-`SessionState>=5` samples only; pre-transition sample evidence is now limited to guarded already-crossed-on-transition detection.
- 2026-05-09 follow-up: finish-reference pct capture now retries on post-transition `SessionState>=5` ticks while reference is unset, so a missing first SS5 sample no longer leaves multiclass fallback unarmed for the full lifecycle.

## 2026-05-09 — RaceFinish replay player-cross fallback + class field-size freeze reliability fix

- Behavior change: `RaceFinish.PlayerSnapshotActive` can now trigger before `SessionState==6` fallback when replay/fast-forward misses player finish-like `CarIdxSessionFlags` bits.
- Refined player snapshot trigger for replay reliability to use robust player-checkered seams (`DataCorePlugin.GameData.Flag_Checkered` and `SessionFlagsDetails.IsCheckered*`) as primary player-finish evidence when class snapshot is active and player snapshot is pending.
- Preserved priority hierarchy and invariants: per-car finish-like flags + player checkered seams trigger player capture before fallback; `SessionState==6` remains last-resort safety fallback.
- Fixed `RaceFinish.PlayerClassFieldSize` freeze reliability by avoiding zero-opponent telemetry lock-in at class snapshot and using existing effective class cohort fallback when available.
- Result: player-facing frozen `RaceFinish` values now freeze at actual player finish crossing in replay paths instead of drifting until session complete fallback.
## 2026-05-09 — Debug Property Snapshot CSV system (Event Marker-triggered)
- Follow-up test fix: Property Snapshot export path now targets `Program Files (x86)/SimHub/Logs/LalaPluginData` (fallback `Documents/SimHub/Logs/LalaPluginData` if Program Files x86 is unavailable) to align with active SimHub install log location.
- Follow-up review fix: snapshot trigger now consumes/stamps marker press count while snapshot mode is disabled, preventing stale disabled-window presses from firing immediate captures when re-enabled later in the same session.
- Follow-up review fixes: `Car.Debug.*` now classifies into `RawDebug` before generic `Car.*`, and snapshot triggering now keys off per-press marker count (not pulse-edge bool) so every Event Marker action produces a snapshot even inside the 5s pulse window.
- Follow-up review fix: added missing CSV sanitizer/value-string helpers used by Property Snapshot writer (`SanitizeCsvValue`, `SnapshotValueToString`) so build path compiles and CSV quoting handles commas/quotes/newlines safely.
- Follow-up review fixes: property snapshot writes are now guarded by failure handling (one-time disable + warning on IO exceptions), and rolling CSV schema is now stable with always-present `ChangedVsPrevious` column (`NA` when changed-comparison is disabled for that capture).
- Classification: **both** (new debug settings/workflow + internal docs alignment).
- Added Debug `Enable Property Snapshot` flow: when enabled and Event Marker is pressed, plugin writes `PropertySnapshot_<UTC>_Sess<SessionTimeSec>.csv` under `SimHub/Logs/LalaPluginData/`.
- Snapshot rows include `SimHubProperty`, `InternalSource`, `Value`, `GroupType`; optional `ChangedVsPrevious` column compares values against the prior marker snapshot (`NA` when no baseline exists).
- Added checkbox-scoped group filtering (`Select All`, Fuel/Strategy, Car/Opp/H2H, Pit/PitExit, Shift Assist, Message System, League Class, Raw Debug) and optional rolling append output (`PropertySnapshot_Rolling.csv`).
- Added core export `Debug.PropertySnapshotEnabled` (`1`/`0`) for dash/debug visibility.

## 2026-05-09 — RaceFinish live-then-freeze player fields + frozen denominator exports
- Classification: **both** (dash-facing RaceFinish behavior refinement + new exports + docs alignment).
- Kept finish detection ownership unchanged (`Race.EndPhase` / class/player snapshot triggers remain the same seams); changes are constrained to `RaceFinish` display behavior.
- Player-facing RaceFinish fields now publish neutral defaults before class snapshot, live values while class snapshot is active and player snapshot is pending, then freeze at player snapshot:
  - `RaceFinish.PlayerOverallPosition`, `RaceFinish.PlayerClassPosition`,
  - `RaceFinish.PlayerFuelLeft`,
  - `RaceFinish.PlayerBestLap`, `RaceFinish.PlayerBestLapSec`.
- Added frozen denominator exports for clean `Pxx / yy` finish rendering:
  - `RaceFinish.PlayerOverallFieldSize`,
  - `RaceFinish.PlayerClassFieldSize`.
- Denominator exports now freeze at class snapshot activation (not player snapshot) to prevent dropout/quitter denominator shrink between leader finish and player finish.
- `RaceFinish.PlayerOverallPosition` remains strict overall-rank semantics only (`PlayerLeaderboardPosition` source); no class-position fallback is used.
- `RaceFinish.PlayerFinishGapSec` remains canonical gap-to-leader timer (`class-winner-finish -> player-finish` elapsed), and `RaceFinish.ClassWinnerGapSec` remains compatibility mirror only.

- 2026-05-09 follow-up: CSV fallback player `+1` is now mode-gated.
  - player-included increment no longer runs in non-CSV modes (`NameOnly`/`Disabled`) during live-row outages; this keeps outage behavior aligned with active mode semantics.
- 2026-05-09 follow-up: preserved player-included cohort semantics in LeagueClass.Player.DriverCount CSV fallback.
  - when live competing-driver rows are unavailable, CSV fallback now adds the player into cohort count unless the player already has a valid CSV membership in the same effective class.
- 2026-05-09 follow-up: gated LeagueClass.Player.DriverCount CSV fallback by active League Class mode.
  - CSV fallback cohort count now runs only in CSV-capable modes (`CsvOnly` / `CsvThenName`), preventing stale in-memory CSV counts from leaking into `NameOnly` semantics during live-row outages.
- 2026-05-09 follow-up: LeagueClass.Player.DriverCount CSV fallback now applies class-definition enablement gate.
  - CSV fallback cohort count now returns `0` for disabled class definitions, matching resolver behavior (`Source=NATIVE` invalid path) and avoiding fallback/live semantic drift.

- 2026-05-09 LeagueClass.Player.DriverCount cohort-count reliability fix:
  - fixed enabled+resolved League Class cases where `LeagueClass.Player.DriverCount` could publish `0` despite valid player class resolution and loaded CSV mappings.
  - added live-row name fallback probes (`UserNameRaw`, `UserNameProcessed`) in the competing-driver scan path.
  - added fallback to CSV cohort count for the resolved player class when live competing-driver identity rows are temporarily unavailable; native-class fallback behavior when League Class is disabled/unresolved remains unchanged.
## 2026-05-08 — RaceFinish split snapshots + player finish gap timer
- Classification: **both** (RaceFinish contract refinement + dash-facing behavior update).
- Refactored RaceFinish from single-shot capture to split stages:
  - class snapshot (`RaceFinish.ClassSnapshotActive`) captures class winner identity when class winner finishes;
  - player snapshot (`RaceFinish.PlayerSnapshotActive`) captures player fields and class-best lap when player finishes.
- `RaceFinish.Active` now means either stage is active (`ClassSnapshotActive || PlayerSnapshotActive`).
- Added `RaceFinish.PlayerFinishGapSec` as class-winner-to-player elapsed finish timer:
  - `0` before class snapshot,
  - live increasing while class snapshot active and player snapshot pending,
  - frozen final value when player snapshot activates.
- `RaceFinish.ClassWinnerGapSec` now mirrors `PlayerFinishGapSec` for compatibility.
- Primary triggers use per-car finish-like flags; SessionState `6` remains fallback-only for missing captures.
- Class snapshot now gates activation on usable class-winner identity (`ClassLeaderValid` + leader idx + non-blank identity fields), with held-last-valid identity fallback; if identity is still unavailable, capture retries until valid identity appears or SessionState `6` forces last-resort fallback.
- `PlayerFinishGapSec` timer start now requires an identity-backed class snapshot; SessionState `6` fallback with missing identity does not start a live class-finish timer.

## 2026-05-08 — Finish detection phase 1: per-car session-flag authority for class/overall leader finish latches
- Classification: **both** (runtime finish-latch semantic correction + docs alignment).
- `UpdateFinishTiming(...)` now prefers per-car `CarIdxSessionFlags` finish-like bits (`checkered`/`crossed`) to latch `Race.ClassLeaderHasFinished` and `Race.OverallLeaderHasFinished`.
- Overall leader resolution now prefers the true overall leader identity seam (`CarIdxPosition==1 && CarIdxClassPosition==1`) with existing fallback to position-only leader idx when needed.
- SessionState remains lifecycle authority and is retained only as single-class backup for `Race.OverallLeaderHasFinished` when per-car flags are unavailable.

## 2026-05-08 — ClassLeader transient-hold follow-up: identity stale-field guard
- Classification: **internal-only** (consistency fix within existing `ClassLeader.*` contract).
- When class-leader identity lookups miss for a valid resolved `ClassLeaderCarIdx`, `ClassLeader.Name` / `ClassLeader.CarNumber` / `ClassLeader.AbbrevName` now clear to empty before lookup attempts, preventing stale identity text from prior leader frames while current leader index/lap/gap are updated.
- No change to class leader selection semantics, hold window semantics, or finish/race snapshot contracts.

## 2026-05-08 — ClassLeader transient-miss hold stabilization + duplicate leader-finished export cleanup
- Classification: **both** (live class-leader dash stability improvement + export surface cleanup).
- `ClassLeader.*` publication now tolerates short transient unresolved frames: when class-leader resolution misses briefly, the last valid class-leader payload is held for a bounded short window instead of hard-clearing on first miss.
- Sustained unresolved or ineligible-session conditions still clear `ClassLeader.*` to invalid defaults; genuine leader changes still publish immediately when resolution is valid.
- Added bounded state-transition logs for class-leader hold activation/clear (no per-tick log loop).
- Removed duplicate/derived export attach `Race.LeaderHasFinished` from the core finish export surface; retained authoritative `Race.OverallLeaderHasFinished` and `Race.ClassLeaderHasFinished`.

## 2026-05-08 — RaceFinish snapshot capture ordering fix (PR #695 follow-up)
- Classification: **internal-only** (ordering fix for existing `RaceFinish.*` contract; no export list change).
- Moved `RaceFinish` one-shot capture trigger out of `UpdateFinishTiming(...)` and into the `DataUpdate` flow **after** `UpdateClassLeaderExports(...)` and `UpdateClassBestExports(...)` refresh.
- Prevents first `SessionState 5/6` capture from freezing pre-refresh class winner/class best/gap/class-position values during finish-transition ordering churn.
- Reset semantics remain unchanged (`session_state_left_post_finish` and finish/session reset paths).

## 2026-05-08 — RaceFinish frozen snapshot exports v1
- Classification: **both** (new dash-facing finish exports + runtime latch/docs alignment).
- Added plugin-owned `RaceFinish.*` post-race snapshot latch in `LalaLaunch`: capture once on first observed race `SessionState==5` (fallback `==6`), hold frozen values through `SessionState 5/6`, reset when leaving post-finish lifecycle into next session/race cycle.
- Added exports: `RaceFinish.Active`, `RaceFinish.PlayerOverallPosition`, `RaceFinish.PlayerClassPosition`, `RaceFinish.PlayerFuelLeft`, `RaceFinish.PlayerBestLap`, `RaceFinish.PlayerBestLapSec`, `RaceFinish.ClassWinnerName`, `RaceFinish.ClassWinnerAbbrevName`, `RaceFinish.ClassWinnerGapSec`, `RaceFinish.ClassBestLap`, `RaceFinish.ClassBestLapSec`.
- Class winner + finish gap snapshot from existing class-leader seams; class best lap snapshot from existing effective-class-aware `ClassBest.*` seam so class winner and class-best holder can differ by design.
- Added bounded one-shot info logs on snapshot capture and reset; no per-tick race-finish logging added.
- Explicitly deferred from v1: `RaceFinish.PitStops`, `RaceFinish.LastPitTime`, `RaceFinish.LastPitTimeSec`.

- 2026-05-08 Phase 2B fuel export audit documentation sync landed:
  - marked `Fuel.Refuel.*` as canonical race-running next-stop refuel guidance surface across inventory/subsystem dash docs, while preserving `StrategyDash.NextRefuel*` as pre-green/planning (not obsolete);
  - reinforced dashboard guidance to prefer plugin-owned `Fuel.Refuel.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Contingency.*`, and `Fuel.Delta.*` for runtime fuel widgets instead of dash-side NCALC chains;
  - added cleanup caution: do not remove/rename fuel-facing exports until both dashboard JSON usage audit and internal C# consumer audit are complete;
  - clarified compatibility notes (`Pit.FuelControl.PushSaveMode*` alias surface, legacy one-release `SetPlan` mapping to DATA SAVED + SOURCE STBY) and reaffirmed protected runtime families/command-send behavior unchanged.


## 2026-05-08 — League Class canonical subsystem documentation sweep
- Added canonical subsystem doc `Docs/Subsystems/League_Class_System.md` covering purpose, concepts, resolver precedence/fallback, boundaries, exports, effective PositionInClass semantics, UI workflow, dash guidance, non-goals, and troubleshooting.
- Updated cross-references in `Project_Index`, `Subsystems/H2H.md`, `Subsystems/Dash_Integration.md`, `Internal/SimHubParameterInventory.md`, `Internal/Plugin_UI_Tooltips.md`, `RepoStatus.md`, and `CHANGELOG.md` to point to canonical League Class documentation and reduce duplicated explanations.
- Scope explicitly limited to analysis + documentation; runtime code, dashboard JSON, exports/actions, and resolver logic unchanged.
## 2026-05-08 — Fuel projection stable-source text companion export
- Classification: **both** (new dash-facing helper export + docs alignment).
- Added `Fuel.ProjectionLapTime_StableSourceText` as a presentation-only companion to raw `Fuel.ProjectionLapTime_StableSource`.
- Raw token export remains unchanged/canonical for diagnostics/contracts; no projection math, precedence, stable deadband, or source selection behavior changed.
- Mapping contract: `pace.stint→LIVE STINT`, `pace.last5→LIVE AVG5`, `profile.avg→PROFILE`, `fuelcalc.estimated→PLANNER EST`, `telemetry.lastlap→LAST LAP`, `fallback.none→NO DATA`.
- Unknown/unmapped non-blank values pass through unchanged; blank/null defensively resolves to `NO DATA`.

## 2026-05-08 — Projection stable-source token audit docs update
- Classification: **internal-only** (documentation accuracy only; no runtime behavior change).
- Documented the full code-derived raw token set for `Fuel.ProjectionLapTime_StableSource`: `pace.stint`, `pace.last5`, `profile.avg`, `fuelcalc.estimated`, `telemetry.lastlap`, `fallback.none`.
- Added stable hold/deadband semantics note so consumers treat the token as provenance of the currently published stable value.
- Corrected `SimHubParameterInventory` wording that previously implied only stint/last5/profile/fallback provenance.

## 2026-05-07 — League Class final polish: Dash binding + ShortName-first ClassName presentation
- Classification: **both** (dash control binding UX completion + class-name presentation contract polish).
- Added Dash Control `Bindings` row `League Class Toggle` bound to existing plugin action `LalaLaunch.LeagueClass.ToggleEnabled` (no duplicate action surface introduced).
- Kept existing toggle runtime semantics unchanged (enable guard, resolver reload, and quiet CSV self-check behavior on enable in CSV-capable modes).
- Updated League-aware class-name presentation helper behavior so dash-facing `ClassName` exports use effective League `ShortName` when present, with fallback to effective League `Name` when `ShortName` is blank.
- Preserved existing fallback and protected domains: when League is disabled/unresolved, native class name behavior is unchanged; no changes to class color, position-in-class, selector/order/filter/timing logic, or manual-override scope.

## 2026-05-07 — Fuel.Refuel DATA projection follow-up: timed reprojection gate + selected-burn contingency conversion
- Classification: **both** (runtime tactical refuel correctness follow-up + docs alignment).
- Removed the overly strict reprojection pre-gate requiring `simLapsRemaining > 0`; DATA-governed Fuel.Refuel reprojection now attempts whenever selected lap seconds are valid and relies on `FuelProjectionMath.ProjectLapsRemaining(...)` to decide if session context is sufficient.
- `Fuel.Refuel` contingency-in-laps conversion now uses the selected runtime refuel burn basis (`selectedBurn`) inside `ComputeRuntimeRefuelOutputs(...)`, ensuring PUSH/SAVE/STBY(NORM advisory)/NORM all convert laps-to-litres against the burn basis actually used by `Fuel.Refuel.NextLitres`.
- Invalid-context reset behavior remains deterministic (`Valid=false`, `NextText=CHECK FUEL`, stale source context cleared).

## 2026-05-07 — Fuel.Refuel DATA-governed projected-laps alignment
- Classification: **both** (runtime tactical refuel correctness alignment + docs sync).
- `Fuel.Refuel.NextLitres` projection basis now follows the same DATA-governed lap basis selected by `ResolveRuntimeRefuelBasis(...)`/`Fuel.Refuel.LapSource` instead of always consuming live stable projected laps.
- DATA LIVE keeps LIVE-first behavior when `LapSource=LIVE` and live projection is valid; non-live selected lap sources (`PLAN/PROFILE/DEFAULT`) now reproject remaining laps from selected lap seconds with existing race projection semantics.
- If a safe DATA-governed projected laps value cannot be resolved, runtime refuel outputs now deterministically fail invalid (`Fuel.Refuel.Valid=false`, `Fuel.Refuel.NextText=CHECK FUEL`).
- Protected domains unchanged: no command transport/send changes and no behavior changes to `Fuel.Pit.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, boxed latches, or StrategyDash next-refuel helpers.

## 2026-05-07 — Fuel.Refuel review follow-up: invalid-context reset + PLAN-derived PUSH/SAVE source labels
- Classification: **internal-only** (runtime export correctness/labeling fixes with unchanged public export set).
- Fixed `Fuel.Refuel` invalid reset staleness: invalid paths now also reset `BurnSource/LapSource` to `DEFAULT` and refresh context exports (`DataMode`, `BurnMode`) from current Pit Fuel Control state (with safe `LIVE`/`STBY` fallback) so invalid ticks cannot publish stale prior valid context.
- Refined DATA SAVED + PUSH/SAVE fallback source labelling: when fallback burn is derived from selected NORM basis, `Fuel.Refuel.BurnSource` now mirrors actual derivation (`SAVED`/`PROFILE`/`LIVE`) instead of always forcing `DEFAULT`; `DEFAULT` is now reserved for true generic fallback derivation.
- Preserved scope: no command send/transport changes and no behavior changes to `Fuel.Pit.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, boxed refuel latches, or StrategyDash next-refuel helpers.

## 2026-05-07 — Fuel.Refuel NextStopCap follow-up (runtime live-cap decision seam)
- Classification: **both** (runtime tactical refuel correctness adjustment + docs alignment).
- Corrected `Fuel.Refuel.NextLitres` capacity semantics:
  - final-stop vs multi-stop threshold now uses runtime effective restricted tank capacity seam (`ResolveRuntimeLiveMaxTankCapacity`, live-cap first with planner/profile fallback only when live cap is unavailable),
  - multi-stop displayed litres remain add-oriented guidance using runtime add-cap seam (`Fuel.Pit.TankSpaceAvailable`) so export values are not overstated as full tank size add amounts while on track.
- Tightened validity: unresolved/invalid capacity context now fails `Fuel.Refuel.Valid`; zero decision capacity with positive requirement no longer silently resolves as valid `NO REFUEL`.
- Protected domains unchanged: no changes to `Fuel.Pit.TankSpaceAvailable` or `Fuel.Pit.WillAdd` semantics, command send/transport, StrategyDash helpers, boxed latches, or existing fuel delta/burn-to-end outputs.

## 2026-05-07 — Fuel.Refuel.* runtime tactical refuel export family
- Classification: **both** (new dash-facing runtime fuel exports + internal contract/docs alignment).
- Added new runtime `Fuel.Refuel.*` exports in `LalaLaunch`:
  - `Fuel.Refuel.NextLitres`, `Fuel.Refuel.NextLitresCeil`, `Fuel.Refuel.NextText`, `Fuel.Refuel.Valid`,
  - `Fuel.Refuel.BurnSource`, `Fuel.Refuel.LapSource`, `Fuel.Refuel.DataMode`, `Fuel.Refuel.BurnMode`.
- Contract:
  - next-stop actionable guidance (not always raw finish-from-here deficit),
  - deterministic final-stop vs multi-stop rule using `FinalStopNeed` vs usable next-stop add capacity,
  - contingency included once on final-stop guidance and not repeatedly stacked on non-final max-fill guidance.
- DATA/SOURCE basis behavior:
  - DATA mirrors Pit Fuel Control `LIVE/SAVED`,
  - SOURCE mirrors `NORM/PUSH/SAVE/STBY`; STBY computes advisory NORM basis and remains command-neutral,
  - DATA SAVED paths do not select LIVE/SIM source tokens.
- Protected domains preserved: no command-send transport/state-machine behavior changes and no semantic changes to `Fuel.Delta.*`, `Fuel.Pit.*`, `Fuel.RequiredBurnToEnd*`, boxed refuel latches, or StrategyDash next-refuel helpers.
## 2026-05-07 — League Race UI polish pre-merge cleanup
- Classification: **internal-only** (targeted UX-noise/perf cleanup; no new feature-surface changes).
- Refined League Race helper warning gate so duplicate CSV rows alone do not force yellow status text.
- Added shared player preview resolution helper to avoid repeated resolver calls across preview text/preview fields/warning checks.
- Removed redundant double-reload path in `ToggleLeagueClassEnabled()` by guarding the post-self-check reload call.

## 2026-05-07 — League Race settings UI polish + LeagueClass toggle action self-check
- Classification: **both** (settings UX clarity + existing action behavior polish + docs alignment).
- Compacted CSV status helper into a single summary line while preserving full status counts (`status/rows/valid/invalid/duplicates`).
- Added warning-state helper coloring (yellow) for League Race section issue states (CSV path missing/file missing, CSV load failure/no valid rows, invalid/duplicate rows, unresolved/invalid player effective class while enabled).
- Updated Player race class row behavior:
  - Auto-detect now shows read-only resolved preview fields (`Class Name`, `Short Name`, `Rank`, `Colour Hex` + swatch).
  - Manual override keeps editable persisted fields and unchanged save behavior.
- Kept existing plugin action `LeagueClass.ToggleEnabled` and polished enable behavior with a quiet CSV self-check reload: when toggled ON in CSV-capable modes and CSV file exists but valid rows are not loaded, it now calls the existing reload seam; missing/empty path does not block enable.
- Preserved invariants: no resolver algorithm ownership changes, no Opponents/CarSA/H2H/PitExit/Fuel logic changes, no dashboard JSON changes.
## 2026-05-07 — PreRace one-stop under-fuel severity reclassification (ref #7)
- Classification: **both** (dash-facing status colour/priority intent change + catalog/docs alignment).
- Reclassified `2 STINT PLAN REQUIRES MORE FUEL` from red to orange in `LalaLaunch.PreRace.StatusText/StatusColour` for the one-stop under-fuel path when one-stop remains feasible.
- Rationale: this state is advisory (next-stop refuel-settable) rather than a hard invalid strategy fault; hard-stop red remains `2 STINT PLAN NOT POSSIBLE`.
- Updated `Docs/Internal/FuelSystemMessages_Catalog.csv` ref #7 to orange high-priority advisory semantics aligned near `CHECK NEXT STINT FUEL` behavior intent.

## 2026-05-06 — PR #687 follow-up build fix: CarSA PositionInClass null-coalescing removal
- Classification: **internal-only** (compile blocker fix only; no runtime contract change).
- Removed invalid null-coalescing on non-nullable `CarSASlot.PositionInClass` in `Car.Ahead01..05.PositionInClass` and `Car.Behind01..05.PositionInClass` publish-time attach lambdas.
- Preserved effective-position seam behavior by continuing to call `GetEffectivePositionInClassForPublishedContext(slot.CarIdx, slot.PositionInClass)` with unchanged fallback-to-`0` only when slot is absent.


- 2026-05-06 CarSA slot PositionInClass publish-seam alignment landed:
  - `Car.Ahead01..05.PositionInClass` and `Car.Behind01..05.PositionInClass` now publish via `GetEffectivePositionInClassForPublishedContext(slot.CarIdx, slot.PositionInClass)` at attach time;
  - enabled+resolved League Class paths now surface Opponents effective cohort rank where available, with unchanged native slot fallback when unavailable/disabled;
  - no CarSA slot ordering/filtering/selection or Opponents ranking logic changes.
## 2026-05-06 — PreRace status catalogue rename + StrategyDash phase rebrand + max-fuel split
- Classification: **both** (dash-facing message/phase contract changes + docs/catalog alignment).
- Reworked `LalaLaunch.PreRace.StatusText` literals to the approved stint-focused catalogue wording (`SINGLE STINT...`, `2 STINT PLAN...`, `MULTI STINTS REQUIRED`, etc.) while keeping colour severity semantics unchanged.
- Split former max-fuel warning path into:
  - `MAX START FUEL REQUIRED` for pre-start start-fuel guidance;
  - phase-routed `SET MAX FUEL NEXT STINT` when StrategyDash phase is `START READY` or `RACE`.
- Rebranded StrategyDash phase contract to `0=IDLE`, `1=PRE GRID`, `2=GRIDDING`, `3=START READY`, `5=RACE` (no backward compatibility bridge requested).
- Updated internal contract docs and added a text authority companion (`Docs/Internal/FuelSystemMessages_Catalog.csv`) for PR-safe review of the approved catalogue and split-message behavior (binary spreadsheet update deferred to manual/local conversion workflow).

## 2026-05-06 — DATA LIVE provenance labels fix + StrategyDash.IsAutoStrategy retirement
- Classification: **both** (dash-facing source-label correctness + export removal/docs alignment).
- Updated `ResolveDataGovernedBurnAndPaceBasis(...)` so DATA `LIVE` burn/lap source labels now report true selected provenance (`LIVE` only when stable source is genuinely live), with hierarchy preserved exactly.
- DATA `SAVED` behavior remains strict (`PLAN -> PROFILE -> DEFAULT`) and cannot emit `LIVE`/`SIM` labels.
- Removed retired `StrategyDash.IsAutoStrategy` export/backing/reset wiring.

- PR #684 P1 follow-up: `fuelcalc.estimated` stable projection source is now explicitly classified as `SAVED` in `ResolveDataGovernedBurnAndPaceBasis(...)`, so DATA LIVE lap provenance preserves `LIVE -> PLAN -> PROFILE -> SIM -> DEFAULT` even when stable projection is planner-estimate held.
## 2026-05-06 — Car.Player class presentation uses player effective-class resolver
- Classification: **internal-only** (targeted export-resolution fix for existing class presentation contract; no new exports/UI/actions).
- Fixed `Car.Player.ClassName`, `Car.Player.ClassColor`, and `Car.Player.ClassColorHex` to resolve through the player effective-class path (`ResolveLivePlayerLeagueClassInfo`) instead of the generic race-context driver resolver path.
- Preserved expected fallback behavior: League Class disabled or unresolved still publishes native CarSA class values; enabled + no manual override still follows current CSV/name effective-class resolution; enabled + manual player override now reliably publishes manual override presentation for player exports.
- Out-of-scope invariants preserved: no CarSA sorting/filtering/cache ownership changes, no H2H target/timing changes, and no `Car.Ahead*/Behind*` resolver behavior changes.

## 2026-05-06 — League Class presentation alignment for Car.* and H2HTrack class-facing outputs
- Classification: **both** (dash-facing class presentation behavior update + contract docs alignment).
- `Car.Player.ClassName/ClassColor/ClassColorHex` now publish effective League Class presentation when League Class is enabled and resolved (manual player override included), with native CarSA fallback unchanged when disabled/unresolved.
- `Car.Ahead01..05.*` and `Car.Behind01..05.*` class-facing fields (`ClassName`, `ClassColor`, `ClassColorHex`) now apply publish-time League Class presentation per selected physical slot driver; CarSA slot selection/order/filter/cache ownership remains unchanged.
- `H2HTrack.Ahead/Behind.ClassColor` and `ClassColorHex` now follow the global League Class presentation gate while preserving existing H2HTrack target acquisition, sector/delta/timing, and CarSA-owned physical selection semantics.

## 2026-05-06 — H2H ClassColorHex coverage follow-up
- Classification: **both** (dash-facing class-colour contract completeness + League presentation alignment).
- Added `H2HRace.Ahead/Behind.ClassColorHex` and `H2HTrack.Ahead/Behind.ClassColorHex` exports alongside existing `ClassColor` exports in shared H2H target attach wiring.
- `ClassColorHex` now follows the same League-aware presentation gate as `ClassColor` and always publishes `#RRGGBB` format.

## 2026-05-06 — League Class follow-up: Opp ClassColor format + CarIdx sentinel + H2HTrack presentation alignment
- Classification: **both** (export-format correction + identity read hardening + authorised H2HTrack presentation alignment).
- Fixed `Opp.*.ClassColor` League-aware path to preserve canonical `0xRRGGBB` output while `Opp.*.ClassColorHex` remains `#RRGGBB`.
- Hardened DriverCount player-car identity reads by using `-1` sentinel for missing/unparseable CarIdx properties (no implicit car `0` fallback).
- Enabled League-aware class-color presentation wiring for both `H2HRace` and `H2HTrack` attach paths without changing H2H selection, sector, delta, gap, or timing logic.

## 2026-05-06 — League Class follow-up: manual-override player count + H2HRace-only class-color override
- Classification: **both** (dash-facing export correctness fixes + scope-guarded H2H presentation wiring).
- `LeagueClass.Player.DriverCount` now includes the player row using player resolver semantics in League-active counting (manual override included), while non-player rows continue using driver resolver semantics.
- `AttachH2H*` class-color League override is now enabled for `H2HRace` only; `H2HTrack.*.ClassColor` remains native.

## 2026-05-06 — League Class presentation follow-up review fixes
- Classification: **both** (dash-facing export-correctness fixes + contract docs alignment).
- Fixed `LeagueClass.Player.DriverCount` to count the player’s effective class cohort membership (not total valid CSV rows), with native same-class fallback where available and `0` when unavailable.
- Fixed H2HRace class presentation resolver wiring to pass participant `UserID` when available, restoring CSV-only resolver behavior for class presentation outputs.


## 2026-05-06 — League Class race-context class presentation alignment
- Classification: **both** (dash-facing class-presentation export behavior + internal contract alignment).
- `Opp.Ahead1..5` / `Opp.Behind1..5` now publish class presentation fields (`ClassName`, `ClassColor`, `ClassColorHex`) from resolved effective League Class when League Class is enabled and player effective class is valid; otherwise native behavior remains.
- `H2HRace.Player/Ahead/Behind` class color presentation now follows the same League Class presentation gate while preserving selected-driver identity and existing selector/gap/timing behavior.
- Added `LeagueClass.Player.DriverCount` export for dashboards (`Pxx of xx` support).
## 2026-05-06 — PR #679 build fix: restore PreRace data-governed burn/pace helper
- Classification: **internal-only** (compile restoration + intended source-hierarchy reattachment; no protected runtime-domain changes).
- Restored `ResolveDataGovernedBurnAndPaceBasis(...)` inside `LalaLaunch` near PreRace helpers so `UpdatePreRaceOutputs(...)` compiles and resolves source authority in one place again.
- Preserved approved hierarchy and source tokens:
  - DATA LIVE: BURN `LIVE -> PLAN -> PROFILE -> DEFAULT`; LAP `LIVE -> PLAN -> PROFILE -> SIM -> DEFAULT`.
  - DATA SAVED: BURN `PLAN -> PROFILE -> DEFAULT`; LAP `PLAN -> PROFILE -> DEFAULT`.
- Explicitly kept untouched protected domains: `Fuel.Delta.*`, `Fuel.Pit.*`, `Fuel.RequiredBurnToEnd*`, boxed refuel latches, and `PitFuelControlEngine` target/send behavior.

## 2026-05-06 — PreRace live-facing lap/burn basis helper export
- Classification: **both** (new dash-facing helper export + internal contract docs alignment).
- Added `LalaLaunch.PreRace.LiveFacingBasisText` as a concise combined readout of active pre-race source authority: `LAP <source> / BURN <source>`.
- The new property mirrors existing `LalaLaunch.PreRace.LapTimeSource` and `LalaLaunch.PreRace.FuelSource` values only; no fuel math, strategy ownership, or Pit Fuel Control DATA/SOURCE behavior changed.

## 2026-05-05 — Pit Fuel Control DATA/SOURCE simplification
- Classification: **both** (dash-facing action/export contract change + internal state-machine simplification).
- Retired `SOURCE=SAVED`; Pit Fuel Control SOURCE is now `STBY`/`NORM`/`PUSH`/`SAVE`, with source cycling `STBY -> NORM -> PUSH -> SAVE -> STBY`.
- Added Pit Fuel Control DATA (`LIVE`/`SAVED`) exports and actions (`SetDataLive`, `SetDataPlan`, `CycleData`). DATA defaults to `LIVE` on control/session reset, and every DATA change forces `SOURCE=STBY` with no fuel command send.
- Compatibility: legacy `Pit.FuelControl.SetPlan` remains registered for one release but now maps to `DATA=SAVED` + `SOURCE=STBY` and publishes `FUEL DATA SAVED`; legacy `PushSaveModeCycle` was removed before public release.
- Removed PLAN validity/session-match enforcement from Pit Fuel Control because PLAN is no longer a command source. `NORM` always uses runtime/live burn; `PUSH`/`SAVE` use live burn under DATA LIVE or planner/profile memory burn under DATA SAVED.
- Aligned StrategyDash pre-green next-refuel and burn-plan text with the DATA/SOURCE model: `NORM` uses runtime burn, `PUSH`/`SAVE` follow DATA, `/ LIVE` and `/ MEMORY` suffixes are emitted only when the basis is clear.

## 2026-05-05 — League Class ClassLeader native-gate bypass fix
- Classification: **internal-only** (dash-visible correctness for an already-documented ClassLeader League Class contract; no new exports/UI/actions).
- Root cause: `FindResolvedClassLeaderCarIdx(...)` still let native single-class detection and native player class-short resolution run before the League race-context matcher, so ClassLeader could return the native overall/class leader even while Opponents/H2HRace were already using the valid player effective-class cohort.
- Fix: when `BuildRaceContextLeagueClassMatchDelegate()` is active (League Class enabled + valid player effective class, including manual override), ClassLeader now scans the effective-class cohort by `CarIdxPosition` first, self-matches the player car directly, and does not fall back through native class membership because the player race-context row is incomplete.
- Preserved fallbacks/invariants: League Class disabled or enabled with unresolved player effective class keeps native behavior; opponent/candidate rows still resolve through the existing driver resolver path; no Opponents slot selection changes, no H2HRace/H2HTrack sector/delta changes, no CarSA changes, and no PitExit timing/countdown/gap changes.

## 2026-05-05 — StrategyDash burn-basis alignment + no-stop burn-plan + next-refuel delta
- Classification: **both** (new dash-facing exports/semantics + internal contract alignment).
- Auto PreRace fuel-per-lap fallback order now resolves as: valid stable-live first, then selected planner `FuelCalculator.FuelPerLap` (if `>0`), then existing profile/generic fallback path.
- Added `StrategyDash.BurnPlanText` as a pre-green display helper for no-stop/grid usage (`BURN PLAN: NORM/SAVE/PUSH` with optional `/ LIVE` or `/ MEMORY` suffix when source basis is clear).
- Added `StrategyDash.NextRefuelDeltaLitres` (`requested refuel - StrategyDash.NextRefuelTargetLitres`) and aligned `StrategyDash.NextRefuelStatus` derivation to `abs(delta)` thresholds (`<=0.5 OK`, `<=2.0 CHECK`, else ACTION) to keep status/delta basis locked.
- Kept unchanged: Fuel DATA/MODE/SOURCE button behavior, `PitFuelControlEngine` behavior, `Pit.FuelControl.*` semantics, race-running fuel outputs (`Fuel.Delta.*`, `Fuel.Pit.*`, `Fuel.RequiredBurnToEnd*`), and boxed-refuel latch behavior.

## 2026-05-05 — League Class ClassLeader/PitExit replay identity gate fix
- Classification: **internal-only** (race-context cohort seam correctness; no new exports/UI).
- Root cause: `LalaLaunch.TryBuildRaceContextNativeCarRow(...)` required `TryGetCarDriverInfo(...)` success and synthesized `car:{idx}` identity fallback, so ClassLeader/ClassBest race-context matching could drop to native fallback in replay states where Opponents still had valid session identity.
- Fix: race-context row build now prefers session identity (`UserName`/`CarNumber`/`CarClassColor`) and returns `false` when canonical identity key is unavailable (no synthetic `car:{idx}` fallback).
- Fix: `TryResolveClassSessionBestLap(...)` now only hard-fails on missing native player class-short when League race-context matcher is inactive; with active League matcher it proceeds through the shared race-context delegate path.
- Preserved invariants: Opp race-slot ordering/gap math unchanged, PitExit timing/gap/countdown formulas unchanged, CarSA unchanged, H2HTrack and H2H sector/delta logic unchanged.


## 2026-05-05 — League Race final cohort integration replay follow-up (ClassLeader/ClassBest identity fix)
- Classification: **internal-only** (race-context cohort identity seam correction; no math/export contract additions).
- Fixed `ClassLeader`/`ClassBest` race-context class matching path to construct `OpponentsEngine.NativeCarRow` candidates from session identity sources (`UserID`, `UserName` fallback, `CarNumber`, class-color identity key) rather than synthetic `car:{idx}` rows with blank names.
- Root cause: when `UserID` is unavailable/zero, League Class resolver matching requires driver name fallback; blank candidate names caused false cohort mismatch, so class leader/best stayed on native class.
- Preserved fallbacks/invariants: disabled or unresolved League Class still uses native class matching; no `PitExit`/Opponents gap-timing formula changes, no CarSA/H2HTrack ownership changes.
## 2026-05-05 — StrategyDash start-fuel advice decoupled from PreRace status (PR #660 follow-up)

- Updated `StrategyDash.StartFuelAdviceText/StartFuelStatus` to use a dedicated start-fuel comparison instead of mapping from `LalaLaunch.PreRace.StatusText/StatusColour`.
- Effective start-fuel basis now resolves in order: live/current fuel when `>0`, else setup fallback when `Fuel.Setup.FuelLevelValid==true`, else unknown.
- Comparison uses `StrategyDash.StartFuelRequiredLitres` with `1.0 L` tolerance (`>= required - 1.0` => OK).
- Status/text contract: unknown => `CHECK START FUEL`/`1`; short by more than tolerance => `ADD START FUEL`/`2`; otherwise `START FUEL OK` or `START FUEL MAX`/`0` (max text when within tolerance of cap).
- Kept unchanged: `PreRace.StatusText`, `StrategyDash.NextRefuelAdviceText`, fuel DATA/MODE/SOURCE, `PitFuelControlEngine`, `Fuel.Delta.*`, `Fuel.Pit.*`, and `Fuel.RequiredBurnToEnd*` behavior.

## 2026-05-05 — Build-fix follow-up: nullable pit-loss read + race-context driver-info signature alignment
- Classification: **internal-only** (compile correctness fix; no runtime feature/contract redesign).
- `FuelCalcs.SavePlannerDataToProfile()` now reads existing `PitLaneLossSeconds` with nullable-safe fallback (`?? 0.0`) before diffing planner value.
- `LalaLaunch.IsRaceContextClassMatchForCarIdx(...)` now calls `TryGetCarDriverInfo(...)` with correct out-parameter types/signature (arg 11 remains `out int teamId`), removing CS1503 argument-type failures.
- Race-context matcher row names are now left empty in this seam; class-match behavior remains identity/class-driven and unchanged.

## 2026-05-05 — PR #666 follow-up: planner-save pit-loss learning-mode preservation
- Classification: **internal-only** (metadata persistence correctness; no UI/export contract changes).
- Fixed `FuelCalcs.SavePlannerDataToProfile()` so `PitLaneLossSource`/`PitLaneLossLearningMode` are only forced to `manual` when planner save actually changes pit-loss seconds.
- Prevents ordinary planner saves from silently rewriting learned `boxed_stop` metadata without value conversion, avoiding downstream normalized pit-loss inflation.

## 2026-05-04 — League Race Final Behaviour: ClassLeader/ClassBest/PitExit effective-class cohort completion
- Classification: **both** (dash-visible race-context class cohort behavior update + internal contract/docs alignment).
- Applied existing League Class resolver seam to remaining race-context class outputs:
  - `ClassLeader.*` leader resolution now uses the existing race-context match delegate seam when League Class is enabled and player effective class resolves valid; native class behavior remains fallback in disabled/unresolved-player paths.
  - `ClassBest.*` session-best holder resolution now uses the same race-context match delegate seam with identical fallback behavior.
  - `PitExit.*` same-class cohort scan now uses Opponents `IsRaceContextClassMatch` seam (effective class when enabled+valid, native class fallback otherwise).
- Preserved invariants: no CarSA physical slot/order/filter changes, no `H2HTrack.*` selector changes, no H2H sector/delta math changes, no pit-loss/countdown/progress/gap formula changes beyond class-cohort inclusion.
- PR #669 review follow-up: in League effective-class mode, `FindResolvedClassLeaderCarIdx(...)` now selects class leader by best race order (`CarIdxPosition` lowest positive) across the full effective-class cohort, instead of first matching array index; when `CarIdxPosition` is unavailable/unusable, existing native class-position fallback path is preserved.

### 2026-05-04 — StrategyDash phase compile-fix follow-up (PR #660)
- Classification: **internal-only** (build fix; no fuel/planner/pit semantics redesign).
- Fixed `LalaLaunch.UpdateStrategyDashAdvice(...)` phase detection to consume real call-path session booleans (`isRaceRunning`, `isGridOrFormation`) instead of removed/non-existent fields.
- Kept StrategyDash phase contract unchanged:
  - `1 = PLANNING`
  - `2 = GRID FORMATION` (grid + formation combined)
  - `3 = RACE`
- Scope bounded to StrategyDash phase input plumbing only; no changes to Fuel DATA/MODE/SOURCE behavior, `PitFuelControlEngine`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, boxed refuel latch behavior, or `Pit.FuelControl.*` semantics.
- Follow-up phase-gate correction: `StrategyDash.Phase` now requires `DataCorePlugin.GameRawData.Telemetry.IsOnTrackCar==true` before allowing `2 = GRID FORMATION`; race-running (`3 = RACE`) authority remains unchanged, and non-race/non-on-track remains `1 = PLANNING` even when grid/formation helper flags are true.

### 2026-05-04 — League Race header checkbox-column alignment follow-up
- Classification: **internal-only** (UI layout correction; no runtime/resolver/settings semantic changes).
- Fixed League Class table header misalignment by reserving a fixed checkbox column width (`18`) for both header and row grids in `Detected classes` and `Fallback rules` sections.
- Preserved all existing bindings, editability, tooltips, and color-preview behavior.

- 2026-05-04 League Race settings table alignment polish landed:
  - replaced spaced header text rows with fixed-column Grid headers for Detected classes, Player race class, and Fallback rules so headers align exactly with input columns;
  - removed visible header labels for Enabled and Colour Preview while preserving checkbox and colour-preview controls in all rows;
  - added concise League Class tooltips for CSV Class Name, Match Suffix, Short Name, Rank, and Colour Hex; bindings and editability semantics are unchanged (CSV Class Name remains read-only).

## 2026-05-04 — Offline data module dash toggle export
- Classification: **both** (new dash-facing action/property + internal docs alignment).
- Added plugin action `OfflineDataModule_Toggle` following existing debug-toggle behavior (flip persisted setting + log line).
- Added dash-readable export `OfflineDataModule` (`0/1`) for visibility/control logic.
- No plugin bindings-section UI addition; action/property are available via SimHub action/property surfaces only.
- 2026-05-04 Strategy Live Detect effective-basis/runtime-refresh fix landed:
  - strategy calculation now resolves an explicit effective race basis/length (manual or Live Detect) and no longer falls through to Lap-Limited/manual laps when Live Detect has no valid detected definition;
  - Live Detect timed sessions now always run strategy/stint/first-stint/total-fuel paths from detected minutes, while detected lap-limited sessions use detected laps;
  - Live Detect race-definition updates now cache helper/basis changes even before selection and force recalculation when selected basis/value/availability changes;
  - session-context transitions now trigger an immediate Live Detect refresh when Live Detect is selected, avoiding stale initial-session detection until radio toggling.

- 2026-05-04 Strategy Live Detect review fix-up 2 landed:
  - `CurrentSessionInfo` race-length detection now respects race limit flags (`IsLimitedSessionLaps`/`IsLimitedTime`) before accepting `_SessionLaps`/`_SessionTime`, with Sessions fallback still active when flagged current-session length is unusable;
  - hardened `SafeReadLong` decimal conversion with explicit `long` range guard to prevent overflow throw paths in telemetry updates.

- 2026-05-04 Strategy Live Detect review fix-up landed:
  - when `CurrentSessionInfo.IsRace==true` but current-session race length is unusable, Live Detect now continues to `Sessions01..64` fallback scanning so declared race definitions can still be recovered;
  - replaced telemetry-path lap-count casts in Live Detect detection with tolerant long parsing (`SafeReadLong`) for current-session and SessionsXX underscore/non-underscore lap properties to avoid invalid-cast throw paths.

- 2026-05-04 Strategy Live Detect race-definition source/underscore fix landed:
  - Live Detect now prioritizes `CurrentSessionInfo` only when `CurrentSessionInfo.IsRace==true`, resolving race length from underscore fields first (`_SessionLaps`, `_SessionTime`) with non-underscore compatibility fallback only when underscore values are unusable;
  - `Sessions01..64` fallback now runs only when current session is not race/unavailable/has no usable length, and helper wording now distinguishes `race found, no valid length` from `no declared race found`;
  - bounded Live Detect result-change log now includes source/session/IsRace/limit flags/raw length fields plus selected basis/reason for diagnostics.

- 2026-05-03 League Race CSV detected-classes editing layer landed:
  - Classification: **both** (user-facing League Class UI editability + resolver/settings persistence behavior alignment).
  - Added persisted class-definition layer `LeagueClassDefinitions` keyed by CSV class name with editable fields: `Enabled`, `ShortName`, `Rank`, `ColourHex` (CSV class name remains read-only in UI).
  - CSV detected-classes table now binds to settings-owned definitions (two-way fields) with colour preview, instead of resolver status-only readout rows.
  - Kept resolver ownership split: CSV mapping remains `CustomerId -> raw CSV ClassName`; resolver then applies class-definition metadata when producing effective class output.
  - Rank semantics now preserved as driver-meaning order (`1` fastest/highest, `2` next, ...); no reverse ordering or alphabetical inference. Missing/invalid CSV ranks default to stable detected-order ranks starting at `1`.
  - Disabled class definitions now mark CSV-resolved league class invalid (`Source=Native` fallback path), so drivers safely fall back to native class behavior; manual player override path remains independent.
  - CSV reload merge behavior now preserves edits for matching class names, adds defaults for new names, and prunes removed names by mirroring current detected CSV class set.

- 2026-05-04 League Race detected-classes UI binding/notification fix landed:
  - `LaunchPluginSettings.LeagueClassDefinitions` now raises `PropertyChanged` when CSV reload replaces the list, so WPF `ItemsControl` refreshes rows immediately;
  - `LeagueClassDefinition` now implements `INotifyPropertyChanged` for `Enabled`, `ShortName`, `Rank`, and `ColourHex` so row edits update UI bindings/live preview consistently;
  - plugin now subscribes to League Class definition row changes to save settings immediately and refresh resolver-consumer preview paths without restart;
  - scope remains UI/settings/resolver-binding only (no Opponents/H2H/CarSA/PitExit/ClassLeader cohort ownership changes).


- 2026-05-03 Build triage follow-up (PR range #647-#652):
- 2026-05-03 Strategy Live Detect functional follow-up (#633) landed:
  - added Live Detect helper text under Race Type showing detected declared race session + basis + value, or explicit unavailable wording when no valid declared race exists;
  - Live Detect effective-basis visibility now clears to none when unavailable (no silent Lap-Limited UI fallback), while ownership radio remains Live Detect;
  - added bounded Strategy INFO log on Live Detect result-change only (`session/basis/value/reason`) for declared-race detection diagnostics.

  - fixed Fuel per Lap helper TextBlock XAML compile break by removing duplicate Style assignment in `FuelCalculatorView.xaml` while preserving existing source-text trigger behavior (`FuelPerLapSourceInfo` / Profile / Live);
  - confirmed `InvertBooleanConverter` and `LapTimeValidationRule` class/namespace wiring are valid in-project; reported lookup errors were downstream/designer fallout from the XAML parse failure;
  - fixed League Class race-context delegate accessibility seam by making `OpponentsEngine.NativeCarRow` publicly accessible to match the public delegate signature consumed by `LalaLaunch` (`IsRaceContextClassMatch`), with no opponent ordering/filtering logic changes.

## 2026-05-03 — Strategy follow-up (effective condition notifications + Live Detect planner-basis snapshot)
- Classification: **internal-only** (view-model notification correctness + planner/live comparability correctness).
- `SelectedTrackCondition` now raises `IsEffectiveDryCondition` and `IsEffectiveWetCondition` property notifications when condition changes (including auto-applied telemetry condition flips through the same setter path).
- `GetPlannerSessionMatchSnapshot()` now resolves planner basis/length from effective race basis under Live Detect (detected lap/time), and reports no comparable planner basis until Live Detect has a valid detected race definition.

## 2026-05-03 — Strategy Live Detect review follow-up (basis-recompute + full race-session scan)
- Classification: **internal-only** (strategy ownership correctness + telemetry candidate selection hardening).
- `UpdateLiveDetectedRaceDefinition(...)` now forces strategy recompute/notifications when Live Detect effective basis flips lap<->time even if numeric race-length value is unchanged.
- Live Detect session scan now evaluates `Sessions01..64` race entries fully, prefers valid lap-limited definitions, and only falls back to timed definitions when no valid lap candidate exists.

## 2026-05-03 — Strategy UI ownership-binding follow-up
- Classification: **internal-only** (binding truth-model correctness + telemetry safety hardening).
- Track Condition radio bindings now represent ownership-only state (`Auto` vs manual `Dry/Wet`) while effective dry/wet condition remains separate for calculations/visibility.
- Race Type radio bindings now represent ownership-only state (Lap/Time/Live Detect) with separate effective basis visibility for lap/time sliders while Live Detect is selected.
- Hardened Live Detect session scan against malformed/missing SessionInfo values by using safe reads and tolerant long parsing in telemetry loop.

## 2026-05-03 — Strategy tab UI enhancements bundle (#512, #628, #633)
- Classification: **both** (user-facing Strategy UI ownership clarity + docs alignment).
- Fuel-per-lap helper/source text moved below the input field to avoid clipping in narrow SimHub layouts.
- Track Condition now exposes explicit `Auto`/`Dry`/`Wet` radio ownership with helper text showing automatic detected mode vs manual override mode.
- Race Type now includes persistent `Live Detect`; when selected, race definition is sourced from declared race session metadata (`SessionInfo.Sessions01..64`, `IsRace==true`) and race-length controls are read-only until manual mode is reselected.
## 2026-05-03 — League Race Phase 3 (Opponents + H2HRace effective class cohort seam)

- Classification: **both** (runtime race-context behavior + docs/control-surface alignment).
- Opponents same-class cohort equality seam is now resolver-pluggable:
  - default path remains native class-color matching;
  - League Class path activates only when `LeagueClassEnabled==true` and player effective class resolves valid;
  - in League Class path, Opp race-context cohort comparison is `player effective class` vs `opponent effective class` via `LeagueClassResolver` (manual override applies only to player effective class).
- Disabled mode and enabled+unresolved-player mode explicitly keep native fallback behavior unchanged.
- `H2HRace.*` follows the updated Opponents race-target seam naturally (`Opp.Ahead1`/`Opp.Behind1`); H2H timing/sector/delta math remains unchanged.
- Added plugin action `LeagueClass.ToggleEnabled` (`LalaLaunch.LeagueClass.ToggleEnabled`) that toggles `Settings.LeagueClassEnabled`, applies existing enable-time mode guard, reloads resolver config, and saves settings.
- Preserved boundaries: no CarSA physical selection/order/filter changes, no H2HTrack selector changes, no PitExit class-row filtering changes, no ClassLeader/ClassBest changes.

## 2026-05-03 — PR #651 review follow-up: race-context compile fix + player-row match guard
- Classification: **internal-only** (build restore + cohort guardrail; no new exports/UI/actions).
- Fixed `Opponents.NativeRaceModel.GetBlendedPaceForPosition(...)` to reuse the in-scope race-context class-match delegate captured during `Build(...)`, resolving the undefined-symbol compile break (`CS0103`).
- Hardened `BuildRaceContextLeagueClassMatchDelegate()` so the player row always self-matches by identity before resolver-based opponent class comparison, preventing enabled+manual-override/no-player-CSV-name-match edge cases from excluding the player from same-class cohort construction.
- Preserved subsystem boundaries and behavior scope: opponent effective-class resolution stays resolver-owned, and fallback native class-color matching remains unchanged outside enabled+valid League Class mode.

## 2026-05-02 — League Class startup enable+disabled-mode guard follow-up
- Classification: **internal-only** (settings/UI guard correction only; no resolver/export/runtime cohort changes).
- Updated `ApplyLeagueClassEnableModeGuard()` in `LalaLaunch.cs` to auto-correct `LeagueClassMode` from `Disabled (0)` to `CsvThenName (3)` whenever `LeagueClassEnabled == true` and mode is `0`, covering both:
  - runtime enable transition (`false -> true`), and
  - legacy startup/load state where settings already load as enabled with disabled mode.
- Preserved behavior boundaries:
  - selected non-disabled modes (`1/2/3`) are unchanged,
  - disabled master state (`LeagueClassEnabled == false`) remains unchanged,
  - no Opponents/PitExit/H2H/CarSA/ClassBest or resolver/export contract changes.

## 2026-05-01 — League Class enable-toggle live visibility hotfix
- Classification: **internal-only** (settings/property-notification plumbing only; no resolver/selection/export logic changes).
- Converted `LaunchPluginSettings.LeagueClassEnabled` to a notifying backing-field property (`OnPropertyChanged(nameof(LeagueClassEnabled))`) so the bound enable toggle updates visibility immediately while SimHub remains open.
- Added settings-level League Class property-change hook in `LalaLaunch` to refresh dependent UI-bound plugin properties on `LeagueClassEnabled`/`LeagueClassMode` changes: `LeagueClassStatus`, `LeagueClassPlayerPreviewText`, `LeagueClassShowCsvSection`, `LeagueClassShowFallbackSection`.
- Preserved existing enable-edge mode guard behavior (`Disabled (0)` -> `CsvThenName (3)` only on `false -> true` enable transition) by invoking the same guard from the settings change hook.

## 2026-05-01 — League Class UI disabled-mode + CSV path binding cleanup
- Classification: **both** (settings UI behavior clarity + persisted-setting binding reliability).
- League Class settings UI now collapses all lower controls when disabled and shows only master enable plus disabled status text.
- CSV browse path write remains settings-owned (`LeagueClassCsvPath`) and now immediately reflects in the textbox after browse assignment.
- Reload button path is now guarded against disabled-mode invocation (no-op when disabled); re-enable flow keeps saved settings and existing enable-time mode guard behavior.
## 2026-05-01 — League Race Phase 2 compile blocker hotfix (undefined live identity fields)
- Classification: **internal-only** (compile fix only; no runtime behavior change).
- Replaced `LeagueClass.Player.*` export bindings that referenced undefined `_liveLeagueClassIdentityCustomerId/_liveLeagueClassIdentityName` symbols with a resolver helper that reads live player identity via `TryGetLivePlayerIdentityPreview(...)` and resolves through existing `LeagueClassResolver` seams.
- Restored buildability by removing `CS0103` undefined-identifier failures while preserving existing resolver-backed export semantics.

## 2026-05-01 — League Race Phase 2 debug/metadata exports only
- Classification: **both** (new dash-facing debug metadata exports + docs alignment).
- Added League Class global/player export family in `LalaLaunch` using resolver-only authority: `LeagueClass.Enabled`, `LeagueClass.Mode`, `LeagueClass.ConfigStatusText`, `LeagueClass.LoadedCount`, `LeagueClass.ValidDriverCount`, `LeagueClass.InvalidRowCount`, `LeagueClass.DuplicateRowCount`, `LeagueClass.Player.*` (`Name/ShortName/Rank/ColourHex/Valid/Source/OverrideActive`).
- Added passive League Class metadata exports for Opp slots (`Opp.Ahead1..5.*` / `Opp.Behind1..5.*`): `LeagueClassName`, `LeagueClassShortName`, `LeagueClassRank`, `LeagueClassColourHex`, `LeagueClassValid`, `LeagueClassSource`.
- Added optional passive League Class metadata exports for CarSA physical slots (`Car.Ahead01..05.*` / `Car.Behind01..05.*`) with the same fields.
- Source text contract is uppercase: `CSV`, `NAME`, `MANUAL`, `NATIVE`, `NONE`; unresolved exports publish `Valid=false` with empty/zero display values.
- Preserved invariants: no Opponents same-class selection changes, no PitExit class-row filtering changes, no H2H/ClassLeader/ClassBest behavior changes, no CarSA physical-selection/order/filtering changes.

## 2026-05-01 — League Race Phase 1 player override colour preview binding fix
- Classification: **internal-only** (UI binding correction only; no resolver/settings/export behavior changes).
- `GlobalSettingsView.xaml`: fixed the Player race class preview swatch binding to source from the override colour hex textbox text via `HexToBrushConverter`, matching CSV/fallback preview behavior and preserving transparent output for invalid/blank hex.

## 2026-04-30 — Pit Fuel Control Push/Save mode UI surface + binding row
- Classification: **both** (plugin settings/binding UI surface only + docs alignment).
- Notification hardening: `PitFuelControlPushSaveModeCycle` now explicitly raises Push/Save mode property change notifications after programmatic setting updates so an open toggle UI reflects hardware/dash cycle presses immediately.
- Added Dash Control -> Global Dash Functions -> Fuel two-state toggle `Push/Save Profile Mode` (OFF=`LIVE`, ON=`PROFILE`) bound to the shared Push/Save mode setting via `Settings.PitFuelControlPushSaveMode`.
- Added Settings -> Pit Commands -> Pit Fuel Control binding row for `LalaLaunch.Pit.FuelControl.PushSaveModeCycle` with label `Push/Save Mode Cycle`.
- Preserved invariants: no PitFuelControlEngine logic, fuel math, guard semantics, transport, or command payload changes.

## 2026-04-29 — Runtime pit tank-space cap ownership split (live cap vs planner override)
- Classification: **both** (runtime dash-visible fuel outputs corrected + docs/contract alignment).
- Confirmed root cause: runtime pit-space helper used planner `MaxFuelOverride` as primary cap and min-clamped against live cap, allowing stale preset/profile overrides to under-cap live tank-space math.
- Split tank-cap ownership in `LalaLaunch.cs`:
  - runtime pit exports now use `ResolveRuntimeLiveMaxTankCapacity()` (live cap seam first, safe planner fallback only when live cap unavailable);
  - retained separate planning-cap helper ownership (`ResolvePlanningMaxTankCapacity()`) for planner semantics boundaries.
- Preserved invariants: planner Profile/Live Snapshot behavior, preset apply semantics, pit command/control state machine, and `Fuel.Pit.WillAdd = min(requestedAdd, tankSpace)` clamp shape remain unchanged.

## 2026-05-04 — Pit-loss source-aware drive-through normalization + 2.00s transition allowance
- Classification: **both** (driver-facing pit-loss display semantics + runtime total-stop-loss composition).
- Added persisted per-track pit-loss learning mode (`PitLaneLossLearningMode`) so pit-loss save path records whether the accepted sample came from a boxed stop or drive-through cycle.
- `Fuel.Live.PitLaneLoss_S` and `TrackLearning.PitLoss.ValueSec/Display` now publish a drive-through-equivalent value: when learned from boxed-stop cycles, they subtract the fixed transition allowance and clamp at `0`.
- Shared boxed-stop seam now uses fixed transition allowance `+2.00s` (replacing temporary `1.50s`).
- `Fuel.Live.TotalStopLoss` now composes from drive-through-equivalent pit-lane loss + modeled boxed service (+repair-aware) + fixed transition allowance so transition time is not double-counted while maintaining expected pit-stop totals.


## 2026-05-04 — Pit-loss mode/source consistency fix for FuelCalcs overwrite path
- Classification: **both** (pit-loss export correctness on planner/manual overwrite path + internal state consistency).
- Fixed FuelCalcs track-save path to set `PitLaneLossSource="manual"` and `PitLaneLossLearningMode="manual"` whenever it overwrites `PitLaneLossSeconds`, preventing stale `boxed_stop` mode from incorrectly subtracting transition allowance on edited/manual pit-loss values.
- Scope is narrow and write-path only; no pit-cycle classification, transition allowance constant, or boxed-stop service math changes were made.

## 2026-05-05 — Profiles manager pit-loss manual-edit mode consistency fix
- Classification: **both** (manual pit-loss edit correctness + normalized export consistency).
- `ProfilesManagerViewModel.PitLaneLossSecondsText` now also stamps `PitLaneLossLearningMode="manual"` whenever manual pit-loss edits overwrite `PitLaneLossSeconds`/`PitLaneLossSource`.
- Prevents stale `boxed_stop` learning-mode metadata from surviving Profiles-tab manual edits and causing unintended transition subtraction in normalized pit-loss outputs.

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

## 2026-05-04 — StrategyDash one-stop burn-basis current-tick refresh fix
- Classification: **internal-only** (pre-green advice seam timing correctness; no runtime fuel/pit/control contract changes).
- Fixed `StrategyDash.NextRefuelTargetLitres` one-stop PUSH/SAVE selection path to use current-tick locally resolved burn values inside `UpdatePreRaceOutputs(...)` rather than shared `PushFuelPerLap` / `FuelSaveFuelPerLap` fields that may still hold prior-frame values at that execution point.
- Preserved fallback and ownership boundaries: PUSH uses current-tick push burn when valid, SAVE uses current-tick save burn when valid, otherwise NORM (`preRaceFuelPerLap`); no PitFuelControlEngine behavior changes and no changes to `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, boxed refuel latches, or `Pit.FuelControl.*` semantics.

## 2026-05-04 — StrategyDash V2 pre-green advice seam + PreRace contingency basis correction
- Classification: **both** (new additive dash-facing exports + pre-race fuel-needed basis correction).
- Removed legacy hardcoded PreRace `+2 laps` reserve from `LalaLaunch.PreRace.TotalFuelNeeded`; total-needed now resolves as `base race fuel requirement + active contingency litres` using the existing active contingency seam.
- Preserved ownership boundaries and runtime contracts: no changes to `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, `Pit.FuelControl.*`, boxed refuel latch behavior, or PitFuelControlEngine behavior.
- Added additive `StrategyDash.*` exports for pre-green advice (`Phase/PhaseText`, strategy classification, start-fuel advice/status, next-refuel advice/status, contingency text).
- `StrategyDash.*` is publish-safe during race-running but documented as non-primary there; race-running dashboards should continue using existing runtime fuel/pit/control surfaces.



## 2026-05-04 — Setup fuel fallback export + litre-unit string validation follow-up
- Classification: **both** (dash-facing setup-fuel export seam + parser safety correction + docs alignment).
- Added `Fuel.Setup.FuelLevel`, `Fuel.Setup.FuelLevelValid`, and `Fuel.Setup.FuelLevelSource` in `LalaLaunch.cs`.
- Setup resolver checks setup paths in strict priority and uses the first usable value only:
  1) `CarSetup.BrakesDriveUnit.Fuel.FuelLevel`
  2) `CarSetup.Chassis.Front.FuelLevel`
  3) `CarSetup.Chassis.Rear.FuelLevel`
  4) `CarSetup.Suspension.Rear.FuelLevel`
- String parser safety follow-up:
  - accepts litre-labelled strings (`77.0 L`, `77 L`, `77,0 L`, `litre/litres/liter/liters`);
  - rejects known non-litre unit strings (`gal`, `gallon`, `gallons`) and other unknown units;
  - rejects bare numeric strings (string values require explicit litre units for this seam).
- Numeric raw values remain accepted as litres.
- Added boxed integral raw-type support in `TryParseSetupFuelLitres(...)` for `long`, `short`, `uint`, `ulong`, `ushort`, `byte`, and `sbyte` (existing `double`/`float`/`decimal`/`int` support preserved), with zero/non-positive and non-finite rejection retained.
- Preserved invariants: no overwrite of `Telemetry.FuelLevel`; no changes to `Fuel.LiveFuelPerLap`, pit math, planner math, PreRace strategy logic, or max tank authority.

## 2026-05-04 — PreRace current-fuel basis setup-fallback integration
- Classification: **both** (PreRace on-grid/status behavior improvement + docs alignment).
- `UpdatePreRaceOutputs(...)` now resolves an effective PreRace current-fuel basis using:
  1) live current fuel when valid/positive,
  2) setup fallback (`Fuel.Setup.FuelLevel`) when live current fuel is unavailable/zero and setup fallback is valid **during pre-race/grid/formation only** (SessionState `<4`),
  3) existing zero/fallback behavior when neither is available.
- Active race-running (SessionState `==4`) now keeps live fuel authoritative for PreRace outputs even when live fuel is `0` (setup fallback disabled in race-running phase).
- PreRace one-stop/no-stop/multi-stop delta/status feasibility paths now use that effective PreRace-only current-fuel basis.
- Preserved invariants: no overwrite of telemetry fuel, no live fuel-learning changes, no `Fuel.Pit.*` runtime math changes, no planner/max-tank authority changes.

## 2026-05-01 — League Race Phase 2 debug/metadata exports only
- Classification: **both** (new dash-facing debug metadata exports + docs alignment).
- Added League Class global/player export family in `LalaLaunch` using resolver-only authority: `LeagueClass.Enabled`, `LeagueClass.Mode`, `LeagueClass.ConfigStatusText`, `LeagueClass.LoadedCount`, `LeagueClass.ValidDriverCount`, `LeagueClass.InvalidRowCount`, `LeagueClass.DuplicateRowCount`, `LeagueClass.Player.*` (`Name/ShortName/Rank/ColourHex/Valid/Source/OverrideActive`).
- Added passive League Class metadata exports for Opp slots (`Opp.Ahead1..5.*` / `Opp.Behind1..5.*`): `LeagueClassName`, `LeagueClassShortName`, `LeagueClassRank`, `LeagueClassColourHex`, `LeagueClassValid`, `LeagueClassSource`.
- Added optional passive League Class metadata exports for CarSA physical slots (`Car.Ahead01..05.*` / `Car.Behind01..05.*`) with the same fields.
- Source text contract is uppercase: `CSV`, `NAME`, `MANUAL`, `NATIVE`, `NONE`; unresolved exports publish `Valid=false` with empty/zero display values.
- Preserved invariants: no Opponents same-class selection changes, no PitExit class-row filtering changes, no H2H/ClassLeader/ClassBest behavior changes, no CarSA physical-selection/order/filtering changes.

## 2026-04-30 — Race finish/end-phase authority redesign (SessionState-first + dash stability gates)
- Classification: **both** (new dash-facing race-end exports + finish-detection contract correction).
- Updated `LalaLaunch` finish detection precedence:
  - `Race.EndPhase` now resolves each tick from race `SessionState` authority (`Unknown/Running/AfterZeroLeaderRunning/LeaderFinished/SessionComplete`) with `Race.EndPhaseConfidence`;
  - `Race.LastLapLikely` now gates dash end-lap behavior (`SessionState==5` any race type, plus timed-race `SessionState==4 && SessionTimeRemain<=0`);
  - overall leader finish now latches from `SessionState>=5` high-confidence state authority;
  - single-class class-leader finish now mirrors overall finish; multiclass class finish remains class-targeted (no SessionState-only class-proof promotion);
  - effective `Race.LeaderHasFinished` now uses single-class overall latch directly and multiclass class-valid + class-latch selector.
- Removed session-checkered-flag use as proof of overall leader finish and kept player checkered handling only for driver-side finish/summary timing.
- Added sustained non-race reset guard for finish latches (anti-blip): finish state is no longer cleared on a single transient non-race tick.
- Preserved invariants:
  - no fuel-learning changes,
  - no fuel-projection math redesign,
  - no pit fuel-control/pit-command behavior changes,
  - no dashboard ownership shift into plugin logic.
### 2026-04-30 — League Class enable-time mode guard (Disabled -> CsvThenName)
- Classification: **both** (runtime behavior guard + docs alignment).
- Added a narrow enable-edge guard in `LalaLaunch` so when `LeagueClassEnabled` flips `false -> true` and `LeagueClassMode` is still `Disabled (0)`, mode is auto-set to `CsvThenName (3)`.
- Preserved user intent for all non-disabled modes (no override when user already selected `CsvOnly`, `NameOnly`, or `CsvThenName`).
- Purpose: prevent an inactive enabled state and ensure League Class preview/resolution paths become active immediately after enable.

### 2026-04-30 — Pit Fuel Control Push/Save profile-assisted mode + guard
- Classification: **both** (new dash-facing controls/exports + bounded internal target-selection behavior change).
- Added `Pit.FuelControl.PushSaveMode` (`0=LIVE`, `1=PROFILE`) and `Pit.FuelControl.PushSaveModeText` exports plus new action `Pit.FuelControl.PushSaveModeCycle`.
- Added settings-layer guard slider `Pit Fuel Push/Save Profile Guard (%)` (`0..30`, default `10`).
- Behavior: only internal Pit Fuel Control `PUSH`/`SAVE` target composition can switch to profile track max/min burn values (wet/dry aware) when eligible; both burns are clamped inside configured guard band `[stableNorm*(1-guard), stableNorm*(1+guard)]`; `NORM` and `SAVED` remain unchanged.
- Safety/fallback: profile-assisted path requires valid active profile + matching track stats + valid condition burn values + valid stable NORM burn for guard; otherwise silently falls back to existing live PUSH/SAVE behavior (no warnings/no action block). Push/Save mode toggle now performs immediate one-shot target refresh send in `MAN`/`AUTO` only when current source is `PUSH` or `SAVE`; explicit toggle refresh send-failure semantics align with `SourceCycle` shape (`PIT CMD FAIL` + `STBY`, and AUTO disarms).

### 2026-04-29 — PreRace Auto stable-source provenance follow-up
- Classification: **both** (dash-facing source-provenance correctness + docs contract clarification).
- Fixed Auto PreRace source-label seam in `LalaLaunch` when `selectedStrategy == Auto` and `LiveFuelPerLap_Stable > 0`:
  - source mapping now follows the selected stable-source label exactly (`Live => live`, `Profile => profile`, otherwise `fallback`).
- Removed profile-availability inference from that stable-consumption branch so Auto cannot report `profile` when the selected stable value came from non-profile fallback authority.
- Preserved invariants:
  - Auto fallback branch for `LiveFuelPerLap_Stable <= 0` still chooses profile fuel directly when available and labels `profile`,
  - no pre-race fuel-maths redesign,
  - no new persistent state.

### 2026-04-29 — PreRace fuel-source label clarification + Auto profile-source fallback fix
- Classification: **both** (dash-facing source-label contract clarity + narrow pre-race source correctness fix).
- Updated `LalaLaunch.PreRace.FuelSource` classification to remove ambiguous legacy labels:
  - removed `planner` and `simhub`,
  - accepted values are now `live`, `profile`, `planner-profile`, `planner-manual`, `fallback`.
- Added a narrow manual-mode pre-race source classifier in `LalaLaunch` that reuses existing planner ownership state:
  - `FuelCalculator.IsFuelPerLapManual`,
  - `FuelCalculator.FuelPerLapSourceInfo`.
- Preserved Auto-mode invariant (runtime-only labels):
  - Auto continues to publish only `live`/`profile`/`fallback` (never planner-classified values).
- Fixed Auto-mode source-label correctness seam:
  - when Auto uses stable fuel-per-lap with non-live/non-profile stable source text, pre-race source now resolves to `profile` if profile baseline fuel is available, else `fallback`,
  - prevents false `fallback` labeling when profile/track fuel burn is actually driving pre-race fuel delta.
- Preserved invariants:
  - no pre-race fuel maths redesign,
  - no preset-source label introduced,
  - no new persistent state.

### 2026-04-28 — Lap-based contingency scaling for tactical Push/Save deltas
- Classification: **both** (driver-facing tactical fuel-delta correctness + docs contract alignment).
- Fixed `UpdateLiveFuelCalcs` tactical required-to-finish composition so lap-configured contingency is resolved per burn basis instead of reusing one stable-basis litre reserve for all modes.
  - normal required litres now use stable-burn contingency litres,
  - push required litres now use push-burn contingency litres,
  - save required litres now use save-burn contingency litres.
- Preserved invariants:
  - public `Fuel.Contingency.Litres/Laps/Source` exports remain stable-basis display/debug seams,
  - litre-configured contingency behavior remains unchanged (same fixed litre reserve for normal/push/save),
  - reserve remains owned only on required-to-finish side (no reserve added to requested fuel, `Fuel.Pit.WillAdd`, or clamp amounts),
  - Pit Fuel Control live targets continue to consume `-Fuel.Delta.LitresCurrent*` seams without re-adding contingency.

### 2026-04-28 — Pit Fuel Control contingency double-count follow-up
- Classification: **both** (driver-facing pit fuel target correctness + docs contract alignment).
- Fixed `BuildPitFuelControlSnapshot()` live `NORM/PUSH/SAVE` target composition to consume contingency-aware tactical deltas directly without re-adding contingency.
  - `TargetNormLitres/TargetPushLitres/TargetSaveLitres` now use `max(0, -Fuel_Delta_LitresCurrent*)`.
  - prevents contingency from being counted twice when tactical deltas already include contingency on required-to-finish.
- Preserved invariants:
  - `SAVED` remains planner-owned via `PlannerNextAddLitres`,
  - tactical delta contingency protection remains unchanged and remains owned in the fuel runtime delta seams.

### 2026-04-28 — Burn To End exports + contingency-aware tactical fuel deltas
- Classification: **both** (driver-facing Strategy fuel guidance semantics + export contract extension/docs alignment).
- Added new Fuel runtime exports in `LalaLaunch`:
  - `Fuel.RequiredBurnToEnd`, `Fuel.RequiredBurnToEnd.Valid`, `Fuel.RequiredBurnToEnd.State`, `Fuel.RequiredBurnToEnd.StateText`, `Fuel.RequiredBurnToEnd.Source`
  - `Fuel.Contingency.Litres`, `Fuel.Contingency.Laps`, `Fuel.Contingency.Source`
- Burn-to-end behavior:
  - raw burn-to-end uses `LiveLapsRemainingInRace_Stable` and active contingency reserve;
  - displayed `Fuel.RequiredBurnToEnd` is clamped to save/push burn bounds;
  - state/text use raw unbounded burn-to-end (`CRITICAL/SAVE/HOLD/PUSH`) with fixed hold-band rule.
- Contingency authority:
  - planner-first (`FuelCalcs` live contingency),
  - profile track fallback (`TrackStats` contingency),
  - default fallback `1.5 laps`;
  - zero contingency remains valid.
- Updated tactical driver-facing delta semantics:
  - `Fuel.Delta.LitresCurrent/Plan/WillAdd` and Push/Save variants now include contingency on required-to-finish side only.
- Preserved invariants:
  - `Fuel.Pit.WillAdd` remains clamp mirror (`min(requestedAdd, tankSpace)`),
  - no pit-window/severity/pit-command/fuel-control/tyre-control redesign.

### 2026-04-28 — Pit Fuel/Tyre fault-pipeline consistency + ownership-leak follow-up
- Classification: **internal-only** (diagnostic fault-pipeline correctness/consistency; no command-path behavior redesign).
- Fuel follow-up:
  - external mirror/takeover handling now clears pending owned mirror expectations before remap/cancel state application;
  - request-fault bit evaluation is now ownership-gated (`IsAutoModeActive || AutoArmed`) so stale pending expectations cannot leak after ownership surrender.
- Tyre follow-up:
  - AUTO correction-send ticks now explicitly suppress `Pit.TyreControl.Fault` to `0` in the same tick as the correction send;
  - unmappable requested-compound truth (`HasTireServiceSelection && IsTireServiceSelected && HasRequestedCompound && !hasTruth`) now hard-suppresses fault to `0` across mode paths.
- Preserved invariants:
  - no command payload changes,
  - no transport/timing/retry changes,
  - no fuel math changes,
  - no tyre mode-cycle/AUTO ownership redesign.

### 2026-04-28 — Pit Tyre AUTO-correction settle-window fault timing hotfix
- Classification: **internal-only** (diagnostic export timing correction; no command/AUTO behavior change).
- Updated `PitTyreControlEngine.OnTelemetryTick()` to re-evaluate settle suppression *after* `HandleAuto(...)` before assigning `Pit.TyreControl.Fault`.
- Prevents one-tick false non-zero fault flashes when AUTO issues a correction send in the same tick and starts a new settle window.
- Preserved invariants:
  - no command behavior changes,
  - no AUTO correction logic changes,
  - no retries added,
  - no payload changes.

### 2026-04-28 — Pit Fuel/Tyre diagnostic fault timing correction follow-up
- Classification: **internal-only** (diagnostic export timing correction; no command-path behavior change).
- Updated telemetry-tick fault timing so both `Pit.FuelControl.Fault` and `Pit.TyreControl.Fault` are evaluated from final post-tick state after mirror/remap/cancel handling in the same tick.
- Added intentional-transition suppression to prevent false one-tick flashes:
  - Fuel fault now suppresses to `0` on same-tick external mirror remap handling.
  - Tyre fault now suppresses to `0` on same-tick truth-mirror remap and AUTO-cancel remap handling.
- Preserved invariants:
  - no command behavior changes,
  - no mode remap behavior changes,
  - no retries added,
  - no payload changes.

### 2026-04-28 — Pit Tyre Control DRY/WET diagnostic fault unmappable-truth suppression follow-up
- Classification: **internal-only** (diagnostic fault evaluation correction; no command-path behavior change).
- Updated `PitTyreControlEngine.ComputeFault(...)` DRY/WET branch so unmappable requested-compound truth (`hasTruth == false`) returns fault `0` instead of raising request faults.
- Preserved existing behavior:
  - known DRY/WET truth mismatch after settle still raises fault bits,
  - command sends/transport/AUTO correction behavior is unchanged,
  - truth-mirror behavior is unchanged.

### 2026-04-28 — Pit Fuel/Tyre selector diagnostic fault exports
- Classification: **both** (new dash-facing diagnostic exports + internal contract/docs alignment).
- Added Fuel Control diagnostic export `Pit.FuelControl.Fault` with numeric contract:
  - `0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`.
- Added Tyre Control diagnostic export `Pit.TyreControl.Fault` with numeric contract:
  - `0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`.
- Preserved invariants:
  - no pit fuel command payload changes,
  - no pit tyre command payload changes,
  - no retries/correction sends added,
  - fault evaluation is suppressed during existing post-command settle/suppression windows and unknown-truth states to avoid normal latency flash.
### 2026-04-28 — Strategy max-fuel display follow-up: remove preset-state display dependency path
- Classification: **internal-only** (binding/notification hygiene to lock in Issue #552 behavior contract).
- Updated `RaisePresetStateChanged()` to stop raising `MaxFuelOverrideDisplayValue` notifications.
  - `MaxFuelOverrideDisplayValue` is now explicitly authoritative-value driven (`MaxFuelOverride` in Profile mode, live cap in Live Snapshot mode), so preset badge/state changes no longer imply a display-value ownership path.
- Preserved invariants:
  - preset apply still writes `MaxFuelOverride` in Profile mode,
  - `MaxFuelOverride` setter remains the owner of dependent display/percent/warning notifications + strategy/preset refresh,
  - no fuel maths or Live Snapshot lock semantics changed.

### 2026-04-28 — Strategy max-fuel override UI desync fix after preset apply
- Classification: **both** (driver-visible Strategy UI binding correction + internal consistency with existing planner authority contract).
- Updated `FuelCalcs.MaxFuelOverrideDisplayValue` profile-mode getter to always mirror authoritative `MaxFuelOverride`.
  - removes stale `_appliedPreset` display override that could hold slider/textbox visuals at preset value while strategy math and helper percent used updated manual value.
- Preserved invariants:
  - Live Snapshot max-fuel lock/authority remains unchanged (still sourced from live cap and non-editable),
  - no fuel math, pit fuel control, or export contract changes.

### 2026-04-24 — Pit feedback dash visual mapping correction (Caution steady, Warning 750ms blink)
- Classification: **both** (dash-facing visual contract correction + documentation alignment).
- Corrected pit feedback dash visual mapping in subsystem/dash/inventory docs:
  - Severity 3 `Caution` is now steady (`no blink`);
  - Severity 4 `Warning` now blinks for 1 second at `750ms`.
- Preserved invariants:
  - no `PitCommandEngine` code changes,
  - no payload/transport/timing/state-machine behavior changes.

### 2026-04-24 — Pit feedback priority gating follow-up (severity-aware active replacement/suppression)
- Classification: **both** (dash-visible pit feedback sequencing behavior + docs/CSV contract alignment).
- Updated `PitCommandEngine.PublishMessage(...)` feedback publishing priority logic:
  - if no pit feedback is active, publish normally;
  - if a pit feedback message is active, equal/higher severity replaces immediately and restarts the existing 3000 ms hold;
  - if a pit feedback message is active, lower severity feedback is suppressed and the active message/hold remain unchanged.
- Preserved invariants:
  - no counters/sequence exports added,
  - no command payload/transport/timing changes,
  - no Fuel Control/Tyre Control command-state logic changes,
  - repeated identical message at equal severity still retriggers/restarts hold.
- Updated pit feedback contracts/docs:
  - subsystem + dash docs now explicitly describe severity-priority suppression/replacement behavior;
  - SimHub parameter inventory now documents the same priority rule and dash visual severity mapping (`0..4`);
  - pit logic CSV contracts now include a `Severity` column and use uppercase `PIT CMD FAIL` wording consistently.

### 2026-04-24 — Pit feedback reset seam follow-up: avoid on-track edge clears
- Classification: **internal-only** (reset seam hardening; no command/transport behavior change).
- Updated `LalaLaunch` pit-control on-track edge handlers so `PitCommandEngine.ResetFeedbackState()` is **not** invoked from `Telemetry.IsOnTrackCar` transitions.
- `ResetFeedbackState()` remains wired only to explicit lifecycle/reset seams (`ResetAllValues`, `ManualRecoveryReset`) to avoid transient telemetry gap/on-track flaps clearing active pit feedback mid-window.
- Preserved invariants:
  - no payload/timing/transport changes,
  - no fuel/tyre mode-cycle changes,
  - no severity mapping changes.

### 2026-04-24 — Pit command feedback severity standardization + restartable active hold contract
- Classification: **both** (dash-facing pit feedback contract extension + bounded internal feedback ownership refactor).
- Updated `PitCommandEngine` to be the single feedback severity owner across built-in pit actions, Fuel Control, Tyre Control, custom messages, and raw pit commands:
  - added `Pit.Command.Severity` (`0..4`) and `Pit.Command.SeverityText` (`None/Info/Advisory/Caution/Warning`);
  - centralized message-to-severity mapping in `PitCommandEngine.Publish*` feedback path.
- Standardized feedback hold contract:
  - `Pit.Command.Active` remains the canonical dash trigger surface;
  - every feedback publish re-arms the same active window even for repeated identical `DisplayText`;
  - active hold is now standardized at 3000 ms (`MessageHoldMs`), with no counter/sequence export added.
- Added feedback reset behavior at existing lifecycle reset seams:
  - clears `Pit.Command.DisplayText`,
  - clears active hold,
  - resets severity to `0 / None`.
- Preserved invariants:
  - no command payload changes,
  - no command transport mode/timing changes,
  - no fuel AUTO or tyre AUTO logic redesign.

### 2026-04-24 — Tyre Control follow-up: preserve `PIT CMD FAIL` visibility across immediate truth-mirror remap
- Classification: **both** (driver-visible failure feedback persistence + narrow internal timing gate).
- Updated `PitTyreControlEngine` send-failure handling:
  - raw tyre send failure now arms a short failure-hold timestamp;
  - during that short hold, passive truth-mirror feedback publish (`TYRE OFF/DRY/WET`) is suppressed only.
- Preserved existing control semantics:
  - truth-mirror mode remap still occurs during the hold,
  - command payloads are unchanged (`#cleartires`, `#t tc 0`, `#t tc 2`),
  - mode cycle is unchanged,
  - no retries/counters/confirmation windows/new guards were added.

### 2026-04-24 — Tyre Control feedback wording polish + `Pit.Command.Active` retrigger verification
- Classification: **both** (driver-visible tyre feedback wording alignment + internal feedback-pulse contract clarification).
- Updated `PitTyreControlEngine` feedback wording contract:
  - plugin-driven mode actions now publish `TYRE CHANGE OFF/DRY/WET/AUTO`;
  - AUTO correction publishes `TYRE AUTO CHANGE DRY/WET`;
  - AUTO manual takeover publishes `TYRE AUTO CANCELLED`.
- Added passive truth-mirror feedback publication outside AUTO:
  - remaps now publish state-only wording (`TYRE OFF` / `TYRE DRY` / `TYRE WET`) only when mirrored mode actually changes;
  - passive feedback is not emitted immediately after plugin-driven commands unless a later external truth change occurs.
- Verified `Pit.Command.Active` restart behavior in `PitCommandEngine`:
  - no code change required; every feedback publish already re-arms the same active display window even when message text is unchanged.
- Preserved invariants:
  - tyre payloads unchanged (`#cleartires`, `#t tc 0`, `#t tc 2`),
  - no retries/confirmation-timeout tyre failures added,
  - no transport/fuel-control/dashboard changes.
### 2026-04-24 — Pit Fuel Control feedback polish follow-up (owned OFF->MAN echo + MAN >MAX wording)
- Classification: **both** (driver-visible feedback correction + narrow ownership-consumption fix).
- Updated `PitFuelControlEngine` owned-mirror expectation expiry ordering:
  - same-tick telemetry changes now run owned-consumption first;
  - expectation expiry now runs only on no-change ticks for each dimension, preserving stale-pending cleanup while preventing plugin-owned `OFF -> MAN` `dpFuelFill=ON` echoes from being misclassified as external `REFUEL SET ON BY MFD`.
- Updated MAN over-space feedback wording only:
  - MAN `>MAX` feedback now publishes `REFUEL <SRC> <requested>L >MAX`;
  - AUTO `>MAX` feedback remains `AUTO FUEL <requested>L >MAX`;
  - command payloads/transport/fuel maths/override behavior are unchanged.

### 2026-04-24 — Pit Fuel Control suppression gate fix + suppression-reason diagnostics throttling
- Classification: **both** (driver-visible action unfreeze in valid sessions + internal observability/noise control).
- Updated `LalaLaunch.BuildPitFuelControlSnapshot(...)` suppression gating:
  - removed blanket Offline Testing suppression from Fuel Control;
  - suppression now applies only to truly invalid snapshot contexts (`no-plugin-manager`, `no-session`);
  - snapshot now carries `SuppressFuelControlReason` for diagnostics.
- Updated `PitFuelControlEngine` suppression diagnostics:
  - action blocked logs now include suppression reason (`suppressed:<reason>`);
  - entry snapshot logs now include `suppressReason=<...>`;
  - telemetry suppression logging is now transition/throttled (logs on transition/reason change and periodic throttle) with explicit `suppression-cleared` log when suppression ends.
- Preserved invariants:
  - no payload/transport/fuel-math redesign,
  - no internal `Pit.ToggleFuel` use in Fuel Control,
  - no Tyre Control changes.

### 2026-04-24 — Pit Fuel Control frozen-action diagnostics instrumentation (entry-path + silent-return reasons)
- Classification: **both** (driver-visible blocked-action feedback/logs + internal observability instrumentation).
- Added Fuel Control instrumentation without command-model redesign:
  - `LalaLaunch` Fuel Control action methods now log action-entry receipts (`PitFuelControlModeCycle/SourceCycle/SetPush/SetNorm/SetSave/SetPlan action received`) before forwarding to `PitFuelControlEngine`;
  - `PitFuelControlEngine` public action entry points now emit compact state snapshots (mode/source/armed/suppression/ownership/plan/target/override/last-sent) for ModeCycle/SourceCycle/SetPush/SetNorm/SetSave/SetPlan and gated `OnLapCross`;
  - added explicit reason logs before early returns/no-send branches across action paths and ownership seams (`snapshot-null`, `suppressed`, `off-hard-guard`, `auto-plan-blocked`, `plan-invalid`, `source-stby`, `target-invalid`, `send-failed`, `auto-not-armed`, `lap-cross-no-material-delta`, `iracing-autofuel-ownership`, `external-mirror-change`, `owned-mirror-consumed`).
- Kept invariants unchanged:
  - no use of `Pit.ToggleFuel` inside Fuel Control,
  - no fuel maths/transport/payload changes,
  - no Tyre Control changes,
  - no CSV behavior-contract redesign.

### 2026-04-24 — Tyre Control truth-mirror telemetry mapping follow-up
- Classification: **both** (driver-visible truth classification correction + docs contract alignment).
- Updated `PitTyreControlEngine.IsRequestedCompoundInDesiredFamily(...)` to map `PitSvTireCompound` truth as:
  - dry family: `0`
  - wet family: `1`
- Preserved outgoing one-shot command contract:
  - DRY command remains `#t tc 0`
  - WET command remains `#t tc 2`
- Scope intentionally narrow:
  - no retries, no transport changes, no fuel-control changes.

### 2026-04-24 — Tyre Control AUTO entry follow-up: keep initial evaluation pending while truth is unknown
- Classification: **both** (driver-visible AUTO first-evaluation correctness fix + narrow internal state correction).
- Updated `PitTyreControlEngine.HandleAuto(...)` initial-evaluation branch so AUTO entry no longer clears pending evaluation when tyre truth is unavailable (`hasTruth == false`).
  - while truth is unknown, AUTO now keeps `_autoPendingInitialEvaluation=true` and does not update `_autoLastDesiredWet`;
  - when truth becomes known, the normal first AUTO evaluation runs once:
    - mismatched truth vs declared target sends one correction command,
    - already-matching truth clears pending with no send.
- Preserved invariants:
  - one-shot/no-retry command model unchanged,
  - no new guards/counters/retry logic added,
  - scope limited to `PitTyreControlEngine` behavior seam.

### 2026-04-24 — Tyre Control simplification: one-shot combined commands + 1.0s settle truth-following model
- Classification: **both** (driver-visible tyre-control behavior correction + internal state-machine simplification).
- Reworked `PitTyreControlEngine` command contract to match live-tested MFD behavior:
  - `OFF => #cleartires`, `DRY => #t tc 0`, `WET => #t tc 2` (transport still owns trailing `$` normalization),
  - no standalone tyre-service `#t` command path and no split service/compound command sequence in Tyre Control.
- Removed tyre-control retry/attempt/timeout-failure machinery and plugin-owned intent/suppression ownership windows.
  - each driver action sends at most one tyre command,
  - each AUTO correction decision sends at most one tyre command,
  - no automatic resend loops.
- Added single post-send settle hold (1.0s):
  - outside AUTO, mode mirrors known MFD truth after settle and never fights manual MFD tyre edits,
  - unknown/ambiguous truth remains hold/no-send fail-safe.
- Simplified AUTO behavior:
  - AUTO entry is feedback-only (`TYRE AUTO`) and does not blindly send,
  - AUTO corrects only when known truth mismatches declared-wet target,
  - manual takeover inside AUTO cancels/remaps with `TYRE AUTO CANCELLED` and no fight-back command.
- Simplified failure feedback policy:
  - `PIT CMD FAIL` now comes only from raw transport send failure (`ExecuteRawPitCommand` returned false),
  - timeout-driven tyre confirmation failures were removed.
### 2026-04-24 — Pit Fuel Control review follow-up: AUTO+PLAN ModeCycle impossible-state no-send recovery
- Classification: **internal-only** (contract-alignment correction; no new surface area).
- Updated `PitFuelControlEngine.ModeCycle()` AUTO branch to explicitly guard impossible `AUTO + PLAN` before the AUTO->OFF send path:
  - now recovers to `Source=STBY`, `AutoArmed=false`,
  - remains AUTO/disarmed,
  - sends no raw command and publishes no OFF feedback from this impossible-state branch.
- This aligns runtime behavior with the already-updated CSV row (`AUTO / PLAN / ModeCycle => no-send recovery`) and avoids unintended `#-fuel$` sends from impossible PLAN-in-AUTO state.

### 2026-04-23 — Pit Fuel Control contract alignment follow-up: direct-command wording + AUTO/MAN feedback parity + invalid PLAN failure feedback
- Classification: **both** (driver-visible Fuel Control feedback/state-contract alignment + internal guard correction).
- Updated `PitFuelControlEngine` to align direct-command behavior with `FuelModesLogicCSV.csv` while preserving explicit raw command ownership (`#fuel$`, `#-fuel$`, `#fuel X$`, additive overshoot payload):
  - `MAN -> AUTO` immediate source send branch now keeps the source-send feedback (`AUTO REFUEL SET <SRC> X L`) instead of overriding with generic mode text;
  - AUTO source sends now use AUTO wording (`AUTO REFUEL SET ...`) and AUTO over-space wording (`AUTO FUEL <requested>L >MAX`) instead of MAN wording;
  - `MAN STBY/PLAN -> AUTO STBY` feedback now uses `AUTO REFUEL STBY`;
  - `AUTO -> OFF` now publishes `REFUEL OFF`;
  - MAN invalid `SetPlan` (planner/live mismatch) now publishes `Pit Cmd Fail` instead of silently no-op;
  - impossible `AUTO + PLAN` state is now guarded to safe no-send recovery (`AUTO STBY`, disarmed) and no longer falls through to PUSH source send.
- Updated subsystem/docs contract surfaces (`FuelModesLogicCSV.csv`, subsystem doc) to match the corrected behavior wording and impossible-state handling.
- Preserved invariants:
  - no `Pit.ToggleFuel` use inside Fuel Control,
  - no retry loops or command-transport redesign,
  - no Tyre Control changes.

### 2026-04-23 — Pit Fuel Control testing/polish pass: OFF->MAN feedback alignment + raw-command observability + max-feedback table alignment
- Classification: **both** (driver-visible OFF->MAN send/feedback correction + internal observability/docs alignment).
- Updated Fuel Control OFF->MAN behavior in `PitFuelControlEngine.ModeCycle()`:
  - OFF->MAN uses `#fuel$` payload (no plus-sign additive form),
  - OFF->MAN selection feedback aligns to table contract (`FUEL MAN STBY`).
- Expanded raw-command failure observability in `PitCommandEngine.ExecuteRawPitCommand(...)`:
  - empty-after-normalization blocked sends now log both `raw` and `normalized` payload fields.
- Updated authoritative behavior table `Docs/Subsystems/FuelModesLogicCSV.csv`:
  - corrected OFF->MAN chat payload row to `#fuel$`,
  - added explicit MAN/AUTO over-tank-space/max-feedback contract rows (`FUEL MAX`, `AUTO FUEL <requested>L >MAX`) without changing outgoing command payload/clamp behavior.
- Preserved invariants:
  - no fuel math redesign,
  - no transport strategy redesign,
  - no retry loops or dashboard JSON changes.

### 2026-04-23 — Tyre Control follow-up: compound confirmation timeout no longer reverts successful DRY/WET MFD changes
- Classification: **both** (driver-visible false timeout failure/remap fix + narrow confirmation-path correction).
- Updated `PitTyreControlEngine.EnsureCompound(...)` pending-confirmation behavior:
  - confirmation success now triggers as soon as requested compound truth exists and matches the desired DRY/WET family (`snapshot.HasRequestedCompound` + family match),
  - confirmation state is cleared immediately on that family convergence so timeout failure cannot run after convergence.
- Restored bounded timeout failure handling for truly unconfirmed sends:
  - timeout now again publishes `PIT CMD FAIL` + manual-truth remap only when requested-family convergence did not occur inside the confirmation window.
- Preserved invariants:
  - no retry-loop reintroduction,
  - no `#t$` service command reintroduction,
  - no transport or broad tyre-control redesign.

### 2026-04-23 — Pit command transport regression fix: block chat-open `T` leakage into typed raw/custom commands
- Classification: **both** (driver-visible pit raw/custom command text integrity fix + transport-path hardening).
- Updated `PitCommandEngine` chat-injection transport sequencing only (no tyre/fuel control state-machine changes):
  - direct postmessage path now sends `Esc` pre-close before `T` open-chat, then types payload + submit;
  - legacy foreground sendinput path now sends `Esc` before `T` as the same stale-open guard.
- Fix intent/outcome:
  - prevents stale-open chat state from absorbing the opener key into payload text (no `t#...` / `tt#...` corruption),
  - raw pit commands like `#tc 0` are now transported as exact payload text.
- Preserved invariants:
  - no command-architecture redesign, no tyre control logic change, no new retry/poll loop behavior.

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
  - `MAN -> AUTO` now sends only for `PUSH/NORM/SAVE`; `STBY` and `SAVED` both enter `AUTO STBY` with no send.
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
  - `MAN -> AUTO` keeps existing immediate amount-send ownership semantics (`PUSH/NORM/SAVE` arm on successful send; `SAVED` remains one-shot then forced `STBY` disarmed; `STBY` stays disarmed),
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
  - entering AUTO from `SAVED` performs one immediate send then always returns to `STBY` disarmed,
  - AUTO source-cycle is now `PUSH -> NORM -> SAVE -> PUSH` (PLAN removed from AUTO cycle; PLAN remains MAN-only cycle option).
- Kept existing invariants intact:
  - AUTO cancel remains edge-triggered,
  - lap-cross AUTO send cadence unchanged,
  - OFF/MAN remain MFD-derived truth and no parallel plugin OFF/MAN state was introduced.
- Follow-up fix (same day): AUTO-entry immediate-send failure now explicitly publishes `Pit Cmd Fail` in both MAN->AUTO send branches (`SAVED` one-shot branch and `PUSH/NORM/SAVE` branch) while preserving existing fallback behavior (`Source=STBY`, `AutoArmed=false`).

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
  - `SAVED` remains blocked from active AUTO operation (AUTO + PLAN forces STBY/disarmed),
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
  - `MAN -> AUTO` when source is `SAVED` still forces `Source=STBY`, keeps `AutoArmed=false`, and publishes `FUEL AUTO STBY`.
- `AUTO -> OFF` now clears `AutoArmed` and publishes `FUEL MODE OFF` without sending a pit command.
- Classification: **both** (driver-visible control-cycle fix + internal contract/docs alignment).

### PR #576 follow-up: FuelSetMax ZERO-phase tank-full bypass + forced-STBY feedback refinement
- Corrected `PitFuelControlEngine.ModeCycle` PLAN-forced-STBY AUTO transition arming:
  - `MAN -> AUTO` when source is `SAVED` now sets `Mode=AUTO`, `Source=STBY`, and `AutoArmed=false` (prevents immediate `AUTO CANCELLED` checks before source reselection).
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
  - `MAN -> AUTO` from `SAVED` forced STBY now publishes `FUEL AUTO STBY`.
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
  - `MAN -> AUTO` while `Source=SAVED` is now allowed, but forces `Source=STBY` instead of hard-blocking/skipping AUTO.
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
- Added fixed `PitExitTransitionAllowanceSec = 2.00` in `LalaLaunch.cs` at the shared boxed-stop prediction seam (`CalculateTotalStopLossSeconds`), yielding:
  - `TotalStopLoss = pit-lane baseline + boxed service model + 2.00s transition allowance`.
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

## 2026-04-30 — Build-fix pass (CS1674/CS0122/CS0236)
- Classification: **internal-only** (compile/legal-access fixes only; no runtime behavior redesign).
- `GlobalSettingsView.xaml.cs`: removed `using (...)` around `Microsoft.Win32.OpenFileDialog` because it is not `IDisposable`; dialog flow and reload behavior unchanged.
- `PitCommandEngine`: added narrow public wrapper `PublishInfoMessage(string)` and switched Push/Save mode-cycle feedback call site to that seam; severity mapping/hold behavior unchanged.
- `LaunchPluginSettings`: changed `LeagueClassMode` default initializer to literal `0` to avoid illegal instance-member reference in initializer; runtime normalization/behavior unchanged.
### 2026-04-30 — League Race Phase 1 UI usability follow-up

- Follow-up fix: removed unintended `LeagueClassShowCsvSection` visibility gate from the Pit Commands transport selector row in `GlobalSettingsView.xaml`, restoring always-visible access to pit transport mode regardless of League Class classification mode.
- Classification: **both** (settings UX visibility/clarity improvements + internal behavior-safe normalization).
- League Class settings UI now defaults/normalizes player override to **Auto-detect** when persisted manual override is invalid (manual selected with blank class name).
- Added explicit column headers for player override and suffix fallback rows, plus a detected-classes readout table after CSV load (read-only in this phase).
- Classification mode now controls section visibility:
  - `CSV only`: CSV path/status + detected classes visible; suffix fallback hidden.
  - `Name suffix only`: suffix fallback visible; CSV path/status hidden.
  - `CSV then name suffix`: both visible.
- Player preview text now uses clearer wording:
  - explicit unavailable text when live identity is not yet available,
  - otherwise shows `Player`, `Source`, and `Resolved class`.
- Preserved safe color-hex handling (invalid hex renders transparent preview) and avoided per-tick preview spam by retaining snapshot-based refresh gating.
### 2026-05-04 — TrackLearning dash export/action family for Offline Testing review/lock flow
- Classification: **both** (new dash-consumable exports/actions + internal contract documentation updates).
- Added narrow `TrackLearning.*` core export family in `LalaLaunch` for profile/track review only:
  - pit loss: `TrackLearning.PitLoss.Display/ValueSec/SourceText/Locked`,
  - markers: `TrackLearning.Markers.EntryPct/ExitPct/Locked`,
  - active-condition routing: `TrackLearning.Condition.IsWet/ModeText/AvgLapDisplay/AvgLapSamples/FuelSave/FuelAvg/FuelMax/FuelSamples/Locked`.
- Added lock-only toggle actions:
  - `TrackLearning.PitLoss.ToggleLock`,
  - `TrackLearning.Markers.ToggleLock`,
  - `TrackLearning.Condition.ToggleLock`.
- Preserved existing ownership/invariants:
  - no learning-rule, fuel-math, pit-loss-calculation, or marker-calculation changes,
  - no relearn/reset/save actions added for this dash surface,
  - existing `TrackMarkersLock` / `TrackMarkersUnlock` actions unchanged.
- Persistence behavior:
  - pit-loss and condition lock toggles persist immediately through existing `ProfilesViewModel.SaveProfiles()` convention,
  - marker toggle uses the existing marker-store lock seam (`SetTrackMarkersLock`) and keeps existing marker persistence conventions.
- 2026-05-04 Strategy tab race-configuration ownership cleanup landed:
  - Race Preset control now hides while `Live Detect` race type is selected, preventing mixed manual/preset/live ownership cues.
  - Live Detect owner transitions no longer clear selected/applied preset state; race-basis ownership alone changes while preset/profile/manual setup values remain intact.
  - Refresh Calcs ownership remains unchanged (recalculation-only; no preset reapply/live-detect retrigger path added).
### 2026-05-04 — Strategy race-ownership cleanup follow-up (P1 review)
- Classification: **internal-only** (state-ownership correction; no fuel/detection formula changes).
- `UpdateLiveDetectedRaceDefinition(...)` now updates manual `RaceLaps` / `RaceMinutes` only while `SelectedRaceType == LiveDetect`; outside Live Detect it still caches helper/basis state but does not mutate manual race-length ownership.
- Exiting Live Detect now clears detected-length caches (`_lastLiveDetectedRaceLaps`, `_lastLiveDetectedRaceMinutes`) in addition to detected basis type, preventing stale basis reuse on later re-entry when fresh detection is unavailable.

## 2026-05-06 — H2HTrack selected-target League identity handoff fix
- Classification: **both** (H2HTrack class presentation correctness + internal contract/docs alignment).
- `BuildH2HTrackSelector(...)` now carries selected CarSA slot `UserID` into `H2HEngine.TargetSelector` so `H2HTrack.Ahead/Behind.ClassColor` and `ClassColorHex` can resolve League Class via the same CSV-first identity seam as CarSA/H2HRace when enabled.
- Kept H2HTrack physical target selection and all sector/delta/timing logic unchanged; unresolved/disabled paths still fall back to native class colour behavior.

## 2026-05-11 — Rolling snapshot CSV legacy-schema guard fix (PR #705 follow-up)
- Classification: **internal-only** (debug rolling CSV compatibility hardening; no runtime telemetry/export contract changes).
- Added a strict rolling wide-schema header guard in `LalaLaunch.WriteSnapshotRollingWide(...)`:
  - valid reusable rolling files must have header column 0 exactly `SimHubProperty`;
  - legacy schema (`SnapshotUtc` column 0) is detected and treated as incompatible.
- Incompatible rolling files (legacy/missing/blank/unknown header) are no longer parsed as wide-property rows; they are safely rewritten in current wide schema on the next capture with a bounded info diagnostic line.

## 2026-05-09 — PR follow-up: finish-like flag wording clarification
- Classification: **internal-only** (documentation wording clarification only; no runtime/code changes).
- Clarified finish-latch wording to describe checkered as the practical finish-like source in this telemetry path, while noting crossed is not relied on as required line-crossing proof.
- Kept existing RaceFinish/leader-finished latch behavior unchanged.

## 2026-05-11 — PR #708 fuel stable-authority follow-up (fallback poisoning fix)
- Classification: **internal-only** (runtime fuel stable-source correctness; no lap-acceptance/profile-persistence/pit-command/dashboard ownership changes).
- Tightened `UpdateStableFuelPerLap(...)` authority ordering to strict `Live (trusted) -> Profile -> Fallback`.
- Removed prior profile-confidence ceiling gate from stable-source selection so valid profile burn cannot be bypassed by fallback when live confidence is still below threshold.
- Added fallback replacement guard: when stable source is currently `Fallback`, any newly valid `Profile` or trusted `Live` candidate now replaces stable source+value immediately (deadband does not block authority recovery).
- Session/combo reset semantics remain intact: stable fuel source/value/confidence are still reset together by existing reset paths.


## 2026-05-11 — Race field-size denominator fix + live Race.* count exports
- Classification: **both** (new dash-facing live exports + race-finish denominator correctness + docs alignment).
- Added live plugin-owned `Race.FieldSize` and `Race.PlayerClassFieldSize` exports in `LalaLaunch.cs`.
- Live field-size counting is roster-first and explicitly excludes pace car rows using both `CompetingDrivers[].IsPaceCar` and `Drivers##.IsPaceCar`; fallback to opponent counters remains only when roster/class roster data is unavailable.
- `RaceFinish.PlayerOverallFieldSize` and `RaceFinish.PlayerClassFieldSize` now freeze from live `Race.*` field-size sources at class snapshot, preserving existing snapshot timing while preventing pace-car overcount and class-size `0` regressions.
- Follow-up fix: replaced undefined `SafeReadBoolProperty(...)` usage with existing bool-read helper path and added CompetingDrivers bracketed+numbered path compatibility fallback reads for roster counting.
- Follow-up fix: corrected overall fallback semantics so `GameData.OpponentsCount` is treated as field size directly (no unconditional `+1`).

## 2026-05-15 — Strategy race-basis owner model + refresh recalculation-only cleanup
- Classification: **both** (user-facing Strategy workflow clarity + internal ownership correctness).
- Added explicit Strategy race-basis owner modes (`Preset`, `LapLimited`, `TimeLimited`, `LiveDetect`) so race type/length authority follows the selected owner deterministically.
- Live Detect now only overrides race basis while selected; preset/profile/manual planning inputs remain intact.
- Refresh Calcs now recomputes strategy outputs only and no longer reloads profile/live owner state.
- Preset modified badge no longer flips when only PreRace mode differs, reducing misleading “calc changed” cues.

## 2026-05-15 — PR #723 follow-up fixes (preset reapply button + preset-save basis fix)
- Classification: **both** (Strategy UX correctness + preset serialization correctness + docs alignment).
- Replaced same-item ComboBox reselect reapply behavior with an explicit compact preset reapply button (`↻`) beside the preset selector.
- Fixed preset-save race-type derivation regression by serializing preset race basis/length from effective race-basis resolver instead of owner-mode booleans.
- Preset-owner-without-preset now surfaces explicit validation (`Select a race preset`) instead of silently using stale/manual fallback basis.
- Removed obsolete destructive Live Detect transition helper that cleared preset state; Live Detect now remains race-basis-only ownership while selected.

## 2026-05-16 — PR #723 semantic cleanup: owner vs effective race-basis separation
- Classification: **internal-only** (Strategy state semantics hardening + regression prevention).
- Clarified owner-vs-effective semantics in `FuelCalcs`: owner helpers remain radio-mode state while modified-state/visibility/serialization paths now consume effective race-basis helpers.
- `IsPresetModified()` race-basis comparisons now use effective basis/length (invalid effective basis no longer reports false clean match).

- Final cleanup tightened race-basis invalidation/notification paths so applied-preset removal while Preset owner is active now immediately invalidates outputs (`Select a race preset`) and prevents stale strategy display.

## 2026-05-16 — Strategy PreRace default mode changed to Single Stop
- Classification: **both** (default workflow behavior update + docs alignment).
- Updated Strategy default PreRace mode from `Multi Stop` to `Single Stop` for new/default planner state and new preset template creation.
- Preserved persistence/backward compatibility semantics:
  - existing saved profile/preset `PreRaceMode` values are unchanged,
  - legacy persisted `Auto` value (`3`) normalization behavior remains unchanged (`3 -> Multi Stop`).
- No planner math/race-basis/live-detect/runtime fuel/dash export behavior changes.
- PR #726 follow-up fix: `LoadProfileData()` car-change reset path now preserves the already loaded persisted `PreRaceMode` instead of forcing the reset default; Single Stop default remains active for cold-start/default state.
- 2026-05-16 Race class denominator authority fix landed.
  - `Race.PlayerClassFieldSize` now resolves from a canonical current-session denominator path and no longer uses League CSV registered membership fallback (`CountValidCsvDriversInClass`) through `LeagueClass.Player.DriverCount`.
  - League-enabled class denominator remains current-session effective cohort first; if unresolved in-session, fallback is native/session telemetry class denominator (not CSV membership count).
  - `RaceFinish.PlayerClassFieldSize` class-snapshot freeze now uses the same canonical denominator helper and allows bounded pending refresh only when the initial frozen value is invalid (`0`) and player snapshot is still pending; finish timing/triggers unchanged.
- 2026-05-17: Tyre change time learning + lock model landed (refuel-lock analogue).
  - Added car-level profile lock field `TireChangeTimeLocked` with backward-compatible default `false` on existing profiles.
  - Added Profiles CAR-tab tyre timing lock UI beside tyre-change time edit control.
  - Added bounded runtime tyre-time learner (`[LalaPlugin:Tyre Learn]`) using individual per-wheel tyre flags only (`dpLFTireChange/dpRFTireChange/dpLRTireChange/dpRRTireChange`), with conservative state machine:
    - arm on all-four selected in pit lane,
    - confirm start in valid in-box service context,
    - complete on all-four clear,
    - reject out-of-bounds/partial/gap/ambiguous candidates.
  - Added lock-aware save seam `SaveTireChangeTimeToActiveProfile(...)`:
    - unlocked save allowed,
    - locked+usable stored value blocks overwrite,
    - locked+missing/unusable stored value allows one-time first fill (matching refuel model philosophy).
  - Strategy/pit timing math ownership unchanged; existing `TireChangeTime` consumption path retained.

## 2026-05-17 — Strategy planner microcopy/docs alignment pass
- Classification: **both** (driver-facing wording clarity + docs contract alignment; no behavior/logic changes).
- Updated Strategy UI tooltips for `Refresh Calcs`, Race Basis owner radios, Live Detect owner semantics, and preset reapply (`↻`) to match active owner/effective-basis behavior.
- Aligned Strategy docs + tooltip inventory wording with current semantics: recompute-only Refresh Calcs, explicit Race Basis ownership, non-destructive Live Detect, and modified-badge divergence meaning.
- 2026-05-18: Property Snapshot PER LAP wiring compile-fix validated (PR #736 follow-up).
  - removed out-of-scope `MaybeWritePropertySnapshot(sessionTimeSec, lapCrossed, completedLaps)` call from `DataUpdate(...)`; manual marker + FREQUENCY automation remain owned there.
  - added narrow `MaybeWritePropertySnapshotPerLap(sessionTimeSec, lapCrossed, completedLaps)` seam called from `UpdateLiveFuelCalcs(...)` immediately after existing lap-cross detection/CompletedLaps context is available.
  - PER LAP automation remains rolling-only, de-duped by completed lap, and reuses existing `DetectLapCrossing(...)` seam (no second lap detector, no fuel/pit/projection logic changes).

## 2026-05-18 — Strategy Profile fuel preview live-refresh after telemetry learning
- Classification: **both** (Strategy tab Profile-mode UI refresh correctness + internal notification-path fix).
- Added a narrow telemetry-to-planner refresh seam: after profile fuel stats are persisted to active `TrackStats` in `LalaLaunch`, runtime now notifies `FuelCalcs` via `NotifyActiveTrackFuelProfileUpdated(...)`.
- `FuelCalcs` now refreshes Profile fuel preview labels (AVG/ECO/MAX), profile fuel-choice availability/button states, and source-dependent strategy recalc eligibility in-place for active track/condition without requiring mode toggle, track reselection, or manual Refresh Calcs.
- Auto-apply guard remains ownership-safe: Fuel-per-lap is auto-updated to profile AVG only when Strategy is in Profile mode and either profile-avg choice is active or profile source had no prior valid profile fuel for that condition.

## 2026-05-19 — PR #744 follow-up: Strategy Profile fuel auto-apply gate widening
- Classification: **both** (Strategy Profile-mode live-refresh ownership correction + user-visible textbox/source update correctness).
- Replaced the prior AVG-only live-refresh auto-apply gate with active-choice-aware profile basis refresh logic in `FuelCalcs.ApplyActiveTrackFuelProfileUpdated(...)`.
- Cold-start fix: when current condition previously had no valid profile fuel, first learned profile fuel now becomes active in Profile mode even if source text was not already Profile (manual entry still protected).
- Wet derived-fallback fix: while in wet Profile mode with no direct wet saved value, dry telemetry persistence is now treated as relevant and can refresh/re-apply the active wet profile basis derived from dry × wet factor.
- Active choice fix: live refresh now re-applies active Profile fuel basis by choice (`Profile avg`, `Profile eco`, `Profile max`) instead of forcing AVG when ECO/MAX is active.
- No DATA authority, Fuel.Refuel, Pit.FuelControl, pit command, or persistence-schema changes.

## 2026-05-19 — PR #744 follow-up #2: source-tracking + identity guard hardening
- Classification: **both** (Strategy live-refresh ownership correctness + cross-track/profile safety hardening).
- `NotifyActiveTrackFuelProfileUpdated(...)` now carries persisted identity (`CarProfile`, `TrackStats`) and `FuelCalcs` now returns early unless the currently selected Strategy profile+track row matches the persisted row by reference.
- Auto-applied Profile fuel refresh now uses a non-manual-safe helper (`ApplyProfileFuelBasis(...)`) that runs under `ApplySourceUpdate(...)`, syncs textbox text, and explicitly keeps `IsFuelPerLapManual=false` while setting `FuelPerLapSourceInfo` to Profile avg/eco/max.
- Preserved behavior boundaries: manual user edits still own manual mode; Live Snapshot unchanged; no DATA authority/Fuel.Refuel/Pit.FuelControl/pit-command/schema changes.

## 2026-05-19 — PR #744 follow-up #3: Profile button source-safe manual-state fix
- Classification: **both** (Strategy Profile button ownership correctness + telemetry-refresh continuity).
- Updated `UseProfileFuelPerLap`, `UseProfileFuelSave`, and `UseProfileFuelMax` to route through `ApplyProfileFuelBasis(...)` instead of direct `FuelPerLap` assignment.
- Profile button selections now preserve non-manual ownership (`IsFuelPerLapManual=false`) while keeping Profile avg/eco/max source labels, so later telemetry profile refresh can continue re-applying the active Profile basis.
- Manual typed textbox entry semantics remain unchanged (manual edits still own `IsFuelPerLapManual=true` and block auto-refresh as intended).
- No DATA authority/Fuel.Refuel/Pit.FuelControl/pit-command/persistence-schema changes.

## 2026-05-19 — PR #744 follow-up #4: wet derived-choice relevance + precision-safe profile text sync
- Classification: **both** (Strategy Profile wet-fallback refresh correctness + numeric precision safety).
- Refined dry-persist relevance while wet Profile mode is active to be choice-specific:
  - AVG choice: dry persistence is relevant only when wet AVG is missing and dry AVG exists.
  - ECO choice: dry persistence is relevant only when wet ECO/min is missing and dry min exists.
  - MAX choice: dry persistence is relevant only when wet MAX is missing and dry max exists.
- Updated `ApplyProfileFuelBasis(...)` to preserve exact numeric `FuelPerLap` by setting rounded display text via backing field + `PropertyChanged` (no `FuelPerLapText` setter parse-back), while keeping `IsFuelPerLapManual=false` and Profile source labels.
- Manual textbox typing semantics unchanged; DATA/Fuel.Refuel/Pit.FuelControl/pit-command/schema unchanged.

## 2026-05-19 — Strategy planner SimHub fallback cleanup + DATA LIVE lap SIM parity
- Classification: **both** (Strategy Profile-mode source/fallback correctness + runtime DATA LIVE lap-source fallback parity).
- `FuelCalcs.LoadProfileData()` now applies truthful Profile-mode fallback chains instead of stale/manual-looking carryover labels on no-profile tracks:
  - lap: `PROFILE -> SIMHUB EST (DriverCarEstLapTime) -> DEFAULT`,
  - fuel: `PROFILE -> SIMHUB COMPUTED (Fuel_LitersPerLap) -> DEFAULT`.
- Strategy helper/source labels now match actual provenance on load: `Profile avg (...)`, `SimHub est`, `SimHub`, or `Default`; no-profile default fuel no longer reports `profile`.
- Removed misleading no-profile lap fallback label `Manual (user entry)` during auto-load path; `Manual` remains user-entry-only semantics.
- `LalaLaunch.ResolveDataGovernedBurnAndPaceBasis(...)` SIM lap candidate for DATA LIVE now reads `DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverCarEstLapTime` (instead of last-lap time), enabling `LAP SIM` startup parity with existing burn SIM fallback when available.
- DATA SAVED behavior unchanged (`PLAN -> PROFILE -> DEFAULT`), and runtime burn authority/formulas/pit-control contracts unchanged.

## 2026-05-19 — PR #748 follow-up: Strategy fuel fallback branch fix + live-context SimHub guard
- Classification: **both** (Strategy planner fallback correctness + stale-cache prevention guard).
- Fixed `FuelCalcs.LoadProfileData()` fuel fallback placement so fallback chain now runs even when `AvgFuelPerLapDry` is missing/<=0 (no-profile target case).
- Profile-mode fuel load now resolves in one unconditional chain after profile checks: `PROFILE -> SIMHUB -> DEFAULT`.
- Added active live-context guard for planner SimHub fallback reads (`GetSimHubEstimatedLapTime`, `GetSimHubComputedFuelPerLap`): SimHub fallback now requires `IsLiveSessionActive` and planner selected car/track identity match to current live session identity.
- When disconnected/no-live/unmatched planner selection, SimHub fallbacks are blocked and planner falls through to `Default`, preventing stale cached DataCore values from being consumed.
- DATA SAVED authority, runtime burn/refuel formulas, Pit Fuel Control, pit commands, manual-entry semantics, and persistence schema unchanged.

## 2026-05-19 — PR #748 follow-up #2: first live-load SimHub guard window + wet-basis preservation
- Classification: **both** (Strategy planner startup fallback correctness + wet-profile stability fix).
- Updated planner SimHub fallback guard to accept a scoped pending live-session identity window during `ApplyLiveSession(...)` before `IsLiveSessionActive` flips, so first live no-profile Strategy load can use SimHub lap/fuel fallback immediately when selected planner identity matches incoming live identity.
- Guard still blocks SimHub fallback when no active/pending live context match exists (disconnected/unmatched planner selection), preventing stale cached DataCore fallback usage.
- Preserved wet fuel basis when dry avg is missing: `LoadProfileData()` no longer blindly zeroes `_baseDryFuelPerLap` in that case; when wet avg exists it now seeds a non-zero synthetic dry basis from wet/factor to prevent later `ApplyWetFactor`/condition-refresh paths from collapsing wet fuel to zero.
- DATA SAVED authority, Fuel.Refuel math, Pit.FuelControl, pit commands, manual entry semantics, and profile schema unchanged.

## 2026-05-28 — Fuel Revamp Phase 3C Export Rationalisation design pass (analysis-only)
- Classification: **internal-only** (analysis/docs only; no runtime or dashboard package edits).
- Completed Phase 3C export-consumer mapping for temporal duplicate families across fuel/pit/strategy/projection seams.
- Confirmed core guard remains enforced: no export removal/rename is allowed until both are mapped per export family:
  - dashboard JSON/package consumer usage,
  - internal C# consumer usage.
- Current repo dashboard-package status for this branch: no dashboard JSON / Dash Studio package files are present in-repo to edit; dashboard contract mapping is therefore based on canonical dash integration/parameter docs and exported attach surface inventory.
- Runtime boundaries preserved: no C# runtime math changes, no export attach changes, no compatibility cleanup/refactor, and no Property Snapshot grouping change required in this analysis pass.

## 2026-05-28 — Fuel Revamp Phase 3E Dashboard Migration Design pass (no export removal)
- Classification: **internal-only** (design/planning documentation only).
- Reconfirmed strict migration rule: no export removal/rename until both mappings are complete and dashboard usage is migrated + validated.
- Added Phase 3E dashboard migration design matrix and checklist to `Docs/Subsystems/Dash_Integration.md`.
- Current workspace note: repository does not currently contain dashboard `.djson` files under `Docs/Dash Files/`; exact widget/formula replacement edits remain deferred to implementation phase with dashboard package files present.
- Runtime boundaries preserved: no C# changes, no runtime formula changes, no export registration changes, no dashboard JSON edits.


## 2026-05-28 — Strategy Live Detect default + non-destructive preset apply
- Classification: **both** (Strategy startup/default UX + Live Detect/preset ownership correctness + docs alignment).
- Strategy startup now prefers Live Detect race-basis owner when live session context is active only while the owner is untouched; explicit Preset/Lap/Time selections and manual Race Minutes/Laps edits are preserved.
- Race Preset selector/reapply controls are visible only in Preset or Live Detect owner modes (not manual Lap/Time), and remain available in Live Detect.
- Selecting/reapplying/updating applied presets while Live Detect is selected now applies setup values without stealing race-basis ownership away from Live Detect.
- Refresh Calcs semantics unchanged (recompute-only); no runtime fuel model/pit command/telemetry ownership changes.
