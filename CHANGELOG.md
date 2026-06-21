# Changelog

This changelog is user-facing release history.

For internal between-release development history, see `Docs/Internal/Development_Changelog.md`.

## v1.1 (Unreleased)

### Added
- Pit Service Regulations selector for Strategy and Race Presets: Default models sequential fuel then tyres, IMSA models simultaneous service, and NEC models simultaneous service with a car-profile NEC refuel-rate factor.
- Profile CAR tab NEC Refuel Rate Factor (%) setting, defaulting to 100% and used only when NEC pit service regulations are selected.
- Pit Stop Debrief now shows any finite valid completed box delta regardless of magnitude, treats `BOX ... (Δ PENDING)` as missing/invalid/not-completed source only, and labels repair-influenced SummaryText box targets as `BOX MAND REPAIR`, `BOX OPT REPAIR`, or `BOX REPAIRS` while preserving `Pit.Box.LastDeltaSec` as `target - actual` and the debrief summary as `actual - target`.
- Pit Stop Debrief now keeps first box-entry target/elapsed evidence pending until completed box-delta evidence is available, and preserves repair labels even when repair-left telemetry appears after the target latch.
- **Pit Stop Debrief v1** exports latched post-stop verdict fields for alerts/debug dashboards (`LalaLaunch.Pit.Debrief.*`) and emits one structured final log line per completed debrief.
- Strategy Dash Advanced/Simple mode toggle binding with dashboard-readable status exports (`LalaLaunch.StrategyDash.AdvancedMode`, `LalaLaunch.StrategyDash.ModeText`).
- Pit Fuel Control DATA selector (`LIVE`/`PLAN`) with data actions/exports for dash and hardware bindings.
- **Overview tab** as the plugin front door for quick links, release-check visibility, and at-a-glance status.
- **Plugin-owned pit/custom command workflow** with built-in pit actions, custom-message slots, and fixed direct in-plugin transport.
- **Pit Fuel Control + Tyre Control** command surfaces for dashboard/hardware bindings (`LalaLaunch.Pit.FuelControl.*`, `LalaLaunch.Pit.TyreControl.*`).
- **ClassBest export family** for class session-best holder visibility on dashboards.
- Strategy Dash idle/control-centre status exports (`LalaLaunch.Plugin.VersionNumberText`, `LalaLaunch.Plugin.StatusText`, `LalaLaunch.Plugin.StatusLineText`) and dashboard action `LalaLaunch.MonitorSystemToggle`.

### Changed
- Existing pit-service dashboard outputs now reflect the selected pit-service regulation without requiring new dashboard properties: `Fuel.Live.RefuelRate_Lps` shows the active effective service rate (including NEC percentage when selected), and PitExit countdown/position/gaps use the regulation-aware stop-loss estimate while preserving an active stop's countdown seed unless a longer estimate or distinguishable service-input change needs to refresh it. Stored profile refuel rate remains the normal baseline.
- Existing pit-service dashboard outputs now reflect the selected pit-service regulation without requiring new dashboard properties: `Fuel.Live.RefuelRate_Lps` shows the active effective service rate (including NEC percentage when selected), and PitExit countdown/position/gaps refresh from the updated regulation-aware stop-loss estimate. Stored profile refuel rate remains the normal baseline.
- Profile CAR tab Pit Stop / Pit Assist layout now keeps Base Tank constrained, aligns the Refuel Rate / NEC factor / Tyre Change Time numeric editors, shows Refuel Rate to two decimals, and places Pit Entry Decel/Buffer as full-width bottom sliders while preserving existing ownership and save/load behavior.
- Runtime boxed-stop prediction and Strategy Planner stop modelling now share one regulation-aware pit service-time authority, keeping PitExit/total stop loss and planner stop timing aligned while preserving existing export names.
- Pit Stop Debrief summary wording now becomes progressively useful during the stop, reports entry/box signed deltas, service from actual fuel added plus tyres, and `STRAT Δ` for the final strategy comparison while keeping exit prediction in debug/log fields instead of the driver-facing summary.
- Documentation sweep: added canonical technical subsystem doc `Docs/Subsystems/League_Class_System.md` and aligned cross-references in H2H/Dash/internal docs for League Class behavior.
- League Class presentation alignment now extends to CarSA-facing class identity fields and H2HTrack class colors: `Car.Player.ClassName/ClassColor/ClassColorHex`, `Car.Ahead01..05.*` and `Car.Behind01..05.*` class-facing fields, plus `H2HTrack.Ahead/Behind.ClassColor/ClassColorHex` now publish effective League Class presentation when enabled/resolved, with native fallback unchanged.
- Pit Fuel Control SOURCE no longer includes PLAN. SOURCE is now `STBY`/`NORM`/`PUSH`/`SAVE`; DATA chooses whether `PUSH`/`SAVE` use live or planner/profile memory burn assumptions. Legacy `SetPlan` remains as a compatibility action that selects DATA PLAN and parks SOURCE at STBY.
- **Strategy/PreRace status logic** refreshed with scenario-first outcomes and clearer status colors/text for no-stop, one-stop, and multi-stop contexts.
- **Pit command transport behavior** is now fixed to plugin-owned direct window-message delivery; the Settings transport selector and legacy foreground fallback workflow were removed.
- **Pit Fuel Control behavior** refined so AUTO ownership/cancel behavior is clearer and non-AUTO OFF/MAN follows iRacing MFD fuel-enable truth.
- **Pit command feedback contract** now standardizes dash severity exports (`Pit.Command.Severity`, `Pit.Command.SeverityText`), keeps `Pit.Command.Active` as a restartable hold pulse (including repeated identical message retriggers), suppresses lower-severity feedback while higher-severity feedback is still active, and uses specific warning texts for window/chat/send/confirmation failures.
- **Pit feedback severity visual mapping** updated for dashboards: `Caution` is steady (no blink) and `Warning` blinks for 1 second at 750ms.
- **Dashboard/navigation documentation** and Overview-first workflow guidance were aligned across user docs.
- **Monitor System setting placement** moved from Launch UI / Bindings to Dash Control -> Global Dash Functions -> General. Phase 2B pit-stop monitoring now adds edge-triggered `REFUEL OFF`, `MFD FUEL LOW`, and `EXIT FUEL SHORT` warnings while preserving Phase 2A trigger evidence logs.
- **Monitor System warning wording** is clearer for drivers: `CHECK FUEL DATA`, `OPPONENT DATA UNRELIABLE`, `TRAFFIC DATA UNRELIABLE`, and `H2H DATA UNRELIABLE` replace the older internal-style wording without changing triggers, severity, exports, CSV schema, or dashboard JSON.
- **Monitor System late-race fuel guidance** now suppresses end-of-race `REFUEL OFF`, `MFD FUEL LOW`, and predictive `BASELINE SHORT` noise once finish authority says the race is effectively ending, while preserving `EXIT FUEL SHORT` and system-health/reliability warnings.

