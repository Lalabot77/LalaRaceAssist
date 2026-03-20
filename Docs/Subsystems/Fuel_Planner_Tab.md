# Fuel Planner / Strategy Tab

Validated against commit: 3f0af12824336625dd449cc0329702ac50394396
Last updated: 2026-03-20
Branch: work

## Purpose
The Fuel Planner (surfaced as the top-level **Strategy** tab in the plugin UI) is the **human-in-the-loop planning interface** that:
- Presents a snapshot of live session state (fuel, pace, confidence).
- Allows the driver to **select authoritative planning inputs** (lap time + fuel per lap).
- Computes a **deterministic pit plan** using planner inputs, independent of live volatility.
- Exposes “readiness” gates so dashboards can safely auto-apply or inhibit live suggestions.

The planner does **not** replace the Fuel Model.  
It consumes the Fuel Model’s **live snapshot** and produces **explicit planning outputs** that are stable unless the user changes them.

Preset management remains part of the same subsystem, but it is no longer a separate top-level navigation tab. The Strategy tab keeps the inline `Race Preset` selector and opens the existing preset editor via a modal **Preset Manager** dialog beside that selector.

Canonical references:
- Fuel behaviour contract: `Docs/FuelProperties_Spec.md`
- Planner/live flow: `Docs/FuelTab_SourceFlowNotes.md`
- Export definitions: `Docs/SimHubParameterInventory.md`

---

## Scope and boundaries

This doc covers:
- Planner inputs and precedence rules.
- How live data is surfaced vs applied.
- Readiness gating and snapshot semantics.
- Planner outputs and how they relate to live fuel model outputs.

Out of scope:
- Fuel burn acceptance, stability, and projection internals (see `Fuel_Model.md`).
- Dash rendering or UI layout details (see `Subsystems/Dash_Integration.md`).

---

## Inputs (planner-side)

### User-controlled inputs
These are **authoritative** once set by the user:

- **Estimated Lap Time**
  - Manual entry (explicit numeric value).
  - PB-derived race pace.
  - Profile average.
  - Live average (if explicitly applied).

- **Fuel Per Lap**
  - Manual entry.
  - Profile average.
  - Live average.
  - Session max (for safety scenarios).

- **Fuel to Add (MFD request)**
  - Used for pit math and pit window evaluation.
  - Clamped by tank capacity.

- **Race Preset selection**
  - The Strategy tab keeps the inline preset combo as the active apply/select control.
  - The adjacent `Presets...` button opens the modal Preset Manager for create/rename/delete/edit/save-current flows without introducing a second preset-editing path.

### Track-scoped planner persistence
These planner inputs persist on the selected `TrackStats` record rather than on the car profile:
- `Wet Fuel Multiplier`
- `Race Pace Delta`
- `Fuel Contingency Value`
- contingency mode (`laps` vs `litres`)

The current car profile still owns per-car items such as refuel rate, base tank, tyre-change time, and pit-entry settings, but the four planner controls above are now **track-only**.

Planner inputs persist until explicitly changed by the user or reset by session identity change.

---

### Live snapshot inputs (read-only unless applied)
From the Fuel Model and Pace subsystem:
- Live fuel-per-lap (raw + stable).
- Fuel confidence.
- Live projection lap time.
- Tank capacity detection (live cap = MaxFuel × BoP, with BoP defaulting to 1.0).
- Pit loss estimates (lane + stop).
- Session context (race vs non-race).
- Live surface mode (wet/dry from tyre compound) and track wetness label for display.

These values **inform** the planner but do not overwrite user-selected inputs unless the user opts in.

Source of truth for snapshot flow: `FuelTab_SourceFlowNotes.md`.

---

## Internal state

### Planner-selected sources
The planner tracks *what source is currently active* for each input:

- `LapTimeSourceInfo`
  - manual / PB / profile / live
- `FuelPerLapSourceInfo`
  - manual / profile / live / max

