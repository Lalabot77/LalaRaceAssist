# Repository status

Validated against commit: HEAD
Last updated: 2026-04-18
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
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

## Reviewed documentation set
### Changed in LapRef rollover seam transient-zero follow-up
- `LapReferenceEngine.cs`
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
- Kept ownership boundaries intact: dashboards remain presentation/control surfaces while plugin-owned actions own pit command dispatch.
- Preserved focused helper ownership (`PitCommandEngine`) for transport/mapping/feedback/failure logic instead of widening central runtime loops.
- Added bounded observability and short-lived user feedback exports so command success/failure is visible on dash and in SimHub logs.

## Validation note
- Validation recorded against `HEAD` (`LapRef player sector display now persists through rollover while compare/cumulative eligibility is re-armed to current-lap-only truth at lap start`).
