# Fuel Model

This page explains the driver-facing fuel-learning model in Lala Race Assist Plugin.

## 1. What the fuel model does

The fuel model learns how much fuel your current car uses in the current conditions, then turns that into a trustworthy basis for Strategy and race support.

It exists so the driver is not forced to guess fuel burn from one noisy lap or from stale memory.

## 2. How fuel use is learned

The plugin learns fuel use gradually from accepted laps. In practical terms, that means it is looking for laps that represent real running instead of obviously misleading ones.

Early on, the model is still learning. Later, once enough clean data exists, the model becomes much more trustworthy.

## 3. Why values stabilize slowly

Stable fuel values are intentionally not instant. They settle slowly because:

- pit laps are not normal race laps,
- incidents and compromised laps can distort the result,
- conditions can change,
- the plugin is trying to protect you from chasing noisy data.

That slower stabilization is usually a feature, not a flaw.

## 4. Confidence and accepted laps

Confidence is the driver-facing sign that the model has enough good information to be trusted.

A simple mental model is:

- **low confidence:** still learning, be cautious
- **rising confidence:** the picture is getting better
- **good confidence:** the Strategy basis is becoming trustworthy

The plugin does not blindly trust every lap equally. That is why clean representative laps matter so much.

## 5. Relationship to Strategy

The fuel model and Strategy work together, but they are not the same thing.

- The **fuel model** learns and stabilizes burn behavior.
- **Strategy** uses that information, along with other planning inputs, to build a race plan.

If the live fuel model is still weak, Strategy may still be better served by saved profile values until the live session becomes trustworthy.

## 6. Why users should trust it once learned and locked

Once the model has enough clean data and the related profile values are validated and locked, users should usually trust it.

That trust matters because constant second-guessing tends to make race planning worse, not better. The goal is to move from reactive guesswork toward a stable planning basis.

## 7. What to do when the values look wrong

If fuel values look wrong once, keep driving and let the session develop. If they look wrong repeatedly, use this recovery order:

1. Check whether confidence is still low.
2. Check whether the laps being fed into learning were actually representative.
3. Review the selected profile and track context.
4. Relearn only the affected value or condition if needed.
5. Lock again only after the data has clearly settled.

## 8. Practical guidance

- Expect the model to become better over several clean laps, not one.
- Keep dry and wet trust separate in your own thinking.
- Use the model as a reason to trust Strategy more, not as a reason to change the plan every lap.
- Once good values are learned and locked, let them do their job.
