# Property Snapshot Debug Workflow (Internal)

Validated against commit: HEAD
Last updated: 2026-06-08
Branch: work

## Purpose and ownership
This page is the **canonical internal workflow guide** for Property Snapshot debugging operations:
- manual snapshot workflow,
- rolling capture workflow,
- replay workflow,
- snapshot group usage guidance,
- troubleshooting decision matrix,
- escalation workflow.

This page is **not** the canonical owner for:
- export/property definitions (see `Docs/Internal/SimHubParameterInventory.md`),
- log-message contracts/tags (see `Docs/Internal/SimHubLogMessages.md`).

## Manual vs Rolling capture

### Manual capture (one-shot)
Use manual capture when you need a precise moment sample (for example: pit-entry edge, finish-latch edge, or a specific Strategy transition).

Recommended pattern:
1. Enable required debug toggles in plugin settings.
2. Select only the snapshot groups relevant to the issue.
3. Trigger the marker/capture at the event of interest.
4. Save the one-shot file and annotate session context (track/car/session type/replay or live).

Use manual capture when:
- issue is event-edge specific,
- you need low-noise evidence,
- you are validating one state transition.

### Rolling capture
Use rolling capture when you need state evolution across time/laps.

Rolling modes:
- **MANUAL**: rolling file present, writes driven by explicit trigger workflow.
- **FREQUENCY**: time-based captures.
- **PER LAP**: one capture per completed lap (deduplicated by lap index).

Guidance:
- prefer **PER LAP** for strategy/fuel stability issues,
- prefer **FREQUENCY** for rapid state transitions,
- keep enabled groups narrow to reduce noisy rows.

### CSV expectations
- Rolling CSV is a wide, evolving debug artifact for timeline analysis.
- One-shot snapshots remain best for event-edge forensic checks.
- Treat CSV as **debug evidence**, not a runtime contract source.
- Export meaning/source-of-truth stays in Parameter Inventory.

## Replay workflow
Replay is valid for many diagnostics, but some signals can differ from live timing/hydration behavior.

Recommended replay workflow:
1. Identify issue category first (fuel/strategy/pitexit/finish/league/dash).
2. Run a short replay window around the anomaly.
3. Capture one-shot snapshots at anomaly edge and one rolling sequence around it.
4. Cross-check with canonical logs by tag.
5. Confirm whether behavior reproduces in live session.

Replay caveats:
- session transitions and hydration gaps can look different than live,
- finish lifecycle and end-phase edges may tick differently,
- do not promote replay-only artifacts to runtime truth without live corroboration.

## Property Snapshot group usage
Use groups to limit noise and preserve ownership boundaries.

- **Fuel/Strategy**: runtime fuel, strategy basis, pre-race planning deltas, and the `MonitorSystem.*` dash-facing monitor surface.
- **Car/Opp/H2H**: race context, opponents, pit-exit cohort/position surfaces, class context.
- **Pit/PitExit**: pit control, pit timing, entry/exit trip surfaces.
- **Shift Assist**: shift cues and related assist-state surfaces.
- **Message System**: message engine outputs and queue surfaces.
- **League Class**: effective class resolver outputs and class-facing publication seams.
- **Raw Debug**: everything else that is intentionally uncategorized.

Group membership behavior is implementation-owned; this page defines operational usage only.

