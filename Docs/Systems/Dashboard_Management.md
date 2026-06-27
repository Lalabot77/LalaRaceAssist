# Dashboard Management

Dashboard Management is about getting the right information onto the right surface without pretending the dashboard owns the underlying logic. Dashboards display plugin-owned outputs and expose selected controls; the plugin still owns calculations, learning, actions, exports, settings, and persistence.

Start by using dashboard packages that match the plugin version. Then configure visibility, dark mode, brightness, and bindings for the way you actually drive. If you use optional SimHub-side inputs such as ShakeIt TractionLoss, treat them as dashboard support setup: useful when configured, absent when not.

When a dashboard looks wrong, first ask whether it is a presentation problem or a data problem. A stale package, missing binding, hidden visibility setting, or optional property can make a good plugin state look broken. Monitor System warnings can also explain why a widget is failing closed.

Trust dashboards as readable presentations of plugin-owned values. Question dashboard output when package versions are mismatched, SimHub reports missing properties, visibility settings hide a feature, or a formula/widget is trying to recreate logic the plugin already publishes.

For user setup, read [Dashboards](../Features/Dashboards.md). Technical presentation contracts live in [Dash Integration](../Subsystems/Dashboard_Management/Dash_Integration.md) and [Message System V1](../Subsystems/Dashboard_Management/Message_System_V1.md).
