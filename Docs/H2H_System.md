# H2H System

This page explains the driver-facing purpose of the H2H tools in Lala Race Assist Plugin.

## 1. What H2H is for

H2H gives the driver a compact, read-only comparison against useful same-class targets. It helps answer questions like:

- Who am I really racing?
- Am I stronger or weaker over the lap?
- Is the nearby threat a race-position threat or just local track traffic?

It is not a planning workflow and it is not something you constantly manage in the plugin UI.

## 2. The two H2H views

### H2H Race

**What it helps with**

- understanding the same-class cars directly ahead and behind in race order,
- seeing whether you are gaining or losing on the race targets that matter most.

**What it compares**

- you versus the same-class race target ahead,
- you versus the same-class race target behind.

**User outcome**

This is the cleaner “who matters to my result?” view.

### H2H Track

**What it helps with**

- understanding the same-class cars immediately around you on track,
- seeing local pressure or opportunity even when race-order context and local traffic context differ.

**What it compares**

- you versus the nearby same-class car ahead on track,
- you versus the nearby same-class car behind on track.

**User outcome**

This is the better “what is happening around me right now?” view.

## 3. What users will usually see

Depending on the dash, H2H can include:

- target identity,
- last-lap and best-lap context,
- live gap,
- delta-to-best context,
- segment-by-segment comparison.

The exact presentation belongs to the dashboard package, but the comparison itself comes from the plugin.

## 4. Where H2H comes from

From the driver’s point of view, H2H is one feature. Internally, it depends on supporting technical systems.

- **Opponents** helps support the race-order target selection behind **H2H Race**.
- **CarSA** helps support the local on-track target selection behind **H2H Track**.

Those are supporting subsystem docs, not separate user-facing feature pages.

## 5. Important ownership boundary

Keep the ownership clear:

- H2H is a **read-only race-context tool for the driver**.
- The plugin owns the selection and publication of the comparison outputs.
- Dashboards display those outputs.

## 6. When H2H is most useful

H2H is especially useful when:

- you are in a close class fight,
- multi-class traffic makes local context confusing,
- you want a quick feel for whether a nearby car is actually a race-position threat,
- you want a simple visual comparison rather than raw timing pages.

## 7. What H2H is not

H2H is not:

- a fuel or strategy calculator,
- a substitute for the main timing screen,
- a dashboard-owned feature,
- a tool that changes race outcomes on its own.

It is there to improve driver awareness.
