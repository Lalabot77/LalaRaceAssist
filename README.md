# Lala Race Assist Plugin

Lala Race Assist is a SimHub plugin for iRacing that provides driver dashboards, strategy planning, race-awareness context, pit assistance, rejoin support, Standing Start Assist review, Shift Assist cueing, and profile-backed persistence. The plugin owns the learning, calculations, settings, actions, exports, and saved data; dashboards are presentation and control surfaces for those plugin-owned contracts.

## Product surfaces

- **Driver Dash** — tactical in-car support for immediate driving decisions.
- **Strategy Dash** — race-management and planning support for stints, fuel, pit windows, and race context.
- **Plugin UI** — setup, profiles, planning, bindings, settings, and review/debrief workflows.
- **Overlays** — temporary supporting surfaces for alerts, messages, comparisons, and focused tasks.

## Core systems

- **Strategy**
- **Profiles**
- **Pit System**
- **Traffic Awareness**
- **Race Awareness**
- **Shift Assist**
- **Standing Start Assist**
- **Rejoin Assist**
- **Dashboard Management**
- **Monitor System**
- **Driver Tagging**
- **Developer Tools**

## Install summary

1. Copy `LaunchPlugin.dll` into the SimHub plugin folder.
2. Start or restart SimHub and confirm the plugin loads.
3. Import the matching dashboard package.
4. Open the plugin UI, check Monitor System status, choose or create Profiles, create a Strategy, and bind core controls.

`RSC.iRacingExtraProperties.dll` is no longer required for current v1.1 plugin-owned runtime, pit command, or custom command workflows. Users upgrading older installs may still have legacy dashboards or button bindings that reference the old plugin, so review those during upgrade.

See [Quick Start](Docs/Quick_Start.md) for the install-first setup path.

## Documentation

- [Project Index](Docs/Project_Index.md) — canonical documentation map.
- [Quick Start](Docs/Quick_Start.md) — installation and first smoke test.
- [User Guide](Docs/User_Guide.md) — product surfaces, core systems, and driver mental model.
- [Product surfaces](Docs/Product/README.md) — Driver Dash, Strategy Dash, Plugin UI, and Overlays.
- [Core systems](Docs/Systems/README.md) — outside-in product architecture.
- [Feature docs](Docs/Features/README.md) — user-facing feature pages.
- [Subsystem docs](Docs/Subsystems/README.md) — technical/canonical implementation ownership.

## Support and community

Use the project issue tracker or community/support channel for installation problems, dashboard/package mismatches, reproducible bugs, and documentation gaps. Include plugin version, SimHub version, iRacing session type, dashboard package version, and relevant logs where possible.

## Licence

See [LICENSE](LICENSE).
