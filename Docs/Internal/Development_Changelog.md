# Development Changelog

This file tracks internal development history between releases.
The public user-facing release history is maintained in the root `CHANGELOG.md`.

## Pre-v1 public development history

### Initial release foundations
- Established the first public plugin packaging path for SimHub + iRacing testing.
- Set the core split between runtime calculations and dashboard presentation.
- Landed early launch support, fuel tooling, and dashboard package foundations.

### v0.1
- Early user-facing launch workflow and launch dashboards.
- Basic fuel and race-support features for live use.
- Initial profile-based setup flow for repeatable car settings.

### v0.2
- Expanded profile-driven workflow and saved-data usage.
- Improved dashboard package structure for live driving support.
- Added stronger race-session support around planning and dash presentation.

### v0.3
- Added fuller quick-start and user-guide material for testers.
- Strengthened learning/store/lock workflow around fuel and track data.
- Expanded practical dash usage guidance for race sessions.

### v0.4
- Matured pit entry and pit-loss workflows, including learned markers and cleaner pit-cycle guidance.
- Improved dry/wet data separation and condition-aware storage.
- Expanded opponent and race-context tooling, including stronger same-class race support.
- Made track-scoped planning data more practical for venue-specific strategy setup.

## Post-v1.0 development

### Dashboard packaging
- Updated dash control visibility labels to `DRIVER`, `STRATEGY`, and `OVERLAY` while preserving clear tooltips.
- Added a release-facing warning below the debug-mode master toggle to reduce accidental always-on troubleshooting.

### Documentation
- Continued v1 documentation alignment between public guides and subsystem ownership docs.
- Kept Strategy-first wording and live-vs-committed plan distinctions explicit in user-facing docs.

### Release preparation
- Hardened Strategy `Presets...` modal open flow against null preset entries and popup open failures.
- Fixed Preset Manager `PreRace Mode` dropdown behavior by reusing the validated ComboBox interaction pattern.
- Embedded shipped preset defaults and expanded track-marker defaults in owning code paths for predictable first-run packaging.

### Plugin UI
- Added a new top-level `OVERVIEW` tab as the plugin landing page/front door.
- Added quick-link buttons, lightweight runtime status text, and a fail-soft GitHub latest-release check (`Check Now` + `Open Releases`).
- Added dashboard preview placeholders that document the exact drop-in paths/names for future images (`Assets/Overview/*.png`) without shipping binaries in this branch.
