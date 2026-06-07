# CarSA (Car System) — SA-Core v2

## Scope
CarSA provides **session-agnostic**, **class-aware** spatial awareness using iRacing CarIdx telemetry arrays as the source of truth. It publishes the 5 nearest cars ahead and 5 behind on track for Practice, Qualifying, and Race sessions using distance-based gaps derived from car-centric LapDistPct deltas, plus gate-gap v2 relative proximity.

CarSA is independent of the Opponents subsystem (live-session native same-class neighbors + race-scoped native pit-exit prediction) and does not change Opponents or Rejoin Assist behavior.
Head-to-Head now consumes CarSA in two narrow ways:
- `H2HTrack.*` uses CarSA as the **track-target selector seam** (the current ahead/behind slot sets, `CarIdx`, and already-resolved cosmetic metadata) and consumes the selected slot `Gap.TrackSec` as its directional track-loop `LiveGapSec` baseline.
- `H2HTrack.*` selector ownership remains CarSA-owned; published `PositionInClass` can use Opponents effective/live class-position context when available, without moving race-order selection ownership into CarSA.
- `Car.Player.PositionInClass` and H2H player-row `PositionInClass` publication can consume the Opponents effective/live class-position seam when available; CarSA remains the session-agnostic spatial owner.
- Both `H2HRace.*` and `H2HTrack.*` now read `S1..S6State` / `S1..S6DeltaSec` from the CarSA-owned per-car fixed-6-sector cache through the narrow CarSA accessor seam.
LapRef (`LapRef.*`) also consumes the same fixed-6-sector cache as a read-only player-reference input; CarSA remains the sole owner of sector derivation and cache mutation rules.
`H2HRace.*` still uses CarSA only as a bounded local live-session fallback when a known Opponents-selected identity needs help re-resolving `CarIdx`; selector ownership remains in Opponents.
Opponents also consumes a narrow timing seam (`TryGetCheckpointGapSec(playerCarIdx, targetCarIdx, out signedGapSec)`) to improve `Opp.Ahead1/Behind1.GapToPlayerSec` with checkpoint-time-derived true gaps; Opponents keeps neighbor selection ownership.
- The direct checkpoint seam uses a CarSA runtime-owned lap-time scale for adjacent-lap correction. It no longer depends on debug publication state, so System Debug ON/OFF does not change checkpoint-gap availability. Same-lap handling, invalid-scale rejection, and downstream native fallback behavior remain unchanged.
- Direct checkpoint lookup requires both cars to be currently on-track with `TrackSurfaceRaw == OnTrack` and not on pit road. Eligibility is refreshed from current telemetry immediately before Opponents consumes the seam and synchronized again during the normal CarSA update, so pit-entry/off-track transitions fail closed without a prior-tick window. Ineligible per-car direct lookup timestamps are cleared and are not recorded while ineligible, preventing pit-entry/pit-exit stale reuse; Opponents retains native progress/pace fallback when the seam returns false.

## Truth source
- **Primary:** `CarIdx*` telemetry arrays (CarIdxLapDistPct, CarIdxLap, CarIdxTrackSurface, CarIdxOnPitRoad).
- **Raw flags (optional):** `CarIdxSessionFlags`, `CarIdxPaceFlags`, and `CarIdxTrackSurfaceMaterial` when raw telemetry read mode is enabled (drives compromised evidence and debug exports).
- **Identity:** Slot Name/CarNumber/ClassColor are pulled from session info using `DriverInfo.Drivers##.*` only, with retry/hold logic so replays can resolve identities once trusted session data arrives. `DriverInfo.CompetingDrivers[*]` is not used as a live normal-driver fallback.
- **Class rank map:** CarSA builds a per-session class rank map from `CarClassRelSpeed` (preferred) or `CarClassEstLapTime` to label Faster/Slower class statuses.
- **Strength-of-field:** An iRating SOF average is computed across active CarSA slots for quick session context.

