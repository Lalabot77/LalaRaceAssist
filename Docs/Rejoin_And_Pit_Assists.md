# Rejoin and Pit Assists

This page covers the main user-facing driver aids around rejoin, pit entry, and pit-context messaging.

## 1. What these assists are for

These systems are there to help the driver make better decisions under pressure.

They are practical support tools for:

- rejoining safely,
- entering pit lane cleanly,
- understanding pit context,
- reducing avoidable race mistakes.

They do not remove the need for driver judgment.

## 2. Rejoin warnings

Rejoin warnings are there to help when the plugin believes a rejoin or recovery situation is active.

Typical support includes:

- warning that traffic is approaching,
- warning during spin or serious incident situations,
- pit-exit/rejoin context where applicable.

### What users should trust

Trust rejoin warnings most when:

- the profile thresholds suit the car,
- the current session state is normal,
- you are using them as decision support rather than as an absolute command.

### When to cancel or override

Use **Cancel Message** when you need the alert out of the way for the current moment.

That is the right choice when:

- the message is distracting,
- the situation has already changed,
- you understand the context and need the screen clear.

### When repeated wrong behavior usually means setup review

If rejoin behavior is repeatedly wrong, the likely issue is not the dashboard art. Review:

- saved profile thresholds,
- rejoin linger settings,
- clear-speed threshold,
- spin sensitivity or related profile tuning.

## 3. Pit popups

Pit popups are the driver-facing pit-context prompts shown on supported dashboards.

They are useful for things like:

- pit screen context,
- pit-assist visibility,
- automatic pit-related prompts.

### What users should trust

Trust pit popups as context indicators when your pit data is good.

That usually means:

- pit-loss has been learned cleanly,
- pit markers are sensible,
- the track record has been validated and locked where appropriate.

### When to cancel or override

- Use **Toggle Pit Screen** if you want to force the pit screen on or off.
- Use **Cancel Message** if a popup needs to be dismissed right now.

### When repeated wrong behavior usually means data review

If pit popups keep appearing at the wrong time or with the wrong feel, review:

- saved pit markers,
- saved pit-loss data,
- track-specific setup for the current layout.

## 4. Pit Entry Assist

Pit Entry Assist helps you arrive at the pit entry line at the right speed.

It uses the saved marker context plus braking guidance to tell you whether you are:

- comfortably early,
- getting close,
- braking now,
- already late.

### What users should trust

Trust Pit Entry Assist most when:

- the track markers are correct,
- the pit-entry settings fit the car,
- you have validated the system with at least one clean pit entry.

### When to override with judgment

Override it with your own judgment when:

- track conditions are unusual,
- traffic forces a compromised line,
- the saved settings are not yet validated for that car/track.

### When repeated wrong behavior usually means setup review

If the assist consistently feels wrong, review:

- pit entry marker quality,
- pit entry deceleration setting,
- pit entry buffer setting,
- whether the current car/track data was learned cleanly.

## 5. General trust model for these systems

A good rule is:

- **Trust the systems once the underlying data is good.**
- **Cancel or override them when the moment demands it.**
- **Review saved data or thresholds if they are repeatedly wrong.**

That is the normal intended workflow.
