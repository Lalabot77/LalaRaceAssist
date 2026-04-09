# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Updated Opponents native ordering policy to use RaceProgress (`CarIdxLap + CarIdxLapDistPct`) as the primary same-class neighbor selector.
- Kept native `CarIdxClassPosition` as anchor/validation context only so delayed official position updates no longer block immediate target switching after live overtakes.
- Preserved existing Opponents output contracts (`Ahead1/2`, `Behind1/2`) and H2H race-selector integration seam.

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/H2H.md`
- `Docs/Internal/SimHubLogMessages.md`
- `LalaLaunch.cs`

## Delivery status highlights
- Fixed Opponents same-class ordering lag by making RaceProgress-first ordering authoritative for live neighbor selection.
- Retained class-position values for anchor/validation context, but removed them as the primary live ordering driver.
- Kept CarSA timing seam usage for Ahead1/Behind1 gap seconds and left H2H selector contracts unchanged.

## Validation note
- Validation recorded against `HEAD` (`Opponents RaceProgress-first ordering fix + docs sync`).
