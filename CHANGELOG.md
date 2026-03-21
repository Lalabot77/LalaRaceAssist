# Changelog

This changelog is written for GitHub readers and focuses on user-facing changes.

Dates are intentionally omitted where the repo docs available in this workspace do not establish a release date with confidence.

## Initial release

- First public plugin package for SimHub + iRacing use.
- Established the core plugin split between runtime calculations and dashboard presentation.
- Included launch support, fuel tools, and dashboard package foundations.

## v0.1

- Early user-facing launch workflow and launch dashboards.
- Basic fuel and race-support features for live use.
- Initial profile-based setup flow for storing repeatable car settings.

## v0.2

- Expanded profile-driven workflow and saved-data usage.
- Improved dashboard package structure for live driving support.
- Added stronger race-session support around planning and dash presentation.

## v0.3

- Added fuller quick-start and user-guide material for testers.
- Strengthened plugin learning/store/lock workflow around fuel and track data.
- Expanded practical dash usage guidance for race sessions.

## v0.4

- Pit entry and pit-loss workflows matured, including learned markers and cleaner pit-cycle guidance.
- Dry/wet data separation and condition-aware storage improved long-run trust.
- Opponent and race-context tooling expanded, including stronger same-class race support.
- Track-scoped planning data became more practical for venue-specific strategy setup.

## v0.5

Main user-facing changes since the late-February documentation baseline include:

- Added **Shift Assist** as a documented first-class user feature.
- Refactored the top-level planner into the **Strategy** tab story instead of older Fuel-first wording.
- Removed the standalone **Presets** tab and moved preset management to the **`Presets...`** modal flow inside Strategy.
- Clarified **live vs manual** behavior so Live Snapshot owns the relevant values when active and disables the matching manual controls.
- Kept the distinction between **observing live state** and **committing a stable plan** explicit in user docs.
- Repositioned **PreRace** as a display/on-grid layer under Strategy without implying that it changes planner calculations.
- Cleaned up **Dash Control** so it stays dash-oriented, centered on Cancel Message, Toggle Pit Screen, Primary Dash Mode, Declutter Mode, visibility, and grouped global dash functions.
- Moved **launch controls** such as launch mode and post-launch display timing into **Settings → Launch Settings**.
- Added clear user-facing coverage for **H2H Race** and **H2H Track**.
- Expanded practical guidance for **rejoin warnings**, **pit popups**, and **pit entry assist**.
- Added dedicated GitHub-facing docs for the dashboard package, strategy system, H2H system, and driver assists.
- Refreshed repo-facing documentation so the GitHub repo can serve as the canonical user home alongside the canonical subsystem docs.