## Car-centric state cache
CarSA keeps a car-centric shadow state per `CarIdx` that is authoritative for StatusE decisions and gap/closing-rate stability:
- **Spatial deltas:** Forward/backward distance pct, signed delta pct, and closing rate based on per-tick LapDistPct deltas.
- **Track state:** Track surface raw, pit area detection (pit lane or pit stall), on-track flag, and session flags.
- **Latches:**
  - **Out-lap** latches when a car exits pit area onto track; stays active until the next lap completes.
  - **Compromised (off-track)** latches when track surface == OffTrack; stays active until the next lap completes.
  - **Compromised (penalty)** latches when session flags include black/furled/repair/disqualify; stays active until the next lap completes.
- **Grace windows:**
  - **LapPct grace:** 0.5 s grace before clearing delta/closing data on invalid LapDistPct.
  - **Not-in-world grace:** 3.0 s before clearing latches if a car remains NotInWorld.
  - **Blink slot hold:** existing ahead/behind slot identity can be retained for up to 1.0 s when the same `CarIdx` briefly becomes NotInWorld or loses valid `LapDistPct`. During this bounded hold the slot keeps identity/cosmetic fields for H2HTrack/dashboard continuity, but live gap truth is invalidated (`Gap.*` NaN/source 0; slot-01 precision gap NaN when slot 01 is held), `IsOnTrack=false`, and direct checkpoint eligibility and H2HTrack live-selector eligibility remain fail-closed. A held slot does not consume/drop the nearest live candidate; following slots continue to publish the next eligible live cars without duplicating the held `CarIdx`. Valid telemetry for the same `CarIdx` resumes normal slot/gap behavior; timeout releases to normal candidate promotion.

## Fixed-6-sector cache
CarSA owns the per-car fixed-6-sector cache consumed by H2H:
- **Ownership:** CarSA owns the cache and its derivation rules; H2H does not mutate or own the cache.
- **Shape:** per car = `LastCheckpointIndex`, `LastBoundaryIndex`, `LastBoundaryTimeSec`, and 6 sector entries containing only `HasValue` + `DurationSec`.
- **Mapping:** fixed sectors use the existing 60-checkpoint model with coarse boundaries `0,10,20,30,40,50`, so sector spans are `0→10`, `10→20`, `20→30`, `30→40`, `40→50`, `50→0`.
- **Derivation:** a sector is written only when continuous checkpoint progress reaches that sector's end boundary and the immediately previous coarse boundary was already anchored for that same car; otherwise the new boundary is anchored without synthesizing a sector.
- **Continuity rule:** checkpoint advance is computed modulo 60; only advances `1..10` are accepted as continuous. `>10` is treated as a discontinuity and clears that car's cache before re-anchoring.
- **Invalidation:** CarSA reset paths and unusable per-car tracking samples clear the affected cache back to the sentinel/unset state. There is no age timeout in this phase.
- **Read seam:** consumers must use the narrow CarSA-owned accessor (`TryGetFixedSectorCacheSnapshot`) rather than reaching into mutable internals.
- **Current H2H use:** H2H reads the player and target cache snapshots directly every tick. Target swap does not clear rows for bind reasons; the newly selected target publishes immediately from whatever cache state CarSA already has for that `CarIdx`.

## Slot selection (Ahead/Behind)
- Ordering uses car-centric forward/backward distances computed from LapDistPct.
- Forward distance: `(oppPct - myPct + 1.0) % 1.0`
- Backward distance: `(myPct - oppPct + 1.0) % 1.0`
- **TrackSurface semantics:** `IsOnTrack` is true only when TrackSurface == OnTrack. Pit lane/stall is treated as pit area. NotInWorld makes a slot invalid.
- **Pit-road exclusion:** pit-road cars are excluded from candidate slots by default to reduce practice/quali noise.
- **Half-lap filter:** candidates with `distPct` in **[0.40, 0.60]** are skipped per direction to avoid S/F wrap ambiguity (tracked by debug counters).
- **Hysteresis:** a new candidate replaces a slot only if it is **at least 10% closer** (or the current slot is invalid).
- **Slot reset:** when a slot swaps to a new `CarIdx`, gap and status text caches are cleared. Identity is refreshed with retry to avoid replay timing gaps.

