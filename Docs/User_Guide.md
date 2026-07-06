# User Guide

Lala Race Assist is a SimHub plugin for iRacing that helps a driver prepare for a session, drive with better situational awareness, manage race strategy, execute pit stops, and review what happened afterward. It is not meant to replace driving judgment. It is meant to put the most useful information in the right place at the right moment, then keep enough memory between sessions that you do not have to rebuild the same decisions every race weekend.

This guide is the primary user manual for Lala Race Assist. You can read it from beginning to end before using the plugin, or return to individual chapters when you want to understand how a part of the product fits into a normal race workflow. The feature pages remain useful for deeper options and edge cases, but the guide below explains the concepts, recommended practice, and everyday flow.

## 1. Welcome

### What is Lala Race Assist?

Lala Race Assist is built around a simple idea: race driving is already busy, so the software should reduce the amount of mental bookkeeping you need to do. It watches live iRacing data through SimHub, stores learned information in profiles, calculates race and pit guidance, and publishes that guidance to dashboards, overlays, and plugin UI pages.

In practical terms, the plugin helps answer questions such as:

- Do I have enough fuel, and what should I add at the stop?
- Is the car behind a real rival, a class rival, or just nearby traffic?
- Where is my pit entry and pit box, and how good was the stop?
- Which car and track values has the plugin learned, and which ones should I trust?
- Are the plugin, dashboards, and live data healthy enough to rely on?

The answer is rarely a single number. A fuel recommendation is only useful if the fuel data is healthy, the race context is understood, and the profile data behind the calculation is sensible. A pit box countdown is only useful if the track markers and pit profile are valid. Lala Race Assist is therefore organised as a collection of cooperating systems rather than as one isolated dashboard.

### Philosophy

The plugin follows a learn, validate, lock, trust philosophy.

Learning lets the plugin build useful values from your sessions: fuel burn, pit timing, track markers, shift behaviour, and other profile-backed data. Validation is the human step where you decide whether a learned value matches the car, track, setup, and conditions you actually intend to race. Locking protects a value once it is good enough that later noise should not overwrite it. Trust comes last, after the supporting data has earned confidence.

That philosophy matters because iRacing data can be noisy and racing laps are not laboratory tests. Traffic, damage, weather, off-tracks, setup changes, first-lap pit-lane behaviour, or an unusual stop can all produce values that look precise but should not become the long-term reference. Treat Lala Race Assist as a careful assistant: it can observe and calculate quickly, but you still decide what is credible.

The product is also intentionally split by driver attention. Information needed in the next few seconds belongs on the Driver Dash or an overlay. Information needed for stint planning, review, or setup belongs on the Strategy Dash or Plugin UI. The goal is not to show everything everywhere; the goal is to show the right thing where it is useful.

### Plugin overview

At a high level, the plugin has four jobs.

First, it collects and interprets live session data. This includes car state, fuel, pit state, nearby cars, opponents, race order, lap references, and health information.

Second, it stores persistent knowledge. Car profiles and track profiles allow the plugin to remember what it has learned instead of starting from zero every time you join a server.

Third, it publishes outputs and actions to SimHub. Dashboards display plugin-owned information, and dashboard buttons or hardware bindings can trigger plugin-owned actions such as acknowledgement, pit commands, strategy adjustments, and selected toggles.

Fourth, it gives you places to prepare and review. The Plugin UI is where you inspect profiles, configure strategy, review Standing Start Assist runs, manage settings and bindings, tag drivers, and check diagnostics.

## 2. Understanding the Product

Most users experience Lala Race Assist through four product surfaces: Driver Dash, Strategy Dash, Plugin UI, and overlays. They are designed to work together rather than compete with each other.

### Driver Dash

The Driver Dash is the in-car surface. It is for information you may need while the car is moving: nearby traffic, pit entry assistance, pit box approach, rejoin warnings, shift cues, urgent alerts, and compact race context. It should feel glanceable. If something requires detailed reading or setup decisions, it probably does not belong here during a fast lap.

