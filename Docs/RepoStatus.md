# Repository status

Validated against commit: HEAD
Last updated: 2026-04-04
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- Opponents session eligibility was widened from race-only to live opponent sessions (Practice, Qualifying/Open Qualify, Lone Qualify, Race), while keeping Offline Testing out of scope.
- H2HRace selector integration remained unchanged (`Opp.Ahead1` / `Opp.Behind1`), so race-style H2H can now populate in practice/qualifying when leaderboard-neighbor identity is available.
- Race-specific pit-exit prediction remained race-scoped; `PitExit.*` outputs are reset outside Race.
- Opponents pit-exit reset now runs once on Race → non-Race transition, avoiding per-tick reset churn during long non-race sessions.
- Opponents activation log wording now matches current behavior: `Opponents subsystem active (eligible live session).`
- Synced Opponents/H2H subsystem docs and user-facing H2H page with the new eligibility behavior.
- Logged this behavior change in the internal development changelog.

## Reviewed documentation set
### Changed in this sweep
- `Opponents.cs`
- `Docs/Subsystems/Opponents.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/CODEX_TASK_TEMPLATE.txt`
- `Docs/Internal/Code_Snapshot.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Dashboards.md`
- `Docs/User_Guide.md`

## Delivery status highlights
- Replaced strict Opponents race-only eligibility with a bounded live-session eligibility helper at the LalaLaunch call sites.
- Preserved H2HRace family ownership and selector seam; no third H2H mode/family was added.
- Kept Opponents lap-gate removal intact (no completed-lap re-gate) and left dash-side suppression as the preferred way to handle early sparse timing data.
- Kept race-specific pit-exit predictor logic bounded to Race to avoid cross-session semantic drift.
- Removed per-tick non-race pit-exit resets by latching race-active state and resetting only on Race → non-Race transition.

## Validation note
- Validation recorded against `HEAD` (`Opponents pit-exit race-transition reset cleanup + docs sync`).