## Gap & closing semantics
- **Gap.TrackSec:** `distPct * lapTimeEstimateSec` (directional track-loop baseline for the selected ahead/behind side; behind slots are published with the existing negative side sign, and H2HTrack uses the absolute magnitude for its positive display convention).
- **Gap.RelativeSec (GateGap v2):** mini-sector gate timing produces a gate truth gap that is filtered forward in time with a rate EMA, corrected toward fresh truth, and held briefly (sticky publish) if inputs drop; values are mapped to ahead/behind sign so wraps stay direction-safe. Falls back to `Gap.TrackSec` when no gate data exists. Normalization guards handle lapped cars and mismatch fallbacks when gate gaps diverge from track gaps. Gate truth supports both crossing orders: the existing target-crossing path matches a previously recorded player crossing, while a player crossing also reverse-matches eligible non-player timestamps already recorded for that gate. Reverse matches require a current on-track/non-pit eligibility state, a non-future timestamp no older than the smaller of 15 seconds or half the active lap-time scale, a reverse-only `|target lap - player lap| <= 2` bound, and skip targets that crossed the same gate on the current update so the target path remains the sole observation for same-tick pairs. Both paths store `target checkpoint time - player checkpoint time` and feed the same normalization, truth, rate, and filtered-cache pipeline.
- **Gap.RelativeSource:** tracks which input fed the relative gap (filtered, truth, track fallback, sticky hold, invalid).
- **Slot 01 precision gaps:** `Car.Ahead01P.Gap.Sec` / `Car.Behind01P.Gap.Sec` are sharper textual/number-display values for the closest ahead/behind slot. Internal gate truth remains stored as target checkpoint time minus player checkpoint time (ahead negative, behind positive); after selecting fresh gate truth first, or recently defensible filtered checkpoint state while the last truth observation remains within the existing freshness limit, precision locally negates that stored proximity candidate into the public TrackSec sign convention (ahead positive, behind negative). `Gap.TrackSec` remains the directional-loop authority: the direction-normalized candidate is tested at its current value and ± one lap time; only the same-sign candidate closest to TrackSec may publish, and only within `min(6.0 s, max(1.5 s, 8% of |TrackSec|))`. Missing, wrong-direction, or out-of-tolerance checkpoint state falls back directly to TrackSec, so half-lap/short-way checkpoint proximity cannot replace a long directional-loop answer. Precision bypasses `Gap.RelativeSec` sticky-hold semantics, does not publish stale raw truth or indefinitely predicted filtered values, and remains separate from the unchanged visual/proximity `Gap.RelativeSec` contract. H2HTrack continues to consume TrackSec directly.
- Passive League Class metadata is additionally exported on `Car.Ahead01..05` and `Car.Behind01..05` (`LeagueClassName/ShortName/Rank/ColourHex/Valid/Source`) via resolver-only lookup; CarSA physical selection/order/filter logic is unchanged.
- **ClosingRateSecPerSec:** derived from change in absolute delta pct over time; **positive values mean closing**; clamped to ±5 s/s.
- **Lap time estimate:** player average pace, else last lap, else 120 s fallback.
- **LapDelta:** computed from CarIdx lap counters with S/F straddle guards to avoid one-tick spikes when cars are physically close around the line.

## Info banner behaviour
- **Burst model:** `InfoVisibility`/`Info` are driven by short bursts instead of continuous rotation.
  - **S/F burst:** when a same-class slot crosses the start/finish window (0.00–0.05 lap pct), a 9 s burst cycles through **laps-since-pit**, **LL vs BL**, and **LL vs Me** messages (3 s each).
  - **Half-lap burst:** when a same-class slot enters the 0.55–1.00 lap pct window, a 9 s burst cycles **Live Δ** and **laps-since-pit** (3 s phases with fallback to available messages).
  - **Baseline gating:** outside bursts, baseline info only shows when `StatusE == Unknown` with a 1 s debounce to prevent flicker; info is cleared for invalid slots, cross-class opponents, or slot swaps.
