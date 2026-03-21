# Shift Assist

Shift Assist is a driver aid for cleaner, more repeatable upshift timing.

It watches the current gear and RPM, then gives you cues near the target RPM for that gear. It helps the driver react consistently, but it does **not** shift the car for you.

## What it is for

Use Shift Assist when you want:

- a clearer upshift cue than watching the tach alone,
- more consistent shift timing across laps,
- a practical way to learn and review gear-specific shift targets,
- a quick reminder before you run too deep into the limiter.

The plugin remains the source of truth for the targets it stores, learns, and publishes. Dashboards can display the cues, but they do not own the Shift Assist logic.

## What you see and hear

Shift Assist can provide three practical cue types:

- **Shift Sound** for the main audio cue.
- **Shift Light** for visual cue routing.
- **Urgent reminder / redline protection behavior** when you stay in the gear too long after the main cue.

In normal use, the main cue tells you the preferred shift point. The urgent reminder is there to reinforce that you are late, not to replace the main timing cue.

## Core outputs

### Shift Sound

The main Shift Assist beep is the primary upshift cue. It is intended to fire near the target RPM for the current gear, with predictive timing support available so the cue can arrive slightly before the raw target when needed.

### Shift Light

Shift Assist also exposes Shift Light output for dashboards and visual layouts. Depending on the selected routing, the light can follow the primary cue, the urgent cue, or both.

### Urgent reminder / redline protection behavior

If you stay in the gear after the main cue, Shift Assist can issue an additional urgent reminder. Think of it as a late warning near the top of the usable range rather than a separate shift strategy.

It does not change the learned targets, the stored profile data, or the normal shift timing logic. It is only an extra reminder to help you avoid hanging on the limiter.

## Learning workflow

Shift Assist learning is meant to be practical, not complicated.

1. **Enable learning** for the active profile or gear stack.
2. **Do several clean pulls** with strong throttle application so the plugin can gather usable data.
3. **Review the learned values** and look for stable, believable gear-by-gear targets.
4. **Lock gears once stable** so good values stop drifting.

The usual pattern is:

**learn → review → lock → trust**

Do not rush the lock step. If a gear still looks noisy or inconsistent, leave that gear unlocked and keep gathering clean data.

## Profile and storage relationship

Shift Assist uses profile-backed storage.

That means:

- the plugin stores the useful learned values,
- the active profile and gear stack matter,
- resets and locks should be done deliberately,
- dashboards only consume the outputs the plugin publishes.

If you change cars, profile context, or the relevant gear-stack setup, review the stored Shift Assist values instead of assuming the old ones still apply.

## Practical first-use advice

For a first pass:

- start with the feature enabled,
- use the standard cue first before chasing custom tuning,
- do a few clean full-throttle pulls,
- check whether the cues feel believable in each important gear,
- lock only the gears that have clearly settled.

Treat the first session as calibration, not final truth.

## If cues feel early, late, or inconsistent

If Shift Assist feels wrong, use a simple order of operations:

1. Check that you are evaluating it during clean, repeatable pulls.
2. Review the current learned or stored values for the active profile.
3. If the cues are consistently early or late, adjust carefully and then retest.
4. If one gear is unreliable, unlock or relearn that gear instead of disturbing everything else.
5. If the problem is visual-only, review the dash/light presentation separately from the plugin values.

Avoid overreacting to one messy run. Look for repeatable patterns before changing multiple gears or repeatedly resetting learning.

## Important boundary

Shift Assist is a **driver aid**, not an auto-shift system.

It gives you timing cues. The driver still decides when to shift.
