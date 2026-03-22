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

## v1.0 – Public documentation + Strategy workflow consolidation

Main user-facing changes since the late-February documentation baseline include:

- Refactored the top-level planner into the **Strategy** tab story instead of older Fuel-first wording.
- Moved preset management into Strategy through the **`Presets...`** modal workflow instead of a standalone Presets tab.
- Clarified **Live Snapshot** auto-source behavior so the live source owns the relevant values when active and the matching manual inputs are disabled.
- Moved **launch controls** such as launch mode and post-launch display timing into **Settings → Launch Settings**.
- Added clear user-facing coverage for **H2H Race** and **H2H Track**.
- Added a GitHub-facing documentation structure with the public `README.md` plus focused `Docs/` pages for quick start, user guidance, dashboards, strategy, H2H, and driver assists.
- Added **Shift Assist** as a documented first-class user feature.
- Kept the distinction between **observing live state** and **committing a stable plan** explicit in user docs.
- Repositioned **PreRace** as a display/on-grid layer under Strategy without implying that it changes planner calculations.
- Cleaned up **Dash Control** so it stays dash-oriented, centered on Cancel Message, Toggle Pit Screen, Primary Dash Mode, Declutter Mode, visibility, and grouped global dash functions.
- Expanded practical guidance for **rejoin warnings**, **pit popups**, and **pit entry assist**.
- Completed the v1 documentation sweep across the main repo docs and canonical subsystem docs so GitHub readers can move from user pages into technical ownership docs without stale Fuel-tab-era wording or broken responsibility boundaries.
- Refreshed repo-facing documentation so the GitHub repo can serve as the canonical user home alongside the canonical subsystem docs.

## v1.1 – Release polish pass

- Shortened Dash Control visibility column labels to **`DRIVER`**, **`STRATEGY`**, and **`OVERLAY`** while keeping tooltip wording explicit.
- Added a release-facing warning below the debug-mode master toggle so troubleshooting features are less likely to be left on accidentally.
- Fixed the Preset Manager `PreRace Mode` selector by reusing the same working ComboBox behavior as the main Strategy tab, restoring normal dropdown interaction while keeping dark-theme readability.
- Hardened the Strategy `Presets...` modal open flow against null preset entries and open-time popup failures so the editor no longer hard-fails as easily.
- Embedded the shipped preset defaults and expanded track-marker defaults directly in the owning code paths so the repo/package ships with the intended first-run data, including the shorter `IMSA 40m` / `Sprint 20m` preset names and additional seeded tracks.
