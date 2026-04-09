# Opponents subsystem

Validated against commit: HEAD
Last updated: 2026-04-09
Branch: work

Purpose: own all opponent-facing calculations for strict same-class race-target selection, lap-time enrichment, and race-scoped pit-exit forecasting with SimHub exports under `Opp.*` and `PitExit.*`.

Phase-1 Head-to-Head (`H2HRace.*`) consumes Opponents only as a **race-target selector seam** (`Opp.Ahead1` / `Opp.Behind1`) and as the effective-class-position context seam for published `PositionInClass` (target + player rows). H2H timing/state ownership remains in H2H/CarSA.

## Gating and scope
- Runs in live opponent sessions (Practice, Qualifying/Open Qualify, Lone Qualify, Race).
- No completed-lap gate.
- Pit-exit prediction remains race-scoped and resets on Race → non-Race transition.
- Opponents is now **native-only**: no runtime dependency on `IRacingExtraProperties` player/leaderboard/ahead-behind feeds.

## Native data inputs
Opponents now reads from:
- `DataCorePlugin.GameRawData.Telemetry.PlayerCarIdx`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxLap`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxLapDistPct`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxClassPosition`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxBestLapTime`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxLastLapTime`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxOnPitRoad`
- `DataCorePlugin.GameRawData.Telemetry.CarIdxTrackSurface`
- `DataCorePlugin.GameRawData.SessionData.DriverInfo.Drivers##.*`
- `DataCorePlugin.GameRawData.SessionData.DriverInfo.CompetingDrivers[*].*`

## Identity model
- Canonical identity key: `ClassColor:CarNumber`.
- `ClassColor` is normalized to canonical `0xRRGGBB` with signed/integer masking (`& 0xFFFFFF`).
- Plain numeric `CarClassColor` text is treated as decimal by default; explicit `0x`/`#` prefixes force hex parsing.
- Partial identity is rejected. If class color or car number is missing, identity is invalid and the row is not used for published Opp targets.

## Same-class ordering model
- Live same-class target selection is **RaceProgress-first**: class-filtered order by `CarIdxLap` then `CarIdxLapDistPct` (descending), with guards:
  - skip cars without valid lap distance (`0..1`)
  - skip cars not in world (`CarIdxTrackSurface < 0`)
- Native `CarIdxClassPosition` remains available as an **official/anchor input** only; published live race-context `PositionInClass` uses the effective RaceProgress-first order so official timing lag does not block immediate context updates.
- Opponents owns this ordering; CarSA ownership is unchanged.

## Gap and fight model (native)
- Gap export semantics are explicitly split:
  - `Gap.TrackSec` = progress/pace track-gap estimate from native RaceProgress delta.
  - `Gap.RelativeSec` = preferred relative gap (CarSA checkpoint seam for slot 1 per side when valid; otherwise fallback to `Gap.TrackSec`).
  - `GapToPlayerSec` = legacy-compatible mirror of the preferred relative gap value.
- `Opp.Ahead1.Gap.RelativeSec` and `Opp.Behind1.Gap.RelativeSec` can use the CarSA checkpoint-time seam (`TryGetCheckpointGapSec`) when available and sane.
- Fallback for those slots (and primary for Ahead2+ / Behind2+) remains progress delta (`Lap + LapDistPct`) scaled by a pace reference.
- Pace reference prefers current player pace input from LalaLaunch, then player best/last lap fallback.
- `PaceDeltaSecPerLap` and `LapsToFight` behavior remains unchanged in shape (same thresholds and NaN invalid behavior).

## Lap-time enrichment
- Per-car best/last lap uses native `CarIdxBestLapTime` / `CarIdxLastLapTime`.
- Existing identity-keyed blended pace cache remains (`0.70*recent + 0.30*(best*1.01)`).

## Pit-exit prediction model (native)
- Uses same-class progress model only (no leaderboard-relative gap inputs).
- Predicts player post-stop progress from locked pit-entry progress + pit loss while pit trip is active.
- Nearest ahead/behind pit-exit gap seconds now use the same Opponents-owned pace reference seam used by native race-model gap conversion (runtime player pace input when valid, then native player best/last fallback, then 120s guard fallback), rather than a separate local fallback chain.
- Compares predicted player progress against the full same-class rival set to derive:
  - `PitExit.PredictedPositionInClass`
  - `PitExit.CarsAheadAfterPitCount`
  - nearest ahead/behind identities and gaps.
- Refresh cadence:
  - on pit road or active pit trip: per Opponents update tick (responsive path)
  - off pit road and no active pit trip: bounded time-based refresh interval (currently 1.0s minimum spacing) instead of lap-quarter gating.
- Final-120s suppression behavior remains unchanged.

## Invalid-state behavior and logging
- If native prerequisites are missing/incomplete (for example missing player row or invalid identity), Opponents publishes invalid/empty outputs and logs:
  - `[LalaPlugin:Opponents] Native data unavailable -> outputs invalid (<reason>).`
- Logging is cadence-limited / reason-change driven to avoid spam.

## Outputs
- `Opp.Ahead1..5.*`, `Opp.Behind1..5.*` under the existing flat `Opp.*` namespace.
  - Compatibility: existing `Opp.Ahead1/2.*` and `Opp.Behind1/2.*` names remain unchanged.
  - Minimum per-slot shape includes:
    - identity/cosmetic: `CarIdx`, `Name`, `AbbrevName`, `CarNumber`, `ClassName`, `ClassColor`, `ClassColorHex`
    - validity/position/pit: `IsValid`, `IsOnTrack`, `IsOnPitRoad`, `PositionInClass` (effective/live race order)
    - lap context: `LastLap`, `LastLapTimeSec`, `BestLap`, `BestLapTimeSec`, `LapsSincePit`
    - race gaps/fight: `Gap.RelativeSec`, `Gap.TrackSec`, `PaceDeltaSecPerLap`, `LapsToFight`
    - optional metadata: `IRating`, `SafetyRating`, `Licence`, `LicLevel`, `UserID`, `TeamID`, `IsFriend`, `IsTeammate`, `IsBad`
  - Legacy continuity fields still published: `GapToPlayerSec`, `BlendedPaceSec`.
- `Opp.Leader.BlendedPaceSec`, `Opp.P2.BlendedPaceSec`.
- `Opponents_SummaryAhead/Behind` and per-slot variants (`Ahead1..5`, `Behind1..5`).
  - Top-level `Opponents_SummaryAhead/Behind` remains short/readable (first two slots emphasis) for dash compatibility.
- `PitExit.Valid`, `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, `PitExit.Summary`, `PitExit.Ahead.*`, `PitExit.Behind.*`.

## Known limitations vs old Extra-Properties-backed behavior
- No direct leaderboard `RelativeGapToLeader` parity path.
- Gap/pit-exit values are now native progress/pace-derived approximations.
- When native identity/order prerequisites are absent, outputs intentionally remain invalid instead of silently falling back.
