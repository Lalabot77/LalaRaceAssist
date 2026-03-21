# Strategy System

This page explains the user-facing strategy workflow.

## 1. What Strategy is

**Strategy** is the main planning tab and the single entry point for race planning.

It brings together:

- profile data,
- track data,
- live session snapshot data,
- deterministic planner outputs,
- preset management,
- PreRace display support.

## 2. Strategy is not the same as the live fuel model

The plugin keeps a deliberate split between:

- the **live fuel/pace systems**, which observe the current session and build confidence, and
- the **planner**, which creates a stable race plan from selected inputs.

This matters because stable planning should not drift every lap just because live conditions are still settling.

## 3. Planning sources

### Profile/manual planning

Use this when you want a stable plan built from:

- saved track values,
- saved profile values,
- manually chosen planning inputs.

This is the right mode when you want predictability.

### Live Snapshot

Use this when you want the planner to follow the current session snapshot.

Live Snapshot can auto-drive the relevant values when valid live data exists. When those live values are active, the matching manual controls are disabled.

There are no old “Use Live” buttons or toggles in the current documented workflow.

## 4. What Live Snapshot means in practice

Live Snapshot is about **using the current session as the source**.

That includes things like:

- live lap-time context,
- live fuel-per-lap context,
- live confidence,
- live tank-cap information,
- live pace-versus-leader behavior when available.

The important user rule is this:

> Live Snapshot helps you observe and adapt to the session, but it does not erase the conceptual difference between live observation and a stable plan.

## 5. Why confidence matters

The plugin does not blindly trust every lap.

Confidence exists because:

- pit laps are different,
- incident laps can poison the data,
- early laps are often noisy,
- wet and dry conditions need separate treatment.

Until confidence builds, profile values may still be the safer planning basis.

## 6. Presets

Presets are now part of the Strategy workflow.

### Current preset flow

- Choose a race preset from Strategy.
- Open **`Presets...`** to create, rename, edit, save, or delete presets.
- Return to Strategy to apply and use them.

There is no separate top-level Presets tab.

### What presets are good for

Use presets for repeatable race assumptions such as:

- laps vs timed race format,
- contingency,
- tyre-change time,
- max-fuel assumption,
- preferred PreRace mode.

## 7. Track-scoped planning values

Some planning values belong to the current track/layout record rather than the whole car profile.

That helps prevent carrying the wrong assumptions between venues.

Examples include:

- wet multiplier,
- pace delta to leader,
- fuel contingency settings.

## 8. PreRace

PreRace belongs under the Strategy story because it helps the driver understand the planned situation before the start.

But its role is intentionally limited:

- it is **display-oriented**,
- it is for **on-grid/pre-start guidance**,
- it does **not** replace planner calculations,
- it does **not** change the live fuel model.

Whenever you see PreRace mentioned, keep that boundary in mind.

## 9. What users should trust

Once the system has enough clean data, users should usually trust:

- the planner outputs,
- the stable fuel basis,
- the pit-loss basis,
- the saved track data they have validated and locked.

If strategy looks repeatedly wrong, the usual causes are:

- poor live confidence,
- stale or bad saved track data,
- a profile value that was locked too early,
- a mode mismatch between live observation and stable planning intent.

## 10. Practical workflow

### Before the race

- Check the selected profile and track.
- Pick the right preset if you use presets.
- Decide whether you want Profile/manual planning or Live Snapshot.
- Confirm tank assumptions and contingency look sensible.
- Use PreRace for on-grid context only.

### During the race

- Let the plugin keep building live confidence.
- Avoid constant planner tweaking unless something is clearly wrong.
- Use the strategy displays as stable guidance, not as a reason to chase every lap.

### After the race

- Review anything that looked repeatedly wrong.
- Relearn only the affected saved values when needed.
- Keep good locks in place once validated.
