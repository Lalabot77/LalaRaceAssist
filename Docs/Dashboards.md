# Dashboards

This page is the main driver-facing guide to the Lala dashboard package.

## 1. Overview

Lala dashboards exist to make the plugin's outputs readable while driving. They help you see race context, pit context, timing, alerts, and helper widgets in layouts that are practical to use in SimHub.

Keep the ownership split clear:

- **The plugin owns logic, telemetry interpretation, learning, persistence, and stable outputs.**
- **Dashboards own display, page layout, touch areas, and user interaction.**

That means dashboards are not the source of truth for strategy math, fuel learning, H2H selection, rejoin logic, or pit-entry calculations. They display the outputs the plugin publishes and let you interact with those surfaces in a driver-friendly way.

## 2. Installation / import

At a high level, dashboard setup is:

1. Import the Lala dashboard files into SimHub.
2. Assign the imported dashboards to the screen or device you want to use.
3. Bind **Next Dash** and **Previous Dash** if you want reliable non-touch navigation.
4. Use **Dash Control** and your screen assignments to decide which surfaces you actually use while driving.

Dashboards can be assigned per device, and SimHub lets you combine touch navigation with button bindings. For the wider plugin setup flow, see [Quick Start](Quick_Start.md).

## 3. Navigation and bindings

The released dashboard package is built around simple page navigation plus temporary overlays.

### Main page navigation

On the Primary Driver Dash, the normal page flow is driven by:

- **left touch area** = previous page  
- **right touch area** = next page  
- SimHub **Previous Dash** binding  
- SimHub **Next Dash** binding  

You can use touch only, but physical bindings are strongly recommended for race use.

### Binding scope in SimHub

Depending on how you organise your SimHub setup, those bindings can be configured:

- per dash,
- per device,
- or as broader/global bindings.

Use whichever approach best matches your hardware. The important point is that the Lala dashboards are designed to work with SimHub's standard next/previous dash navigation model.

### Auto-dash switching

If you use SimHub auto-dash switching, the active session type can determine which dashboard page becomes your landing page.

Practical expectations for the current Primary Dash package:

- qualifying sessions commonly land on the **Timing** page,
- practice sessions can use the **Practice** page when you select it manually,
- the race-oriented track/racing awareness pages are not the normal offline-testing surfaces when there are no opponents to reason about.

### Overlays are separate

Overlays are **temporary surfaces**, not part of the normal left/right page loop. They appear independently from normal page navigation when their conditions are active.

## 4. Primary Driver Dash

### Primary Dash overview

The Primary Driver Dash is the main race-driving surface. It is organised around a practical page order so you can move between awareness, standings, timing, practice, H2H context, and pit work without changing the underlying plugin behavior.

Current confirmed page order:

1. **Track Page (Nearby Ahead/Behind)**
2. **Racing Page (Class Standings Awareness)**
3. **Timing**
4. **Practice**
5. **Head-to-Head page**
6. **Pit Pop-Up**

![Primary Dash track situational awareness](Images/PrimaryDash/DriverTrackSA.png)

*Primary Driver Dash track-awareness page.*

### Track Page (Nearby Ahead/Behind)

This is the nearby on-track awareness page.

Use it when you want a quick view of:

- the car ahead,
- the car behind,
- immediate on-track context,
- nearby race pressure while still staying focused on driving.

This page is about **local track situation**, not overall race-order classification. It is the "who is around me right now?" surface.

If you prefer a larger-focus view, the repo also includes a zoomed screenshot for this page:

![Track situational awareness zoom view](Images/PrimaryDash/DriverTrackSA_Zoom.png)

*Zoomed Primary Dash view for nearby ahead/behind context.*

### Racing Page (Class Standings Awareness)

This page shifts from local track situation to **same-class race standings awareness**.

Use it when you want to understand:

- who is ahead of you in class standings,
- who is behind you in class standings,
- race-order context even when those cars are not currently on the same bit of track.

In practical terms:

- **Track Page** = nearby on-track context.  
- **Racing Page** = same-class opponents ahead/behind in standings regardless of lap position.

![Primary Dash racing standings awareness](Images/PrimaryDash/DriverDashRacingOpp.png)

*Racing page focused on same-class standings awareness rather than immediate local track position.*

### Timing screen

The **Timing** page is the qualifying-focused timing surface.

Use it for timing-oriented checks such as:

- lap delta,
- personal best comparison,
- all-time best comparison,
- fuel remaining.

The central delta display can cycle between multiple timing modes:

