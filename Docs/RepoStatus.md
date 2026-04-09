# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Added a new Debug-only global setting `ShiftAssistMuteInReplay` (default `true`) to suppress Shift Assist audio specifically during replay sessions.
- Replay detection now uses `DataCorePlugin.GameData.ReplayMode` as the primary signal, with `DataCorePlugin.GameRawData.Telemetry.IsReplayPlaying` as an optional supporting signal.
- Replay mute is evaluated continuously at runtime (not latched); audio suppression clears automatically as soon as replay is no longer active.
- Shift Assist cue generation, shift-light latches/exports, learning, delay capture, and debug CSV flow remain unchanged.

## Reviewed documentation set
### Changed in this sweep
- `LalaLaunch.cs`
- `GlobalSettingsView.xaml`
- `Docs/Subsystems/Shift_Assist.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Code_Snapshot.md`

## Delivery status highlights
- Added `Mute Shift Assist sound in replay` toggle in Debug Options with explicit tooltip semantics.
- Shift Assist runtime audio path now applies a narrow replay-only gate at playback call sites.
- The replay-only gate does not alter Shift Assist engine cue timing or learning/target calculations.

## Validation note
- Validation recorded against `HEAD` (`Debug replay-only Shift Assist audio mute toggle + runtime gate + docs sync`).