Use the Driver Dash as your immediate awareness tool. Before a pit stop, it helps you approach pit entry and the box. In traffic, it helps you recognise whether cars are nearby and whether the situation deserves caution. During a race, it should support decisions without pulling your eyes away from driving for too long.

### Strategy Dash

The Strategy Dash is the race-management surface. It is where fuel, stint, pit, timing, and race-context information can take more space because the decisions are larger than the next braking zone. It is especially useful before the race, during practice, on straights, in cautions or quiet moments, and for a support crew or second screen.

Use the Strategy Dash to understand the plan, not just the latest number. A refuel value is more useful when you also know whether the plugin trusts the data, what reserve or contingency is being applied, and whether the race format or pit assumptions match the session.

### Plugin UI

The Plugin UI is your workshop. It is where you prepare before driving and review after driving. Profiles, Strategy setup, Standing Start Assist review, bindings, dashboard settings, Driver Tagging, Monitor System configuration, and diagnostic tools live here.

A good habit is to open the Plugin UI before an important session and confirm that the active car, track, strategy assumptions, display settings, and health state all make sense. After the session, return to it to inspect learned values and decide what should be kept.

### Overlays

Overlays are temporary attention surfaces. They exist for messages, warnings, comparisons, prompts, or focused information that should appear without requiring a full dashboard page change. Treat overlays as interruptions with purpose. If an overlay appears, read it, act if needed, and acknowledge or let it clear according to the workflow.

### How they work together

A normal session uses all four surfaces at different times. You prepare in the Plugin UI, drive from the Driver Dash, manage the plan with the Strategy Dash, and respond to overlays when the plugin needs your attention. The same underlying plugin data feeds each surface, so the dashboard is not the authority for the calculation; it is the presentation layer for plugin-owned outputs.

## 3. Installation

Installation is intentionally a high-level topic in this manual because the exact file list and release packaging can change. For step-by-step installation and upgrade instructions, use [Quick Start](Quick_Start.md).

The normal installation pattern is simple. Install or update the plugin DLL, import the dashboard package, enable the plugin in SimHub, and confirm that the dashboards and actions are available. Current v1.1 plugin-owned runtime, pit command, and custom command workflows no longer require `RSC.iRacingExtraProperties.dll`. Older community dashboards, local experiments, or button-box bindings may still reference it, so treat those references as migration leftovers to review rather than as a current runtime dependency.

After installation, do not judge the system by a completely empty first run. Profiles may still need to be created or learned, dashboard selection may need to be confirmed, and live iRacing data may not be present until you are in a session. The first useful check is not “does every value look final immediately?” but “does SimHub load the plugin, do the dashboards appear, and does Monitor System become healthy when live data is available?”

## 4. First Run

The first run is about establishing a clean baseline rather than perfecting every setting. Start SimHub, load into an iRacing session, and confirm that the plugin is enabled. Open the Plugin UI and look at the Overview or health/status area before changing lots of options. If the plugin reports a warning, understand that warning before trusting dependent guidance.

### Profiles

Profiles are the plugin's memory. On a fresh installation, the plugin may not yet know enough about your car and track combination to provide confident guidance. Let it observe normal driving first. Avoid locking values immediately unless you already know they are correct. If you change cars, tracks, or major setup assumptions, expect the profile context to matter.

### Initial strategy

For your first session, keep strategy simple. Set the race format and obvious assumptions, then let live data settle. A practice session is ideal because you can complete clean laps, test a pit entry, and review learned values without race pressure. If a strategy number looks surprising during the first few laps, check whether the fuel model has enough stable data and whether the selected data mode matches your intention.

### Basic bindings

You do not need a complicated button box to start. At minimum, make sure you understand the acknowledgement workflow and any pit command actions you intend to use. If you use dashboard buttons, confirm that they call plugin-owned actions rather than old or experimental bindings. Bindings are powerful because they let you operate the system without opening the Plugin UI, but they should be tested before a race.

### First session

