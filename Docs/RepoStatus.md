# Repository status

Validated against commit: HEAD
Last updated: 2026-04-12
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Added plugin-owned pit-box exports for dash use: `Pit.Box.DistanceM` and `Pit.Box.TimeS`.
- Pit-box authority now comes from native session/player sources only (`DriverPitTrkPct`, player track percent, and session track length via existing PitEngine seams).
- `Pit.Box.*` fail-safe behavior is strict: publish `0` outside pit lane, and publish `0` when authority/speed inputs are invalid (no fallback to `IRacingExtraProperties`).

## Reviewed documentation set
### Changed in pit-box export sweep
- `PitEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Subsystems/Launch_Mode.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubLogMessages.md`

## Delivery status highlights
- Added a plugin-owned pit-box percent seam in `PitEngine` (`PlayerPitBoxTrackPct`) fed from native `DriverPitTrkPct` with normalized percent handling.
- Added plugin-owned dash exports `Pit.Box.DistanceM` and `Pit.Box.TimeS` in `LalaLaunch` using wrapped pct delta and session track length, mirroring existing pit-exit display math style.
- Kept strict safe defaults: all pit-box outputs return `0` outside pit lane or on invalid authority, and time returns `0` when speed is too low/invalid.

## Validation note
- Validation recorded against `HEAD` (`plugin-owned pit-box distance/time exports`).
