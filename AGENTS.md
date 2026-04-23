# Agent Entry Point

Start with Docs/Project_Index.md

---

## Mandatory Working Order

1. Read CODEX_CONTRACT.txt
2. Read Project_Index.md
3. Read relevant Subsystem docs
4. Check RepoStatus.md
5. Review Development_Changelog.md for recent changes

---

## Core Rules

* Subsystems own logic
* Do not move logic across subsystem boundaries
* Prefer local edits over central expansion
* Do not introduce new ownership paths without explicit requirement

---

## Documentation Rules

* Documentation must match final behavior
* GitHub docs are part of the product
* Update docs in the same task as code

---

## Stability Rules

* Do not introduce unintended behaviour changes
* Respect system invariants
* Avoid timing or telemetry side effects

---

## Task Approach

* Always perform analysis before editing
* Always identify risks
* Always declare behaviour changes

---

## Repo Truth Hierarchy

1. Subsystem docs (source of truth)
2. RepoStatus.md (current validated state)
3. Code (implementation)
4. Code_Snapshot.md (orientation only)
