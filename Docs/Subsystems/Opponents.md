# Opponents subsystem

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

Purpose: own all opponent-facing calculations for strict same-class race-target selection, nearby/leaderboard pace enrichment, and pit-exit position forecasting with SimHub exports under `Opp.*` and `PitExit.*`.
Phase-1 Head-to-Head (`H2HRace.*`) consumes Opponents only as a **race-target selector seam** (current class-ahead / class-behind identity from `Opp.Ahead1` / `Opp.Behind1`). Persistent H2H timing, live-gap, lap-summary, fixed-6-segment ownership, and canonical H2H class-color publishing remain inside the standalone H2H subsystem; if Opponents clears those race outputs, H2H should also clear/inactivate rather than revive a stale race identity. Opponents does **not** become a far-away timing engine, and any extra `CarIdx` recovery stays a narrow local fallback inside H2H/LalaLaunch rather than moving timing ownership into Opponents.

## Gating and scope
- Runs **race sessions only**; resets caches/outputs if session leaves Race.【F:Opponents.cs†L42-L155】
- No additional completed-lap gate is currently applied; once the session is Race, Opp and PitExit outputs may publish immediately. The subsystem-active log still fires once per activation.【F:Opponents.cs†L58-L155】
- Uses **IRacingExtraProperties only**; no Dahl DLL or SDK arrays.【F:Opponents.cs†L42-L88】

## Identity model
- Stable key: `ClassColor:CarNumber`. Empty class+number returns blank identity (slot ignored).【F:Opponents.cs†L90-L100】
- Nearby slot changes log once per rebind when active; identity caches persist pace history across slot swaps. Logging remains race-session scoped and debug-gated for slot-rebind chatter.【F:Opponents.cs†L277-L421】
- Published race-target identity is now leaderboard-authoritative: Opponents finds the player row in `iRacing_ClassLeaderboard_Driver_XX_*`, then selects `PositionInClass - 1/-2/+1/+2` within the same class for `Opp.Ahead1/2` and `Opp.Behind1/2`. If the player row or neighbor row cannot be resolved, the published target stays empty/invalid; there is no fallback to nearby slots for race-target identity.【F:Opponents.cs†L129-L211】【F:Opponents.cs†L568-L608】

## Data inputs
- Nearby targets: `iRacing_DriverAheadInClass_00/01_*`, `iRacing_DriverBehindInClass_00/01_*` for Name, CarNumber, ClassColor, RelativeGapToPlayer, LastLapTime, BestLapTime, IsInPit, IsConnected. These feeds are still ingested for cache continuity and debug slot-rebind logging, but they no longer determine published race-target identity for `Opp.Ahead1/2.*` or `Opp.Behind1/2.*`.【F:Opponents.cs†L277-L421】
- Leaderboard scan (00–63 until empty row): `iRacing_ClassLeaderboard_Driver_XX_*` for Name, CarNumber, ClassColor, Position, PositionInClass, RelativeGapToLeader, IsInPit/IsConnected, LastLapTime, BestLapTime.【F:Opponents.cs†L489-L525】
- Player identity: `iRacing_Player_ClassColor`, `iRacing_Player_CarNumber`. Pit-exit receives pit loss from LalaLaunch’s stop-loss calculation (validated to ≥0).【F:Opponents.cs†L42-L88】【F:LalaLaunch.cs†L3701-L3731】

## Pace cache & blended pace
- Entity cache keyed by identity; keeps best lap and a 5-lap ring buffer of valid recent laps (rejects ≤0/NaN/huge, skips laps flagged in-pit).【F:Opponents.cs†L627-L717】
- BlendedPaceSec = 0.70×RecentAvg + 0.30×(BestLap×1.01); falls back to recent-only or best×1.01 if missing.【F:Opponents.cs†L693-L717】

