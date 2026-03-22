# Lala Race Assist Plugin

Lala Race Assist Plugin is a SimHub plugin for **iRacing** focused on race engineering, driver support, and dashboard-ready outputs. It combines planning, learned data, and live race context so drivers can make cleaner decisions without pushing core logic into dashboards.

It is built to help with the practical race workflow: plan stints, trust learned fuel and pace data, manage launch and pit situations, and keep key race context visible while driving.

Version **1.0** documentation is now organized so GitHub readers can move from a quick user overview into the canonical subsystem docs without guessing which page owns which truth.

## Core Systems

- **Strategy** - stable planning workflow for race fuel, stint, and context decisions
- **Shift Assist** - RPM-based shift cueing with profile-backed trust and locking workflows
- **Launch System** - launch setup plus saved-run review through Launch Analysis
- **Rejoin Assist** - recovery and rejoin awareness support after incidents or off-tracks
- **Pit Assist** - pit-entry and pit-lane support surfaced through the plugin and dashboards
- **H2H** - same-class race and local-track comparison context
- **Profiles** - long-term saved data by car, track, and condition
- **Fuel Model** - learned fuel burn and confidence that feed the planning workflow
- **Dashboards** - dashboard integration for display, visibility, and interaction with plugin outputs

## Supported Scope

- **Platform:** SimHub
- **Sim:** iRacing only
- **Primary use:** race planning, learned-data workflows, driver aids, and dashboard-supported race context

## Plugin vs Dashboard Responsibility

The plugin owns the **learning, storage, calculations, and exports**. Dashboards are the presentation layer: they show those outputs and provide limited interaction, but they do not own strategy math, saved learning, H2H selection, or launch logic.

## Install Summary

1. Copy the plugin files into your SimHub installation.
2. Keep **`RSC.iRacingExtraProperties.dll` required for now**.
3. Restart SimHub.
4. Import the dashboards you want to use.
5. Open the plugin and begin with **Strategy**, then review **Profiles**, **Dash Control**, and **Settings**.

## Documentation

### Getting Started

- [Quick Start](Docs/Quick_Start.md)
- [User Guide](Docs/User_Guide.md)

### Driver Systems

- [Strategy System](Docs/Strategy_System.md)
- [Shift Assist](Docs/Shift_Assist.md)
- [Launch System](Docs/Launch_System.md)
- [Rejoin Assist](Docs/Rejoin_Assist.md)
- [Pit Assist](Docs/Pit_Assist.md)
- [H2H System](Docs/H2H_System.md)
- [Profiles System](Docs/Profiles_System.md)
- [Fuel Model](Docs/Fuel_Model.md)

### Technical / Canonical Subsystems

- [Project Index](Docs/Project_Index.md) - documentation map and ownership guide
- [Subsystem docs](Docs/Subsystems/) - canonical subsystem behavior, boundaries, and export-level contracts
- [Dash integration notes](Docs/Subsystems/Dash_Integration.md)
- [Changelog](CHANGELOG.md)

## Current UI Structure

The current top-level plugin navigation is:

1. **Strategy**
2. **Profiles**
3. **Dash Control**
4. **Launch Analysis**
5. **Settings**

Presets are managed from **Strategy** through the **`Presets...`** modal flow. There is no separate top-level Presets tab.

## Notes / Important Boundaries

- **PreRace** is display-only. It does not replace the planner or change the live fuel model.
- **Live Snapshot** can auto-drive relevant planning values. When it is active, the corresponding manual controls are disabled.
- Launch controls live under **Settings -> Launch Settings**. **Launch Analysis** remains the saved-run review tab.
- The future/global message system is not documented here as an active user feature.

## Feedback and Requests

If you hit a bug, want a workflow improvement, or need clearer documentation, open a GitHub issue so the public docs and plugin behavior can stay aligned.
