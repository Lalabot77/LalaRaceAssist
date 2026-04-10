# Pit Entry Assist & Deceleration Capture

**Subsystem doc**

Validated against commit: HEAD
Last updated: 2026-04-10
Last reviewed: 2026-01-14
Branch: work

## Purpose
- **Pit Entry Assist** gives a driver-facing cue to hit pit speed at the entry line using distance, speed, and a constant-decel model. It publishes both dash-friendly properties and structured logs for post-run analysis.
- **Deceleration Capture (DecelCap)** is a developer-only instrumentation tool to empirically measure achievable braking decel per car; it is disabled by default and not part of runtime behaviour.

Canonical exports: `Docs/Internal/SimHubParameterInventory.md`
Canonical logs: `Docs/Internal/SimHubLogMessages.md`

---

## Pit Entry Assist — Overview

### Activation & deactivation
- **Arms when:**
  - `PitPhase.EnteringPits`, **OR**
  - Pit limiter **ON** **and** overspeed > **+2 kph** (`Pit.EntrySpeedDelta_kph`), **OR**
  - Pit screen is active (auto or manual) **and** you are **on track** within **≤500 m** of the line (manual arming path).
- **Deactivates when:**
  - Driver enters pit lane (`IsInPitLane`), or
  - Arming conditions drop (limiter off / no overspeed / no entry phase), or
  - Inputs become invalid (missing pit speed, distance >500 m, etc.).

### Flow (high level)
1. Assist arms (conditions above).
2. Braking guidance recomputes every tick (distance, required distance, margin, cue).
3. Driver crosses pit entry line → `ENTRY LINE` log fires once with debrief/time-loss fields.
4. Assist ends (pit entry or disarm).

---

## Manual arming via pit screen

Pit Entry Assist can be forced on early (without limiter or pit phase) by **manually enabling the pit screen**:
- The pit screen toggle enables a **manual** mode on track.
- While manual mode is active, the assist arms as soon as the raw pit-entry distance is **≤500 m** and you are **not yet in the pit lane**.
- Manual arming still uses the **same cue logic and decel/buffer settings**; it simply bypasses the phase/limiter gate so you can practice or pre-arm earlier.

If the pit screen is dismissed or resets to auto, manual arming immediately falls away.

---

## Pit Entry Assist — Core Calculations

- **Distance to pit entry (`Pit.EntryDistanceToLine_m`):**
  - Source of truth: stored track markers (entry pct + cached track length) when valid for the current track.
  - Legacy Extra Properties fallbacks for pit-entry distance/pct are removed.
  - If stored markers are unavailable/invalid for the active track/session, assist stays off and emits a warning log (one-time until markers become valid again).
  - Clamped to **0–500 m** working window; assist resets if ≥500 m or invalid.

- **Speed delta (`Pit.EntrySpeedDelta_kph`):** Current speed − pit speed limit (session pit speed, fallback to iRacing extra).

- **Required braking distance (`Pit.EntryRequiredDistance_m`):** Constant-decel model: `(v² − vTarget²) / (2 × decel)` when above pit speed. Uses per-profile decel (clamped 5–25 m/s²).

- **Margin (`Pit.EntryMargin_m`):** `distanceToLine − requiredDistance`. Positive = early/room, negative = late.

- **Profile parameters:**
  - `Pit.EntryDecelProfile_mps2` and `Pit.EntryBuffer_m` come from the active car profile (Dash tab sliders). Both are clamped to sane ranges when used.

---

## Pit Entry Cue System

Cue level is derived from **margin vs. buffer** (buffer = profile slider):

| Cue | Text | Meaning |
| --- | --- | --- |
| 0 | OFF | Assist inactive / disarmed |
| 1 | OK | Plenty of margin |
| 2 | BRAKE SOON | Inside buffer window |
| 3 | BRAKE NOW | Immediate braking required (≤0 margin) |
| 4 | LATE | Cannot make pit speed at target decel (margin < −buffer) |

- **Logic:** `margin < -buffer → LATE; margin ≤ 0 → BRAKE NOW; margin ≤ buffer → BRAKE SOON; else OK`.
- **Dash string:** `Pit.EntryCueText` mirrors the cue value with dash-friendly text tokens.
- **Dash visuals are independent:** cue selection does not dictate how the dash renders the marker/indicator.

---

## Dash Integration Guidance

- Use **`Pit.EntryMargin_m`** as the **primary continuous signal**.
- Recommended mapping: **vertical sliding marker** on a fixed scale (e.g., ±150 m) rather than buffer-normalised scaling; centre line = margin ≈ 0 (ideal brake point).
- Marker interpretation:
  - **Up** = early / brake less.
  - **Down** = late / brake more.
- Dash Studio tips:
  - Expressions are simple → avoid heavy clamping/branching that causes stepped motion.
  - Force floating-point math with decimal literals (e.g., `150.0`).
  - Keep cue text (`Pit.EntryCueText`) available as a secondary label if desired.

---

## Pit Entry Assist Logging

Three structured INFO logs (edge-triggered):

- **`ACTIVATE`** — once when assist arms. Fields: raw + guided distance, required distance, margin, speed delta, decel, buffer, cue. Used to confirm arming context and baseline margin.
- **`ENTRY LINE SAFE/NORMAL`** — once on pit lane entry when you are at/below the pit limit. Includes speed delta, first compliant distance (if captured), and **time loss vs limiter** based on the compliance distance. Used to evaluate braking timing and track-specific tuning.
- **`ENTRY LINE BAD`** — once on pit lane entry when still above the limit. Includes speed delta and how late you braked (metres). Time loss is omitted/zero in this case.
- **`END`** — once when assist disarms (pit entry or invalidation). Used to confirm clean teardown.

---

## Pit Entry Line Debrief Outputs

On the pit-entry line, the subsystem latches a **debrief** that dashboards or telemetry overlays can show:
- **`Pit.EntryLineDebrief`**: `safe`, `normal`, or `bad`.
- **`Pit.EntryLineDebriefText`**: plain-English summary string (includes time loss when computed).
- **`Pit.EntryLineTimeLoss_s`**: seconds lost versus the pit limiter based on the distance from the first compliant point to the line.

These values update **once per pit entry** and remain until the next assist activation.

---

## Deceleration Capture (DecelCap)

- Developer-only instrumentation to empirically measure braking decel between **200→50 kph** with straight-line filtering.
- **Master switch:** `MASTER_ENABLED` (constant, default `false`). When false, module is inert at runtime and safe to ship compiled.
- When explicitly enabled/armed, it logs high-frequency decel samples (dv/dt and lon accel) and distance between 200–50 kph for the current car/track/session token. START/END logs bracket each run; per-tick logs emit at 20 Hz.
- **Not part of normal behaviour:** requires explicit enable + toggle; otherwise no logs, no side effects.

---

## Configuration & Profiles

- **Per-car profiles (Dash tab):**
  - **Pit Entry Decel (m/s²):** `Pit.EntryDecelProfile_mps2` used in required-distance calc.
  - **Pit Entry Buffer (m):** `Pit.EntryBuffer_m` used in cue thresholds.
- **Why per-car:** braking capability and tyre/booster effects differ by car; per-class is too coarse for consistent pit entry cues.
- **Recommended starting points:**
  - **GT3:** decel ≈ **14 m/s²**, buffer **≈15 m**.
  - **GTP:** similar decel, slightly **higher buffer** for hybrid regen variability.
- Defaults auto-seed on profile creation/copy; adjust per track after reviewing `ENTRY LINE` logs.
