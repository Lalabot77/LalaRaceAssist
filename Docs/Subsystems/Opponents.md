# Opponents subsystem

Validated against commit: HEAD
Last updated: 2026-03-24
Branch: work

Purpose: own all opponent-facing calculations for strict same-class race-target selection, nearby/leaderboard pace enrichment, and pit-exit position forecasting with SimHub exports under `Opp.*` and `PitExit.*`.
Phase-1 Head-to-Head (`H2HRace.*`) consumes Opponents only as a **race-target selector seam** (current class-ahead / class-behind identity from `Opp.Ahead1` / `Opp.Behind1`). Persistent H2H timing, live-gap, lap-summary, fixed-6-segment ownership, and canonical H2H class-color publishing remain inside the standalone H2H subsystem; if Opponents clears those race outputs, H2H should also clear/inactivate rather than revive a stale race identity. Opponents does **not** become a far-away timing engine, and any extra `CarIdx` recovery stays a narrow local fallback inside H2H/LalaLaunch rather than moving timing ownership into Opponents.

## Gating and scope
- Runs in **live opponent sessions** (Practice, Qualifying/Open Qualify, Lone Qualify, Race); resets caches/outputs when session leaves that eligibility scope (for example Offline Testing).
- No completed-lap gate is applied; once the session is eligible, leaderboard-neighbor `Opp.*` outputs may publish immediately.
- Pit-exit prediction remains race-scoped; `PitExit.*` reset once on Race → non-Race transitions (not every non-race tick).
- Uses **IRacingExtraProperties only**; no Dahl DLL or SDK arrays.

## Identity model
- Stable key: `ClassColor:CarNumber`. Empty class+number returns blank identity (slot ignored).
- Nearby slot changes log once per rebind when active; identity caches persist pace history across slot swaps. Logging remains race-session scoped and debug-gated for slot-rebind chatter.
- Published race-target identity is now leaderboard-authoritative: Opponents finds the player row in `iRacing_ClassLeaderboard_Driver_XX_*`, then selects `PositionInClass - 1/-2/+1/+2` within the same class for `Opp.Ahead1/2` and `Opp.Behind1/2`. If the player row or neighbor row cannot be resolved, the published target stays empty/invalid; there is no fallback to nearby slots for race-target identity.

## Data inputs
- Nearby targets: `iRacing_DriverAheadInClass_00/01_*`, `iRacing_DriverBehindInClass_00/01_*` for Name, CarNumber, ClassColor, RelativeGapToPlayer, LastLapTime, BestLapTime, IsInPit, IsConnected. These feeds are still ingested for cache continuity and debug slot-rebind logging, but they no longer determine published race-target identity for `Opp.Ahead1/2.*` or `Opp.Behind1/2.*`.
- Leaderboard scan (00–63 until empty row): `iRacing_ClassLeaderboard_Driver_XX_*` for Name, CarNumber, ClassColor, Position, PositionInClass, RelativeGapToLeader, IsInPit/IsConnected, LastLapTime, BestLapTime.
- Player identity: now resolves natively from `SessionData.DriverInfo.*` by `Telemetry.PlayerCarIdx` first, with bounded fallback to `iRacing_Player_ClassColor` + `iRacing_Player_CarNumber` only if session-info identity is not yet available. Pit-exit receives pit loss from LalaLaunch’s stop-loss calculation (validated to ≥0).

## Extra Properties dependency map (current implementation)
Opponents still has a large `IRacingExtraProperties` dependency surface for class-leaderboard and same-class gap reconstruction:

- **Still Extra-Properties-backed (active runtime path):**
  - `iRacing_ClassLeaderboard_Driver_XX_*` (`Name`, `CarNumber`, `ClassColor`, `Position`, `PositionInClass`, `RelativeGapToLeader`, `IsInPit`, `IsConnected`, `LastLapTime`, `BestLapTime`).
  - `iRacing_DriverAheadInClass_00/01_*` and `iRacing_DriverBehindInClass_00/01_*` (cache continuity + debug slot rebind logs).
