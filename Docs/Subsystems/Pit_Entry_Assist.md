# Pit Entry Assist & Deceleration Capture

**Subsystem doc**

Validated against commit: HEAD
Last updated: 2026-06-11
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
  - pre-line arming conditions drop (limiter off / no overspeed / no entry phase / manual pit screen inactive), or
  - pre-line inputs become invalid (missing pit speed, distance >500 m, missing/invalid markers), or
  - after crossing the pit-entry line, the post-line hold hands over once the player is in pit lane and the existing Pit Box distance seam reports `Pit.Box.DistanceM < 200m`.

### Flow (high level)
1. Assist arms (conditions above).
2. Braking guidance recomputes every tick (distance, required distance, margin, cue).
3. Driver crosses pit entry line → `ENTRY LINE` log fires once with debrief/time-loss fields.
4. Post-line speed-settling text remains available while on pit lane until handover to the Pit Box widget (`Pit.Box.DistanceM < 200m`).
5. During post-line hold, live braking geometry (`Pit.EntryDistanceToLine_m`, `Pit.EntryRequiredDistance_m`, `Pit.EntryMargin_m`, `Pit.EntryCue`) is neutralized while speed-settling text remains active.
6. Handover turns off active cue outputs but preserves the latched entry-line evidence (`Pit.EntryLineDebrief*`, `Pit.EntrySpeedDelta_kph`, and late-distance evidence) long enough for Pit Debrief consumption.
7. Assist ends after handover or disarm.

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

- **Speed delta (`Pit.EntrySpeedDelta_kph`):** Current speed − pit speed limit (native session pit speed only).

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
- **Dash state string:** `Pit.EntryCueText` mirrors the cue value with stable state tokens only (`OFF`, `OK`, `BRAKE SOON`, `BRAKE NOW`, `LATE`). It intentionally does not special-case below-limit speed.
- **Driver-facing brake string:** `Pit.EntryBrakeCueText` is the preferred direct text label for dashboards that want plugin-owned wording. It uses only `OFF`, `FAULT`, `READY`, `BRAKE IN Xm`, `BRAKE NOW`, `BRAKE HARD`, `SLOW DOWN`, `SPEED OKAY`, `BELOW LIMIT`, and `TOO SLOW`; it intentionally does not emit legacy `BRAKE SOON` or `LATE`.
- **Driver-facing brake state:** `Pit.EntryBrakeCueState` is the numeric companion for dash colours/animation: `0=OFF`, `1=FAULT`, `2=READY`, `3=BRAKE IN`, `4=BRAKE NOW`, `5=BRAKE HARD`, `6=SLOW DOWN`, `7=SPEED OKAY`, `8=BELOW LIMIT`, `9=TOO SLOW`.
- **Dash visuals are independent:** cue selection does not dictate how the dash renders the marker/indicator.


### Driver-facing BrakeCue contract

`Pit.EntryBrakeCueText` / `Pit.EntryBrakeCueState` are phase-specific and do not change braking-distance maths or legacy cue tokens. Before the real pit-entry line, distance wording treats the configured buffer point as the fake brake line and never switches to speed-settling just because the fake/buffer line has been passed. After the real pit-entry line, only speed-settling states are published and live braking geometry is neutralized. The countdown distance is `brakeInM = max(0, Pit.EntryMargin_m - Pit.EntryBuffer_m)`, so a 315m margin with a 15m buffer shows `BRAKE IN 300m`, and a 15m margin with a 15m buffer shows `BRAKE NOW`.

