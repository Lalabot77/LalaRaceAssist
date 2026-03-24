# Project Index

Validated against commit: HEAD
Last updated: 2026-03-22
Branch: work

## What this repo is
Lala Race Assist Plugin is a SimHub plugin for iRacing that provides strategy planning, fuel learning, dashboards, launch instrumentation, pit assistance, rejoin support, Shift Assist cueing, profile-backed persistence, and H2H race context.

This page is the canonical documentation map for the **v1 GitHub documentation set**. It should let a reader move cleanly between:
- public/user guidance,
- subsystem-level technical truth,
- internal maintainer references.

## Codex read/start order
1. If present at repo root, read `../AGENTS.md` as the thin agent entry point, then start with `Project_Index.md`.
2. Read `Docs/Internal/CODEX_CONTRACT.txt` for mandatory engineering policy.
3. Use `Docs/Internal/Architecture_Guardrails.md` for subsystem boundaries and ownership guidance.
4. Read the relevant `Subsystems/*.md` files before editing affected areas.
5. Check `RepoStatus.md` for the current validated repo/doc state.
6. Use `Docs/Internal/Code_Snapshot.md` only for orientation when needed; it is not canonical if it conflicts with the docs above.
7. Follow the analysis-first workflow and reusable task framing in `Docs/Internal/CODEX_TASK_TEMPLATE.txt`.

## User documentation
These pages are the GitHub-facing driver/user layer. They explain what the driver sees, how to use the feature, what to trust, and what to review when it feels wrong.

- [Quick Start](Quick_Start.md)
- [User Guide](User_Guide.md)
- [Dashboards](Dashboards.md)
- [Strategy System](Strategy_System.md)
- [Shift Assist](Shift_Assist.md)
- [Launch System](Launch_System.md)
- [Rejoin Assist](Rejoin_Assist.md)
- [Pit Assist](Pit_Assist.md)
- [H2H System](H2H_System.md)
- [Profiles System](Profiles_System.md)
- [Fuel Model](Fuel_Model.md)

## Subsystem documentation
These pages are the technical/canonical subsystem layer. They explain internal ownership, inputs, outputs, calculations, persistence, caching, architecture boundaries, and the plugin-vs-dash contract where relevant.

- [Subsystems/Fuel_Model.md](Subsystems/Fuel_Model.md)
- [Subsystems/Fuel_Planner_Tab.md](Subsystems/Fuel_Planner_Tab.md)
- [Subsystems/Launch_Mode.md](Subsystems/Launch_Mode.md)
- [Subsystems/Shift_Assist.md](Subsystems/Shift_Assist.md)
- [Subsystems/Pit_Timing_And_PitLoss.md](Subsystems/Pit_Timing_And_PitLoss.md)
- [Subsystems/Pit_Entry_Assist.md](Subsystems/Pit_Entry_Assist.md)
- [Subsystems/Rejoin_Assist.md](Subsystems/Rejoin_Assist.md)
- [Subsystems/Opponents.md](Subsystems/Opponents.md)
- [Subsystems/CarSA.md](Subsystems/CarSA.md)
- [Subsystems/H2H.md](Subsystems/H2H.md)
- [Subsystems/Profiles_And_PB.md](Subsystems/Profiles_And_PB.md)
- [Subsystems/Dash_Integration.md](Subsystems/Dash_Integration.md)
- [Subsystems/Pace_And_Projection.md](Subsystems/Pace_And_Projection.md)
- [Subsystems/Trace_Logging.md](Subsystems/Trace_Logging.md)
- [Subsystems/Track_Markers.md](Subsystems/Track_Markers.md)
- [Subsystems/Message_System_V1.md](Subsystems/Message_System_V1.md)
- [Subsystems/MessageEngineV1_Notes.md](Subsystems/MessageEngineV1_Notes.md)
- and other existing technical subsystem docs in `Docs/Subsystems/`.

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

## v1 documentation notes
- `README.md` is the public landing page.
- `Docs/*.md` are the reader-facing feature/system pages.
- `Docs/Subsystems/*.md` are the canonical technical ownership docs.
- `Docs/RepoStatus.md` records the latest documentation sweep and validation note.
- If user-facing pages and subsystem docs ever disagree, update both in the same task so GitHub readers do not get split truths.

## Freshness
- Validated against commit: HEAD
- Date: 2026-03-22
- Branch: work
