# Launch Mode

Validated against commit: HEAD
Last updated: 2026-03-22
Branch: work

## Purpose
Launch Mode owns the live race-start assistance path. It coordinates:
- launch arming and activation,
- the manual launch action,
- launch blocking/abort conditions,
- launch result capture,
- the handoff to Launch Analysis for saved trace review.

For GitHub readers, keep this boundary clear:
- **Settings → Launch Settings** owns launch configuration.
- **Launch Mode** owns the live start-state machine.
- **Launch Analysis** is the separate post-run review surface.

## Scope and boundaries
This subsystem covers the live launch state and recording workflow.
It does **not** own:
- dashboard artwork,
- generic dash visibility toggles,
- rejoin logic,
- saved profile systems outside launch-specific values,
- the user-facing explanation page in `Docs/Launch_System.md`.

## Inputs (source + cadence)
### Runtime telemetry
- Vehicle speed.
- Start-state/session-state context.
- Launch-related telemetry used to determine whether the car is idle, waiting, active, or complete.
- Post-launch telemetry used for summary/trace capture.

### User / settings inputs
- `LaunchMode` action for manual prime / re-enable / abort behavior.
- Launch configuration from **Settings → Launch Settings**.
- Results-display time and launch summary/trace file locations.

### Cross-subsystem blockers
- In-pit state.
- Serious rejoin state (spin / wrong-way style blockers).
- Session identity changes that force a reset.

## Internal state
Key runtime state includes:
- user-disabled latch,
- manual-prime latch,
- current launch phase/state,
- launch abort latch,
- launch success / completion flags,
- launch end timestamp,
- active telemetry logger / summary writer state.

## Calculation blocks (high level)
### 1) Eligibility and blocking
Launch Mode can be blocked before or during a launch attempt when:
- the car is in pits,
- a serious rejoin incident is active,
- session identity changed,
- the runtime no longer considers the launch attempt valid.

When blocked, Launch Mode is prevented from arming or is aborted back to idle.

### 2) Manual action behavior
The `LaunchMode` action behaves as a three-way control:
- if launch was hard-disabled, pressing it re-enables launch mode,
- if launch is idle, pressing it manual-primes a launch attempt,
- if launch is already in progress, pressing it aborts and sets the user-disabled latch.

Manual prime is intentionally time-limited rather than staying armed forever.

### 3) Arming and live launch progression
Once eligible, the subsystem progresses through its launch phases using current telemetry and start-state context.
At a high level it manages:
- idle / waiting behavior,
- active launch behavior,
- completion handling,
- post-launch results visibility.

### 4) Manual-prime timeout
Manual-prime mode has a **30-second timeout**. If the launch does not actually start inside that window, the attempt is canceled and the subsystem behaves like a user-disabled abort until the driver explicitly re-enables it.

### 5) Completion and review handoff
When a launch completes successfully enough to be recorded, the subsystem writes:
- a one-line summary CSV entry when enabled,
- a detailed launch trace file,
- launch-summary data that the **Launch Analysis** tab can later read and review.

## Outputs (exports + logs)
### Core exports
Representative launch-facing exports include:
- `LaunchModeActive`
- launch-state / launch-result visibility outputs used by dashes and the results view
- saved summary / trace data consumed by Launch Analysis

The export inventory remains canonical in `Docs/Internal/SimHubParameterInventory.md`.

### Logs
Launch logging is meant to explain state transitions rather than stream noise. Important log families include:
- manual prime / re-enable / abort actions,
- blocked launch reasons,
- manual-prime timeout,
- launch trace open/append/close errors,
- summary/trace capture outcomes.

Canonical message wording remains in `Docs/Internal/SimHubLogMessages.md` where applicable.

## Dependencies / ordering assumptions
- Rejoin state must be current before launch-block checks run.
- Launch update/order must happen before final launch-result display logic.
- Launch trace and summary writers are downstream of the live launch state rather than independent features.

## Reset rules
Launch state resets on:
- session identity change,
- explicit abort,
- completion teardown,
- manual disable / re-enable flow,
- any broader runtime reset that clears transient launch state.

## Failure modes / safeguards
- **Blocked by pits or serious rejoin:** launch will not arm or continue.
- **Manual-prime timeout:** launch returns to idle and behaves as user-disabled until re-enabled.
- **File IO issues:** traces/summaries may fail to save, but the live launch state machine still completes independently.
- **Replay or unusual timing:** validate with logs rather than assuming live-session timing behavior is identical.

## Test checklist
- Trigger `LaunchMode` from idle and confirm manual-prime behavior.
- Let manual prime expire and confirm timeout + re-enable behavior.
- Trigger a blocked state (pit lane or serious rejoin) and confirm launch cannot proceed.
- Complete a clean launch and confirm summary/trace capture appears for Launch Analysis.
- Confirm post-launch results hide after the configured results-display time.

## v1 documentation note
User-facing docs should explain launch as **Launch Settings + Launch Analysis + live launch behavior**. This subsystem doc owns the technical contract for the live launch path and recording handoff.