| State | Text | Meaning |
| --- | --- | --- |
| 0 | OFF | Assist inactive / disarmed |
| 1 | FAULT | Pit-entry marker, pit speed, track position, track length, or distance solution is invalid/unavailable |
| 2 | READY | Valid and armed, above pit speed, outside the 300m countdown window before the fake brake line |
| 3 | BRAKE IN Xm | Above pit speed, inside the countdown window, before the fake brake line |
| 4 | BRAKE NOW | Reached the configured buffer/fake brake line (`margin <= buffer && margin >= 0`) |
| 5 | BRAKE HARD | Negative margin; inside required braking distance |
| 6 | SLOW DOWN | Post-line only; speed delta is above the pit limit by more than about +2kph, with hysteresis |
| 7 | SPEED OKAY | Post-line only; speed delta is in the settled band (about `-10kph..+2kph`, with hysteresis) |
| 8 | BELOW LIMIT | Post-line only; speed delta is below the limit but not too slow (about `< -10kph` and `>= -20kph`, with hysteresis) |
| 9 | TOO SLOW | Post-line only; speed delta is far below the limit (about `< -20kph`, with hysteresis) |

Post-line speed-status hysteresis keeps the previous speed band when speed delta is within about 1kph of the `+2`, `-10`, or `-20` kph boundary, and resets on assist/session reset. The immediate post-line pit-box handover reset is intentionally cue-only: it disables the active Pit Entry surface without clearing latched entry-line evidence before Pit Debrief can consume the current-stop speed delta, late distance, debrief token/text, time loss, and serial.

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
  - Use `Pit.EntryBrakeCueText` for a direct driver-facing text label when wanted.
  - Use `Pit.EntryBrakeCueState` for colours/animations instead of parsing text.
  - Use `Pit.EntryCueText` only when the widget wants the stable legacy cue-state token and will compose its own wording.

---

## Pit Entry Assist Logging

Three structured INFO logs (edge-triggered):

- **`ACTIVATE`** — once when assist arms. Fields: raw + guided distance, required distance, margin, speed delta, decel, buffer, cue. Used to confirm arming context and baseline margin.
- **`ENTRY LINE SAFE/NORMAL`** — once on pit lane entry when you are at/below the pit limit, or when line overspeed is within the named **+1.0 kph debrief tolerance**. At/below-limit entries include speed delta, first compliant distance (if captured), and **time loss vs limiter** based on the compliance distance. Marginal overspeed entries keep the existing `normal` token and state that the speed delta is within the 1.0 kph margin. Used to evaluate braking timing and track-specific tuning.
- **`ENTRY LINE BAD`** — once on pit lane entry when still more than +1.0 kph above the limit. Includes speed delta and how late you braked (metres) only when the rounded late distance is meaningful; it no longer says `braked 0.0m too late`. The strict at/below-limit path remains `Pit.EntrySpeedDelta_kph <= 0.0`; the separate first-compliant capture uses `<= +1.0 kph` as the compliance/marginal tolerance and as the early-limiter time-loss reference. Time loss is omitted/forced to zero in true bad/overspeed cases because no valid positive early-limiter time-loss estimate exists.
- **`END`** — once when assist disarms (pit entry or invalidation). Used to confirm clean teardown.

---

## Pit Entry Line Debrief Outputs

On the pit-entry line, the subsystem latches a **debrief** that dashboards or telemetry overlays can show:
- **`Pit.EntryLineDebrief`**: `safe`, `normal`, or `bad`. The export contract is intentionally unchanged: overspeed within +1.0 kph maps to `normal` rather than adding a new token.
- **`Pit.EntryLineDebriefText`**: plain-English summary string (includes time loss when computed). For marginal overspeed it reports `NORMAL entry: Speed Δ at line +0.3kph within 1.0kph margin.`
- **`Pit.EntryLineTimeLoss_s`**: raw double seconds lost versus the pit limiter based on the distance from the first compliant/marginal point (`Pit.EntrySpeedDelta_kph <= +1.0`) to the line, calculated as `max(0, distance/currentLineSpeed - distance/pitLimitSpeed)`. `0` in a `bad` line verdict means no valid positive time-loss estimate was computed, not that the line-speed compliance verdict was good.

These values update **once per pit entry** and remain until the next assist activation.

