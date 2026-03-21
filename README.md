# LalaLaunchPlugin

LalaLaunchPlugin is a SimHub plugin for **iRacing** focused on race-ready strategy, dash support, launch instrumentation, pit assistance, rejoin awareness, Shift Assist, and race-context comparison tools.

This repo is the user-facing home for installation, quick-start guidance, the full user manual, subsystem overviews, and release history.

## Supported scope

- **Platform:** SimHub
- **Sim:** iRacing only
- **Primary use:** strategy planning, live race support, dashboard-driven driving aids, and post-launch review

## What it does

- Learns and stores fuel, pace, pit-loss, and track marker data.
- Provides a **Strategy** workflow for planning laps/time races using profile data or live session snapshots.
- Publishes stable outputs to dashboards so the driver is not chasing noisy lap-to-lap changes.
- Supports pit-entry assist, pit popups, rejoin warnings, launch analysis, Shift Assist, and head-to-head race/track comparisons.
- Coordinates multiple dash types, visibility options, and declutter modes without moving calculation ownership into the dashboards.

## Plugin vs dashboard responsibility

The plugin is the source of truth for **learning, storage, and calculations**.

Dashboards are there to **display outputs and offer limited interaction**. They do not become the source of truth for fuel math, strategy logic, H2H selection, or saved data.

## Install summary

1. Copy the plugin files into your SimHub installation.
2. Keep **`RSC.iRacingExtraProperties.dll` required for now**.
3. Restart SimHub.
4. Import the dashboards you want to use.
5. Open the plugin and start with the **Strategy** tab, then review **Dash Control** and **Settings**.

For full instructions, use the docs below.

## Documentation

- [Quick Start](Docs/Quick_Start.md)
- [User Guide](Docs/User_Guide.md)
- [Dashboards](Docs/Dashboards.md)
- [Strategy System](Docs/Strategy_System.md)
- [H2H System](Docs/H2H_System.md)
- [Rejoin and Pit Assists](Docs/Rejoin_And_Pit_Assists.md)
- [Changelog](CHANGELOG.md)

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
- Launch-related controls now live under **Settings → Launch Settings**, not Dash Control.

## Feedback and requests

If you hit a bug or want a feature, open a GitHub issue in this repo so the docs and code can stay in sync.
