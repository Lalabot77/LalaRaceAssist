# Project Index

Validated against commit: HEAD
Last updated: 2026-06-27
Branch: work
Current note: v1.1 documentation architecture now follows the outside-in product model: product surfaces first, core systems second, technical subsystem ownership third, and internal maintainer references last. This was a documentation-only restructure with no runtime, dashboard JSON, SimHub export/action, settings schema, or profile/preset schema change.

## What this repo is

Lala Race Assist Plugin is a SimHub plugin for iRacing that provides driver dashboards, strategy planning, race-awareness context, pit assistance, rejoin support, Shift Assist cueing, race-start review, profile-backed persistence, and diagnostics.

This page is the canonical documentation map for the v1.1 GitHub documentation set. It should let a reader move cleanly between product surfaces, core systems, feature guidance, technical subsystem truth, and internal maintainer references.

## Codex read/start order

1. If present at repo root, read `../AGENTS.md` as the thin agent entry point, then start with `Project_Index.md`.
2. Read `Docs/Internal/CODEX_CONTRACT.txt` for mandatory engineering policy.
3. Use `Docs/Internal/Architecture_Guardrails.md` for subsystem boundaries and ownership guidance.
4. Read the relevant `Docs/Subsystems/**/*.md` files before editing affected areas.
5. Check `RepoStatus.md` for the current validated repo/doc state.
6. Use `Docs/Internal/Code_Snapshot.md` only for orientation when needed; it is not canonical if it conflicts with the docs above.
7. Follow the analysis-first workflow and reusable task framing in `Docs/Internal/CODEX_TASK_TEMPLATE.txt`.

## Product surfaces

Product surfaces are the outside-in entry points for what the driver or support crew sees.

- [Product overview](Product/README.md)
- [Driver Dash](Product/Driver_Dash.md)
- [Strategy Dash](Product/Strategy_Dash.md)
- [Plugin UI](Product/Plugin_UI.md)
- [Overlays](Product/Overlays.md)

## Core systems

Core systems describe product capabilities before diving into implementation details.

- [Systems overview](Systems/README.md)
- [Strategy](Systems/Strategy.md)
- [Profiles](Systems/Profiles.md)
- [Pit System](Systems/Pit_System.md)
- [Traffic Awareness](Systems/Traffic_Awareness.md)
- [Race Awareness](Systems/Race_Awareness.md)
- [Shift Assist](Systems/Shift_Assist.md)
- [Race Starts](Systems/Race_Starts.md)
- [Rejoin Assist](Systems/Rejoin_Assist.md)
- [Dashboard Management](Systems/Dashboard_Management.md)
- [Monitor System](Systems/Monitor_System.md)
- [Driver Tagging](Systems/Driver_Tagging.md)
- [Developer Tools](Systems/Developer_Tools.md)

## User and feature documentation

These pages are the GitHub-facing driver/user layer. They explain what the driver sees, how to use the feature, what to trust, and what to review when it feels wrong.

- [Quick Start](Quick_Start.md)
- [User Guide](User_Guide.md)
- [Feature overview](Features/README.md)
- [Dashboards](Features/Dashboards.md)
- [Strategy System](Features/Strategy_System.md)
- [Pit Assist](Features/Pit_Assist.md)
- [Fuel Guidance](Features/Fuel_Guidance.md)
- [Profiles System](Features/Profiles_System.md)
- [Shift Assist](Features/Shift_Assist.md)
- [Race Starts](Features/Race_Starts.md)
- [Rejoin Assist](Features/Rejoin_Assist.md)
- [H2H System](Features/H2H_System.md)

## Subsystem documentation

Subsystem pages are the technical/canonical implementation layer. They explain internal ownership, inputs, outputs, calculations, persistence, caching, architecture boundaries, and plugin-vs-dash contracts.

- [Subsystem ownership map](Subsystems/README.md)
- [Strategy / Fuel Model](Subsystems/Strategy/Fuel_Model_Subsystem.md)
- [Strategy / Fuel Planner Tab](Subsystems/Strategy/Fuel_Planner_Tab.md)
- [Strategy / Pace and Projection](Subsystems/Strategy/Pace_And_Projection.md)
- [Profiles](Subsystems/Profiles/Profiles.md)
- [Pit Timing and Pit Loss](Subsystems/Pit_System/Pit_Timing_And_PitLoss.md)
- [Pit Entry Assist](Subsystems/Pit_System/Pit_Entry_Assist.md)
- [Pit Commands and Fuel/Tyre Control](Subsystems/Pit_System/Pit_Commands_And_Fuel_Control.md)
- [Track Markers](Subsystems/Pit_System/Track_Markers.md)
- [Traffic Awareness / CarSA](Subsystems/Traffic_Awareness/CarSA.md)
- [Race Awareness / Opponents](Subsystems/Race_Awareness/Opponents.md)
- [Race Awareness / H2H](Subsystems/Race_Awareness/H2H.md)
- [Race Awareness / League Class](Subsystems/Race_Awareness/League_Class_System.md)
- [Race Awareness / LapRef](Subsystems/Race_Awareness/LapRef.md)
- [Race Starts / Launch Mode](Subsystems/Race_Starts/Launch_Mode.md)
- [Shift Assist](Subsystems/Shift_Assist/Shift_Assist.md)
- [Rejoin Assist](Subsystems/Rejoin_Assist/Rejoin_Assist.md)
- [Dashboard Integration](Subsystems/Dashboard_Management/Dash_Integration.md)
- [Message System V1](Subsystems/Dashboard_Management/Message_System_V1.md)
- [Message Engine V1 Notes](Subsystems/Dashboard_Management/MessageEngineV1_Notes.md)
- [Trace Logging](Subsystems/Developer_Tools/Trace_Logging.md)

