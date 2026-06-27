# Traffic Awareness

## What it does

Traffic Awareness describes nearby cars and immediate track situation: what is around the car now.

## First-use setup

Use it for local track context. It should fail closed when session data is weak. CarSA is implementation terminology; user docs should talk about nearby cars and track situational awareness.

## What to trust / review

Trust it when the session has healthy car-position data and Monitor System is not warning about traffic reliability. If it feels wrong, check telemetry health and whether the situation is local traffic or race order.

## What it does not own

Traffic Awareness does not own race-order target selection, strategy math, or official standings.

## Related docs

- [H2H System](../Features/H2H_System.md)
- [CarSA](../Subsystems/Traffic_Awareness/CarSA.md)
- [H2H](../Subsystems/Race_Awareness/H2H.md)