- **Burst latching:** bursts are latched per CarIdx and cancelled if the slot changes identity mid-burst.

## Status enums
CarSA publishes a minimal base status enum:
- `Unknown = 0`
- `Normal = 1`
- `InPits = 2` (set whenever `CarIdxOnPitRoad` is true).

## StatusE ladder (SA-Core v2)
CarSA publishes a Traffic SA “E-number” ladder per slot for dash filtering. StatusE uses car-centric latches, pit-area detection, and class rank data.

**Numeric mapping (locked):**

| Range | Value | Status | Short | Long |
| --- | --- | --- | --- | --- |
| LOW | 0 | Unknown | UNK |  |
| MID | 100 | OutLap | OUT | Out lap |
| MID | 110 | InPits | PIT | In pits |
| MID | 121 | CompromisedOffTrack | OFF | Lap Invalid |
| MID | 122 | CompromisedPenalty | PEN | Penalty |
| MID | 130 | HotlapWarning | FAST! | Hot lap interference warning |
| MID | 131 | HotlapCaution | PUSH | Push lap |
| MID | 132 | HotlapHot | HOT | Hot lap |
| MID | 140 | CoolLapWarning | SLOW! | Cool lap interference warning |
| MID | 141 | CoolLapCaution | COOL | Cool lap |
| HIGH | 200 | FasterClass | FCL | Faster class |
| HIGH | 210 | SlowerClass | SCL | Slower class |
| HIGH | 220 | Racing | RCE | Racing |
| HIGH | 230 | LappingYou | +nL | Up +n Laps |
| HIGH | 240 | BeingLapped | -nL | Down -n Laps |

## StatusE + colour decode (plugin defaults)

The dash uses **two colour channels** per CarSA slot:
- `StatusBgHex`: background colour selected from the StatusE map.
- `BorderHex`: border colour selected from the tag/priority border-mode resolver.

### A) StatusE background colour map (`StatusBgHex`)

Default map from `LaunchPluginSettings.CarSAStatusEColorMap`:

| StatusE | Value | Hex | Colour name | Notes |
| --- | ---: | --- | --- | --- |
| Unknown | 0 | `#000000` | Black | Fallback/default state. |
| OutLap | 100 | `#696969` | Dim Gray | Out-lap latch active. |
| InPits | 110 | `#C0C0C0` | Silver | Pit road / pit-area state. |
| SuspectInvalid | 120 | `#FFA500` | Orange | Enum exists; reserved/compat path. |
| CompromisedOffTrack | 121 | `#FF0000` | Red | Off-track compromise latch. |
| CompromisedPenalty | 122 | `#FFA500` | Orange | Penalty compromise latch. |
| HotlapWarning | 130 | `#FF0000` | Red | FAST interference warning. |
| HotlapCaution | 131 | `#FFFF00` | Yellow | PUSH intent band. |
| HotlapHot | 132 | `#FFFF00` | Yellow | HOT intent band. |
| CoolLapWarning | 140 | `#FF0000` | Red | SLOW interference warning. |
| CoolLapCaution | 141 | `#FFFF00` | Yellow | COOL intent band. |
| FasterClass | 200 | `#000000`* | Black* | *When `ClassColorHex` is valid, CarSA uses class colour instead of map value.* |
| SlowerClass | 210 | `#000000`* | Black* | *When `ClassColorHex` is valid, CarSA uses class colour instead of map value.* |
| Racing | 220 | `#008000` | Green | Same-class racing context. |
| LappingYou | 230 | `#0000FF` | Blue | Opponent up laps on player. |
| BeingLapped | 240 | `#ADD8E6` | Light Blue | Opponent down laps to player. |

### B) Tagged-driver crossover (border decode via `BorderHex`)

Tagging does **not** change StatusE value; it changes the border via a priority resolver.

