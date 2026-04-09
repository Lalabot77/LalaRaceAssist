# LapRef (Offline Reference Lap Comparison)

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

## Purpose
LapRef is a standalone dash-facing comparison subsystem for **player-only reference laps**.

It compares:
- player last validated lap context
- session best validated lap (this session)
- profile all-time best lap (active car/track/condition)

LapRef mirrors H2H's fixed 6-sector presentation concept but is fully separate from `H2HRace.*` / `H2HTrack.*` contracts.

## Ownership and boundaries
- **LapRef owns** validated-lap snapshot state, session-best state, profile-best materialization, and `LapRef.*` exports.
- **LapRef does not own** sector derivation. It consumes CarSA fixed-sector cache read-only via `TryGetFixedSectorCacheSnapshot`.
- **LapRef does not own** wet/dry detection. It uses runtime `_isWetMode` routing.
- **LapRef does not own** lap validation rules. It captures only from the existing validated-lap gate in `UpdateLiveFuelCalcs`.
- **LapRef does not modify** H2H behavior, Opponents behavior, or CarSA derivation rules.

## Snapshot model
A captured validated lap snapshot carries:
- `LapTimeSec`
- fixed sectors `S1..S6` with has-value semantics
- `ActiveSegment`
- `LapRef`
- condition context (`IsWet`)
- combo/session identity context (`CarModel`, `TrackKey`, `SessionToken`)

Missing sectors remain empty. Lap time can still be valid without sector data.

## Capture and update flow
1. Existing validated-lap gate accepts a lap in `UpdateLiveFuelCalcs`.
2. LapRef captures snapshot from the player's CarSA fixed-sector cache (if available).
3. Snapshot becomes player reference row and competes for in-memory session best.
4. Existing profile PB seam persists lap-time PB; sector fields are persisted condition-wise when available.
5. Each tick, LapRef rematerializes profile-best from active profile + track + wet/dry condition and publishes `LapRef.*`.

## State semantics
Fixed-sector state follows H2H-like semantics:
- `0 = empty`
- `1 = pending`
- `2 = valid`

For side rows (`LapRef.Player.*`, `SessionBest.*`, `ProfileBest.*`):
- `valid` means real sector duration exists
- `empty` means sector is missing

For comparison rows (`LapRef.Compare.*`):
- `valid` when both player + reference sectors are real
- `pending` when exactly one side has real sector
- `empty` when neither side has real sector

## Export family
Family-level:
- `LapRef.Valid`
- `LapRef.Mode`
- `LapRef.PlayerCarIdx`
- `LapRef.ActiveSegment`

Side rows:
- `LapRef.Player.*`
- `LapRef.SessionBest.*`
- `LapRef.ProfileBest.*`

Comparison rows:
- `LapRef.Compare.SessionBest.*`
- `LapRef.Compare.ProfileBest.*`

Each side exposes:
- `Valid`, `LapTimeSec`, `ActiveSegment`
- `S1..S6State`, `S1..S6Sec`

Each comparison exposes:
- `S1..S6State`, `S1..S6DeltaSec`

## Reset behavior
LapRef session-best/player snapshot state resets when:
- session token changes
- session type changes
- car model changes
- track key changes
- wet/dry mode flips

Profile-best is rematerialized from profile data after reset.
