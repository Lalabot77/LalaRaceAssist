# Launch System

The Launch system helps you prepare, execute, and review race starts more consistently.

Think of it as two related parts:

- **live launch behavior** that supports the start itself,
- **Launch Analysis** for reviewing saved launch results afterwards.

The plugin remains the source of truth for launch calculations, state, and saved data. Dashboards can show launch information, but they do not own the launch logic.

## What the Launch system is for

Use the Launch system when you want to:

- build a repeatable starting baseline,
- reduce guesswork around launch targets and tolerances,
- capture launch results for later review,
- refine your setup from patterns instead of one-off impressions.

It is meant to support race starts without turning launch setup into a constant tuning distraction.

## Where launch controls live

Launch controls now live in:

**Settings -> Launch Settings**

That is where you should expect to review and adjust launch-related setup, including practical launch targets, tolerances, and capture behavior.

## What Launch Analysis is for

**Launch Analysis** is the separate review tab for saved launch traces and summaries.

Use it after a run to answer questions like:

- Was the launch close to the target?
- Did the start look clean and repeatable?
- Did it bog, bite too hard, or show anti-stall-style intervention?
- Is there a pattern across several launches, or was one run just messy?

Launch Analysis is for review and comparison. It is not where the live launch logic lives.

## Live launch behavior vs post-launch review

A useful way to think about the system is:

- **Launch Settings** controls the live setup.
- **Launch Analysis** helps you review what actually happened.

During the start, the system is concerned with the live launch behavior.
After the start, Launch Analysis helps you inspect saved summaries and traces so you can decide whether any tuning change is justified.

## Summary and trace capture

At a practical level, the Launch system can save:

- a **summary** for quick review,
- a **trace** for deeper launch-shape inspection.

You do not need to treat every trace like engineering data. The practical goal is to compare several launches and look for repeatable trends.

## Typical tuning concepts

Most users will think about launch setup in terms of a few practical concepts:

### Launch targets

These are the baseline values you are trying to hit consistently at the start.

### Tolerances

These define how tightly the launch should stay around the intended target before you consider it meaningfully off.

### Bite / anti-stall / bog-down style behavior

These are the practical feel questions you review after a launch:

- Did it bite too hard?
- Did it fall into a bog?
- Did it look like the start needed more protection against stalling?

Use those observations carefully and only after you have more than one representative launch to compare.

## Practical workflow

A stable workflow usually looks like this:

1. **Set a baseline** in **Settings -> Launch Settings**.
2. **Record launches** instead of changing values after every run.
3. **Review traces and summaries** in **Launch Analysis**.
4. **Refine carefully** when you see the same problem repeated across several launches.

This is usually better than chasing one bad start with a major retune.

## Avoid over-tuning

Do not keep rewriting launch settings because of a single poor launch.

Bad starts can come from the driver, the session context, or one messy attempt. Wait for repeatable evidence in the saved summaries and traces before you make meaningful changes.

A calm baseline with measured review is usually more useful than constant launch tweaking.
