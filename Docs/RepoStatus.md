# Repository status

Validated against commit: HEAD
Last updated: 2026-04-13
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Boxed refuel gauge seam now splits latch timing by meaning in `LalaLaunch.cs`: `Fuel.Pit.Box.WillAddLatched` latches immediately on boxed+refuel-selected state, while `Fuel.Pit.Box.EntryFuel` still waits for first refuel-flow signal.
- In-box refuel deselect now clears boxed refuel exports immediately (`Fuel.Pit.Box.EntryFuel`, `Fuel.Pit.Box.WillAddLatched`, `Fuel.Pit.AddedSoFar`, `Fuel.Pit.WillAddRemaining`) instead of waiting for full box-phase reset.
- Preserved runtime fuel-math semantics for `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`, and `Fuel.Delta.LitresWillAdd`.
- Added fixed boxed-service modeled overhead: `CalculatePitBoxModeledTargetSeconds()` now returns `max(fuelTime, tireTime) + 1.0s`, representing stationary box overhead only (boxing/settle/service slop), not lane travel.
- Kept downstream seams aligned via single upstream ownership: `Pit.Box.TargetSec`, `Pit.Box.RemainingSec`, and `Fuel.Live.TotalStopLoss` now inherit the same +1.0s boxed-service correction naturally through the shared modeled target seam.
- Follow-up fixed `Pit.Box.LastDeltaSec` stop-end sampling: stop delta now uses current pit stop elapsed authority at the active→inactive transition, with cached elapsed used only as an invalid-value fallback.
- Follow-up fixed PR 561 latch basis: settle-phase `Pit.Box.TargetSec` now freezes the effective target `max(modeledTargetSec, repairRemainingSec)` so repairs seen before latch are included in the frozen stop target.
- `Pit.Box.TargetSec` now latches/freeze after a brief in-box settle period (1.0s elapsed) so late stop-model drift does not move the active countdown.
- `Pit.Box.RemainingSec` now counts down from that latched target while preserving repair-left authority behavior (`max(modeledRemaining, repairRemaining)`).
- Added `Pit.Box.LastDeltaSec` with post-stop semantics: computed at stop end as `(latched target - final elapsed)`, positive=quicker, negative=slower, visible for 5 seconds, then reset to `0`.
- Added reset hygiene for pit-box countdown internals and post-stop delta visibility so stale values do not leak into subsequent stops.
- Updated canonical subsystem/export docs and internal development changelog to match the new pit-box behavior contract.

## Reviewed documentation set
### Changed in player track-pct + pit-exit blend task
- `LalaLaunch.cs`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/SimHubLogMessages.md`
- `README.md`
- `CHANGELOG.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`

## Delivery status highlights
- Kept ownership boundaries intact: Opponents remains race-scoped pit-exit prediction owner; CarSA ownership was not widened.
- Added plugin-owned `Car.Player.TrackPct` under the existing `Car.Player.*` family with strict normalization/sanitization behavior.
- Added plugin-owned `PitExit.TimeToExitSec` without changing Opponents predictor internals or removing legacy pit-exit exports.
- No new log lines were added; `Docs/Internal/SimHubLogMessages.md` remained unchanged.

## Validation note
- Validation recorded against `HEAD` (`PitExit.TimeToExitSec zero-countdown availability fix`).
