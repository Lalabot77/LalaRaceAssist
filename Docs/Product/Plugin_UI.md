# Plugin UI

Plugin UI is the setup, configuration, and review surface for Lala Race Assist. It covers overview/status, Strategy planning, Profiles, Standing Start Assist setup/review, bindings, settings, Driver Tagging, debug options, and debrief workflows.

## When you use it

Use Plugin UI before driving to configure Profiles, Strategy, bindings, dashboard behavior, pit assumptions, and Standing Start Assist settings. Use it after driving to review learned data, start traces, debriefs, and diagnostics.

## What it owns

Plugin UI owns visible configuration and review workflows. It can expose settings and actions, but the underlying names and persisted contracts remain stable unless a dedicated contract-change task says otherwise.

## What it does not own

It does not rename SimHub exports/actions, settings fields, profile/preset schema, dashboard JSON contracts, or internal class names just because a label changes. Product wording now uses **Standing Start Assist**; the technical implementation may still use Launch Mode and launch trace names.

## Where to go next

- [Quick Start](../Quick_Start.md)
- [Profiles](../Systems/Profiles.md)
- [Strategy](../Systems/Strategy.md)
- [Standing Start Assist](../Systems/Standing_Start_Assist.md)
- [Developer Tools](../Systems/Developer_Tools.md)