A good first session is a calm practice run. Drive several clean laps, enter pit lane deliberately, stop in the box if you can, and then return to the Plugin UI to inspect what the plugin learned. The goal is not speed. The goal is to create trustworthy reference data.

## 5. Profiles

Profiles let Lala Race Assist remember the car and track. Without them, every session would feel like the first session. With them, the plugin can reuse known fuel burn, pit timing, track markers, shift-related information, and other learned values.

### Car profiles

A car profile stores values that belong to the car more than to the circuit. Examples include fuel tank assumptions, refuel behaviour, tyre service timing, Shift Assist data, and car-specific pit service considerations. If two cars behave differently, they should not be forced to share a single mental model.

Review car profile values when you start using a new car, after a major plugin update, or after discovering that a service assumption is wrong. A profile is useful only when it represents the car you are actually driving.

### Track profiles

A track profile stores values tied to the circuit and pit lane. Pit entry, pit box references, pit loss, and track-specific learning are good examples. Tracks with unusual pit layouts deserve extra attention because the same “pit stop” concept can look very different depending on where the timing line, pit entry, and stall are located.

Validate track profile information during practice. A pit marker learned from a compromised or unusual entry should not be treated as sacred. Relearn only the affected values when possible instead of discarding good data elsewhere.

### Learning

Learning is how the plugin turns your driving into data. Clean, representative driving produces the best learning. If you are stuck in traffic, saving fuel aggressively, carrying damage, or making a deliberately unusual pit entry, expect learned data to be less representative.

Give the plugin good examples. For fuel, run normal laps long enough for a stable pattern. For pit timing, perform realistic pit entries and stops. For Shift Assist, drive the car in the conditions where you want the cues to be useful.

### Validation

Validation is the step where you decide whether the learned value makes sense. Compare it with what you know from the car, your setup, the session, and repeated attempts. One surprising value is a prompt to investigate, not a command to trust.

For example, if a track's pit loss looks much larger than expected, ask whether you served repairs, missed the box, took tyres, crossed an unusual timing line, or entered on a first lap path that should not represent a normal stop. If a fuel burn value is much lower than normal, ask whether the laps were traffic-limited or fuel-saving laps.

### Locking

Lock values when they are good enough that you want to protect them from later noise. Locking is especially helpful for values that are difficult to relearn during a race weekend or values that should remain stable across messy sessions.

Do not lock everything just because a value exists. Locking bad data makes the plugin confidently wrong. The best workflow is to learn, inspect, repeat if needed, then lock once the value has earned confidence.

### Typical profile workflow

Before practice, confirm the active car and track. During practice, drive clean reference laps and complete at least one representative pit entry or stop if the system you care about depends on it. After practice, review learned values, correct or relearn anything suspicious, and lock only values you trust. Before the race, avoid unnecessary relearning unless you have a specific reason.

## 6. Strategy

Strategy is the system that turns race format, fuel use, pit service assumptions, profile data, and live context into a plan. It is not just a fuel calculator. It is the bridge between what the car is doing now and what the stint or race requires.

### Planning

Planning starts before the race. Confirm the race length, fuel capacity, reserve philosophy, pit service assumptions, contingency, and whether saved or live data should drive the recommendation. If the session has unusual rules, make sure the plugin's assumptions match the race you are actually entering.

A useful plan is one you understand. If you cannot explain why the plugin recommends a number, do not blindly follow it. Check the data mode, fuel burn basis, reserve, pit window, and confidence state.

### Live strategy

Live strategy adapts as the session develops. Fuel burn changes with traffic, pace, weather, drafting, saving, and mistakes. Race time and lap projections can shift. Pit stop timing can become more or less attractive as rivals stop.

The key is to avoid overreacting to every small fluctuation. Let live data settle, especially early in a stint. When confidence is low, treat the plan as provisional. When confidence improves and the inputs match reality, the guidance becomes more useful.

### Fuel philosophy

