# Changelog

This changelog is user-facing release history.

For internal between-release development history, see `Docs/Internal/Development_Changelog.md`.

## v1.1 (Unreleased)

### Added
- Strategy Dash Advanced/Simple mode toggle binding with dashboard-readable status exports (`LalaLaunch.StrategyDash.AdvancedMode`, `LalaLaunch.StrategyDash.ModeText`).
- Pit Fuel Control DATA selector (`LIVE`/`PLAN`) with data actions/exports for dash and hardware bindings.
- **Overview tab** as the plugin front door for quick links, release-check visibility, and at-a-glance status.
- **Plugin-owned pit/custom command workflow** with built-in pit actions, custom-message slots, and fixed direct in-plugin transport.
- **Pit Fuel Control + Tyre Control** command surfaces for dashboard/hardware bindings (`LalaLaunch.Pit.FuelControl.*`, `LalaLaunch.Pit.TyreControl.*`).
- **ClassBest export family** for class session-best holder visibility on dashboards.

### Changed
- Documentation sweep: added canonical technical subsystem doc `Docs/Subsystems/League_Class_System.md` and aligned cross-references in H2H/Dash/internal docs for League Class behavior.
- League Class presentation alignment now extends to CarSA-facing class identity fields and H2HTrack class colors: `Car.Player.ClassName/ClassColor/ClassColorHex`, `Car.Ahead01..05.*` and `Car.Behind01..05.*` class-facing fields, plus `H2HTrack.Ahead/Behind.ClassColor/ClassColorHex` now publish effective League Class presentation when enabled/resolved, with native fallback unchanged.
- Pit Fuel Control SOURCE no longer includes PLAN. SOURCE is now `STBY`/`NORM`/`PUSH`/`SAVE`; DATA chooses whether `PUSH`/`SAVE` use live or planner/profile memory burn assumptions. Legacy `SetPlan` remains as a compatibility action that selects DATA PLAN and parks SOURCE at STBY.
- **Strategy/PreRace status logic** refreshed with scenario-first outcomes and clearer status colors/text for no-stop, one-stop, and multi-stop contexts.
- **Pit command transport behavior** is now fixed to plugin-owned direct window-message delivery; the Settings transport selector and legacy foreground fallback workflow were removed.
- **Pit Fuel Control behavior** refined so AUTO ownership/cancel behavior is clearer and non-AUTO OFF/MAN follows iRacing MFD fuel-enable truth.
- **Pit command feedback contract** now standardizes dash severity exports (`Pit.Command.Severity`, `Pit.Command.SeverityText`), keeps `Pit.Command.Active` as a restartable hold pulse (including repeated identical message retriggers), suppresses lower-severity feedback while higher-severity feedback is still active, and uses specific warning texts for window/chat/send/confirmation failures.
- **Pit feedback severity visual mapping** updated for dashboards: `Caution` is steady (no blink) and `Warning` blinks for 1 second at 750ms.
- **Dashboard/navigation documentation** and Overview-first workflow guidance were aligned across user docs.

### Fixed
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
