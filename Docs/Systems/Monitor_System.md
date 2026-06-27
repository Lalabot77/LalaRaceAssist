# Monitor System

## What it does

Monitor System is the health and reliability report for plugin data and driver-facing trust decisions.

## First-use setup

It is enabled by default. Use it to see whether fuel, pit, traffic, race-awareness, or other monitored data is ready, unreliable, recovered, or faulted.

## What to trust / review

Trust Monitor System as a report-by-exception signal. If it warns, fix the underlying data/source rather than ignoring it.

## What it does not own

Monitor System reports health; it does not automatically fix data, reset systems, send pit commands, or change calculations.

## Related docs

- [MonitorSystem Messages](../Internal/MonitorSystem_Messages.csv)
- [SimHubLogMessages](../Internal/SimHubLogMessages.md)
- [Dash Integration](../Subsystems/Dashboard_Management/Dash_Integration.md)