Fuel guidance should be conservative enough to finish but not so padded that it ruins the strategy. Lala Race Assist separates concepts such as burn basis, reserve, contingency, and refuel recommendation so you can understand why a number is what it is.

The safest mindset is to decide your reserve philosophy before pressure arrives. In practice, test whether the plugin's normal burn matches your driving. In qualifying or race trim, account for the fact that traffic and pace can differ from practice. In endurance or league racing, make sure the pit-service rules and class context match the event.

### Pace

Pace influences strategy because lap time affects race distance, pit windows, and comparisons with rivals. A single fastest lap is not always the right planning basis. Representative pace matters more than hero laps when estimating a stint.

If your pace changes because of tyres, fuel saving, weather, damage, or traffic, interpret strategy changes in that context. The plugin can calculate quickly, but it cannot know your intent unless the selected assumptions and live inputs describe it.

### Confidence

Confidence tells you how much faith to place in the current data. A confident recommendation comes from enough stable, relevant evidence. A low-confidence recommendation can still be useful, but it should be treated as a warning to verify the inputs.

If confidence is poor, look for the cause before the pit window arrives. Missing profile data, unstable fuel burn, unusual session state, and unreliable telemetry can all reduce trust. Monitor System warnings are especially important because they tell you when the foundation under the strategy may be weak.

### Typical strategy workflow

Before joining the race, set the broad assumptions. In practice, let the plugin gather clean data. Before qualifying or the race, confirm the selected strategy mode and reserve. During the race, watch for meaningful changes rather than tiny noise. Before a pit stop, confirm the refuel recommendation and pit service assumptions. After the race, review whether the plan matched reality and adjust profiles or defaults only where evidence supports it.

## 7. Pit System

The Pit System covers the full stop: finding pit entry, reaching the box, timing the service, sending or reviewing pit commands, and understanding the stop afterward. Pit stops are high-risk because many small mistakes produce large race losses. The plugin's job is to make the process more deliberate.

### Pit entry

Pit entry assistance helps you approach the pit lane correctly. It depends on track knowledge and live vehicle state. The most useful entry guidance comes from a representative, validated track profile. If the pit entry marker or expected deceleration point is wrong, the display can be confident but unhelpful.

Practice pit entry before relying on it. Learn where the system expects the entry, what speed margin it communicates, and how it reports the entry afterward. Treat entry debrief as feedback for the next stop, not as a punishment.

### Pit box

Pit box assistance helps you find and stop at the stall. This can be one of the hardest parts of a race because the driver is managing speed, relative traffic, service selections, and the exact stopping point at the same time.

A good pit box workflow starts before race day. Validate the track profile, confirm that the box distance behaves sensibly, and understand how the Driver Dash presents the approach. During the stop, use the cue as guidance but keep driving the car; if iRacing or traffic forces a strange approach, the profile can only help within the evidence it has.

### Pit timing

Pit timing is about understanding how much time a stop costs and why. The plugin separates entry, box, service, pit loss, and debrief concepts so that one bad piece of the stop does not hide the others.

This distinction is important. A slow pit entry is different from missing the box. Waiting for repairs is different from losing time through poor stopping. A fuel-only stop should not be judged like a four-tyre stop. When reviewing pit timing, look for the part of the stop that actually caused the delta.

### Pit commands

Pit commands allow the plugin or dashboard bindings to help set fuel, tyres, and related service choices. They are powerful because a correct command at the right time reduces cockpit workload. They are also worth testing because service rules, button bindings, and old local setups can cause confusion if you assume they are correct without verification.

Before racing, test the commands you intend to use. Know which button sets fuel, which clears fuel, which changes tyres, and which actions are only display controls. Current plugin-owned pit command workflows do not require `RSC.iRacingExtraProperties.dll`, but older bindings may still refer to old action paths.

### Pit debrief

Pit debrief turns the stop into reviewable feedback. It helps answer whether the entry was good, whether the box was missed or overshot, whether service timing matched expectation, and whether repairs influenced the stop. This is most useful after practice stops and after the race.

