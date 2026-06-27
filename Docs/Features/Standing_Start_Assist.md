# Standing Start Assist

> Product name: **Standing Start Assist**. Technical implementation name: **Launch Mode / launch trace files**. Contract warning: documentation and UI wording do not rename SimHub exports/actions, persisted settings, dashboard JSON contracts, profile/preset schema fields, or code contracts.

![Standing Start Review example](../Images/LaunchAnalysis.png)

Standing Start Assist supports standing-start setup, clutch-drop/start capture, saved summaries, launch trace review, and post-start analysis.

## What it is for

Use Standing Start Assist when a car/session has a standing start and you want repeatable support for:

- start setup,
- live start capture,
- clutch-drop and wheelspin review,
- saved summaries/traces,
- post-start tuning decisions.

It is not for launching the plugin or changing plugin startup behavior.

## Setup workflow

1. Configure Standing Start Settings in the plugin UI.
2. Capture several clean starts before tuning aggressively.
3. Review summaries and traces in Standing Start Review.
4. Look for repeatable patterns rather than one-off execution mistakes.

## What to trust

Trust repeatable evidence across multiple comparable starts more than a single run. If the review looks wrong, check start settings, trace availability, and whether the run was clean enough to compare.

## Contract notes

The technical subsystem still uses Launch Mode terminology where that reflects existing implementation names. Do not rename `LaunchPlugin.dll`, SimHub action names, exports, persisted settings, schema fields, dashboard contracts, or launch trace file contracts from this product wording alone.

## Related docs

- [Standing Start Assist system](../Systems/Standing_Start_Assist.md)
- [Launch Mode technical subsystem](../Subsystems/Standing_Start_Assist/Launch_Mode.md)