Border-mode priority (highest first):
1. Manual **Teammate** tag → `TEAM`
2. Manual **Bad** tag → `BAD`
3. Manual **Friend** tag → `FRIEND`
4. Telemetry teammate match → `TEAM`
5. Class leader → `LEAD`
6. Other class → `OCLS`
7. Fallback → `DEF`

Default border map from `LaunchPluginSettings.CarSABorderColorMap` + resolver override:

| BorderMode | Trigger source | Hex | Colour name | Notes |
| --- | --- | --- | --- | --- |
| FRIEND | Manual Friend tag | `#00FF00` | Lime | Hardcoded resolver override (ignores map for FRIEND). |
| TEAM | Manual Teammate tag OR telemetry teammate | `#FF69B4` | Hot Pink | Comes from border map defaults. |
| BAD | Manual Bad tag | `#FF0000` | Red | Comes from border map defaults. |
| LEAD | Position in class = 1 (same class) | `#FF00FF` | Magenta | Comes from border map defaults. |
| OCLS | Other-class car | `#0000FF` | Blue | Comes from border map defaults. |
| DEF | Default/no special flag | `#F5F5F5` | White Smoke | Comes from border map defaults. |
| (final fallback) | Invalid/missing map values | `#A9A9A9` | Dark Gray | Resolver fallback if no valid default border colour. |

### C) Tag names used by the driver-tag system

Driver tags are normalized to one of:
- `Friend`
- `Teammate` (also accepts legacy `Team`)
- `Bad`

These tags feed the border-mode resolver above and therefore define the visual crossover between the tagged-driver system and CarSA slot styling.

**StatusE logic (per slot, priority order):**
1. If the slot is on pit road or in a pit-area surface ⇒ `InPits` (reason `pits`).
2. If car-centric compromised penalty latch is active ⇒ `CompromisedPenalty` (reason `cmp_pen`).
3. Else if car-centric compromised off-track latch is active ⇒ `CompromisedOffTrack` (reason `cmp_off`).
4. If slot is invalid / NotInWorld / not on track ⇒ `Unknown` (reason `unknown`).
5. If car-centric out-lap latch is active ⇒ `OutLap` (reason `outlap`).
6. If `LapDelta > 0` ⇒ `LappingYou` (reason `lap_ahead`).
7. If `LapDelta < 0` ⇒ `BeingLapped` (reason `lap_behind`).
8. If same class as player ⇒ `Racing` (reason `racing`).
9. If other class and class ranks exist ⇒ `FasterClass` or `SlowerClass` (reason `otherclass`).
10. If other class but rank is unavailable ⇒ fallback based on ahead/behind direction (`FasterClass` when behind, `SlowerClass` when ahead; reason `otherclass_unknownrank`).

Gap-based relevance gating is disabled in SA-Core v2.

## Hot/Cool intent bands
Hot/Cool intent uses `DeltaBestSec` with seconds-based bands (full system in Practice/Qual/Warmup):
- `deltaBest < 0.00` ⇒ **HOT** (intent `Hot`, status `HotlapHot` unless FAST conflict warning applies).
- `0.00 <= deltaBest <= 0.50` ⇒ **PUSH** (intent `Push`, status `HotlapCaution` unless FAST conflict warning applies).
- `0.50 < deltaBest <= 1.00` ⇒ **NONE** (no message, no Hot/Cool StatusE override).
- `deltaBest > 1.00` ⇒ **COOL** (intent `Cool`, status `CoolLapCaution` unless SLOW conflict warning applies).

`FAST!` and `SLOW!` remain interference warnings driven by the existing conflict test (`behind + conflict` for FAST, `ahead + conflict` for SLOW). They are not DeltaBest intent bands.

## SessionType policy
- **Practice/Qual/Warmup:** full Hot/Cool system applies per the existing intent and conflict rules. Existing non-race suppression remains `Racing`/`LappingYou`/`BeingLapped` (220/230/240), and StatusE stays gated by `SessionState == 4` with the current settle delay.
- **Race:** FAST/SLOW interference warnings (`HotlapWarning`/`CoolLapWarning`, 130/140) remain enabled, while base intent messages HOT/PUSH/COOL (`HotlapHot`/`HotlapCaution`/`CoolLapCaution`, 132/131/141) are suppressed.
- **Lone Qualify + Offline Testing:** StatusE is forced to `Unknown` for all slots.

