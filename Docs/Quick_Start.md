# Quick Start

This guide is for getting the plugin working quickly with trustworthy data.

If you want the full detail afterwards, continue to the [User Guide](User_Guide.md).

## 1. What this plugin is for

LalaLaunchPlugin is a SimHub plugin for **iRacing** that:

- learns fuel, pace, pit-loss, and marker data,
- stores that data per car, track, and condition,
- publishes stable outputs for dashboards,
- supports strategy planning, launch review, pit assistance, rejoin warnings, Shift Assist, and H2H race context.

## 2. Install

Copy the plugin files into your SimHub installation, then restart SimHub.

### Required for now

- `LaunchPlugin.dll`
- `RSC.iRacingExtraProperties.dll`

### Optional

- `DahlDesign.dll` if you want the supported Lala dash package that depends on it.

## 3. Import dashboards

Typical layout:

- **Primary race dash** for the main driver view.
- **Strategy / support dash** for extra planning and support data.
- **Overlay** for compact alerts.
- **Lovely-based layouts** if you use the Lovely ecosystem.

Dashboards display plugin outputs. They do not learn data or become the source of truth.

## 4. First plugin check

Open the plugin in SimHub and confirm the top-level tabs are:

1. **Strategy**
2. **Profiles**
3. **Dash Control**
4. **Launch Analysis**
5. **Settings**

If you are looking for presets, use **Strategy → `Presets...`**. There is no separate top-level Presets tab.

## 5. Bind the important controls

Open **Dash Control** and bind at least these:

- **Cancel Message**
- **Toggle Pit Screen**

Strongly recommended after that:

- **Primary Dash Mode**
- **Declutter Mode**

These are the main everyday dash controls.

## 6. First session habits

### Drive clean laps first

Before judging the numbers, drive a few clean laps without:

- pit entry,
- obvious incidents,
- invalid laps,
- messy off-tracks.

The plugin needs clean laps to build trustworthy live fuel and pace confidence.

### Learn pit data once per car/track

Do at least one clean pit cycle so the plugin can learn or confirm:

- pit-lane loss,
- pit entry marker,
- pit exit marker.

Once those values look sensible, review and lock them in **Profiles**.

### Do not lock too early

Avoid locking fuel or average lap-time values immediately. Let the system learn representative data first.

## 7. Start with Strategy

The **Strategy** tab is the planning entry point.

### Understand the two planning modes

- **Profile / manual planning:** stable planning from saved data and your chosen inputs.
- **Live Snapshot:** follows live session inputs when they are available and ready.

When Live Snapshot is active, the relevant manual controls are disabled. There are no old “Use Live” toggles to manage.

### PreRace reminder

PreRace belongs to the Strategy workflow, but it is only an **on-grid/display layer**. It does not change planner calculations.

## 8. During a race: what to trust

Once the system has settled, you can usually trust:

- stable strategy outputs,
- locked pit-loss and marker data,
- pit-entry assist,
- pit popups,
- rejoin warnings,
- H2H race/track widgets on supported dashboards.

If something is repeatedly wrong, the usual cause is saved data or thresholds that need review.

## 9. Quick fixes when something looks wrong

- **Fuel or pace looks wrong:** unlock the affected values and gather clean laps.
- **Pit-loss looks wrong:** relearn with one clean pit cycle, then lock again.
- **Pit entry cues feel wrong:** review pit markers and pit-entry settings in the profile.
- **Shift Assist cues feel early, late, or inconsistent:** review the learned or stored Shift Assist values for the active profile, then retest cleanly.
- **Launch tuning feels inconsistent:** keep a stable baseline, record several launches, then review summaries/traces in **Launch Analysis** before changing settings.
- **Rejoin or pit popups are distracting:** use **Cancel Message** in the moment, then review thresholds or saved data later.
- **Dash looks wrong:** remember the plugin owns the data; the dash is usually only showing it.

## 10. Next reading

- [User Guide](User_Guide.md)
- [Dashboards](Dashboards.md)
- [Strategy System](Strategy_System.md)
- [Shift Assist](Shift_Assist.md) for cueing, learning, and locking guidance
- [Launch System](Launch_System.md) for Launch Settings and Launch Analysis workflow
- [H2H System](H2H_System.md)
- [Rejoin and Pit Assists](Rejoin_And_Pit_Assists.md)
