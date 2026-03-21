# User Guide

This guide explains the supported user-facing parts of Lala Race Assist Plugin and the dashboard package.

> **Scope note:** The global message system is not operational yet and is intentionally not documented here as a user feature. Radio Messages can still be treated as a functional dash feature where the current dashboards expose them.

## 1. Responsibility split

### Plugin

The plugin:

- learns data,
- stores data,
- protects data,
- performs calculations,
- publishes outputs for dashboards.

### Dashboards

Dashboards:

- display those outputs,
- provide situational awareness,
- provide limited interaction through bindings or touch areas.

Dashboards do **not** become the source of truth for strategy, fuel math, H2H selection, learned data, or persistence.

## 2. Current top-level navigation

The current plugin order is:

1. **Strategy**
2. **Profiles**
3. **Dash Control**
4. **Launch Analysis**
5. **Settings**

Important changes reflected in this repo guide:

- The main planning tab is **Strategy**, not Fuel.
- There is **no separate Presets tab**.
- Presets are managed from **Strategy** using the **`Presets...`** modal flow.
- Launch-related controls now belong under **Settings → Launch Settings**.

## 3. Strategy workflow

Strategy is the main planning workflow.

### Profile/manual vs Live Snapshot

There are two user-facing planning states to understand:

- **Profile/manual planning** uses your selected profile, track values, and chosen planner inputs.
- **Live Snapshot** follows the live session snapshot when those inputs are available.

Key rule: **watching live state is not the same as committing a stable plan**.

The plugin keeps this distinction intentionally:

- Live Snapshot helps you follow the current session.
- Profile/manual planning gives you a stable strategy basis.

When live control is active, the matching manual controls are disabled. Older “Use Live” wording is no longer part of the normal flow.

### Presets

Presets are now managed inside Strategy.

Use them for repeatable race setup values such as:

- race length mode,
- contingency,
- tyre-change time,
- max-fuel assumptions,
- PreRace mode preference.

### PreRace

PreRace is part of the Strategy story, but it remains **display-oriented only**.

Use it for on-grid or pre-start information. Do **not** treat it as planner logic or live fuel-model logic.

## 4. Profiles and saved data

Profiles are the plugin’s long-term memory.

### Car profile

Car-level storage includes things such as:

- launch defaults,
- pit-entry settings,
- base tank assumptions,
- rejoin and assist thresholds,
- Shift Assist settings.

### Track record

Track/layout-level storage includes things such as:

- pit-lane loss,
- entry/exit markers,
- dry/wet pace and fuel data,
- track-scoped planning defaults.

### Locking philosophy

The normal lifecycle is:

**learn → validate → lock → trust**

Lock good values once they are representative. If something becomes unreliable, unlock only the affected item, relearn it cleanly, then lock again.

## 5. Dash Control

Dash Control is now strictly dash-oriented.

### Main bindings to care about

- **Cancel Message**
- **Toggle Pit Screen**
- **Primary Dash Mode**
- **Declutter Mode**

### Global Dash Functions

The grouped user-facing structure is:

- **General**
- **Dark Mode**
- **Lovely integration**
- **Fuel**

Keep these thought of as display/consumption controls, not core runtime logic.

### What is no longer part of Dash Control

Do not look here for:

- launch mode,
- post-launch display time,
- event marker debug actions as a normal dash feature.

Launch controls belong in **Settings → Launch Settings**.

## 6. Launch settings and launch analysis

Launch behaviour is configured in **Settings → Launch Settings**. For the user-facing launch workflow and tuning habits, see [Launch System](Launch_System.md).

Typical launch controls include:

- launch mode binding,
- post-launch results display time,
- launch targets and tolerances,
- launch summary and trace capture locations,
- launch logging options.

**Launch Analysis** is the separate top-level tab used to review saved launch traces and summaries. Keep live launch setup in Settings, then use Launch Analysis for post-run review.


## 7. Shift Assist

Shift Assist is a driver aid for RPM-based shift cueing. It can provide a **Shift Sound**, **Shift Light**, and an additional urgent reminder if you stay in the gear too long after the main cue.

Use it to improve shift timing consistency, then review learned values and lock gears once they are stable. It is still a cueing aid, not an automatic shift feature.

For setup and first-use guidance, see [Shift Assist](Shift_Assist.md).

## 8. Dashboards

The supported dashboard package normally includes:

- a **primary race dash**,
- a **strategy/support dash**,
- an **overlay**,
- optional **Lovely-based layouts**.

Use visibility settings, primary mode, and declutter mode to decide what appears where. The dashes display outputs; they do not own calculations.

For details, see [Dashboards](Dashboards.md).

## 9. H2H and race context

H2H is a read-only race-context aid for the driver.

- **H2H Race** compares the same-class targets ahead and behind in race order.
- **H2H Track** compares the same-class cars immediately around you on track.

Use it for awareness and comparison, not as something you constantly configure.

For details, see [H2H System](H2H_System.md).

## 10. Rejoin, pit entry, and pit popups

The plugin includes practical driver aids for:

- rejoin warnings,
- pit popups,
- pit entry assist.

Trust them most when your saved markers, pit-loss values, and thresholds are known-good.

If a popup is wrong once, cancel or override it and keep driving. If it is wrong repeatedly, review the underlying saved data or threshold settings.

For details, see [Rejoin and Pit Assists](Rejoin_And_Pit_Assists.md).

## 11. Practical best practice

- Start each new car/track combination by gathering clean laps.
- Validate pit-loss and track markers before locking them.
- Use Strategy as the single planning entry point.
- Keep the distinction between **live observation** and **stable planning** clear in your own workflow.
- Use Dash Control for presentation and visibility, not to “fix” calculation logic.
- Review profile values when a driver aid is repeatedly wrong.
