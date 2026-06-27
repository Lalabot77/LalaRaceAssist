# Quick Start

This install-first path gets Lala Race Assist running, verifies the plugin and dashboards, and points you to the v1.1 documentation structure.

## 1. Install the plugin

1. Close SimHub.
2. Copy `LaunchPlugin.dll` into the SimHub plugin folder.
3. Start SimHub.
4. Confirm Lala Race Assist appears in the SimHub plugin list.

`LaunchPlugin.dll` remains the required plugin DLL name. Documentation wording such as Race Starts does not rename plugin files, SimHub actions, exports, settings, profile fields, preset fields, or dashboard contracts.

## 2. Import dashboards

1. Import the matching Lala Race Assist dashboard package.
2. Confirm the Driver Dash and Strategy Dash are available in SimHub.
3. Use the dashboard package that matches the plugin release whenever possible.

Dashboards present plugin-owned outputs. They do not own strategy math, fuel learning, pit timing, race-awareness selection, Shift Assist learning, Race Starts recording, or persistence.

## 3. Confirm the plugin loads

Open the plugin UI and confirm the expected top-level areas are available. Current UI labels may still use legacy wording where contracts or pending UI-label work require it; public documentation uses the v1.1 product terms Driver Tagging and Race Starts.

## 4. Check Monitor System

Open the Monitor System status in the plugin or dashboard and confirm it is not reporting a fault. If Monitor System reports unreliable data, fix that before trusting race guidance.

See [Monitor System](Systems/Monitor_System.md).

## 5. Choose or create Profiles

Profiles store car, track, sound, pit, Shift Assist, and related persisted data. Personal-best/reference data is stored data inside Profiles, not a separate subsystem title.

See [Profiles](Systems/Profiles.md) and [Profiles System](Features/Profiles_System.md).

## 6. Create Strategy

Set up the race plan, fuel/reserve expectations, pit service assumptions, and relevant race preset information in Strategy.

See [Strategy](Systems/Strategy.md), [Strategy System](Features/Strategy_System.md), and [Fuel Guidance](Features/Fuel_Guidance.md).

## 7. Bind core controls

Bind the controls you expect to use while driving, including dashboard controls, pit commands, Strategy controls, Race Starts controls, Shift Assist controls, and message acknowledgement.

Public docs may say Race Starts, but existing SimHub action names remain unchanged unless a future task explicitly approves a contract change.

## 8. First smoke test

Before racing, run a short session and confirm:

- Driver Dash opens and updates.
- Strategy Dash shows sensible race/fuel context.
- Monitor System is healthy or clearly explains missing data.
- Profiles load the expected car/track data.
- Pit System widgets fail closed when no pit data is available.
- Traffic Awareness and Race Awareness populate only when session data supports them.
- Shift Assist, Rejoin Assist, Race Starts, and Driver Tagging are configured only where you intend to use them.

## Next reading

- [User Guide](User_Guide.md)
- [Product surfaces](Product/README.md)
- [Core systems](Systems/README.md)
- [Feature docs](Features/README.md)
- [Technical subsystem map](Subsystems/README.md)
