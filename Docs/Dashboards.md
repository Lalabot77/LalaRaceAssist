# Dashboards

This page covers the practical dashboard package at a user-facing level.

## 1. What dashboards are for

Dashboards are the display layer for LalaLaunchPlugin.

They are there to:

- show strategy outputs,
- show driver aids,
- present race context,
- offer limited interaction through bindings or touch areas.

They do **not** own calculations, saved data, strategy math, or learning rules.

## 2. Main dashboard types

### Primary race dash

Use this as the main driver-facing view.

Typical use:

- current race information,
- core strategy outputs,
- pit and rejoin alerts,
- shift and launch visibility where applicable.

### Strategy / support dash

Use this for extra context that does not always need to live on the main screen.

Typical use:

- strategy support values,
- additional fuel and stint context,
- H2H or traffic support,
- setup and monitoring screens.

### Overlay

Use this when you want compact alerts or support widgets on top of another layout.

Typical use:

- alerts,
- compact pit information,
- rejoin warnings,
- minimal strategy support.

### Lovely-based layouts

If you use Lovely integration, the repo may also include or support Lovely-oriented layouts.

Treat these the same way: they are still a presentation layer consuming plugin outputs.

## 3. Dash visibility and page intent

Dash Control lets you manage what appears on the main dash, message/support dash, and overlay separately.

Typical visibility choices include:

- launch assist,
- pit assists,
- automatic pit screen,
- rejoin assist,
- verbose race messages,
- race flags,
- radio messages,
- traffic alerts.

These visibility choices change **where** information appears. They do not change the underlying subsystem logic.

## 4. Main dash controls

The core dash controls are:

- **Cancel Message**
- **Toggle Pit Screen**
- **Primary Dash Mode**
- **Declutter Mode**

### Cancel Message

Use this to dismiss or temporarily suppress pit/rejoin messaging when you need less distraction.

### Toggle Pit Screen

Use this to show or hide the pit screen when you want to override the automatic pit-screen behavior.

### Primary Dash Mode

Use this to cycle the main dash presentation mode.

### Declutter Mode

Use this to reduce visual load when you want a cleaner display.

## 5. Global Dash Functions

User-facing groups are:

### General

For broad dash behavior such as automatic screen selection at session start.

### Dark Mode

For global dash dark-mode behavior, brightness, and manual/auto choices.

### Lovely integration

For Lovely-specific dark-mode coordination when Lovely is present.

### Fuel

For user-facing dash consumption settings around fuel confidence display and pit-in reserve intent.

These are presentation-oriented controls. They do not transfer ownership of strategy or fuel logic to the dash.

## 6. Strategy, PreRace, and dash wording

The dashboards may show:

- live strategy values,
- planner values,
- PreRace information.

Keep the roles straight:

- **Planner values** come from the Strategy workflow.
- **Live values** come from the runtime live systems.
- **PreRace** is an on-grid/display layer only.

Do not treat PreRace screens as proof that the dash owns planner calculations.

## 7. Pit and rejoin screens

Pit and rejoin items are some of the most important practical dash surfaces.

- Pit screens can appear automatically when pit context is active.
- The driver can override with **Toggle Pit Screen**.
- Rejoin alerts are there to support decision-making, not to replace driver judgment.
- If alerts are repeatedly wrong, the usual fix is profile review or saved-data review.

## 8. H2H on dashboards

Supported dashes may show:

- **H2H Race** for same-class race-order comparisons,
- **H2H Track** for nearby same-class on-track comparisons.

These are read-only race-context tools. The dash shows them; the plugin owns the comparison outputs.

## 9. Practical layout advice

A simple setup usually works best:

- **Main dash:** race-critical information only.
- **Support dash:** extra strategy and race-context detail.
- **Overlay:** compact alerts and helper widgets.

If a screen feels busy, use **Primary Dash Mode**, **Declutter Mode**, and visibility toggles before assuming a subsystem is wrong.
