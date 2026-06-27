# Developer Tools

Developer Tools are for diagnosing problems, collecting support evidence, and maintaining the plugin. They include debug options, Property Snapshot, trace logging, diagnostic CSVs, and log/message inventories.

Most drivers should leave these tools alone during normal racing. Turn them on when you are investigating a specific issue or when support asks for evidence. Extra diagnostics can be valuable, but they can also produce noise if you collect them without a clear question.

Use Developer Tools to observe existing systems, not to redefine them. If a diagnostic CSV or snapshot looks surprising, compare it with the owning subsystem doc and current RepoStatus before assuming the runtime behaviour is wrong.

Trust these tools as support evidence when they were captured with the right setting, session, and reproduction steps. Question them when debug settings changed mid-run, the session context was wrong, or the output belongs to a different subsystem than the issue you are investigating.

Start with [Property Snapshot Debug Workflow](../Internal/Property_Snapshot_Debug_Workflow.md), [Trace Logging](../Subsystems/Developer_Tools/Trace_Logging.md), [SimHub Parameter Inventory](../Internal/SimHubParameterInventory.md), and [SimHub Log Messages](../Internal/SimHubLogMessages.md).
