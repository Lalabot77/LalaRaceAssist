# Track Markers (Pit Entry/Exit) — Auto-Learn, Storage, MSGV1

Validated against commit: HEAD
Last updated: 2026-03-24
Branch: work

## Purpose
Capture pit entry/exit lines per track, keep them stable across sessions via persisted storage, and surface lifecycle notifications through MSGV1. The pattern is intentionally reusable: **auto-learn → lock → notify**.

## Source of truth and storage
- **Source of truth:** Persisted markers in `PluginsData/Common/LalaPlugin/TrackMarkers.json` (`PitEngine` store). Live telemetry is only used to propose captures; stored values drive exports and UI snapshots. Legacy `LalaPlugin.TrackMarkers.json` is migrated on load if present.
- **Keys:** Canonicalised track key from SimHub (`TrackCode`, then display/config/name fallbacks). Unknown/blank keys short-circuit all marker handling.
- **Embedded seed support:** If the persisted store does not exist yet, `PitEngine` seeds defaults from an embedded in-code track-marker set. Only records with valid seeded entry and exit percents are accepted; invalid / `NaN` / incomplete entries are skipped rather than coerced. The shipped embedded rows also carry fixed stored timestamps, so first-run seeded JSON stays deterministic instead of picking up the current clock time.
- **Defaults after seeding:** New tracks still start **locked = true** with empty percents when no valid embedded seed entry exists for that track. Lock can be toggled via UI or actions; lock state is persisted with the markers.

## Inputs and guards
- **Track length:** Parsed once per session from `WeekendInfo.TrackLength` (kilometres). Values outside **500–20000 m** are ignored.
- **Position:** Normalised `TrackPositionPercent`, clamped to [0,1]; entry captures require **≥0.50**, exit captures require **≤0.50** to avoid cross-lap confusion.
- **Speed / stall guards:** Entry capture requires **>5 kph** and not in stall; exit capture requires **>10 kph** and not in stall.

## Auto-learn flow
1) **Edge detection:** Entry edge = pit-lane flag rising; exit edge = pit-lane flag falling. Both run even on the first valid lap after start.
2) **Overwrite rules:**
   - Missing marker → capture.
   - Track-length refresh pending → capture and clear refresh flag.
   - Unlocked marker with delta **>0.1 %** (`TrackMarkerDeltaTolerancePct`) → capture.
   - Locked marker remains unless refresh forced; mismatches trigger MSGV1 (below).
3) **First complete pair:** When both entry and exit are valid, emit a `FirstCapture` trigger (latched per session).
4) **Persistence:** Writes immediately to JSON on every accepted capture or lock toggle; reload restores previous in-memory state on failure.

## Track length handling
- **Session baseline:** First valid track length per session is latched as `SessionStartTrackLengthM`. Live length is tracked thereafter.
- **Change detection:** Delta **>50 m** marks `TrackLengthChanged`, forces **unlock**, and flags both edges for refresh. A MSGV1 length-delta trigger is enqueued with start/current/delta metres.
- **Refresh completion:** When both refreshed edges are captured after a length change, a `LinesRefreshed` trigger fires (latched once per session).
- **Accepted limitation:** Replay/session-identity quirks can surface inconsistent track lengths; length-delta messages are informational and do not block use.

## Lock semantics
- **Locked = true:** Stored markers are authoritative. Live captures only notify on mismatch (MSGV1) unless a track-length refresh forced unlock already.
- **Locked = false:** Live captures that differ by >0.1 % overwrite and persist immediately.
- **Auto-unlock:** Track-length change (>50 m) clears lock before refresh to ensure updated markers can be stored.
- **Manual control:** Lock checkbox in Profiles → Tracks panel writes through to store; actions `TrackMarkersLock/Unlock` mirror the same setter.

## UI behaviour (Profiles → Tracks panel)
- **Snapshot fields:** Stored pit entry %, stored pit exit %, last-updated UTC, and lock checkbox. Values reflect the **selected track** snapshot and auto-refresh when the selection changes.
- **Lock toggle:** Writes immediately to the store (debounced via `_suppressTrackMarkersLockAction` to avoid recursive refresh).
- **Reload button:** Forces `TrackMarkers.json` reload from disk (for manual edits), then rebinds the snapshot view.

## MSGV1 messaging behaviour
- **Release status:** These MSGV1 toasts are development-track behavior and are not part of the shipped plugin v1.0 runtime contract.
- **Pulses produced by `PitEngine` → `LalaLaunch` → `SignalProvider`:**
  - `TrackMarkers.Pulse.Captured` (first full pair captured)
  - `TrackMarkers.Pulse.LengthDelta` (track length changed)
  - `TrackMarkers.Pulse.LockedMismatch` (locked store differs from live detection beyond tolerance)
- **Evaluators:** `Eval_TrackMarkersCaptured`, `Eval_TrackMarkersLengthDelta`, `Eval_TrackMarkersLockedMismatch` consume the pulses once per track key and latch tokens to avoid repetition.
- **Definitions:** Messages are definition-driven in `MessageDefinitionStore`/`Messages.json` (`MsgId`: `trackmarkers.captured`, `trackmarkers.length_delta`, `trackmarkers.lock_mismatch`); no legacy/adhoc messaging paths are used.
- **Active behaviour:** MSGV1 displays a low/med priority toast per trigger; payload includes track key and stored vs candidate values (for mismatch) for log clarity. Pulses expire after ~3 s if not consumed.

## Pit-exit HUD exports (distance/time)
- `LalaLaunch` derives `PitExit.DistanceM` and `PitExit.TimeS` from the stored exit marker, cached track length, live car track position, and speed. Outputs are zeroed when not in pit lane and update on the 250 ms poll cadence using the `inLane` flag already computed for PitLite, avoiding unnecessary churn while running on track.
- Calculations preserve prior wrapping/clamping behaviour (forward delta, wrap at S/F, integer rounding) and reuse the existing speed epsilon for time estimates; only the gating/cadence changed.

## Reuse guidance
- Treat stored markers as canonical input for pit-related features; use live telemetry only to **propose** changes.
- Reuse the **auto-learn + lock + MSGV1 notify** pattern for other per-track artefacts to keep UX consistent (capture, allow lock, notify on drift).
