# Monitor System

Monitor System is the plugin's health and reliability report. It exists so you know whether the guidance on screen is based on trustworthy data, weak data, recovered data, or a fault condition.

Leave it enabled unless you have a specific reason not to. It is most useful as a report-by-exception system: when everything is healthy it should stay quiet, and when something important is unreliable it should tell you what area needs attention.

Monitor System does not fix the data for you. If it warns about fuel, traffic, opponents, pit context, or another monitored area, treat that as a reason to review the underlying source rather than a message to dismiss and ignore.

Trust Monitor System as a reliability indicator. Question the affected feature when Monitor System warns, after reconnects/session transitions, or when a dashboard value looks plausible but the health state says the supporting data is not ready.

For message inventories and dashboard contracts, see [MonitorSystem Messages](../Internal/MonitorSystem_Messages.csv), [SimHub Log Messages](../Internal/SimHubLogMessages.md), and [Dash Integration](../Subsystems/Dashboard_Management/Dash_Integration.md).