## Fight prediction (dash support)
- Uses my pace (from LalaLaunch) vs opponent blended pace once active; my pace is sanitized to remove invalid/huge values.【F:Opponents.cs†L42-L88】【F:Opponents.cs†L129-L211】
- For published race targets, GapToPlayerSec is derived from the absolute difference between the target row’s `RelativeGapToLeader` and the player row’s `RelativeGapToLeader`, so same-class neighbors remain valid even across multi-lap gaps.【F:Opponents.cs†L170-L211】
- Ahead: closingRate = opponent − mine; requires closingRate > +0.05 s/lap and positive gap to publish LapsToFight (capped at 999). Otherwise NaN (no catch).【F:Opponents.cs†L191-L211】
- Behind: closingRate = mine − opponent; requires closingRate > +0.05 s/lap and positive gap to publish LapsToFight (NaN when no threat/invalid).【F:Opponents.cs†L191-L211】
- Summary strings split: `Opponents_SummaryAhead` (A1/A2) and `Opponents_SummaryBehind` (B1/B2). Per-slot variants: `Opponents_SummaryAhead1/2`, `Opponents_SummaryBehind1/2`. Format example — Ahead: `A1 #25 +0.6s Δ-0.12s/L LTF=5 | A2 #11 +1.4s Δ-0.05s/L LTF=—`; Behind: `B1 #39 -0.7s Δ+0.08s/L LTF=9 | B2 #32 -1.6s Δ+0.03s/L LTF=—`. Uses `—` when data unavailable.【F:Opponents.cs†L78-L155】

## Pit-exit prediction
- Finds player row in class leaderboard; predicted gap to leader after pit = player.RelGapToLeader + pitLossSec (pit loss forced to 0 when invalid). Each same-class, connected row computes `delta = row.RelGapToLeader − playerPredictedGapToLeaderAfterPit`. Negative delta means the car is ahead after pit; positive delta means behind.【F:Opponents.cs†L507-L553】
- On pit entry, the predictor **locks** the player gap-to-leader and pit-loss inputs for the duration of the pit trip, preventing drift if leaderboard gaps update mid-stop. Lock clears once pitTripActive ends.【F:Opponents.cs†L783-L832】
- PredictedPosition = 1 + count of same-class connected cars where delta < 0. Logs when validity toggles or predicted position changes while active; predicted-position-change logs require debug toggle + lap gate.【F:Opponents.cs†L532-L566】
- Update cadence: runs every tick on pit road; off pit road, runs only when the player enters a new lap quarter (TrackPct × 4 → 0-3). Prediction is skipped near race end if reliable session time remaining is ≤120 s.【F:Opponents.cs†L612-L742】
- Nearest ahead/behind exports come from the same scan: nearest ahead = largest negative delta (closest ahead); nearest behind = smallest positive delta (closest behind). Only same-class, connected cars are considered.【F:Opponents.cs†L532-L658】
- Prediction-change logs are gated to reduce chatter: emits only on ≥2 place change, ≥2 s since last log, or pitTripActive/onPitRoad transitions (predictor outputs unchanged).【F:Opponents.cs†L676-L694】
- Snapshot data (player positions, gap-to-leader, pitLoss, predicted pos, cars ahead, **locked gap/pit loss**, and derived predicted gap) is captured for one-shot pit-in/out logging in LalaLaunch. Math audit lists boundary candidates around the pit-loss compare and uses `d = (candidateGap - playerGap) - pitLoss` for the same-class set; it is emitted with the pit-in snapshot.【F:Opponents.cs†L862-L978】【F:LalaLaunch.cs†L4712-L4784】
- A one-lap-delayed “pit-out settled” log is emitted after crossing the lap boundary following pit exit to record the final settled position and gap-to-leader at the end of the out-lap.【F:Opponents.cs†L695-L738】
- Publishes PitExit.Valid/PredictedPositionInClass/CarsAheadAfterPitCount/Summary plus nearest ahead/behind identity/gap fields; defaults reset when invalid.【F:Opponents.cs†L507-L579】【F:Opponents.cs†L775-L789】

## Outputs
- `Opp.Ahead1/2.*`, `Opp.Behind1/2.*` → strict same-class standings neighbors around the player row (no nearby fallback), published as Name, CarNumber, ClassColor, GapToPlayerSec (absolute leaderboard-relative gap), BlendedPaceSec, PaceDeltaSecPerLap, and LapsToFight (NaN = no fight/invalid). Summaries at `Opponents_SummaryAhead/Behind` (plus per-slot variants).【F:Opponents.cs†L129-L211】
- Optional leader pace: `Opp.Leader.BlendedPaceSec`, `Opp.P2.BlendedPaceSec`.【F:Opponents.cs†L84-L88】【F:Opponents.cs†L720-L736】
- Pit exit exports: `PitExit.Valid`, `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, `PitExit.Summary`, `PitExit.Ahead.*`, `PitExit.Behind.*` (Name/CarNumber/ClassColor/GapSec).【F:Opponents.cs†L507-L566】【F:Opponents.cs†L775-L789】
