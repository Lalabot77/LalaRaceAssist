# Quick Start

This guide gets Lala Race Assist Plugin working quickly with trustworthy data. For the full overview afterwards, continue to the [User Guide](User_Guide.md).

## 1. What the plugin is for

Lala Race Assist Plugin is a SimHub plugin for **iRacing** that helps the driver with:

- stable race **Strategy** planning,
- dashboard-driven situational awareness,
- profile-backed **fuel** and **pace** learning,
- **Shift Assist** and **Launch** support,
- **Rejoin** and **Pit** assists,
- **H2H** race-context comparisons.

## 2. Install

Copy the plugin files into your SimHub installation, then restart SimHub.

### Required for now

- `LaunchPlugin.dll`
- `RSC.iRacingExtraProperties.dll`

## 3. Import dashboards

Typical layout:

- **Primary race dash** for the main driver view
- **Strategy/support dash** for planning and context
- **Overlay** for compact alerts
- **Lovely-based layouts** if you use the Lovely ecosystem
- **Head 2 Head** for 6 sector 'How goes it' during racing - ideal use is as a simhub overlay on screen

Dashboards display plugin outputs. They do not learn data or replace the plugin as the source of truth. For dashboard-specific guidance, see [Dashboards](Dashboards.md).

## 4. First plugin check

Open the plugin in SimHub and confirm the main navigation order:

1. **Strategy**
2. **Profiles**
3. **Dash Control**
4. **Launch Analysis**
5. **Settings**

Important current rules:

- **Strategy** is the main planner.
- Presets are managed from **Strategy** through **`Presets...`**.
- Launch setup lives in **Settings → Launch Settings**.

## 5. Best first session

For a new car/track combination:

1. Start with **Strategy** and confirm the selected profile and track.
2. Drive a few clean laps so the plugin can begin learning fuel, pace and pit entry/exit points.
3. Review **Profiles** before locking anything.
4. Import the dashboards you actually plan to use.
5. Add only the core controls you need at first:
   - **Cancel Message**
   - **Toggle Pit Screen**
   - **Primary Dash Mode**
   - **Declutter Mode**

## 6. What to trust early vs later

Early in a combo, treat the plugin as **learning**. Once you have clean representative data, move toward the normal pattern:

**learn → validate → lock → trust**

That applies especially to:

- profile values,
- fuel model confidence,
- pit-loss and marker data,
- Shift Assist learning,
- any assist that depends on saved thresholds or track markers.

## 7. Where to go next

- [User Guide](User_Guide.md) for the full driver-facing overview
- [Strategy System](Strategy_System.md) for planning workflow
- [Profiles System](Profiles_System.md) for saved-data trust and locking
- [Fuel Model](Fuel_Model.md) for learning and confidence
- [Dashboards](Dashboards.md) for display layout guidance
- [Shift Assist](Shift_Assist.md) for cue setup and locking
- [Launch System](Launch_System.md) for launch setup and review
- [Rejoin Assist](Rejoin_Assist.md) and [Pit Assist](Pit_Assist.md) for driver aids
- [H2H System](H2H_System.md) for race-context comparisons
