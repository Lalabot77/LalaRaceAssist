# Quick Start

This install-first path gets Lala Race Assist running, verifies the plugin and dashboards, and points you to the v1.1 documentation structure.

## 1. Install the plugin

1. Close SimHub.
2. Copy `LaunchPlugin.dll` into the SimHub plugin folder.
3. Start SimHub.

`LaunchPlugin.dll` is required. `RSC.iRacingExtraProperties.dll` is no longer required for active v1.1 runtime, pit command, custom command, or current plugin-owned paths. Old dashboards or user-local legacy experiments may still contain old references, but the v1.1 plugin workflow should not require installing it.

## 2. Confirm the plugin loads

Confirm Lala Race Assist appears in the SimHub plugin list, then open the plugin UI and confirm the main areas are available. Public documentation uses **Standing Start Assist** and **Driver Tagging**; internal contracts may still use launch/friend names where those are existing code, action, export, setting, or schema names.

## 3. Import dashboards

1. Import the matching Lala Race Assist dashboard package.
2. Confirm the Driver Dash and Strategy Dash are available in SimHub.
3. Use the dashboard package that matches the plugin release whenever possible.

Dashboards present plugin-owned outputs. They do not own strategy math, fuel learning, pit timing, race-awareness selection, Shift Assist learning, Standing Start Assist recording, or persistence.

## 4. Bind core controls

Bind the controls you expect to use while driving, including dashboard controls, pit commands, Strategy controls, Standing Start Assist controls, Shift Assist controls, and message acknowledgement.

For current installs, use the plugin-owned pit/custom command actions documented in the Bindings UI and Pit System docs. Older `iRacingExtraProperties` pit/custom command actions should not be used for the v1.1 plugin-owned workflow. If you are upgrading existing dashboards, Stream Deck pages, button boxes, or wheel bindings, review them for old action IDs before assuming the new plugin flow is in control.

Public docs may say Standing Start Assist, but existing SimHub action names remain unchanged unless a future task explicitly approves a contract change.

## 5. Check Monitor System

Open Monitor System status in the plugin or dashboard and confirm it is not reporting a fault. If Monitor System reports unreliable data, fix that before trusting race guidance.

See [Monitor System](Systems/Monitor_System.md).

## 6. Choose or create Profiles

Profiles store car, track, sound, pit, Shift Assist, and related persisted data. Personal-best/reference data is stored data inside Profiles, not a separate subsystem title.

See [Profiles](Systems/Profiles.md) and [Profiles System](Features/Profiles_System.md).

## 7. Create Strategy

Set up the race plan, fuel/reserve expectations, pit service assumptions, and relevant race preset information in Strategy.

See [Strategy](Systems/Strategy.md), [Strategy System](Features/Strategy_System.md), and [Fuel Guidance](Features/Fuel_Guidance.md).

## 8. First smoke test

Before racing, run a short session and confirm:

- Driver Dash opens and updates.
- Strategy Dash shows sensible race/fuel context.
- Monitor System is healthy or clearly explains missing data.
- Profiles load the expected car/track data.
- Pit System widgets fail closed when no pit data is available.
- Traffic Awareness and Race Awareness populate only when session data supports them.
- Shift Assist, Rejoin Assist, Standing Start Assist, and Driver Tagging are configured only where you intend to use them.

## 9. Next reading

- [User Guide](User_Guide.md)
- [Product surfaces](Product/README.md)
- [Core systems](Systems/README.md)
- [Feature docs](Features/README.md)
- [Technical subsystem map](Subsystems/README.md)
