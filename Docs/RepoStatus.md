# Repository status

Validated against commit: HEAD
Last updated: 2026-03-19
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/H2H.md` updated for this scoped H2H follow-up so the canonical subsystem doc matches the bounded race `CarIdx` fallback and the corrected lap-wrap segment latching rule.
- `Docs/SimHubParameterInventory.md` updated so the published H2H export contract describes the bounded race `CarIdx` recovery path and the per-segment lap-wrap latch semantics accurately.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- `H2HRace.*` still uses Opponents as the selector source and still prefers exact `ClassColor:CarNumber` resolution first, but it can now recover a live nearby `CarIdx` from current CarSA slots via a bounded car-number/name fallback when class-color identity differs between selector sources.
- `H2HRace.*` identity/cosmetic fields can therefore stay populated while the live target `CarIdx` is recovered more robustly, without moving selector ownership into Opponents or CarSA.
- `H2HRace.*` and `H2HTrack.*` now keep previously published valid segment states/deltas latched across lap wrap until the new lap replaces that same segment, instead of downgrading published values back to `pending/0` as one side rolls first over start-finish.
- No plugin UI/settings were added, no H2H export names changed, no debug CSV logging was introduced, and CarSA/Opponents ownership boundaries were preserved.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