## Internal / developer documentation

These pages support maintainers, support work, and Codex tasks. They are not part of the normal driver-facing documentation layer.

- [Internal/CODEX_CONTRACT.txt](Internal/CODEX_CONTRACT.txt)
- [Internal/Architecture_Guardrails.md](Internal/Architecture_Guardrails.md)
- [Internal/CODEX_TASK_TEMPLATE.txt](Internal/CODEX_TASK_TEMPLATE.txt)
- [Internal/Plugin_UI_Tooltips.md](Internal/Plugin_UI_Tooltips.md)
- [Internal/SimHubParameterInventory.md](Internal/SimHubParameterInventory.md)
- [Internal/SimHubLogMessages.md](Internal/SimHubLogMessages.md)
- [Internal/Code_Snapshot.md](Internal/Code_Snapshot.md)
- [Internal/Release_Checklist.md](Internal/Release_Checklist.md)
- [Internal/Development_Changelog.md](Internal/Development_Changelog.md)
- [Internal/Property_Snapshot_Debug_Workflow.md](Internal/Property_Snapshot_Debug_Workflow.md)
- [Internal/iRacingExtraProperties_Dependency_Audit.md](Internal/iRacingExtraProperties_Dependency_Audit.md)

## Topic owner matrix

| Topic | Canonical owner doc |
|---|---|
| Strategy planning | `Docs/Systems/Strategy.md` + `Docs/Subsystems/Strategy/Fuel_Planner_Tab.md` |
| Fuel guidance / fuel trust workflow | `Docs/Features/Fuel_Guidance.md` + `Docs/Subsystems/Strategy/Fuel_Model_Subsystem.md` |
| Profiles and persisted data | `Docs/Systems/Profiles.md` + `Docs/Subsystems/Profiles/Profiles.md` |
| Pit System | `Docs/Systems/Pit_System.md` + `Docs/Subsystems/Pit_System/*` |
| Traffic Awareness | `Docs/Systems/Traffic_Awareness.md` + `Docs/Subsystems/Traffic_Awareness/CarSA.md` |
| Race Awareness | `Docs/Systems/Race_Awareness.md` + `Docs/Subsystems/Race_Awareness/*` |
| Race Starts | `Docs/Systems/Race_Starts.md` + `Docs/Subsystems/Race_Starts/Launch_Mode.md` |
| Dashboard Management | `Docs/Systems/Dashboard_Management.md` + `Docs/Subsystems/Dashboard_Management/Dash_Integration.md` |
| Monitor System | `Docs/Systems/Monitor_System.md` + `Docs/Internal/MonitorSystem_Messages.csv` |
| Driver Tagging | `Docs/Systems/Driver_Tagging.md` |
| Developer Tools | `Docs/Systems/Developer_Tools.md` + `Docs/Subsystems/Developer_Tools/Trace_Logging.md` |
| Race end / finish lifecycle contract | `Docs/Internal/SimHubParameterInventory.md` (`RaceFinish.*`, `Race.EndPhase.*`) + `Docs/Internal/SimHubLogMessages.md` |
| Property Snapshot debug workflow | `Docs/Internal/Property_Snapshot_Debug_Workflow.md` |

## Terminology matrix

| Public/product term | Technical / contract term | Canonical owner |
|---|---|---|
| Strategy | Fuel Planner / Strategy Tab | `Docs/Subsystems/Strategy/Fuel_Planner_Tab.md` |
| Fuel Guidance | Fuel Model / runtime fuel logic | `Docs/Subsystems/Strategy/Fuel_Model_Subsystem.md` |
| Profiles | Profiles and stored PB/profile data | `Docs/Subsystems/Profiles/Profiles.md` |
| Pit System | Pit Entry + Pit Timing + Pit Commands/Fuel Control + Track Markers | `Docs/Subsystems/Pit_System/*` |
| Traffic Awareness | CarSA / track situational awareness | `Docs/Subsystems/Traffic_Awareness/CarSA.md` |
| Race Awareness | Opponents / race H2H / League Class / LapRef | `Docs/Subsystems/Race_Awareness/*` |
| Race Starts | Launch Mode / launch trace contracts | `Docs/Subsystems/Race_Starts/Launch_Mode.md` |
| Driver Tagging | Existing tagged/friend-driver persistence and styling inputs | `Docs/Systems/Driver_Tagging.md` |
| Dashboard Management | Dash Integration contract + dashboard package usage | `Docs/Subsystems/Dashboard_Management/Dash_Integration.md` |
| RaceFinish | Finish lifecycle / end-phase contract | `Docs/Internal/SimHubParameterInventory.md` + `Docs/Internal/SimHubLogMessages.md` |
| Property Snapshot | Internal observability workflow | `Docs/Internal/Property_Snapshot_Debug_Workflow.md` |

## Contract reminders

- Public/product documentation may use Driver Tagging and Race Starts terminology.
- Internal action names, SimHub export names, persisted setting names, JSON schema fields, profile/preset schema fields, and dashboard contracts remain unchanged unless an explicit future task approves otherwise.
- If user-facing pages and subsystem docs ever disagree, update both in the same task so GitHub readers do not get split truths.
