# Changelog

This changelog is user-facing release history.

For internal between-release development history, see `Docs/Internal/Development_Changelog.md`.

## v1.1 (Unreleased)

### Added
- **Overview tab** as the plugin front door for quick links, release-check visibility, and at-a-glance status.
- **Plugin-owned pit/custom command workflow** with built-in pit actions, custom-message slots, and in-plugin transport selection.
- **Pit Fuel Control + Tyre Control** command surfaces for dashboard/hardware bindings (`LalaLaunch.Pit.FuelControl.*`, `LalaLaunch.Pit.TyreControl.*`).
- **ClassBest export family** for class session-best holder visibility on dashboards.

### Changed
- **Strategy/PreRace status logic** refreshed with scenario-first outcomes and clearer status colors/text for no-stop, one-stop, and multi-stop contexts.
- **Pit command transport behavior** now defaults to direct window-message delivery with bounded legacy fallback options.
- **Pit Fuel Control behavior** refined so AUTO ownership/cancel behavior is clearer and non-AUTO OFF/MAN follows iRacing MFD fuel-enable truth.
- **Pit command feedback contract** now standardizes dash severity exports (`Pit.Command.Severity`, `Pit.Command.SeverityText`), keeps `Pit.Command.Active` as a restartable hold pulse (including repeated identical message retriggers), and suppresses lower-severity feedback while higher-severity feedback is still active.
- **Pit feedback severity visual mapping** updated for dashboards: `Caution` is steady (no blink) and `Warning` blinks for 1 second at 750ms.
- **Dashboard/navigation documentation** and Overview-first workflow guidance were aligned across user docs.

### Fixed
- Corrected one-stop feasibility checks to use pit-stop refill capacity rather than start-of-race free tank space.
- Fixed wet/dry PB and pace persistence routing edge cases around lap-validation timing and wet-compound detection.
- Hardened pit/custom command observability and confirmation wording so attempted transport is not shown as guaranteed in-sim effect.
- Fixed Pit Fuel Control OFF->MAN command emission to avoid additive plus-sign payload form and aligned over-max fuel feedback wording contract (`FUEL MAX` / `AUTO FUEL … >MAX`) without changing refuel math or send strategy.

## v1.0 – Initial Public Release

### Added
- Public plugin package for SimHub + iRacing users.
- Public documentation set (`README.md` plus focused `Docs/` guides).
- Strategy workflow documentation as the main planning story.
- Shift Assist and H2H documentation as first-class user features.

### Changed
- Consolidated planning language around the Strategy tab and Strategy preset workflow.
- Clarified live snapshot behavior, launch settings location, and dashboard control guidance.
- Synced user-facing and subsystem docs so navigation between user docs and technical ownership docs is coherent.

### Fixed
- Removed stale/legacy wording in public docs to match current v1 behavior and workflows.