Do not read a pit debrief as one universal grade. Read it as a breakdown. If the debrief says repairs influenced the stop, do not compare the raw time directly against a no-repair service target. If a delta is pending or unknown, treat that as missing evidence rather than as proof of success or failure.

### How the Pit System works together

A stop begins before the pit lane. The track profile and live data support pit entry guidance. Once you are in lane, the system transitions toward the box and service context. Pit commands and service selections determine what should happen in the stall. Timing and debrief then compare what happened with what was expected.

For the driver, the recommended workflow is: validate pit markers in practice, test commands before the race, use Driver Dash cues during the stop, avoid changing service assumptions at the last possible moment unless necessary, and review the debrief afterward.

## 8. Traffic Awareness

Traffic Awareness is about the cars around you now. It is immediate, spatial, and safety-focused. The system helps you understand nearby cars, closing situations, and rejoin or overlap risk without turning every nearby car into a strategic rival.

### Nearby cars

Nearby-car information is most useful when it is treated as situational awareness rather than race control. The plugin can show that a car is close, but you still decide how to place your car. In multiclass sessions, a nearby faster-class car may be more urgent than a same-class car several seconds away; in a sprint, a close same-class car may matter more than a distant leader.

### Situational awareness

Situational awareness is about reducing surprises. Use the Traffic Awareness displays to confirm what mirrors, spotter calls, and relative timing are already telling you. If all sources agree, you can drive with more confidence. If they disagree, drive conservatively until the situation is clear.

### Traffic displays

Traffic displays should be glanceable. They are not meant to be a full race classification page. Their value is speed: who is near, where are they, and should you adjust your line or attention? If you want opponent history, class ranking, or head-to-head context, that belongs more naturally in Race Awareness.

## 9. Race Awareness

Race Awareness is about the race around you: rivals, class context, head-to-head comparisons, and lap references. It complements Traffic Awareness but answers a different question. Traffic asks “who is near me?” Race Awareness asks “who matters to my result?”

### Rivals

Rivals are the cars that affect your race outcome. Sometimes they are physically nearby. Sometimes they are ahead or behind by a pit cycle, a class split, or a strategic offset. The plugin helps surface those relationships so you are not relying only on the raw iRacing relative.

Use rival information to interpret pressure. A car catching you may be irrelevant if it is out of class or on a different lap, or it may be critical if it is your direct position battle. The point is not to ignore traffic; it is to avoid confusing every car with a strategic rival.

### League Class

League Class helps the plugin understand custom class groupings or league-specific class behaviour. This matters when official iRacing class data is not enough to describe the race you are actually running. If League Class is disabled, unresolved, or warning, class-sensitive guidance should be interpreted with that limitation in mind.

For league racing, check League Class before the race rather than discovering a class issue after the green flag. A healthy class context makes rivals, H2H, and multiclass interpretation more meaningful.

### H2H

H2H, or head-to-head, focuses on selected comparisons. It is useful when you care about a particular opponent, class rival, or race battle. Unlike general traffic, H2H is not only about proximity; it is about relationship and trend.

Use H2H when you want to know whether the race is coming toward you or moving away from you. It can help you judge whether to defend, save, push, or wait for a pit cycle to resolve.

### LapRef

LapRef provides lap-reference context used by race-awareness features. In everyday terms, it helps the plugin compare positions and progress in a way that is more useful than isolated live distance. You usually do not manage LapRef directly as a driver, but you benefit from it when race-order and comparison displays behave coherently.

If race-awareness displays look wrong, treat LapRef health as one of the underlying pieces to check along with opponent data, class configuration, and Monitor System status.

## 10. Driver Assistance

Driver Assistance systems help with repeatable, high-attention tasks. They do not drive the car for you. They give cues, review, or warnings at moments where consistency matters.

### Shift Assist

Shift Assist helps with shift cueing for cars where repeatable shift timing matters. It can learn from driving, use car-profile data, and publish cues through the dashboard/audio workflow depending on configuration. The best Shift Assist setup is car-specific and validated under the conditions where you will race.

