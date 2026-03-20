# Repository status

Validated against commit: HEAD
Last updated: 2026-03-20
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status (requested set)
- `Docs/Subsystems/Fuel_Planner_Tab.md` updated so the canonical Fuel planner doc now records that pace-vs-leader is editable only in Profile mode, auto-follows live leader delta in Live Snapshot mode, and falls back to `0.0` instead of stale manual/profile values when live leader pace is unavailable.
- `Docs/Plugin_UI_Tooltips.md` updated to remove the obsolete leader-delta reset-to-live tooltip and describe the Live Snapshot lock behaviour for the pace-vs-leader control.
- `Docs/RepoStatus.md` refreshed for the current validation summary.

## Delivery status highlights
- Fuel planner leader delta now follows the selected planning source model: Profile mode keeps the manual/stored race-pace delta workflow, while Live Snapshot mode clears manual override state on entry and continuously uses the current live leader delta when available.
- The Fuel tab no longer needs a separate "Reset to live" / "Use live" leader-delta recovery path; the manual slider is locked in Live Snapshot mode and strategy calculations fall back to zero leader delta when no live leader pace is available.
- Live snapshot resets and car/track combination clears now wipe live/manual leader-delta runtime state without reviving stale hidden manual values during Live Snapshot mode, while still preserving track-stored profile behaviour for Profile mode.

## Notes
- `Docs/Code_Snapshot.md` remains non-canonical orientation-only documentation.
