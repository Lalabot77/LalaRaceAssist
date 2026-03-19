# Repository status

Validated against commit: HEAD
Last updated: 2026-03-19
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/H2H.md` updated for the scoped H2H correctness follow-up: lap-wrap segment carryover, canonical H2H class-color formatting, and the bounded race `CarIdx` fallback path.
- `Docs/Subsystems/CarSA.md` and `Docs/Subsystems/Opponents.md` updated so their selector-only wording still matches final H2H ownership after the race-resolution hardening.
- `Docs/SimHubParameterInventory.md` updated so the published H2H export contract now describes canonical `ClassColor` formatting and lap-wrap segment latching accurately.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- `H2HRace.*` and `H2HTrack.*` now keep previously published segment states/deltas latched across lap crossing until the new lap rebuilds each segment, instead of pushing immediate empty/zero outputs at start/finish.
- H2H `ClassColor` outputs now publish one canonical uppercase hex format: `#RRGGBB`.
- `H2HRace.*` still uses Opponents as the selector source, but when direct identity-table lookup misses it can now use a narrow local CarSA-slot fallback to recover the live target `CarIdx` without moving ownership into CarSA or Opponents.
- `H2HTrack.*` remains the more volatile family because local CarSA slots can still rebind as nearby cars change.
- No plugin UI/settings were added, no H2H export names changed, no debug CSV logging was introduced, and CarSA/Opponents ownership boundaries were preserved.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