## Raw telemetry flags
- Player-level raw flags are published as `Car.Player.PaceFlagsRaw`, `Car.Player.SessionFlagsRaw`, `Car.Player.TrackSurfaceMaterialRaw` when soft debug + raw telemetry mode are enabled.
- Player raw track position is exported as `Car.Player.TrackPct` (plugin-owned normalized `CarIdxLapDistPct` in `0..1`; invalid/unavailable -> `0`). This is lap-distance context only and not race-order position.
- Slot-level raw flags (`SessionFlagsRaw`, `TrackSurfaceMaterialRaw`) are populated when raw telemetry mode includes slots (and soft debug is on).
- Debug outputs report whether each raw array was readable and the read mode/failure reason.

## Exports (SA-Core v2)
Prefix: `Car.*`

System:
- `Car.Valid`
- `Car.Source` (`CarIdxTruth`)
- `Car.SlotsAhead` (5)
- `Car.SlotsBehind` (5)
- `Car.iRatingSOF` (League-aware when League Class is enabled+resolved: averages iRating over the player effective-class cohort; otherwise full-field average fallback)
- `Car.Ahead01P.Gap.Sec`
- `Car.Behind01P.Gap.Sec`
- `Car.Player.PaceFlagsRaw`
- `Car.Player.SessionFlagsRaw`
- `Car.Player.TrackSurfaceMaterialRaw`

Slots (Ahead01..Ahead05, Behind01..Behind05):
- Identity: `CarIdx`, `Name`, `CarNumber`, `ClassColor`, `ClassColorHex`, `ClassName`, `PositionInClass`, `IRating`, `Licence`, `SafetyRating`
- State: `IsOnTrack`, `IsOnPitRoad`, `IsValid`
- Spatial: `LapDelta`, `Gap.TrackSec`, `Gap.RelativeSec`, `Gap.RelativeSource`, `LapsSincePit`, `BestLapTimeSec`, `LastLapTimeSec`, `BestLap`, `BestLapIsEstimated`, `LastLap`, `DeltaBestSec`, `DeltaBest`, `EstLapTimeSec`, `EstLapTime`
- Derived: `ClosingRateSecPerSec`, `Status`, `StatusE`, `StatusShort`, `StatusLong`, `StatusEReason`, `HotScore`, `HotVia`
- Info banners: `InfoVisibility`, `Info` (bursted: laps-since-pit, last-lap vs best, last-lap vs me, live Δ; baseline gated to `StatusE == Unknown`)
- Raw telemetry (mode permitting): `SessionFlagsRaw`, `TrackSurfaceMaterialRaw`

Debug (`Car.Debug.*`):
- `PlayerCarIdx`, `PlayerLapPct`, `PlayerLap`, `SessionTimeSec`, `SourceFastPathUsed`
- Raw telemetry availability: `HasCarIdxPaceFlags`, `HasCarIdxSessionFlags`, `HasCarIdxTrackSurfaceMaterial`, `RawTelemetryReadMode`, `RawTelemetryFailReason`
- Slot debug: `Ahead01.CarIdx`, `Ahead01.ForwardDistPct`, `Behind01.CarIdx`, `Behind01.BackwardDistPct`
- Sanity/counters: `InvalidLapPctCount`, `OnPitRoadCount`, `OnTrackCount`, `TimestampUpdatesThisTick`, `FilteredHalfLapCountAhead`, `FilteredHalfLapCountBehind`
- Optional (debug-gated): `LapTimeEstimateSec`, `HysteresisReplacementsThisTick`, `SlotCarIdxChangedThisTick`
- Optional (debug-gated): `LapTimeUsedSec`

