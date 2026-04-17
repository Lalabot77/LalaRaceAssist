# Repository status

Validated against commit: HEAD
Last updated: 2026-04-17
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Added plugin-owned pit command action endpoints for Strategy Dash / PitPopUp (`LalaLaunch.Pit.*`) and removed remaining dashboard dependency on `IRacingExtraProperties` pit action bindings.
- Added focused `PitCommandEngine` helper with explicit transport ownership and observability:
  - SDK-first mode is explicit but currently unavailable in this plugin reference set (one-time warning + fallback),
  - macro-hotkey fallback is the active transport path (`PitMacroKey*`, default `F13..F18`),
  - transport mode is surfaced as `Pit.CommandTransportMode`.
- Kept pit read-side authority unchanged: pit service status/selection remains telemetry-based (`dpFuelFill`, tyre selectors, `PlayerCarPitSvStatus` and related seams).
- Updated subsystem/user/internal docs and development changelog to match final action ownership and fallback setup contract.

## Reviewed documentation set
### Changed in plugin-owned pit command actions task
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
- `Docs/Subsystems/Fuel_Model.md`

## Delivery status highlights
- Kept ownership boundaries intact: dashboards remain presentation/control surfaces while plugin-owned actions now own pit command dispatch.
- Isolated pit command transport/mapping logic into `PitCommandEngine` rather than widening unrelated `LalaLaunch` runtime logic.
- Added explicit fallback visibility/logging so SDK-requested mode cannot fail silently.

## Validation note
- Validation recorded against `HEAD` (`Plugin-owned pit command actions added with explicit SDK-first contract and macro-hotkey fallback`).
