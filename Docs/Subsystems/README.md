# Subsystems

Subsystem docs are the technical/canonical implementation layer. Product docs may use outside-in names such as Race Starts or Driver Tagging; subsystem docs preserve implementation truth and existing contracts.

Do not rename SimHub exports, SimHub actions, persisted setting names, dashboard JSON contracts, profile schema fields, preset schema fields, or code contracts based on documentation wording alone.

## Technical ownership map

| Product/core system | Technical subsystem docs | Ownership note |
|---|---|---|
| Strategy | [Fuel Model](Strategy/Fuel_Model_Subsystem.md), [Fuel Planner Tab](Strategy/Fuel_Planner_Tab.md), [Pace and Projection](Strategy/Pace_And_Projection.md) | Strategy owns planning; Fuel Model owns runtime fuel logic. |
| Profiles | [Profiles](Profiles/Profiles.md) | Profiles own persistence; PB/reference values are stored profile data. |
| Pit System | [Pit Timing and Pit Loss](Pit_System/Pit_Timing_And_PitLoss.md), [Pit Entry Assist](Pit_System/Pit_Entry_Assist.md), [Pit Commands and Fuel/Tyre Control](Pit_System/Pit_Commands_And_Fuel_Control.md), [Track Markers](Pit_System/Track_Markers.md) | Pit System owns pit timing, entry, box, command, fuel/tyre, debrief, and marker flows. |
| Traffic Awareness | [CarSA](Traffic_Awareness/CarSA.md) | CarSA owns spatial/nearby-car awareness. |
| Race Awareness | [Opponents](Race_Awareness/Opponents.md), [H2H](Race_Awareness/H2H.md), [League Class](Race_Awareness/League_Class_System.md), [LapRef](Race_Awareness/LapRef.md) | Opponents owns race-order targets; H2H/LapRef/League Class provide race-context awareness. |
| Race Starts | [Launch Mode](Race_Starts/Launch_Mode.md) | Public docs say Race Starts; technical implementation may still say Launch Mode. |
| Shift Assist | [Shift Assist](Shift_Assist/Shift_Assist.md) | Shift Assist owns cueing/learning/audio support. |
| Rejoin Assist | [Rejoin Assist](Rejoin_Assist/Rejoin_Assist.md) | Rejoin Assist owns rejoin guidance. |
| Dashboard Management | [Dash Integration](Dashboard_Management/Dash_Integration.md), [Message System V1](Dashboard_Management/Message_System_V1.md), [Message Engine V1 Notes](Dashboard_Management/MessageEngineV1_Notes.md) | Dash layer owns presentation only. |
| Developer Tools | [Trace Logging](Developer_Tools/Trace_Logging.md), [Fuel modes CSV](Developer_Tools/FuelModesLogicCSV.csv), [Tyre modes CSV](Developer_Tools/TyreModesLogicCSV.csv) | Diagnostics observe existing systems; they do not own runtime behavior. |

## Template

Use [_Template.md](_Template.md) for new subsystem documentation.
