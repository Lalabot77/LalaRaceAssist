# Opponents subsystem

Validated against commit: HEAD
Last updated: 2026-04-08
Branch: work

Purpose: own all opponent-facing calculations for strict same-class race-target selection, lap-time enrichment, and race-scoped pit-exit forecasting with SimHub exports under `Opp.*` and `PitExit.*`.

Phase-1 Head-to-Head (`H2HRace.*`) consumes Opponents only as a **race-target selector seam** (`Opp.Ahead1` / `Opp.Behind1`). H2H timing/state ownership remains in H2H/CarSA.

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
- Partial identity is rejected. If class color or car number is missing, identity is invalid and the row is not used for published Opp targets.

## Same-class ordering model
- Primary: use native `CarIdxClassPosition` when sufficient same-class rows report valid positions.
- Guard: class-position ordering is enabled only when the player has a valid positive class position; otherwise Opponents falls back to lap-progress ordering to avoid warm-up misordering.
- Fallback: class-filtered order by `CarIdxLap` then `CarIdxLapDistPct` (descending), with guards:
  - skip cars without valid lap distance (`0..1`)
  - skip cars not in world (`CarIdxTrackSurface < 0`)
- Opponents owns this ordering; CarSA ownership is unchanged.

## Gap and fight model (native)
- `Opp.Ahead1.GapToPlayerSec` and `Opp.Behind1.GapToPlayerSec` now prefer a CarSA checkpoint-time seam (`TryGetCheckpointGapSec`) when available and sane.
- Fallback for those slots (and primary for Ahead2/Behind2) remains progress delta (`Lap + LapDistPct`) scaled by a pace reference.
- Pace reference prefers current player pace input from LalaLaunch, then player best/last lap fallback.
- `PaceDeltaSecPerLap` and `LapsToFight` behavior remains unchanged in shape (same thresholds and NaN invalid behavior).

## Lap-time enrichment
- Per-car best/last lap uses native `CarIdxBestLapTime` / `CarIdxLastLapTime`.
- Existing identity-keyed blended pace cache remains (`0.70*recent + 0.30*(best*1.01)`).

## Pit-exit prediction model (native)
- Uses same-class progress model only (no leaderboard-relative gap inputs).
- Predicts player post-stop progress from locked pit-entry progress + pit loss while pit trip is active.
- Compares predicted player progress against class rivals to derive:
  - `PitExit.PredictedPositionInClass`
  - `PitExit.CarsAheadAfterPitCount`
  - nearest ahead/behind identities and gaps.

## Invalid-state behavior and logging
- If native prerequisites are missing/incomplete (for example missing player row or invalid identity), Opponents publishes invalid/empty outputs and logs:
  - `[LalaPlugin:Opponents] Native data unavailable -> outputs invalid (<reason>).`
- Logging is cadence-limited / reason-change driven to avoid spam.

## Outputs
- `Opp.Ahead1/2.*`, `Opp.Behind1/2.*` (Name/CarNumber/ClassColor/GapToPlayerSec/BlendedPaceSec/PaceDeltaSecPerLap/LapsToFight).
- `Opp.Leader.BlendedPaceSec`, `Opp.P2.BlendedPaceSec`.
- `Opponents_SummaryAhead/Behind` and per-slot variants.
- `PitExit.Valid`, `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, `PitExit.Summary`, `PitExit.Ahead.*`, `PitExit.Behind.*`.

## Known limitations vs old Extra-Properties-backed behavior
- No direct leaderboard `RelativeGapToLeader` parity path.
- Gap/pit-exit values are now native progress/pace-derived approximations.
- When native identity/order prerequisites are absent, outputs intentionally remain invalid instead of silently falling back.
