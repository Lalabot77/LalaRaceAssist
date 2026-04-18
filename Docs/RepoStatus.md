# Repository status

Validated against commit: HEAD
Last updated: 2026-04-18
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- PR 570 cleanup follow-up:
  - custom-message triggers now refresh `Pit.Command.LastAction` and `Pit.Command.LastRaw` with truthful custom action/text diagnostics,
  - parameter inventory now treats pit/custom settings fields as persisted UI/settings notes (not normal runtime export rows).
- Polished pit-command user feedback text:
  - `Clear All` feedback now publishes `Pit Clear All`.
  - fuel-add paths now publish `Fuel MAX` when add actions hit effective max/clamp (including +1/+10 clamp edge cases and `FuelSetMax`).
- Added Settings → `Pit Commands` expander with fixed built-in pit-action binding rows, driver-facing purpose text, focus reliability note, and preview-only `Auto-focus iRacing before pit/custom message send` setting surface.
- Added Settings → `Custom Messages` expander with ten user-editable custom slots (friendly label + message text) and per-slot binding rows.
- Added plugin-owned custom message actions `LalaLaunch.CustomMessage01..10` that dispatch via existing in-plugin direct chat injection and reuse short-lived `Pit.Command.*` feedback exports.
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
### Changed in LapRef active-segment + cumulative delta task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

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
- Validation recorded against `HEAD` (`LapRef comparison now uses live current-lap completed sectors, and redundant per-reference active-segment exports were pruned`).
