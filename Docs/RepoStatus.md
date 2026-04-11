# Repository status

Validated against commit: HEAD
Last updated: 2026-04-11
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Completed a repo-wide runtime sweep removing remaining `IRacingExtraProperties` fallback reads from in-scope runtime code paths.
- H2H class-best is now native-only (`H2H*.ClassSessionBestLapSec` remains `0` when native class-best authority is unavailable) with a bounded warning log.
- Pit Entry Assist pit-speed authority is now native session-only; legacy pit-speed fallback is removed with a bounded warning log when authority is unavailable.
- Messaging legacy ExtraProperties traffic/signal paths are removed; unavailable signals/lanes now remain unavailable with bounded warnings instead of silent fallback.
- Hotfix follow-up applied for fallback-removal regressions:
  - MSGV1 removed-signal warning state now persists across `SignalProvider` re-instantiation, preventing per-tick warning spam.
  - Pace/projection no longer hardcodes `simLapsRemaining=0`; timed-race projection now keeps a native/runtime fallback seed (`DataCorePlugin.GameData.LapsRemaining` then last-known runtime values) without reintroducing ExtraProperties.
- Follow-up correction applied for stale session carry-over:
  - Projection fallback carry-state is now cleared on session/fuel-model resets and manual recovery, preventing prior-session laps-remaining leakage into a fresh session before native laps-remaining becomes available.
- Preserved subsystem ownership boundaries (Opponents remains native-only race-order owner, CarSA session-agnostic spatial owner, H2H consumer/publisher only).

## Reviewed documentation set
### Changed in this sweep
- `LalaLaunch.cs`
- `PitEngine.cs`
- `MessagingSystem.cs`
- `Messaging/SignalProvider.cs`
- `ProfilesManagerViewModel.cs`
- `Docs/Subsystems/H2H.md`
- `Docs/Subsystems/Pit_Entry_Assist.md`
- `Docs/Subsystems/Message_System_V1.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in follow-up hotfix
- `LalaLaunch.cs`
- `Messaging/SignalProvider.cs`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in stale-fallback reset follow-up
- `LalaLaunch.cs`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Subsystems/Opponents.md`
- `Docs/Subsystems/CarSA.md`

## Delivery status highlights
- No in-scope runtime `IRacingExtraProperties` reads remain in C# code; removed paths now either use existing native/plugin-owned sources or intentionally publish unavailable/invalid outputs.
- Opponents remains native-only and unchanged in ownership model.
- Warning logs are bounded (one-time or recovery-driven) for removed fallback authorities in H2H, Pit Entry Assist, MessagingSystem, and MSGV1 signal provider.

## Validation note
- Validation recorded against `HEAD` (`Hotfix follow-up for stale laps-remaining fallback leakage across session reset`).
