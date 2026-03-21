# User Guide

This guide is the central driver-facing overview for Lala Race Assist Plugin. It explains what the plugin owns, what the driver sees, and where to find the detailed user pages for each system.

> **Scope note:** The future/global message system is not operational yet and is intentionally not documented here as an active user feature. Radio Messages can still be treated as current dashboard behavior where supported.

## 1. How to read the docs

Use this page as the overview, then jump to the dedicated pages for the systems you actively use:

- [Quick Start](Quick_Start.md)
- [Dashboards](Dashboards.md)
- [Strategy System](Strategy_System.md)
- [Shift Assist](Shift_Assist.md)
- [Launch System](Launch_System.md)
- [Rejoin Assist](Rejoin_Assist.md)
- [Pit Assist](Pit_Assist.md)
- [H2H System](H2H_System.md)
- [Profiles System](Profiles_System.md)
- [Fuel Model](Fuel_Model.md)

## 2. Plugin vs dashboard responsibility

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

Dashboards do **not** become the source of truth for strategy, fuel math, H2H selection, saved learning, or persistence.

## 3. Current plugin navigation

The current top-level plugin order is:

1. **Strategy**
2. **Profiles**
3. **Dash Control**
4. **Launch Analysis**
5. **Settings**

Important workflow rules:

- The main planning tab is **Strategy**, not Fuel.
- There is **no separate Presets tab**.
- Presets are managed from **Strategy** using the **`Presets...`** modal flow.
- Launch controls belong under **Settings → Launch Settings**.
- **Launch Analysis** remains the separate post-run review tab.

## 4. The main user-facing systems

### Strategy

Strategy is the main planning workflow. It lets you choose between stable profile/manual planning and live-driven snapshot planning without confusing those two jobs. See [Strategy System](Strategy_System.md).

### Profiles

Profiles are the plugin’s long-term memory for car, track, and condition-specific data. They are what make the other systems become trustworthy over time. See [Profiles System](Profiles_System.md).

### Fuel model

The fuel model learns gradually, builds confidence, and feeds Strategy with a trustworthy burn basis. See [Fuel Model](Fuel_Model.md).

### Dashboards

Dashboards are the display layer. They show outputs, visibility states, and context, but they do not own the calculations underneath. See [Dashboards](Dashboards.md).

### Shift Assist

Shift Assist gives RPM-based driver cues and becomes more trustworthy once its learned values are stable and locked. See [Shift Assist](Shift_Assist.md).

### Launch

Launch setup is handled in Settings, while Launch Analysis is used afterwards to review saved starts. See [Launch System](Launch_System.md).

### Rejoin and pit aids

Lala Race Assist Plugin includes separate driver-facing pages for recovery/rejoin support and pit-lane support:

- [Rejoin Assist](Rejoin_Assist.md)
- [Pit Assist](Pit_Assist.md)

### H2H

H2H is a read-only race-context aid that helps the driver compare race-order and local-track threats without becoming a separate planning workflow. See [H2H System](H2H_System.md).

## 5. Trust model

A good mental model for the whole plugin is:

**learn → validate → lock → trust**

Use that pattern for:

- profile values,
- fuel burn and pace baselines,
- pit-loss and marker data,
- Shift Assist targets,
- launch defaults,
- assist thresholds that repeatedly affect what you see while driving.

If a system is wrong once, keep driving. If it is wrong repeatedly, review the saved data or thresholds behind it instead of assuming the dashboard art is the problem.

## 6. Practical best practice

- Start each new car/track combination by gathering clean laps.
- Use **Strategy** as the single planning entry point.
- Keep the difference between **live observation** and **stable planning** clear in your workflow.
- Lock only values that have settled and make sense.
- Use **Dash Control** for visibility and presentation, not to fix calculation logic.
- Review profile-backed data when a driver aid is repeatedly wrong.

## 7. Recommended reading order

For most drivers, this order works well:

1. [Quick Start](Quick_Start.md)
2. [Strategy System](Strategy_System.md)
3. [Profiles System](Profiles_System.md)
4. [Fuel Model](Fuel_Model.md)
5. [Dashboards](Dashboards.md)
6. Then the feature pages you use most: [Shift Assist](Shift_Assist.md), [Launch System](Launch_System.md), [Rejoin Assist](Rejoin_Assist.md), [Pit Assist](Pit_Assist.md), and [H2H System](H2H_System.md)