### Fixed
- Fixed Pit Entry brake cues so once the pit limiter is active and the car is within the existing Pit Entry compliance tolerance (up to +1.0 kph), the driver sees an existing compliant speed cue instead of continued `BRAKE NOW` / `BRAKE HARD` guidance.
- Fixed `FUEL DATA RECOVERED` so it auto-clears to `MONITOR READY` after a short confirmation hold when no newer MonitorSystem message has replaced it.
- Fixed the Fuel Data selector display while idle/no live session/no profile so `LalaLaunch.Fuel.Refuel.DataMode` visibly follows the selected DATA mode without sending pit commands or changing refuel math.
- Fixed Pit Stop Debrief box delta and fuel-target latching so valid completed-box deltas use the existing `Pit.Box.LastDeltaSec` authority without changing its dashboard sign contract, exact `0.0s` box deltas no longer stay pending, and non-zero requested fuel targets survive box-exit reset ticks while in-box refuel cancels clear the target.
- Fixed Pit Stop Debrief service/timing review latches so fuel-added evidence survives box-exit gauge reset and actual-vs-predicted loss compares total-stop-equivalent values.
- Fixed PreRace/Friends export registration so dashboards should use single-prefixed `LalaLaunch.PreRace.*` and `LalaLaunch.Friends.Count` names instead of accidental `LalaLaunch.LalaLaunch.*` names.
- Corrected one-stop feasibility checks to use pit-stop refill capacity rather than start-of-race free tank space.
- Fixed wet/dry PB and pace persistence routing edge cases around lap-validation timing and wet-compound detection.
- Hardened pit/custom command observability and confirmation wording so attempted transport is not shown as guaranteed in-sim effect.
- Fixed Pit Fuel Control OFF->MAN command emission to avoid additive plus-sign payload form and aligned over-max fuel feedback wording contract (`FUEL MAX` / `AUTO FUEL … >MAX`) without changing refuel math or send strategy.
- Fixed live pit tank-space/WillAdd guidance so stale Strategy max-fuel presets no longer under-cap runtime refuel space when live session tank-cap authority is available.

## v1.0 – Initial Public Release

### Added
- Pit Fuel Control Push/Save basis mode toggle (`LIVE`/`PROFILE`) with mode action/export support for dash and hardware bindings.
- Public plugin package for SimHub + iRacing users.
- Public documentation set (`README.md` plus focused `Docs/` guides).
- Strategy workflow documentation as the main planning story.
- Shift Assist and H2H documentation as first-class user features.

### Changed
- Pit Fuel Control PUSH/SAVE can optionally use profile-backed burn values (guarded around live stable NORM burn); NORM and PLAN behavior remain unchanged.
- Consolidated planning language around the Strategy tab and Strategy preset workflow.
- Clarified live snapshot behavior, launch settings location, and dashboard control guidance.
- Synced user-facing and subsystem docs so navigation between user docs and technical ownership docs is coherent.

### Fixed
- Removed stale/legacy wording in public docs to match current v1 behavior and workflows.
