# Profiles

Profiles are the memory of Lala Race Assist. They hold the car, track, pit, sound, Shift Assist, and related values that make guidance repeatable instead of starting from scratch every session.

The useful habit with Profiles is simple: let the plugin learn, check whether the value looks believable, then lock it only once you are happy it represents the car accurately. Locking too early is one of the easiest ways to make later guidance feel wrong. A bad lap, a messy pit entry, or the wrong setup can all produce values that look precise but should not be trusted yet.

Think about what belongs to the car and what belongs to the track. Refuel rate and some assist settings are car-level ideas. Pit markers, pit loss, and track-specific observations belong to the current venue/layout. When something feels wrong, relearn the affected value rather than wiping good data elsewhere.

Trust profile-backed guidance when the values were learned from clean evidence and reviewed after the fact. Question it when you changed car, setup, track layout, weather assumptions, pit method, or recently imported/copied profile data.

For user workflow, read [Profiles System](../Features/Profiles_System.md). The technical persistence contract lives in [Profiles](../Subsystems/Profiles/Profiles.md).
