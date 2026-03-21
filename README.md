# Lala Race Assist Plugin

Lala Race Assist Plugin is a SimHub plugin for **iRacing** focused on race-ready strategy, dashboard support, launch instrumentation, pit assistance, rejoin awareness, Shift Assist, and head-to-head race context.

This repository now separates documentation into three layers:
- **User-facing GitHub docs** in `Docs/` for drivers and dashboard users.
- **Technical subsystem docs** in `Docs/Subsystems/` for internal behavior and ownership.
- **Internal/developer docs** in `Docs/Internal/` for maintainers, support work, and Codex tasks.

## Supported scope

- **Platform:** SimHub
- **Sim:** iRacing only
- **Primary use:** strategy planning, race support, dashboards, launch review, and profile-backed driver aids

## What it does

- Learns and stores fuel, pace, pit-loss, marker, and profile data by car, track, and condition.
- Provides a **Strategy** workflow for stable race planning using saved data or live session snapshots.
- Publishes stable outputs to dashboards so the driver is not chasing noisy lap-to-lap changes.
- Supports Shift Assist, the Launch system, rejoin awareness, pit guidance, H2H race context, and profile-backed trust/lock workflows.

## Plugin vs dashboard responsibility

The plugin is the source of truth for **learning, storage, calculations, and exports**. Dashboards display those outputs and provide limited interaction, but they do not own strategy math, saved data, H2H selection, or launch logic.

## Install summary

1. Copy the plugin files into your SimHub installation.
2. Keep **`RSC.iRacingExtraProperties.dll` required for now**.
3. Restart SimHub.
4. Import the dashboards you want to use.
5. Open the plugin and start with **Strategy**, then review **Profiles**, **Dash Control**, and **Settings**.

## User documentation

- [Quick Start](Docs/Quick_Start.md)
- [User Guide](Docs/User_Guide.md)
- [Dashboards](Docs/Dashboards.md)
- [Strategy System](Docs/Strategy_System.md)
- [Shift Assist](Docs/Shift_Assist.md)
- [Launch System](Docs/Launch_System.md)
- [Rejoin Assist](Docs/Rejoin_Assist.md)
- [Pit Assist](Docs/Pit_Assist.md)
- [H2H System](Docs/H2H_System.md)
- [Profiles System](Docs/Profiles_System.md)
- [Fuel Model](Docs/Fuel_Model.md)
- [Changelog](CHANGELOG.md)

## Technical and internal docs

- [Project Index](Docs/Project_Index.md)
- [Subsystem docs](Docs/Subsystems/)
- [Internal / developer docs](Docs/Internal/)

## Current UI structure

The current top-level plugin navigation is:

1. **Strategy**
2. **Profiles**
3. **Dash Control**
4. **Launch Analysis**
5. **Settings**

Presets are managed from **Strategy** through the **`Presets...`** modal flow. There is no separate top-level Presets tab.

## Notes

- PreRace is an **on-grid/display layer** only. It does not replace the planner or change the live fuel model.
- Live Snapshot mode can auto-drive relevant planning values. When live control is active, the corresponding manual controls are disabled.
- Launch-related controls live under **Settings → Launch Settings**, while **Launch Analysis** remains the saved-run review tab.
- The future/global message system is not documented here as an active user feature.

## Feedback and requests

If you hit a bug or want a feature, open a GitHub issue in this repo so the docs and code can stay aligned.
