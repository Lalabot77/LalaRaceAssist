# Repository status

Validated against commit: 23d4d55073be68286ab35a258737ce12048eeddf
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/Fuel_Planner_Tab.md` updated so the canonical planner doc now states that Live Snapshot max fuel is locked to the live detected cap only, while Profile mode retains manual/preset max-fuel behavior.
- `Docs/Plugin_UI_Tooltips.md` updated so the Strategy-tab max-fuel tooltip text explicitly calls out the Profile-mode edit path and the Live Snapshot lock-to-live-cap behavior.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- Fuel Planner max-fuel selection now follows the same clean planning-source model as the recent pace-vs-leader fix: Profile mode keeps manual/preset behavior, while Live Snapshot uses only the live detected cap as the active planner tank basis.
- Entering Live Snapshot no longer falls back to a remembered profile/preset/manual max-fuel value when the live cap is missing; instead the planner reports the missing live-cap validation state and blocks strategy outputs until valid live cap telemetry is available.
- Ongoing live cap changes now flow straight through the planner tank basis and strategy outputs in Live Snapshot, including first-stint fuel, without any preset-side override path.
- The Strategy-tab max-fuel control remains visually unchanged but is now documented as Profile-editable and Live Snapshot-locked to the detected live cap.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
