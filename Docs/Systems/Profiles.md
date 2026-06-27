# Profiles

## What it does

Profiles store the car, track, sound, pit, Shift Assist, and related values that make guidance repeatable.

## First-use setup

Use a learn -> validate -> lock -> trust workflow. Separate car-level values from track-level values, lock only stable values, and relearn only the affected values when something changes.

## What to trust / review

Trust locked values only after clean evidence. If profile-driven guidance feels wrong, check whether the value was learned on bad laps, stale track data, or a different car/configuration.

## What it does not own

Profiles do not own Strategy calculations, pit command transport, dashboard presentation, or official race data.

## Related docs

- [Profiles System](../Features/Profiles_System.md)
- [Profiles](../Subsystems/Profiles/Profiles.md)