- **Now native-first (with fallback):**
  - Player identity (`ClassColor`, `CarNumber`) resolves via `SessionData.DriverInfo.DriversXX` / `CompetingDrivers[]` using `Telemetry.PlayerCarIdx`, then falls back to `iRacing_Player_*` only when native identity is unavailable.
- **Indirect consumers:**
  - `H2HRace.*` remains a selector consumer of `Opp.Ahead1` / `Opp.Behind1`. H2H does not own race selector identity and does not rebuild class leaderboard state itself.

## Native same-class leaderboard migration design (analysis-first)
This section captures the approved staged design target without claiming the full migration is already implemented.

### Required native row contract (Opponents-owned model)
Minimal fields needed to reproduce current `Opp.*` + `PitExit.*` behavior:
- `CarIdx` (stable join key to telemetry arrays and CarSA caches).
- `IdentityKey` (`ClassColor:CarNumber`), `Name`, `CarNumber`, `ClassColor`.
- `PositionInClass` (or reconstructable equivalent ordering rank).
- `IsConnected`, `IsInPit`.
- `LastLapSec`, `BestLapSec` (pace cache feed).
- `GapToClassLeaderSec` (or functionally equivalent monotonic class-order distance used by gap-to-player and pit-exit delta math).

### Proposed native truth sources by field
- **Identity / cosmetics:** `SessionData.DriverInfo.DriversXX` primary; `CompetingDrivers[]` fallback.
- **CarIdx and per-car timing:** CarSA-owned caches and existing plugin arrays (`CarIdxBestLapTime`, `CarIdxLastLapTime`, `CarIdxLapDistPct`, `CarIdxLap`, `CarIdxClassPosition`).
- **Class membership:** normalized `CarClassColor` from session info (same source already used by CarSA/H2H identity resolution).
- **Class ordering:** first candidate = `Telemetry.CarIdxClassPosition`; fallback candidate = distance-along-lap ordering from `CarIdxLap` + `CarIdxLapDistPct` with lapping guards.
- **Gap model (leader/player/neighbours):** phased approach:
  1) temporary parity path keeps `RelativeGapToLeader` from Extra Properties;
  2) native phase replaces this with class-local cumulative timing built from CarSA checkpoints/fixed-sector + lap-distance interpolation.

### Confidence / risk by field
- **High confidence:** player identity, car identity metadata, class membership, lap-time fields.
- **Medium confidence:** class ordering from `CarIdxClassPosition` (needs validation under reconnect/replay edge-cases).
- **Lower confidence (not first-step safe):** exact `RelativeGapToLeader` parity for lapped/multi-lap race states without Extra Properties.

### Recommended migration order
1. **Completed in this task:** player identity native-first session-info resolution (fallback retained).
2. Introduce an Opponents-local native leaderboard row helper that hydrates identity/class/lap-time/order from session+telemetry without changing published behavior.
3. Dual-run validation (native rows + current Extra Properties rows) with debug-only mismatch counters (no output contract changes).
4. Switch same-class neighbour selection to native ordering source after mismatch thresholds are acceptable.
5. Replace gap-to-leader dependency with native gap model for `GapToPlayerSec` and `PitExit.*`.
6. Remove remaining `iRacing_ClassLeaderboard_Driver_XX_*` dependency once parity is validated.

## Pace cache & blended pace
- Entity cache keyed by identity; keeps best lap and a 5-lap ring buffer of valid recent laps (rejects ≤0/NaN/huge, skips laps flagged in-pit).
- BlendedPaceSec = 0.70×RecentAvg + 0.30×(BestLap×1.01); falls back to recent-only or best×1.01 if missing.

