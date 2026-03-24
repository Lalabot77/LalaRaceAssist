# Internal Release Checklist (Lala Race Assist)

> Maintainer-only process checklist for future plugin releases.
> 
> Scope: practical release workflow validated during the first successful public release.

## How to use this checklist

- [ ] Copy this checklist into the release tracking issue/notes for the target version.
- [ ] Work top-to-bottom; do not mark a section complete until every critical checkbox is done.
- [ ] Track open findings inline as **Blocker**, **Acceptable for release**, or **Post-release cleanup**.

---

## 1) Release planning

- [ ] Define release scope in one sentence (what is included, what is intentionally excluded).
- [ ] Triage known issues into:
  - [ ] **Blockers** (must fix before publish)
  - [ ] **Non-blockers** (safe to ship)
  - [ ] **Deferred cleanup** (schedule after release)
- [ ] Decide target version number and confirm it matches release notes + GitHub release title.
- [ ] Draft release notes early (features, fixes, known caveats, upgrade notes).
- [ ] Confirm no hidden scope creep since final test pass.

## 2) Code and plugin readiness

- [ ] Build plugin in release configuration successfully.
- [ ] Confirm no unintended dependency regressions (especially removed/renamed dash properties).
- [ ] Confirm subsystem boundaries are still respected (no cross-subsystem shortcuts added late).
- [ ] Confirm user-facing behavior changes are reflected in docs (README, Quick Start, User Guide, relevant feature pages).
- [ ] Confirm internal docs/log/property inventories are still aligned where applicable.

## 3) Clean install / new-user test

- [ ] Remove any existing local plugin copy from SimHub.
- [ ] Remove/reset local plugin settings/data needed to validate first-run behavior.
- [ ] Install plugin package as a new user would.
- [ ] Verify first-run defaults seed correctly.
- [ ] Verify required plugin tabs/pages load.
- [ ] Verify plugin startup has no exceptions or repeated warning spam.

## 4) Dashboard package validation

- [ ] Import each dashboard package into a clean/fresh dashboard context.
- [ ] Verify no missing-property or broken-expression errors.
- [ ] Verify shared widgets/components are current (no stale embedded copies).
- [ ] Verify old third-party property dependencies were fully removed.
- [ ] Verify key visibility/navigation behavior works (tab switches, conditional panels, fallbacks).

## 5) Runtime validation (session + logs)

- [ ] Run at least one real session or representative replay session.
- [ ] Exercise major systems at least once:
  - [ ] Strategy/fuel planning surfaces
  - [ ] Pit-related surfaces
  - [ ] Launch-related displays
  - [ ] H2H/opponent context
  - [ ] Dash visibility/driver cues
- [ ] Separate replay-specific oddities from true release blockers.
- [ ] Review SimHub logs for:
  - [ ] Exceptions/errors
  - [ ] Missing properties
  - [ ] Expression faults
  - [ ] Stale dependency/property references

## 6) Documentation and assets

- [ ] Confirm `README.md` renders cleanly on GitHub (headings, links, images, spacing).
- [ ] Confirm `Docs/Quick_Start.md` is current for install and first setup.
- [ ] Confirm `Docs/User_Guide.md` is current for practical usage.
- [ ] Confirm screenshots/assets are cleanly named, stored, and linked.
- [ ] Confirm optional setup items are documented where users actually need them.
- [ ] Finalize release notes text from validated behavior (not draft assumptions).

## 7) GitHub release preparation

- [ ] Verify release artifacts (zip/package contents, naming, expected files).
- [ ] Verify repository landing page presentation (README top section, key links, first impression).
- [ ] Verify Issues/Discussions/templates are in a usable state for incoming feedback.
- [ ] Prepare announcement copy (Discord/community) with:
  - [ ] What is new
  - [ ] How to install/update
  - [ ] Known caveats
  - [ ] Where to report issues

## 8) Final go / no-go gate

- [ ] Reclassify every remaining item as:
  - [ ] **Blocker → stop release**
  - [ ] **Acceptable risk → ship with note**
  - [ ] **Future cleanup → backlog**
- [ ] Download the actual GitHub release package and install/test that exact artifact.
- [ ] Confirm package contents and published docs/release notes match exactly.
- [ ] Explicit go/no-go decision recorded by owner.

## 9) Post-release owner actions

- [ ] Monitor early Issues/Discussions for the first feedback window.
- [ ] Label/triage early bug reports quickly (blocker, hotfix candidate, next release).
- [ ] Capture lessons learned while fresh (what failed, what saved time, what to tighten).
- [ ] Update this checklist when workflow improvements are proven.

---

## First-release lessons to preserve

- [ ] Always run a true clean-install test before publishing.
- [ ] Always re-import dashboards fresh to catch stale property references.
- [ ] Treat replay-only quirks carefully; do not over-classify them as blockers without impact check.
- [ ] Search logs for missing properties/expressions before calling release-ready.
- [ ] Validate the final downloadable GitHub artifact yourself before public announcement.
- [ ] Keep docs and package synchronized so users do not see mixed instructions.