Use practice to build confidence. If the car, setup, or driving style changes, review whether the cue still makes sense. Do not assume a shift cue learned in one context is ideal for every track, fuel load, or draft situation.

### Standing Start Assist

Standing Start Assist helps review standing-start execution. It is about preparation and post-run analysis more than mid-corner guidance. The public product term is Standing Start Assist, while some internal contracts and existing technical names may still use Launch terminology because those names are part of established plugin contracts.

Use it to compare launches, understand consistency, and refine your routine. The useful question is not simply “was this launch good?” but “what did I do differently, and does the evidence support changing my start procedure?”

### Rejoin Assist

Rejoin Assist provides extra caution when returning to the racing surface. A rejoin is one of the highest-risk moments in a session because the driver may be recovering from an incident while traffic is still moving at race speed.

Treat Rejoin Assist as a safety layer, not permission. If it warns, slow the process down. If it does not warn but visibility is poor or traffic is uncertain, rejoin cautiously anyway. The goal is to reduce risk, not to optimise the rejoin like a racing line.

## 11. Dashboard Management

Dashboard Management controls how the product appears in SimHub: which dashboards are installed, which screens they use, how visibility behaves, and how display comfort options such as dark mode and brightness are applied.

### Dashboard package

The dashboard package contains the Driver Dash, Strategy Dash, overlays, and related dashboard assets. Keep the package aligned with the plugin version whenever possible. A dashboard from an older package may still open, but it may not understand current outputs, wording, or intended workflows.

When upgrading, think of the plugin and dashboards as a matched set. If a value looks missing only on a dashboard, confirm that the correct dashboard package is installed before assuming the calculation is broken.

### Display options

Display options let you decide where each surface belongs. A common pattern is Driver Dash on the in-car display or primary SimHub dashboard, Strategy Dash on a secondary screen or tablet, and overlays on top of the driving view or dashboard stack. The best layout is the one that reduces attention switching.

Avoid showing the same urgent information in too many places. Duplication can be useful for reliability, but too much duplication trains you to ignore alerts.

### Dark mode and brightness

Dark mode and brightness exist for comfort and readability. Long races, night sessions, and multi-screen setups can make dashboard brightness more important than it first appears. Set brightness so that alerts remain visible without washing out the driving view.

If automatic behaviour is enabled, still check it in the conditions you actually race. A setting that looks good on the desktop may be too bright in VR or too dim on a small external screen.

### Screen selection

Screen selection is part of race preparation. Before an event, verify that the Driver Dash, Strategy Dash, and overlays appear on the intended screens. Do not wait until the formation lap to discover that a dash opened on the wrong monitor.

If you change hardware, Windows display order, SimHub layouts, or dashboard packages, run a quick display check before joining a serious session.

## 12. Driver Tagging

Driver Tagging lets you mark people in ways that help you interpret future sessions. The public wording is Driver Tagging, while some technical/internal contract names may still refer to Friends because those names already exist in actions, settings, or schemas.

### Friends

Friends are drivers you want the system to recognise positively. This can be useful in league racing, team practice, or recurring public sessions. A friend tag does not change race rules; it changes your context when that person appears.

### Team mates

Team mate tagging helps identify drivers who are part of your group or endurance effort. In a busy session, this can reduce confusion between “car I should race” and “car I should coordinate with.”

### Problem drivers

Problem-driver tags are memory aids. They can remind you that a driver has previously required extra caution. Use them responsibly. A tag should help you manage risk, not encourage retaliation or assumptions that override what is happening now.

A good tagging workflow is simple: tag only when it will help future decisions, keep tags meaningful, and review them occasionally so old impressions do not become permanent noise.

## 13. Monitor System

Monitor System is the plugin's health report. It tells you when important inputs or subsystems are healthy, warning, stale, or unreliable. This matters because guidance is only as good as the data behind it.

### Health reporting

