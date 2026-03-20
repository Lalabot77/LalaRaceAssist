# Repository status

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Opponents.cs` now resolves `Opp.Ahead1/2.*` and `Opp.Behind1/2.*` strictly from same-class leaderboard neighbors around the player's `PositionInClass`, instead of publishing identity from nearby/proximity slots.
- `Opponents.cs` still ingests `iRacing_DriverAheadInClass_*` / `iRacing_DriverBehindInClass_*` for cache continuity and slot-rebind debug logging, but those feeds no longer drive the published race-target identity consumed by H2H.
- `Docs/Subsystems/Opponents.md`, `Docs/SimHubParameterInventory.md`, and `Docs/Project_Index.md` now document the standings-based selector truth source and the no-nearby-fallback rule for published race targets.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- Published Opp race targets now use leaderboard-authoritative same-class standings neighbors (`PositionInClass - 1/-2/+1/+2`) so multi-lap class gaps stay valid and isolated players no longer get substituted with nearer on-track cars.
- Selector ownership did not move: `H2HRace` still follows Opponents-resolved identity and `H2HTrack` still follows CarSA same-class ahead/behind slot selection.
- If the player's leaderboard row or a strict same-class neighbor cannot be resolved, Opponents now leaves that published target empty/invalid rather than falling back to nearby slot identity.
- Pace enrichment remains cache-based, using the existing entity cache/blended pace logic on top of leaderboard-authoritative identity and lap data.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