- session personal best delta  
- estimated lap time  
- all-time best delta  

This can be changed using the **centre touch area** or the configured dash navigation binding.

This is the page you should expect to be most relevant in qualifying workflows, including when session-based auto-switching selects a more timing-oriented landing page.

**Screenshot placeholder:** no dedicated Timing-page screenshot is currently confirmed in the repo's Primary Dash image folder.

### Practice screen

The **Practice** page keeps the timing-style layout but adds more driver-input context.

Current confirmed use:

- timing-style information remains visible,
- throttle and brake inputs are included,
- ABS and TC alerts can be surfaced here,
- the page can be manually selected in practice sessions.

The throttle and brake bars allow you to see:

- brake application percentage,
- throttle application percentage,
- ABS activity,
- traction control / wheel spin alerts.

These alerts are designed primarily as **driver feedback during practice**, rather than race awareness.

Note: the **brake peak indicator** feature is currently temporarily disabled while undergoing an update.

![Primary Dash practice page](Images/PrimaryDash/DriverDashPractice.png)

*Practice page with timing-style context plus driver-input visibility.*

### Head-to-Head page

The **Head-to-Head** page gives you the same H2H view as the standalone H2H overlay, but inside the normal Primary Dash page flow.

Keep the role narrow:

- it is an additional way to view H2H context,
- it does not change H2H logic,
- it remains a display surface for plugin-owned H2H outputs.

For broader H2H concepts, also see [H2H System](H2H_System.md).

![Primary Dash pit pop-up](Images/PrimaryDash/Head2HeadOverlay.png)

### Pit Pop-Up

The **Pit Pop-Up** is the pit-focused page in the Primary Dash flow.

It may appear **automatically when entering pit lane**, or it can be opened manually using the **Pit Screen binding**, depending on your workflow.

Current confirmed elements to expect here:

- traffic side bar,
- top alert area,
- fuel lines,
- pit controls,
- fuel gauge,
- pit box assist.

This is the main **pit work surface** for the Primary Dash package.

![Primary Dash pit pop-up](Images/PrimaryDash/pitpopup.png)

*Pit Pop-Up page with pit controls, fuel information, alert space, and pit-box support.*

### Overlay system / Lala Alerts

The Primary Dash package also uses a separate overlay layer. The main overlay host is **LalaAlerts**.

This is where temporary overlay messaging and widget-driven alerts live, including:

- side **spotter bars** that appear when a car is alongside,
- traffic alerts,
- penalty alerts,
- fuel alerts,
- radio-style messages,
- lap-invalid alerts,
- session-finished alerts,
- position-change alerts.

Think of LalaAlerts as a **temporary alert surface** that sits alongside the normal page flow rather than replacing it.

![Lala Alerts overlay](Images/PrimaryDash/AlertsOverlay.png)

*LalaAlerts overlay showing the separate temporary alert layer used by the Primary Dash package.*

## 5. Shared widgets / overlays

These shared widgets belong to the dashboard package, but the plugin still owns the underlying calculations and activation rules.

### PitEntryAssist

PitEntryAssist is the focused pit-entry braking aid. It is there to help you judge the approach to pit speed and the entry line using plugin-owned pit-entry outputs.

See also [Pit Assist](Pit_Assist.md) for the wider pit-support explanation.

### PitPopUp

PitPopUp is the pit-focused temporary screen or page that brings pit controls, fuel lines, alert space, and pit-box support together when pit work becomes relevant.

### RejoinAssist

RejoinAssist is the recovery/rejoin warning surface. It helps you judge whether you are clear to rejoin, but it remains a presentation layer for plugin-owned rejoin and threat logic.

See [Rejoin Assist](Rejoin_Assist.md) for the full driver-facing system guide.

### LaunchAssist

LaunchAssist surfaces launch-related driver information on supported dashboards. It does not replace **Settings → Launch Settings** or **Launch Analysis**, and it does not become the owner of launch logic.

### StallWidget

StallWidget is a compact helper surface for stalling/restart awareness where the relevant dashboard layout includes it.

## 6. Remaining dashboard package sections

### Strategy Dash

Documentation pending.

### Head-to-Head overlay

Documentation pending.

## 7. Screenshot notes

The screenshots embedded above are limited to images that are already present in the repo under `Docs/Images/PrimaryDash/`.

Where a useful page-specific screenshot is not currently available in that folder, this guide uses a simple placeholder note rather than inventing a filename or implying an image exists when it does not.