A healthy Monitor System state means the plugin is not currently aware of a blocking problem in the areas it monitors. It does not mean every profile value is perfect or every strategic assumption is correct. It means the live foundation is usable.

Use health reporting as a first stop when something looks odd. If Strategy, Traffic Awareness, Race Awareness, or pit guidance seems wrong, check whether Monitor System is already telling you that a dependency is stale or unhealthy.

### Warnings

Warnings should be read, not dismissed reflexively. Some warnings mean “do not trust this guidance yet.” Others mean “the system is recovering” or “a supporting configuration is incomplete.” The correct response depends on the warning, but the general rule is to avoid making important race decisions from a warned subsystem until you understand the cause.

### Reliability

Reliability comes from stable inputs, sensible profiles, and healthy subsystem state. Monitor System helps with live reliability, but it cannot validate every human assumption. A healthy monitor with a wrong race length or bad profile value can still produce a poor plan. Use it together with the profile and strategy checks described earlier.

### What to do if warnings appear

If a warning appears before the race, fix the underlying issue if possible. Check plugin enablement, live session state, profile selection, dashboard package version, class configuration, and data availability. If a warning appears during the race, decide whether the affected information is critical. You may continue driving while treating that part of the guidance as advisory or unavailable.

After the session, review the warning in context. If it was caused by a configuration issue, correct it before the next event. If it was caused by temporary telemetry or session state, note when it occurred and whether the system recovered.

## 14. Typical Race Workflow

This chapter shows how the systems interact during a normal race weekend. The exact order can vary by series, but the rhythm is usually the same: prepare, learn, validate, race, stop, finish, review.

### Pre-race

Before joining or while sitting in the garage, confirm that SimHub loads the plugin and that the correct dashboard package is available. Open the Plugin UI and check the active car and track profiles. Review any locked values that matter for the event: fuel capacity, refuel rate, pit service assumptions, pit markers, and shift data.

Next, check Strategy. Set the race format and reserve philosophy. Decide whether live data, saved data, or manual assumptions should drive the plan. If this is a league race, confirm League Class configuration before relying on class-sensitive displays. Finally, verify dashboard placement, brightness, and bindings.

At this stage, the product surfaces already have different jobs. Plugin UI is for setup. Strategy Dash is for the plan. Driver Dash is only being checked to make sure it is ready. Monitor System tells you whether the foundation is healthy enough to proceed.

### Practice

Practice is where you give the plugin good evidence. Drive clean laps at representative pace so fuel and pace assumptions can settle. If the race will require a pit stop, practice pit entry and the pit box. If Shift Assist matters for the car, spend time driving in the conditions where you want shift cues to apply.

After a run, return to the Plugin UI. Review what was learned. If a value is clearly wrong because the lap or stop was compromised, relearn that value instead of letting it pollute the race plan. If a value looks right after repeated evidence, consider locking it.

### Qualifying

Qualifying often produces data that is fast but not representative of race fuel or traffic. Treat qualifying laps carefully when thinking about strategy. The plugin may observe them, but you should decide whether they describe the race you are about to run.

Before leaving qualifying, confirm that the Strategy Dash still reflects race assumptions, not a short-run mindset. Recheck fuel, contingency, reserve, and pit service settings.

### Race

At race start, the Driver Dash and overlays become the primary attention surfaces. Use them for immediate awareness, warnings, and urgent cues. Strategy Dash remains useful for bigger decisions, but it should not distract from the opening laps.

As the race settles, watch confidence and Monitor System state. If fuel burn stabilises and the race length is clear, strategy guidance becomes more actionable. If traffic, cautions, or unusual pace distort the data, interpret the recommendation with that context.

Race Awareness helps you understand whether nearby cars are relevant to your result. Traffic Awareness helps you survive the cars around you. They overlap during battles, but they are not the same system.

### Pit stop

Before the pit window, confirm the refuel recommendation and intended service. If you use pit commands, send or verify them early enough that you are not solving a binding problem at pit entry. As you approach the lane, Driver Dash pit entry cues become more important than long-form strategy context.

