# Lala Plugin – Tester Quick Start Guide

Get you racing with reliable data, fast.

**Read time:** ~10 minutes.

This guide focuses on the minimum setup and habits needed to get trustworthy data. Advanced tuning and inactive/back-shelf systems are intentionally left out.

## 1. What This Plugin Does

Lala Plugin is a SimHub race-engineering plugin for iRacing. In normal use it:

- Learns fuel usage, lap pace, pit-lane loss, and track-specific markers.
- Stores data per car, per track/layout, and per condition (dry/wet).
- Feeds stable values to dashboards so you are not chasing noisy lap-to-lap swings.
- Provides pit, rejoin, launch, and race-context assistance where the supported dashes expose it.

**Important mindset:** let it learn → lock good values → trust the dash. If you constantly tweak things mid-run, you will usually make the data worse.

## 2. Installation

### 2.1 Copy Plugin DLLs

Go to your main SimHub folder (for example `C:\Program Files (x86)\SimHub`), copy these files in, then restart SimHub.

**Required:**
- `RSC.iRacingExtraProperties.dll`
- `LaunchPlugin.dll`

**Optional:**
- `DahlDesign.dll` (only needed if you plan to use the Lala dash package)

### 2.2 Import Dashboards

Import the dashboards you want by double-clicking the package or using SimHub import. Typical layout:

- **Lala-Dash** = primary race dash, often on a phone or tablet behind the wheel.
- **Strategy / secondary dash** = extra strategy and support data on another device or screen area.
- **Lala Overlay** = extra alerts that can be overlaid anywhere on screen.
- **Lovely Dashboard – Lala Edition(s)** = optional Lovely-based layouts; these can overwrite existing Lovely files, so import carefully.

All of the dashes can also be converted to overlays in SimHub Dash Studio if that suits your layout better.

## 3. Mandatory Setup

Open **SimHub → Lala Plugin → Dashes / dash-control area**.

### 3.1 Bind These Two Buttons

Bind both of these to something you can reach while driving:

- **Cancel Msg Button**: cancels pit and rejoin popups and temporarily suppresses repeated alerts.
- **Pit Screen Toggle**: manually shows or hides pit-related screens when you want to override the automatic pit screen behaviour.

If you bind nothing else, bind these two.

### 3.2 Dash Visibility Toggles

Use the main dash / secondary dash / overlay visibility toggles to control what appears where. If you are unsure, start with the defaults and only disable items that you know you do not want.

## 4. First Session Checklist

This prevents bad data and broken profiles.

### 4.1 First Laps (Learning Phase)

Drive 3–5 clean laps with:

- no off-tracks,
- no pit entries,
- no black flags.

Let fuel and pace stabilise before judging the numbers.

### 4.2 Learn Pit-Lane Loss and Markers (Do This Once Per Track)

1. Drive several clean laps first.
2. Enter pit lane cleanly and complete a normal pit entry/exit cycle.
3. Finish at least one full out-lap.
4. When pit-lane loss and markers look sensible, go to **Profiles / Tracks** and lock the values you trust.

You usually only need to do this once per car/track unless the track changes or the saved data is clearly wrong.

### 4.3 What Not to Lock Yet

Do not lock fuel-per-lap or average lap-time values during very early testing. Let the plugin build representative dry/wet data first, then lock it when it looks sensible.

## 5. Fuel Planning Basics

The Fuel tab has two distinct modes:

- **Live Snapshot mode**: shows what the current session is doing right now and follows the live leader pace delta when available.
- **Profile mode**: uses your stored profile/track planning inputs and stays stable until you change them.

If Live Snapshot does not have a valid live leader pace yet, the pace-vs-leader effect falls back safely instead of reusing stale hidden manual values.

## 6. During a Race

### What to trust once the system has settled

- Stable fuel numbers and pit strategy outputs.
- Locked pit-lane loss and marker-driven pit guidance.
- Pit-entry assist and rejoin warnings.
- H2H / race-context widgets if your dash package includes them.

### What to avoid

- Constant UI tweaking mid-race.
- Locking values after a messy lap or bad pit cycle.
- Assuming every early-stint number is final before confidence has built up.

## 7. If Something Looks Wrong (Quick Fixes)

- **Fuel looks wrong** → unlock the affected fuel values and drive clean laps.
- **Pit timing looks wrong** → unlock pit loss, do one clean pit cycle, then re-lock it.
- **Pit entry cues look wrong** → verify the saved track markers and re-learn if needed.
- **Too many popups** → use Cancel Msg and reduce the visibility toggles that feed your screens.
- **Weird numbers after an update** → zero and relearn only the affected data, not everything.

Rule of thumb: **unlock → relearn cleanly → lock again**.

## 8. Where Data Is Stored

- **Plugin data (JSON):** `SimHub\PluginsData\Common\LalaPlugin`
- **Logs:** `SimHub\Logs`

Only edit those files manually if you know exactly what you are doing.

## 9. Help Improve the System

Please report bugs, odd behaviour, and reproducible edge cases using the shared logging / feedback process for the plugin package you received.

## 10. Final Advice

This plugin rewards clean driving and patience. Let it learn, lock good values, and do not fight it mid-session.
