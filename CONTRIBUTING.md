# Contributing

This project uses strict engineering discipline.

---

## Before contributing

* Read Docs/Project_Index.md
* Read Docs/Internal/CODEX_CONTRACT.txt
* Read Docs/RepoStatus.md and relevant entries in Docs/Internal/Development_Changelog.md
* Understand subsystem boundaries

## Git and validation

* Make implementation changes on a dedicated feature branch based on the latest `origin/main`; never develop directly on `main`.
* Do not commit or push without explicit user approval.
* Keep the plugin target at .NET Framework 4.8 and use C# 7.3 as enforced by the project file.
* Build the solution and run applicable tests before reporting implementation work complete; explain any validation that could not run.

---

## Development principles

* Keep changes scoped and minimal
* Do not introduce new systems without approval
* Respect ownership boundaries

---

## Documentation

* All changes must update documentation
* GitHub-facing docs are part of the product
* Documentation must reflect final behavior

---

## Pull Requests

Each PR must include:

* Clear description of change
* Behaviour impact (yes/no)
* Risk assessment
* Documentation updates
* Changelog classification

---

## What not to do

* Do not refactor unrelated code
* Do not expand scope
* Do not introduce breaking changes without approval

---

## Philosophy

* Stability over cleverness
* Clarity over abstraction
* Explicit behaviour over implicit assumptions