## Troubleshooting decision matrix
| Issue type | Primary tool | Snapshot groups | Key exports (examples) | Key logs | Replay caveats | Escalation path |
|---|---|---|---|---|---|---|
| Fuel runtime mismatch | Rolling + one-shot at lap edge | Fuel/Strategy | `Fuel.*`, `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.Contingency.*` | `[LalaPlugin:Fuel Burn]`, `[LalaPlugin:FUEL PER LAP]` | Early-lap confidence and replay hydration can mask live authority transitions | Validate in live session; then compare with Strategy subsystem assumptions |
| Strategy mismatch | One-shot + PER LAP rolling | Fuel/Strategy | `StrategyDash.*`, `LalaLaunch.PreRace.*`, planner-facing fuel basis exports | `[LalaPlugin:Strategy]`, `[LalaPlugin:Leader Lap]` | Live Detect/session metadata may resolve differently in replay windows | Confirm selected planner owner/source before escalation |
| PreRace discrepancy | One-shot pre-grid + at green transition | Fuel/Strategy | `LalaLaunch.PreRace.*`, `StrategyDash.*` | `[LalaPlugin:Strategy]` and fuel basis logs | Pre-green vs race-running authority seam differs by design | Escalate with exact phase (`SessionState`) evidence |
| PitExit issue | Rolling around pit-in/out + one-shot at both edges | Car/Opp/H2H + Pit/PitExit + League Class | `PitExit.*`, class-position/context exports | `[LalaPlugin:PitExit]`, `[LalaPlugin:Opponents]` | Replay may alter opponent row availability timing | Include league mode state + pit-loss source in escalation |
| League issue | One-shot at resolver transition + short rolling sequence | League Class + Car/Opp/H2H | League/effective-class exports and class-facing fields | League and class-resolution logs in canonical log doc | Replay may show transient unresolved identity rows | Provide league definition + fallback state in escalation note |
| RaceFinish issue | One-shot at finish-latch + rolling through end-phase | Car/Opp/H2H + Fuel/Strategy | `RaceFinish.*`, `Race.EndPhase.*`, related race-state exports | `[LalaPlugin:Finish]`, RaceDenom diagnostics | Finish lifecycle ticks can differ in replay and post-race churn | Corroborate with live reproduction when possible |
| Dash visibility issue | One-shot with control state + matching log window | Fuel/Strategy and/or relevant feature group | Dash-facing state exports and visibility mode fields | `[LalaPlugin:Dash]`, `[LalaPlugin:PitScreen]` | Replay may not mirror real input cadence | Verify dashboard expression uses plugin-owned exports only |
| Replay anomaly | Combined one-shot + rolling + log timeline | Narrow to affected subsystem groups | Issue-specific canonical exports only | Issue-specific canonical logs | Replay-only anomalies can be false positives | Mark as replay-only until live verified |
| Property Snapshot issue | Snapshot controls + rolling CSV checks | Raw Debug + affected subsystem groups | `Debug.PropertySnapshot.*` + affected family | Property snapshot debug logs | Replay itself usually not primary cause | Escalate with control-state + mode + schema/reset context |

## Common log usage flow
1. Filter logs by subsystem tag first.
2. Pair log timestamps with snapshot rows.
3. Confirm active authority/source from exports.
4. Only then infer behavior correctness.

Canonical log tag details remain in `Docs/Internal/SimHubLogMessages.md`.

## Escalation workflow
When opening an internal debugging escalation, include:
- issue category,
- live vs replay,
- selected snapshot groups,
- one-shot file(s) and rolling interval,
- key log excerpt tags,
- expected behavior vs observed behavior,
- whether repro is deterministic.

## Race end / finish lifecycle note
RaceFinish remains distributed ownership by design:
- export contract: `SimHubParameterInventory` (`RaceFinish.*`, `Race.EndPhase.*`),
- observability contract: `SimHubLogMessages` (finish diagnostics, RaceDenom/lifecycle tags),
- validation history: `RepoStatus` and `Development_Changelog`,
- subsystem context: lifecycle explanation in existing subsystem docs.

No standalone `Docs/Subsystems/RaceFinish.md` is introduced in this sweep.


## 2026-06-08 Debug UI/master-gate update
- `Enable debugging mode` / `EnableSoftDebug` is the operational master gate for Property Snapshot. When it is off, manual Event Marker captures and rolling captures are inert; an active rolling runtime state is stopped/fail-safe by the master gate without clearing the persisted child settings.
- Settings UI visibility is parent-driven: Property Snapshot child controls are hidden until `Enable Property Snapshot` is on, rolling controls/status are hidden until `Write rolling combined CSV` is on, and `Frequency Hz` is hidden unless rolling mode is `FREQUENCY`.
- Rolling frequency is normalized to the runtime cap of 1–2 Hz. Entering a higher value such as 20 is stored/displayed as 2, matching the capture interval used at runtime.
- START/STOP/RESET status uses the existing rolling status resolver (`OFF` / `READY` / `RECORDING`) and is refreshed from settings UI button clicks, rolling mode/enable changes, master debug toggle changes, and the plugin debug-UI refresh callback fired by rolling START/STOP/RESET actions. Dashboard/plugin actions continue to resolve status through the same exported status text.
- Event Marker still triggers a manual Property Snapshot only when both master debugging mode and `Enable Property Snapshot` are enabled; other Event Marker consumers are not changed.
