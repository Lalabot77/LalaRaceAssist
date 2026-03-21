# Shift Assist

Shift Assist is a driver aid for cleaner, more repeatable upshift timing in Lala Race Assist Plugin.

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

- **Shift Sound** for the main audio cue
- **Shift Light** for visual cue routing
- **Urgent reminder / redline protection behavior** when you stay in the gear too long after the main cue

In normal use, the main cue tells you the preferred shift point. The urgent reminder is there to reinforce that you are late, not to replace the main timing cue.

## Learning workflow

Shift Assist learning is meant to be practical, not complicated.

1. **Enable learning** for the active profile or gear stack.
2. **Do several clean pulls** with strong throttle application so the plugin can gather usable data.
3. **Review the learned values** and look for stable, believable gear-by-gear targets.
4. **Lock gears once stable** so good values stop drifting.

The normal pattern is:

**learn → review → lock → trust**

Do not rush the lock step. If a gear still looks noisy or inconsistent, leave that gear unlocked and keep gathering clean data.

## Profile relationship

Shift Assist uses profile-backed storage. That means:

- the active profile matters,
- learned values should be reviewed in profile context,
- resets and locks should be deliberate,
- dashboards only consume the outputs the plugin publishes.

If you change cars or profile context, review the stored Shift Assist values instead of assuming the old ones still apply.

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
3. If the cues are consistently early or late, adjust carefully and retest.
4. If one gear is unreliable, unlock or relearn that gear instead of disturbing everything else.
5. If the problem is visual-only, review the dash/light presentation separately from the plugin values.

Avoid overreacting to one messy run. Look for repeatable patterns before changing multiple gears or repeatedly resetting learning.

## Important boundary

Shift Assist is a **driver aid**, not an auto-shift system. It gives you timing cues; the driver still decides when to shift.
