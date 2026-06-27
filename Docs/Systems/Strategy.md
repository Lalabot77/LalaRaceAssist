# Strategy

Strategy is where you turn a race format into a plan you can actually drive. It brings together the selected profile, track data, race length, fuel assumptions, pit service rules, and live context so the dashboard can answer practical questions: can you make the stint, when is the window, and how much margin do you have?

Use Strategy deliberately rather than constantly chasing the last lap. Before the race, decide whether the plan should come from saved profile/manual values or from a live session snapshot. Profile/manual planning is best when you want a stable repeatable plan; live snapshot is useful when the current session is clearly the best source of truth. Presets are worth using for race formats you run often because they stop you rebuilding the same assumptions every time.

Pit service regulations are a manual Strategy assumption. Pick Default, IMSA, or NEC because that is the rule set you intend to model, not because you expect the plugin to guess it from the series name. Once the session starts, let fuel confidence settle before you react to every small movement in the numbers.

Trust Strategy when the profile, race length, pit rules, contingency, and live confidence all match the race you are in. Question it when a preset was applied to the wrong format, a profile value was learned from bad data, live confidence is still immature, or the race definition does not match the server.

For the practical workflow, continue with [Strategy System](../Features/Strategy_System.md) and [Fuel Guidance](../Features/Fuel_Guidance.md). Technical planning ownership lives in [Fuel Planner Tab](../Subsystems/Strategy/Fuel_Planner_Tab.md) and runtime fuel logic lives in [Fuel Model](../Subsystems/Strategy/Fuel_Model_Subsystem.md).
