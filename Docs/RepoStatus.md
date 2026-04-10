# Repository status

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Opponents Pit Exit now uses a two-phase model: unchanged pre-pit behavior off pit road, plus active pit-cycle remaining-time prediction while on pit road / active pit trip.
- Active pit-cycle prediction now seeds same-class rival pit-road baseline state at cycle start before transition detection, then tracks post-start pit-road entries and excludes rivals behind who pit after our cycle starts from normal on-track pass-before-exit threat treatment while they remain on pit road.
- Preserved subsystem ownership boundaries (Opponents race-order owner and Pit Exit owner, CarSA spatial/timing owner, H2H consumer/publisher).

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/H2H.md`

## Delivery status highlights
- Pit Exit pre-pit behavior remains stable while active pit-cycle prediction now uses remaining countdown time instead of repeatedly applying full fresh pit loss mid-stop.
- Same-class rivals already on pit road at our active-cycle start are no longer misclassified as post-start entrants due to initial empty-state; only true later entrants are classified as post-start pit entries.
- Pit Exit remains Opponents-owned, race-scoped, and full same-class-field comparison based; late-race last-120s suppression remains unchanged.

## Validation note
- Validation recorded against `HEAD` (`Opponents Pit Exit v2 follow-up: seeded rival pit-road baseline fix with docs sync`).