### Race pace delta behaviour
- Each track stores a **default** race-pace delta for **Profile** planning mode.
- Loading a track does **not** force manual leader-delta mode.
- **Profile mode:** the pace-vs-leader slider remains editable, and planner saves persist the stored/default pace delta (or an explicit manual override).
- **Live Snapshot mode:** the effective planner delta follows the current live leader delta whenever live leader pace is available; entering Live Snapshot clears any manual leader-delta override instead of preserving it behind the scenes.
- **Live Snapshot fallback:** if live leader pace is unavailable, the effective leader delta falls back to `0.0` rather than reusing a stale manual or stored profile delta, so the planner does not masquerade as live while using old data.
- The old manual "use live/reset to live" recovery path is no longer part of normal leader-delta operation.

### Track-condition handling (dry vs wet)
- **Manual selection:** Drivers can explicitly switch the planner into dry or wet mode (per-track).
- **Live Snapshot sync:** When the live surface mode is known (tyre compound), Live Snapshot mode auto-switches the planner to match unless the user has manually overridden the condition.
- **Wet factor support:** If wet mode is selected but only dry profile stats exist, the planner scales dry values by the selected track's wet-factor percentage and labels the source accordingly.

### Max fuel override handling (profile vs live)
- **Profile mode:** `MaxFuelOverride` is clamped to the profile base tank (`BaseTankLitres`), remains manually editable, and the UI shows a percent-of-base-tank badge.
- **Preset max fuel input:** Preset max fuel is expressed as a **percentage of base tank** (default 100%), making presets portable across cars with different base tanks.
- **Preset application in Profile mode:** applying/selecting a preset can still set the active planner max fuel exactly as before, subject to the existing profile-side clamp.
- **Live Snapshot mode:** the planner tank basis comes only from the live detected session cap (`MaxFuel × BoP`). Manual/profile/preset max-fuel values do not remain active underneath Live Snapshot.
- **Live Snapshot UI lock:** the max-fuel slider/input is non-editable in Live Snapshot and shows the live cap when available.
- **Live Snapshot fallback:** if the live cap is unavailable, the planner uses `0` as the effective tank basis, surfaces an explicit “Live max fuel cap unavailable” validation error, and blocks strategy outputs rather than silently reusing an older profile/preset/manual value.
- **Mode transitions:** switching into Live Snapshot stores the prior Profile-mode override for later restoration but immediately stops using it as the active planner tank basis; switching back to Profile restores normal editability, restores the remembered profile value, and reapplies the selected preset (if any) under Profile-mode rules.
- **Ongoing live updates:** while Live Snapshot is active, live cap changes automatically update the planner tank basis and downstream strategy outputs, including first-stint fuel.

These are exposed to the UI so the driver can see **why** a number changed.

---

### Readiness and suggestion flags
- **IsFuelReady**
  - True only when Fuel Model stable confidence meets readiness threshold.
  - Used by dashboards and auto-apply logic to avoid premature live usage.

- **IsLiveLapPaceAvailable**
- **IsLiveFuelPerLapAvailable**
  - Indicate that live values exist, not that they should be used.

- **ApplyLiveFuelSuggestion / ApplyLiveLapSuggestion**
  - User-facing toggles.
  - Availability is tracked separately from application.

This separation is intentional and contractual.

---

## Calculation blocks (planner-side)

### 1) Source selection (authoritative precedence)
For both lap time and fuel per lap:

1. Manual entry (highest priority).
2. Explicit user action (PB / LIVE / PROFILE buttons).
3. Profile default (on load).
4. Live snapshot (suggestion only, unless applied).

At no point does live data silently override a user-selected value.

Contractual flow: `FuelTab_SourceFlowNotes.md`.

---

### 2) Planner projections
Using planner-selected lap time + fuel per lap:
- Total fuel required to finish.
- Fuel delta (surplus/deficit).
- Number of stops required by plan from the planner-feasible strategy result.
- Planner stop count and planner outputs are fully independent from the PreRace mode selector. PreRace Auto mirrors planner totals/stints when planner values are available, with runtime fallback only if planner values are unavailable. Auto delta only applies live pit-menu refuel intent when planner-required next add is greater than zero; otherwise it reuses the planner fuel-basis fallback so arbitrary pit-menu add does not mask Auto deficit.
- Dash-facing pre-race visibility uses `LalaLaunch.PreRace.Stints` instead of separate stop-count exports.
- Pit add requirement.

