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

### LapRef timing-source alignment + PB trigger/rollover parity fix
- Aligned LapRef validated lap-time capture to the same authoritative player seam trusted by H2H/core (`CarIdxLastLapTime` for player `CarIdx`), with guarded fallback to the validated-gate candidate only when that array seam is unavailable.
- Updated the validated-lap → PB-trigger handoff so `_lastValidLapMs` now latches from that same authoritative captured lap value, eliminating first-beat misses caused by stale/non-authoritative lap-time capture at S/F.
- Kept PB persistence invariants unchanged:
  - PB lap-time update still occurs through existing `TryUpdatePBByCondition(...)` seam.
  - sector fields persist only when real fixed-sector values exist.
  - legacy lap-time-only PB rows (no sectors) remain valid and supported.
- Tightened LapRef player-row rollover feel toward proven H2H behavior by sourcing display refresh directly from live CarSA fixed-sector cache presence (while compare/cumulative truth remains current-lap re-armed only).
- Classification: **both** (runtime LapRef/PB correctness behavior + canonical LapRef/export docs update).

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