## Fight prediction (dash support)
- Uses my pace (from LalaLaunch) vs opponent blended pace once active; my pace is sanitized to remove invalid/huge values.
- For published race targets, GapToPlayerSec is derived from the absolute difference between the target row’s `RelativeGapToLeader` and the player row’s `RelativeGapToLeader`, so same-class neighbors remain valid even across multi-lap gaps.
- Ahead: closingRate = opponent − mine; requires closingRate > +0.05 s/lap and positive gap to publish LapsToFight (capped at 999). Otherwise NaN (no catch).
- Behind: closingRate = mine − opponent; requires closingRate > +0.05 s/lap and positive gap to publish LapsToFight (NaN when no threat/invalid).
- Summary strings split: `Opponents_SummaryAhead` (A1/A2) and `Opponents_SummaryBehind` (B1/B2). Per-slot variants: `Opponents_SummaryAhead1/2`, `Opponents_SummaryBehind1/2`. Format example — Ahead: `A1 #25 +0.6s Δ-0.12s/L LTF=5 | A2 #11 +1.4s Δ-0.05s/L LTF=—`; Behind: `B1 #39 -0.7s Δ+0.08s/L LTF=9 | B2 #32 -1.6s Δ+0.03s/L LTF=—`. Uses `—` when data unavailable.

## Pit-exit prediction
- Finds player row in class leaderboard; predicted gap to leader after pit = player.RelGapToLeader + pitLossSec (pit loss forced to 0 when invalid). Each same-class, connected row computes `delta = row.RelGapToLeader − playerPredictedGapToLeaderAfterPit`. Negative delta means the car is ahead after pit; positive delta means behind.
- On pit entry, the predictor **locks** the player gap-to-leader and pit-loss inputs for the duration of the pit trip, preventing drift if leaderboard gaps update mid-stop. Lock clears once pitTripActive ends.
- PredictedPosition = 1 + count of same-class connected cars where delta < 0. Logs when validity toggles or predicted position changes while active; prediction-change logs remain debug-gated and cadence-filtered.
- Update cadence: runs every tick on pit road; off pit road, runs only when the player enters a new lap quarter (TrackPct × 4 → 0-3). Prediction is skipped near race end if reliable session time remaining is ≤120 s.
- Nearest ahead/behind exports come from the same scan: nearest ahead = largest negative delta (closest ahead); nearest behind = smallest positive delta (closest behind). Only same-class, connected cars are considered.
- Prediction-change logs are gated to reduce chatter: emits only on ≥2 place change, ≥2 s since last log, or pitTripActive/onPitRoad transitions (predictor outputs unchanged).
- Snapshot data (player positions, gap-to-leader, pitLoss, predicted pos, cars ahead, **locked gap/pit loss**, and derived predicted gap) is captured for one-shot pit-in/out logging in LalaLaunch. Math audit lists boundary candidates around the pit-loss compare and uses `d = (candidateGap - playerGap) - pitLoss` for the same-class set; it is emitted with the pit-in snapshot.
- A one-lap-delayed “pit-out settled” log is emitted after crossing the lap boundary following pit exit to record the final settled position and gap-to-leader at the end of the out-lap.
- Publishes PitExit.Valid/PredictedPositionInClass/CarsAheadAfterPitCount/Summary plus nearest ahead/behind identity/gap fields; defaults reset when invalid.

## Outputs
- `Opp.Ahead1/2.*`, `Opp.Behind1/2.*` → strict same-class standings neighbors around the player row (no nearby fallback), published as Name, CarNumber, ClassColor, GapToPlayerSec (absolute leaderboard-relative gap), BlendedPaceSec, PaceDeltaSecPerLap, and LapsToFight (NaN = no fight/invalid). Summaries at `Opponents_SummaryAhead/Behind` (plus per-slot variants).
- Optional leader pace: `Opp.Leader.BlendedPaceSec`, `Opp.P2.BlendedPaceSec`.
- Pit exit exports: `PitExit.Valid`, `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, `PitExit.Summary`, `PitExit.Ahead.*`, `PitExit.Behind.*` (Name/CarNumber/ClassColor/GapSec).