These values are **planner-only** and intentionally decoupled from live volatility.

Exports are prefixed or grouped to distinguish planner vs live-derived values.

---

### 3) Pit loss integration
Planner pit math incorporates:
- Fuel add time (liters × refuel rate).
- Optional tyre change time.
- Pit lane travel loss (direct or DTL-based).

Loss values are sourced from the live pit timing subsystem but applied deterministically in the planner.

---

### 4) Snapshot refresh rules
Live snapshot values update continuously, but planner-selected values:
- Only change on explicit user action.
- Or reset on session identity change.

This ensures that the planner remains **predictable mid-race**.

Additional Live Session rules:
- The Live Session panel is **live-only**. When no live samples exist, summaries render `-` rather than falling back to profile values.
- When the car/track combination changes, the live snapshot is cleared immediately to prevent stale values (including max-fuel displays).

---

## Outputs (exports + UI bindings)

### Key planner outputs
(See `SimHubParameterInventory.md` for full list.)

Typical outputs include:
- Planner fuel required to end.
- Planner fuel delta.
- Planner pit stop count.
- Planner pit add amount.
- Planner projected pit loss.

These are distinct from live fuel model outputs and are safe to use for:
- Strategy displays.
- Dash overlays.
- Pre-race planning via `LalaLaunch.PreRace.*` adapter outputs.

---

### UI indicators
- Source labels (manual / live / profile / PB).
- Confidence/readiness indicators.
- Live suggestion availability markers.

These indicators exist to explain behaviour, not just decorate the UI.

---

## Dependencies / ordering assumptions
- Fuel Model must update before planner snapshot refresh.
- Pace subsystem must update before lap-time suggestions are valid.
- Tank capacity detection must be resolved before planner pit math is trusted.

---

## Reset rules

Planner state resets on:
- Session identity change (new session token).
- Car or track change.

On reset:
- Planner-selected sources revert to profile defaults.
- Manual overrides are cleared.
- Live snapshot is rehydrated but not applied.
- Live Snapshot UI fields clear to `-` until live samples populate.

Reset semantics are shared with the Fuel Model and documented centrally in:
`Docs/Reset_And_Session_Identity.md`.

---

## Failure modes / edge cases

- **Low confidence live data**
  - Live values may appear but `IsFuelReady == false`.
  - Planner must not auto-apply.

- **Tank capacity unknown**
  - Planner pit math may be inhibited or flagged.
  - UI should show explicit error/unknown state.

- **Replay sessions**
  - Live availability flags may flicker.
  - Planner stability should be verified via logs.

- **User confusion**
  - Most “why didn’t LIVE apply?” questions are explained by:
    - readiness gate,
    - source precedence,
    - or explicit manual override.

---

## Test checklist

1. **Pre-race load**
   - Planner defaults to profile lap time and fuel.
   - Live snapshot visible but not applied.

2. **Live suggestion gating**
   - Early laps: live values appear but `IsFuelReady == false`.
   - Once confidence rises: `IsFuelReady == true`.

3. **Explicit LIVE apply**
   - Press LIVE for lap time or fuel.
   - Source label updates.
   - Planner outputs recompute deterministically.

4. **Manual override**
   - Enter manual value.
   - Live suggestions no longer override.

5. **Session reset**
   - Change subsession/session.
   - Planner resets cleanly with no stale values.

---

## TODO / VERIFY

- TODO/VERIFY: Confirm exact confidence threshold used to set `IsFuelReady` and whether it differs for lap time vs fuel.  
- TODO/VERIFY: Confirm whether planner pit loss always prefers DTL or falls back to direct lane loss when DTL unavailable.  
- TODO/VERIFY: Confirm which planner outputs are exported as `_S` (smoothed) vs numeric only.
