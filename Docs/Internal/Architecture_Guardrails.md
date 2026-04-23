# Architecture Guardrails

## Core Principle

Each subsystem owns its logic.

Do not blur ownership boundaries.

---

## Subsystem Ownership Rules

* CarSA owns spatial awareness
* Opponents owns race-order targets
* Strategy owns planning
* Fuel Model owns runtime fuel logic
* Profiles own persistence
* Dash layer owns presentation ONLY

---

## Allowed Changes

* Modify logic inside owning subsystem
* Extend subsystem using existing seams
* Add outputs within subsystem scope

---

## Forbidden Changes

* Moving logic between subsystems
* Creating cross-subsystem dependencies
* Replacing ownership without explicit instruction

---

## Data Flow Rules

* Upstream systems provide data
* Downstream systems consume data
* No circular dependencies

---

## Behaviour Stability

* Do not change behaviour unless task explicitly requires it
* Maintain all invariants
* Prevent regression across recent changes

---

## Documentation Alignment

* Subsystem docs must reflect behaviour
* RepoStatus must reflect validated state
* No undocumented behaviour changes allowed

---

## When in doubt

* Keep logic where it is
* Do not expand scope
* Do not guess intent