## Debug export (optional)
When `EnableCarSADebugExport` is enabled (and soft debug is on), CarSA writes a lightweight CSV snapshot on **every DataUpdate tick** (buffered, flushed every 20 lines or 4 KB):
- Path: `SimHub/Logs/LalapluginData/CarSA_Debug_YYYY-MM-DD_HH-mm-ss_<TrackName>.csv` (UTC timestamp, sanitized track name; repeated `_` collapsed, trimmed, clamped to 60 chars)
- Columns (grouped, validation export; expect HotLap/CoolLap extensions later):
  - **Top-level context:** `SessionTimeSec`, `SessionState`, `SessionTypeName`, `PlayerCarIdx`, `PlayerLap`, `PlayerLapPct`, `PlayerCheckpointIndexNow`, `PlayerCheckpointIndexCrossed`, `NotRelevantGapSec`.
- **Per-slot (Ahead01..Ahead05, Behind01..Behind05):** existing spatial/status fields include `CarIdx`, `DistPct`, `GapTrackSec`, `GapRelativeSec`, `GapRelativeSource`, `ClosingRateSecPerSec`, `LapDelta`, `IsOnTrack`, `IsOnPitRoad`, `StatusE`, `TrackSurfaceRaw`, and `SessionFlagsRaw`. Diagnostics-only assignment evidence adds `AssignmentMode`, `CandidatePresentThisTick`, `CandidateRankThisTick`, `RetainedByHysteresis`, `BlinkHeld`, `RetentionEligible`, `RetentionReason`, and `LapDistPctValid`. `CandidateRankThisTick` is one-based within the bounded five-car candidate array for that side (`0` when absent). `RetentionEligible` records the existing pre-assignment `TryComputeDistance` result for the previously bound identity; it does not add or change an eligibility gate. `AssignmentMode` is `live_candidate`, `retained_current`, `blink_hold`, `cleared`, or `unknown`, with `RetentionReason` distinguishing current-candidate matches, invalid/duplicate replacement, 10%-closer replacement, hysteresis retention, no-candidate retention, and blink hold. These fields observe the existing assignment decision only and do not alter candidate acquisition, cursor consumption, duplicate suppression, hysteresis, blink hold, or final slot ordering.
  - **Player tail:** `PlayerTrackSurfaceRaw`, `PlayerSessionFlagsRaw`.

## Off-track probe export (optional)
When `EnableOffTrackDebugCsv` is enabled (and a probe `OffTrackDebugProbeCarIdx` is configured), the plugin writes `OffTrackDebug_<Track>_<Timestamp>.csv` under `SimHub/Logs/LalapluginData/` with raw telemetry and latch state for the probe car. If `OffTrackDebugLogChangesOnly` is enabled, rows are written only when the snapshot changes:
- **Context:** session time/state, session flags (hex + dec), probe CarIdx, and per-car telemetry (`CarIdxTrackSurface`, `CarIdxTrackSurfaceMaterial`, `CarIdxSessionFlags`, `CarIdxOnPitRoad`, `CarIdxLap`, `CarIdxLapDistPct`).
- **Latch state:** off-track now, off-track streak, first-seen time, compromised-until lap, compromised-off-track/penalty active flags, and latch enable.
- **Player incidents:** player CarIdx, incident count, and incident delta for the tick.

