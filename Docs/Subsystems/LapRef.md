# LapRef (Offline Reference Lap Comparison)

Validated against commit: HEAD
Last updated: 2026-04-18
Branch: work

## Purpose
LapRef is a standalone dash-facing comparison subsystem for **player-only reference laps**.

It compares:
- player live current-lap progress context
- session best validated lap (this session)
- profile all-time best lap (active car/track/condition)

LapRef mirrors H2H's fixed 6-sector presentation concept but is fully separate from `H2HRace.*` / `H2HTrack.*` contracts.

## Ownership and boundaries
- **LapRef owns** validated-lap snapshot state, session-best state, profile-best materialization, and `LapRef.*` exports.
- **LapRef does not own** sector derivation. It consumes CarSA fixed-sector cache read-only via `TryGetFixedSectorCacheSnapshot`.
- **LapRef does not own** wet/dry detection. It uses runtime `_isWetMode` routing.
- **LapRef does not own** lap validation rules. It captures only from the existing validated-lap gate in `UpdateLiveFuelCalcs`.
- **LapRef lap-time authority** is aligned to the same player telemetry seam used by H2H/core (`CarIdxLastLapTime` for player row/session-best capture path, with guarded fallback only when that seam is unavailable).
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

LapRef also maintains a separate **live current-lap comparison view** for the player side:
- active segment comes from live player lap pct mapping
- completed sectors for the current lap are read from CarSA fixed-sector cache
- only completed sectors behind the current segment are treated as comparable
- no partial current-sector elapsed values are synthesized
- completed sector boxes persist across lap rollover and are overwritten progressively as new-lap sectors complete (H2H-like presentation continuity)
- compare/cumulative truth uses current-lap re-armed sector eligibility so prior-lap carried visual boxes do not contribute after rollover

## Capture and update flow
1. Existing validated-lap gate accepts a lap in `UpdateLiveFuelCalcs`.
2. LapRef resolves authoritative validated lap time from player `CarIdxLastLapTime` (fallback to the gate candidate only when needed), then captures snapshot from the player's CarSA fixed-sector cache (if available).
3. Snapshot becomes player reference row and competes for in-memory session best.
4. Existing profile PB seam persists lap-time PB; sector fields are persisted condition-wise when available.
5. Each tick, LapRef rematerializes profile-best from active profile + track + wet/dry condition.
6. Each tick, LapRef also materializes the player **live current-lap comparison sectors** from the current CarSA cache snapshot + current active segment and publishes `LapRef.*`.
   - Player-row sector display continuity follows H2H-like cache behavior: any valid CarSA sector value can refresh the corresponding displayed player box immediately, while unchanged boxes naturally persist until replaced.
   - This live row is not hard-cleared every tick; it preserves prior completed sectors at lap start and replaces each slot only when the corresponding new-lap sector is actually completed.
   - Live compare/cumulative evaluation uses a separate current-lap comparable snapshot that clears on lap rollover while player row display continuity remains intact.

PB safety note:
- Profile PB remains telemetry-owned (validated-lap seam); Profiles UI no longer permits manual PB entry.
- This preserves consistency between persisted PB lap times and optional persisted PB sector values.

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

At new-lap start, comparison validity is re-armed from empty current-lap eligibility:
- prior-lap carried display sectors do not count for compare validity
- compare rows reactivate only as current-lap sectors complete

## Export family
Family-level:
- `LapRef.Valid`
- `LapRef.Mode`
- `LapRef.PlayerCarIdx`
- `LapRef.ActiveSegment`
- `LapRef.DeltaToSessionBestSec`
- `LapRef.DeltaToSessionBestValid`
- `LapRef.DeltaToProfileBestSec`
- `LapRef.DeltaToProfileBestValid`

Side rows:
- `LapRef.Player.*`
- `LapRef.SessionBest.*`
- `LapRef.ProfileBest.*`

Comparison rows:
- `LapRef.Compare.SessionBest.*`
- `LapRef.Compare.ProfileBest.*`

Each side exposes:
- `LapRef.Player.*`: `Valid`, `LapTimeSec`, `ActiveSegment`, `S1..S6State`, `S1..S6Sec`
- `LapRef.SessionBest.*`: `Valid`, `LapTimeSec`, `S1..S6State`, `S1..S6Sec`
- `LapRef.ProfileBest.*`: `Valid`, `LapTimeSec`, `S1..S6State`, `S1..S6Sec`

Each comparison exposes:
- `S1..S6State`, `S1..S6DeltaSec`

## Active segment contract
- `LapRef.ActiveSegment` is always the **live player segment context** (`1..6`, else `0`).
- `LapRef.Player.ActiveSegment` mirrors that same live segment.
- `LapRef.SessionBest.ActiveSegment` / `LapRef.ProfileBest.ActiveSegment` are intentionally not exported; static references do not own live progression state.

## Cumulative delta contract
- `LapRef.DeltaToSessionBestSec` and `LapRef.DeltaToProfileBestSec` are cumulative deltas versus the selected reference using **completed current-lap sectors only**.
- Cumulative rule: include only sectors that are completed on the live current lap (`S1..S(active-1)`), and only when **both player live sector + reference sector are valid**.
- Delta formula: `playerSum - referenceSum`.
- Current in-progress sector is never treated as complete.
- Future sectors are never included.
- If there are no valid compared sector pairs, cumulative delta publishes `0`.
- Valid guards:
  - `LapRef.DeltaToSessionBestValid`
  - `LapRef.DeltaToProfileBestValid`
  - each is true only when at least one real compared sector pair was included.
- Lap rollover rule:
  - at normal start/finish rollover, cumulative valid flags re-arm to `false` and cumulative values publish `0`
  - rollover detection also treats boundary transition samples (`previous > 1 && current == 0`) as lap-start re-arm events to handle transient lap-pct seam zeros (e.g., `6 -> 0 -> 1`)
  - cumulative values become valid again only after at least one current-lap completed sector contributes a real player/reference pair

## Reset behavior
LapRef session-best/player snapshot state resets when:
- session token changes
- session type changes
- car model changes
- track key changes
- wet/dry mode flips
- explicit engine reset

Lap rollover handling is intentionally separate from reset:
- normal lap boundary progression does **not** clear visible completed sector boxes
- completed sectors persist through the new-lap start until replaced by newly completed sectors for that lap

Profile-best is rematerialized from profile data after reset.
Family-level reset values:
- `LapRef.ActiveSegment = 0`
- `LapRef.Player.ActiveSegment = 0`
- `LapRef.DeltaToSessionBestSec = 0`
- `LapRef.DeltaToSessionBestValid = false`
- `LapRef.DeltaToProfileBestSec = 0`
- `LapRef.DeltaToProfileBestValid = false`