In pit lane, follow entry and box guidance while still driving the car. Stop accurately, serve the planned fuel/tyres/repairs, and watch for messages or warnings. After exiting, let the system resolve the stop before judging the debrief. Some values require completed evidence and may not be final at the first instant you leave the box.

### Finish

Near the finish, strategic guidance can become less important than correct race-end interpretation. Be careful with late-race fuel warnings, class context, and finish state. Continue to drive to the rules of the session, not only to a dashboard assumption.

After the checkered flag, avoid making immediate profile changes while adrenaline is high. Save the review for a calm moment unless there is an obvious configuration issue to fix.

### Review

Review is where Lala Race Assist becomes better for next time. Look at the final fuel result, pit debrief, profile changes, standing-start review if relevant, and any Monitor System warnings. Ask what should be learned, what should be locked, and what should be ignored as session-specific noise.

A good review does not change everything. It changes the few values that the evidence supports. Over time, this is how the plugin becomes more useful without becoming fragile.

## 15. Troubleshooting

Troubleshooting should start broad and then narrow down. First, ask whether the plugin is loaded, the dashboard package is current, live iRacing data is available, and Monitor System is healthy. Many apparent feature problems are actually setup, display, or health problems.

If a dashboard value is missing, confirm that the correct dashboard package is installed and that the dashboard is connected to the current plugin outputs. If the Plugin UI shows sensible data but the dashboard does not, the problem is likely presentation or package alignment rather than core calculation.

If strategy looks wrong, check the race format, data mode, fuel burn basis, reserve, contingency, confidence, and profile values. Do not fix a surprising fuel number by changing random settings; identify the input that makes the number surprising.

If pit guidance looks wrong, check the track profile, pit entry marker, pit box reference, service assumptions, and whether the stop was representative. For detailed pit behaviour and advanced cases, use [Pit Assist](Features/Pit_Assist.md) and the [Pit System](Systems/Pit_System.md) page.

If traffic or race awareness looks wrong, check Monitor System first, then class configuration, opponent availability, and whether the issue is spatial traffic or strategic race order. Traffic details are covered in [Traffic Awareness](Systems/Traffic_Awareness.md), while race-order features are covered in [Race Awareness](Systems/Race_Awareness.md) and [H2H System](Features/H2H_System.md).

If profiles seem wrong, do not delete everything immediately. Identify whether the problem belongs to the car profile, track profile, or a single learned value. The [Profiles System](Features/Profiles_System.md) page is the practical next stop for detailed profile workflows.

If you are unsure where to look, start with Monitor System and then follow the affected feature page. The goal is to find the owner of the problem before changing settings.

## 16. Where Next?

The documentation is organised from user-facing guidance toward implementation detail.

Start with the repository [README](../README.md) for the product overview, then use [Quick Start](Quick_Start.md) for installation and first setup. This User Guide is the primary manual and should give you the conceptual model for normal use.

After that, use [Feature Documentation](Features/README.md) when you want practical detail about a specific feature such as dashboards, strategy, pit assist, fuel guidance, profiles, Shift Assist, Standing Start Assist, Rejoin Assist, or H2H. Feature pages explain detailed options, advanced behaviour, edge cases, and configuration reference.

Use [System Documentation](Systems/README.md) when you want to understand the product capability as a whole: Strategy, Profiles, Pit System, Traffic Awareness, Race Awareness, Dashboard Management, Monitor System, Driver Tagging, and Developer Tools. System pages explain what a system does and how it relates to neighbouring systems.

Use [Subsystem Documentation](Subsystems/README.md) when you need technical ownership, contracts, inputs, outputs, persistence, or implementation boundaries. Subsystem pages are the source of truth for internal behaviour when user-facing language and implementation details need to meet.

Use [Internal Documentation](Internal/CODEX_CONTRACT.txt) only when you are maintaining the repo, preparing support work, auditing contracts, or running Codex tasks. Internal docs are not the normal path for learning how to drive with the plugin.
