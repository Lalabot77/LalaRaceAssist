# User Guide

This guide explains the v1.1 product model for Lala Race Assist without duplicating every feature page. Start with the product surfaces, then use the core-system and feature docs for deeper setup and troubleshooting.

## Driver mental model

Lala Race Assist is organized outside-in:

1. **Product surfaces** are where you see or operate the system.
2. **Core systems** explain what capability is being provided.
3. **Subsystem docs** preserve the technical implementation truth and contracts.

The plugin owns learning, calculations, exports, actions, settings, profiles, presets, and persistence. Dashboards display and control those plugin-owned contracts; they do not own the underlying logic.

## Product surfaces

- [Driver Dash](Product/Driver_Dash.md) — tactical, in-car, immediate driving support.
- [Strategy Dash](Product/Strategy_Dash.md) — strategy, race-management, and planning support.
- [Plugin UI](Product/Plugin_UI.md) — setup, profiles, planning, bindings, settings, review, and debrief workflows.
- [Overlays](Product/Overlays.md) — temporary support surfaces for alerts, messages, and focused tasks.

See [Product overview](Product/README.md).

## Core systems

- [Strategy](Systems/Strategy.md) — race planning, fuel guidance, pit windows, and live strategy context.
- [Profiles](Systems/Profiles.md) — persisted driver/car/track/profile data; PB is stored data, not a subsystem title.
- [Pit System](Systems/Pit_System.md) — pit timing/loss, pit entry, pit box, pit commands, fuel/tyre control, debrief, and track markers.
- [Traffic Awareness](Systems/Traffic_Awareness.md) — nearby cars and track situational awareness, including CarSA and track H2H where relevant.
- [Race Awareness](Systems/Race_Awareness.md) — race order/context, rivals, race H2H, League Class, LapRef, and race-position awareness.
- [Shift Assist](Systems/Shift_Assist.md) — shift cueing, learning, review, and audio support.
- [Race Starts](Systems/Race_Starts.md) — start setup, live start capture, and Race Starts Analysis.
- [Rejoin Assist](Systems/Rejoin_Assist.md) — safe track rejoin support.
- [Dashboard Management](Systems/Dashboard_Management.md) — dashboard package usage, visibility, dark mode, and presentation contracts.
- [Monitor System](Systems/Monitor_System.md) — health and reliability status.
- [Driver Tagging](Systems/Driver_Tagging.md) — tagged-driver workflow and visual support.
- [Developer Tools](Systems/Developer_Tools.md) — debug options, snapshots, traces, diagnostic CSVs, and inventories.

See [Systems overview](Systems/README.md).

## Feature docs

Feature docs remain the practical user pages for specific workflows:

- [Dashboards](Features/Dashboards.md)
- [Strategy System](Features/Strategy_System.md)
- [Pit Assist](Features/Pit_Assist.md)
- [Fuel Guidance](Features/Fuel_Guidance.md)
- [Profiles System](Features/Profiles_System.md)
- [Shift Assist](Features/Shift_Assist.md)
- [Race Starts](Features/Race_Starts.md)
- [Rejoin Assist](Features/Rejoin_Assist.md)
- [H2H System](Features/H2H_System.md)

## Naming and contract notes

Public/product docs use **Driver Tagging** instead of Friends and **Race Starts** instead of Launch System where describing the driver-facing product. Technical docs may still say Launch Mode, Friends, or other legacy/internal names when referring to existing implementation contracts.

Do not infer a contract rename from documentation wording. SimHub export names, SimHub action names, persisted setting names, dashboard JSON contracts, profile schema fields, and preset schema fields remain unchanged unless a future task explicitly changes them.

## Technical truth

When product docs and implementation details diverge in wording, use [Subsystems](Subsystems/README.md) for technical ownership and contract truth. When in doubt, prefer subsystem docs and RepoStatus over orientation snapshots.
