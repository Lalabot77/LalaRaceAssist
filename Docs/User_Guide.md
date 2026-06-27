# User Guide

Lala Race Assist is best understood as a driving assistant built around two jobs: helping you drive the car in the moment, and helping you manage the race around that driving. The plugin does the learning, calculations, persistence, actions, and exports. The dashboards and overlays make that information usable when you are busy.

## The surfaces you actually use

Most users meet the plugin through four surfaces.

The **Driver Dash** is the in-car surface. It is for quick decisions: traffic nearby, pit entry, pit box approach, rejoin support, alerts, Shift Assist cues, and small pieces of strategy context that matter right now.

The **Strategy Dash** is the race-management surface. It is for the bigger picture: stint planning, fuel guidance, pit windows, race context, and decisions that are not always tied to the next braking zone.

The **Plugin UI** is where you prepare and review. Use it for Profiles, Strategy setup, bindings, settings, Standing Start Assist review, Driver Tagging, and diagnostics.

Supporting **overlays** are temporary surfaces. They appear when a message, warning, prompt, or focused comparison needs attention without becoming your main dashboard page.

That split is deliberate. Driving and managing a race are different mental tasks, so the plugin gives them different surfaces.

## Driving versus managing the race

When you are driving, the useful question is usually “what do I need to know in the next few seconds?” That is Driver Dash territory. It should help you stay aware of nearby traffic, pit cues, rejoin risk, and urgent warnings without asking you to read a manual at 250 kph.

When you are managing the race, the useful question is “what does this mean for the stint or result?” That belongs on Strategy Dash and in the Plugin UI. Fuel guidance, pit timing, race order, presets, and post-run review need more context and less urgency.

If a piece of information feels too detailed for the Driver Dash, it probably belongs in Strategy Dash or the Plugin UI. If it needs to interrupt you during the lap, it probably belongs on Driver Dash or an overlay.

## How the core systems fit together

Profiles come first because they are the plugin's memory. They hold car, track, pit, sound, Shift Assist, and related values so the plugin does not start from nothing every session. Let values learn, confirm they make sense, and lock them only when you trust them.

Strategy builds on that memory. It turns profile data, race format, pit service assumptions, contingency, and live context into a plan. A good Strategy setup is deliberate: choose whether saved/manual inputs or live snapshot data should be in charge, then avoid chasing every noisy lap while confidence is still settling.

The Pit System handles the most mistake-prone part of a race: getting in, stopping correctly, commanding service, and understanding the time loss. Validate pit markers and pit loss early. Once those basics are right, pit entry, pit box, command, and debrief guidance become much more useful.

Traffic Awareness is about the cars around you now. Race Awareness is about the race order and rivals that matter strategically. In multiclass racing those are not always the same thing, so the documentation separates them on purpose.

Shift Assist helps with repeatable shift cueing where a car benefits from it. Standing Start Assist helps review standing-start execution and setup. Rejoin Assist provides extra caution when returning to the racing surface. Monitor System sits across all of this as the health report: if it says the data is unreliable, treat the affected guidance as suspect until the underlying issue is understood.

## Learn, validate, lock, trust

A recurring philosophy in Lala Race Assist is: learn, validate, lock, trust.

The plugin can learn useful values from your driving, but learned values are not automatically good values. A messy lap, a compromised pit entry, traffic, damage, or the wrong setup can all produce data that looks precise while still being wrong for future use.

Validation is the human step. Look at what the plugin learned and ask whether it matches the car, track, and session. If it does, lock the value so later noise does not degrade it. If it does not, relearn only the affected value instead of throwing away everything that was already good.

Trust comes last. The more critical the decision, the more important it is that the supporting value has gone through that path.

## Ownership matters

The plugin owns calculations. Dashboards own presentation. Profiles own persistence. Monitor System reports health.

That means a dashboard should not be treated as the source of strategy math, fuel learning, pit timing, traffic selection, or race-order logic. It displays plugin-owned outputs and may trigger plugin-owned actions, but it does not become the authority.

It also means public wording does not rename contracts. User docs say **Standing Start Assist** and **Driver Tagging**, but technical docs may still mention Launch Mode, launch traces, Friends, action names, exports, settings, or schema fields where those are existing contracts.


## Upgrade note

Current v1.1 plugin-owned runtime, pit command, and custom command workflows no longer require `RSC.iRacingExtraProperties.dll`. Older community dashboards, local experiments, or button-box bindings may still reference it, so treat those as migration leftovers to review rather than as a current runtime dependency.

## How to keep learning

The documentation follows the same outside-in idea as the plugin.

Start with [Product surfaces](Product/README.md) when you want to understand where something appears. Read [Core systems](Systems/README.md) when you want to understand what capability is involved. Use [Feature docs](Features/README.md) for practical workflows such as dashboards, Strategy, Pit Assist, Shift Assist, Standing Start Assist, Rejoin Assist, Profiles, Fuel Guidance, and H2H. Use [Subsystems](Subsystems/README.md) when you need technical ownership or contract truth. Internal docs are mainly for maintenance, support, release work, and Codex tasks.

If you are new, read [Quick Start](Quick_Start.md), then the system page for the thing you are trying to configure. If something feels wrong, check Monitor System, check the owning system page, and then move down into the feature or subsystem docs only as needed.
