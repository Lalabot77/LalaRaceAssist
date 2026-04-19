# Repository status

Validated against commit: HEAD
Last updated: 2026-04-19
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Pit Fuel Control control-model follow-up corrected action semantics:
  - `Pit.FuelSetMax` is now a real MAX/ZERO behavioral toggle on transport (`MAX -> ZERO -> MAX -> ZERO`), while `Pit.Command.FuelSetMaxToggleState` still flips on every press.
  - `ModeCycle` now forces `Source=STBY` on `AUTO -> MAN`.
  - `ModeCycle` no longer hard-blocks `MAN -> AUTO` when `Source=PLAN`; it allows AUTO and immediately forces `Source=STBY` so the driver must reselect a live source.
  - `ModeCycle` selection feedback remains plugin-owned and explicit (`FUEL SRC STBY` when forced-STBY guardrail applies).
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

## Delivery status highlights
- Kept changes bounded to existing runtime seams and exports: corrected projection behavior at the shared timed-race authority seam rather than introducing parallel dash property families.
- Aligned Pit Fuel Control source targets with non-clamped requirements for live modes and retained planner-owned PLAN validity/authority.
- Kept ownership boundaries intact: dashboards remain presentation/control surfaces while plugin-owned actions own pit command dispatch.
- Preserved focused helper ownership (`PitCommandEngine`) for transport/mapping/feedback/failure logic instead of widening central runtime loops.
- Added bounded observability and short-lived user feedback exports so command success/failure is visible on dash and in SimHub logs.
- Extended focused-helper ownership with `PitFuelControlEngine` for pit fuel source/mode state and decision logic while keeping `LalaLaunch` as action/export wiring.
- Kept scope bounded to pit action surface/feedback seam extensions (no dashboard JSON/UI expansion): direct source-select + zero-fuel actions, selection-only source/mode feedback publication, and max-fuel toggle-state export.

## Validation note
- Validation recorded against `HEAD` (`LapRef authoritative capture now uses CarIdx last-lap seam with rollover freshness guard vs validated-gate candidate, keeping player/session-best/PB handoff on the just-finished lap when CarIdx lags by one tick`).