---

## Deceleration Capture (DecelCap)

- Developer-only instrumentation to empirically measure braking decel between **200→50 kph** with straight-line filtering.
- **Master switch:** `MASTER_ENABLED` (constant, default `false`). When false, module is inert at runtime and safe to ship compiled.
- When explicitly enabled/armed, it logs high-frequency decel samples (dv/dt and lon accel) and distance between 200–50 kph for the current car/track/session token. START/END logs bracket each run; per-tick logs emit at 20 Hz.
- **Not part of normal behaviour:** requires explicit enable + toggle; otherwise no logs, no side effects. It remains diagnostic-only and is not wired into runtime Pit Entry Line Debrief classification.

---

## Configuration & Profiles

- **Per-car profiles (CAR tab, visually grouped under Pit Stop / Pit Assist):**
  - **Pit Entry Decel (m/s²):** `Pit.EntryDecelProfile_mps2` used in required-distance calc.
  - **Pit Entry Buffer (m):** `Pit.EntryBuffer_m` used in cue thresholds.
  - These controls may appear near pit service fields, but Pit Entry Assist retains calculation ownership.
- **Why per-car:** braking capability and tyre/booster effects differ by car; per-class is too coarse for consistent pit entry cues.
- **Recommended starting points:**
  - **GT3:** decel ≈ **14 m/s²**, buffer **≈15 m**.
  - **GTP:** similar decel, slightly **higher buffer** for hybrid regen variability.
- Defaults auto-seed on profile creation/copy; adjust per track after reviewing `ENTRY LINE` logs.

## Pit Debrief consumption

Pit Debrief consumes existing Pit Entry Assist readouts after a completed stop. `Pit.EntryLineDebrief` remains the assist verdict (`safe`/`normal`/`bad`); marginal line overspeed within +1.0 kph is classified as `normal` to preserve the existing token contract and maps to `Pit.Debrief.Entry.LimiterQualityText=NORMAL`. True bad overspeed above +1.0 kph still maps to `Pit.Debrief.Entry.LimiterQualityText=POOR` for debug/log evidence and now drives both `Pit.Debrief.Entry.QualityText=BAD` and the compact entry headline to `ENTRY BAD`, using plugin-owned structured speed-delta and late-distance evidence such as `ENTRY BAD (+7.9kph / 3.3m late)` instead of the timing-loss bracket; the override requires current-stop bad-entry evidence (fresh line verdict plus speed above the +1.0 kph tolerance or meaningful late distance), stale previous-stop bad tokens are ignored, and once current evidence is captured, that evidence is preserved against later pit-entry-assist reset/default refresh values during the same debrief collection. `Pit.Debrief.Entry.SummaryText` is the plugin-owned entry-only first section of full `Pit.Debrief.SummaryText`; both are refreshed from the same entry-section formatter so staged dashboard pit-entry messages do not need to reconstruct the wording. For `safe`/`normal` compliance tokens, `Pit.Debrief.Entry.QualityText` and the `ENTRY ... (Δ ...)` headline remain performance-oriented from the raw double `Pit.EntryLineTimeLoss_s`: `>0.5s` is `POOR`, `>0.1s` is `NORMAL`, and `≤0.1s` is `GOOD`. This preserves the useful performance/readout separation for legal or within-margin entries while preventing a genuine bad line-speed compliance result with `0.0s`/no-positive-estimate time loss from appearing as `ENTRY GOOD` in either `Pit.Debrief.Entry.QualityText` or the driver-facing summaries. `Pit.Debrief.Entry.LineTimeLossSec` remains timing/performance only and may remain `0.0` for bad compliance entries when no positive timing estimate exists; it is not faked from compliance evidence.

The v1 debrief does not infer an actual deceleration quality. `Pit.Debrief.Entry.DecelQualityText` remains `UNKNOWN` until an existing authoritative actual-decel source is available and explicitly wired by a later task.