## Car Tracking Probe CSV export (optional)
When Debug mode is enabled, `EnableCarTrackingProbeCsv` is enabled, and the Debug UI `START` button is pressed, the plugin writes `CarTrackingProbe_<Track>_<Timestamp>.csv` under `SimHub/Logs/LalapluginData/`. Capture frequency is bounded by `CarTrackingProbeCsvFrequencyHz` (normalized to 1–20 Hz), and rows are buffered/flushed periodically. `STOP` flushes and pauses capture; `RESET CSV` stops capture and removes the current runtime CSV file if one exists.
- **Probe targets:** the player from `PlayerCarIdx`, Probe A from `CarTrackingProbeACarIdx`, and Probe B from `CarTrackingProbeBCarIdx`.
- **Per-probe fields:** `CarIdx`, identity/class data from `DriverInfo.Drivers##` only (`Name`, `AbbrevName`, `CarNumber`, `ClassName`, `ClassColor`, `IsPaceCar` when available), raw CarIdx arrays (`CarIdxLap`, `CarIdxLapCompleted`, `CarIdxLapDistPct`, `CarIdxTrackSurface`, `CarIdxOnPitRoad`, `CarIdxClassPosition`, `CarIdxPosition`, `CarIdxBestLapTime`, `CarIdxLastLapTime`, `CarIdxSessionFlags`, `CarIdxPaceFlags`). `DriverInfo.CompetingDrivers` is intentionally not used as a normal-driver fallback for this export.
- **Player-vs-probe fields:** lap deltas from both lap bases, raw progress deltas, circular track delta percent, estimated track seconds from the diagnostic lap-time scale, same-checkpoint gap when the existing read-only CarSA checkpoint seam reports one, checkpoint validity, optional checkpoint age (blank when unavailable), selected/published gap source when the probe matches a current CarSA/Opp/H2H target, and an eligibility reason.
- **Correlation fields:** current `Car.Ahead01/Behind01`, precision gap, `Opp.Ahead1/Behind1`, `H2HTrack.Ahead/Behind`, and `H2HRace.Ahead/Behind` target/gap fields are copied read-only into the row for same-tick comparison. Slot-01 precision correlation also records the exact TrackSec authority, `RawCandidateSec` as the selected stored/proximity checkpoint candidate before local sign normalization, `ReconciledCandidateSec` as the best direction-normalized lap-adjusted candidate, acceptance tolerance, candidate source, chosen source, and reject reason for both ahead and behind.
- **Checkpoint-truth event diagnostics:** bounded read-only groups for `Car.Ahead01P`, `Car.Behind01P`, Probe A, and Probe B record sampled checkpoint progression/skip evidence, last truth source and gate provenance, target/player gate times and laps, raw/normalized/directional values, TrackSec correlation, same-tick overwrite evidence, prior-truth metadata, RelativeSec mismatch-clearing evidence, and precision candidate difference/relative-error fields where a slot-01 precision diagnostic exists. Event/progression evidence is collected only while Car Tracking Probe capture is active, latched across faster telemetry ticks, and consumed only after a Car Tracking Probe row has successfully snapshotted it, so stopped-window/pre-START events are not exposed and in-window events between lower-frequency CSV samples are not lost or repeated indefinitely; latest-event fields keep the most recent event while `*SinceSnapshot` counters aggregate multiple truth updates, same-tick overwrites, prior-truth overwrites, RelativeSec clears, checkpoint crossings, and skipped-checkpoint totals/maxima in the same capture window. Race-start latch reset clears diagnostic checkpoint timing/progression, and disabling Soft Debug invalidates pending provenance before it can be exposed after re-enable. `TruthSource` remains event provenance (`target_after_player`, `reverse_player_after_target`, `none`, or `existing/unknown`); the existing precision `CandidateSource` separately identifies `checkpoint_truth` versus `checkpoint_filtered`. Snapshot consumption clears capture-window events/counters while preserving a non-event `existing/unknown` baseline when runtime checkpoint truth is still active, keeping subsequent prior-truth metadata logically consistent. These fields observe existing checkpoint and publication decisions only; they do not interpolate crossings, arbitrate truth, alter runtime cache clearing, or change CarSA/Opponents/H2H/PitExit behavior.
- **Invariant:** this writer is a separate diagnostics-only CSV system. It does not alter CarSA slot selection, CarSA gap math, checkpoint-gap runtime semantics, Opponents ordering, H2H selectors, dashboard exports, or Property Snapshot behavior.

## Performance notes
- Single-pass candidate selection with fixed arrays (no per-tick allocations).
- No LINQ or string formatting in per-tick loops.
- Car-centric cache avoids per-slot computations for closing and status latches.

- 2026-04-30 Phase 1 League Race Class infrastructure: CarSA spatial selection/hysteresis logic remains unchanged; no effective-class selection injection in this phase.
