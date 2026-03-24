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

Go to the bottom left of the left menu and click "Add/Remove Features". Find the Lala Plugin, activate it, and enable the show-in-left-menu option.

### Required for now

- `LaunchPlugin.dll`
- `RSC.iRacingExtraProperties.dll`

### Plugin Activation

Copy the plugin files into your SimHub installation, then restart SimHub.

## 3. Import dashboards

Typical layout:

- **Primary race dash** for the main driver view
- **Strategy/support dash** for planning and context
- **Overlay** for compact alerts
- **Lovely-based layouts** if you use the Lovely ecosystem
- **Head 2 Head** as a separate context surface if you use it

High-level setup flow:

1. Import the dashboards into SimHub.
2. Assign each dashboard to the device or screen where you want to use it.
3. Bind SimHub's **Next Dash** and **Previous Dash** controls if you want reliable non-touch navigation.
4. Use touch areas as a backup or convenience layer rather than the only race-use control path.

Dashboards display plugin outputs. They do not learn data or replace the plugin as the source of truth. For the structured page guide, navigation model, and overlay explanation, see [Dashboards](Dashboards.md).

### Optional: ShakeIt Motors traction-loss export

Some Lala dashboards and launch-related visuals can show wheelspin / traction-loss indications from SimHub's ShakeIt Motors output. This setup is **optional**. The plugin still works normally without it.

If you want those indicators:

1. Open **SimHub**.
2. Go to **ShakeIt Motors**.
3. Open the **Wheel slip** effect and enable it.
4. In the **Export** section, tick **Export output value as a property**.
5. Set the property name to exactly `TractionLoss`.

That exposes `[ShakeITMotorsV3Plugin.Export.TractionLoss.All]` for dashboards or visuals that use it. Without this setup, wheelspin-related indicators may be unavailable.

### Current constraints (v1.x)

- Some dashboard indicators and behaviors depend on optional SimHub exports or optional setup steps.
- Wheelspin / traction-loss visuals require the optional `TractionLoss` ShakeIt export above.
- Primary Dash navigation behavior may continue to evolve during early public releases.
- In plugin UI, **Primary Dash Mode** binding is currently a placeholder and does not perform an action.
- **`RSC.iRacingExtraProperties.dll` is still required**.

## 4. First plugin check

Open the plugin in SimHub and confirm the main navigation order:

1. **Overview**
2. **Strategy**
3. **Profiles**
4. **Dash Control**
5. **Launch Analysis**
6. **Settings**

![Strategy tab after install](Images/StrategyTab.png)
*After install, Overview is the landing tab; use it for quick links and status, then move to Strategy for planning.*

Important current rules:

- **Overview** is the landing/front-door tab (links, status, and update check).
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
   - **Next Dash**
   - **Previous Dash**
   - **Declutter Mode**

### Dash navigation quick setup

The dashboards can be used by touch, but binding controls is strongly recommended. For a first usable setup:

- Bind SimHub's **Next Dash** and **Previous Dash** commands to physical controls you can reach easily.
- On the Primary Driver Dash, the **left** touch area moves to the previous page and the **right** touch area moves to the next page.
- SimHub bindings can be configured per dash, per device, or globally depending on your setup.
- Auto-dash switching can choose a sensible landing page by session type, while overlays remain separate temporary surfaces instead of becoming part of the normal page loop.

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
