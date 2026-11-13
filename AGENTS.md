# Agent Entry Point

Start with `Docs/Project_Index.md`.

---

## Mandatory Working Order

1. Read `Docs/Project_Index.md`.
2. Read `Docs/Internal/CODEX_CONTRACT.txt`.
3. Read relevant `Docs/Subsystems/` documents.
4. Check `Docs/RepoStatus.md`.
5. Review relevant entries in `Docs/Internal/Development_Changelog.md`.

## Git and build rules

* Work in the local repository on a dedicated feature branch created from the latest `origin/main`; never develop directly on `main`.
* Do not commit or push without explicit user approval.
* Keep the target framework at .NET Framework 4.8 and use the explicitly configured C# 7.3 language version.
* For builds, locate the installed Visual Studio `MSBuild.exe` (for example with `vswhere`) and use it as the authoritative engine for this classic WPF/.NET Framework solution. Do not treat `dotnet build` as authoritative unless it independently produces the WPF-generated code and passes.
* Report verification separately at four levels: compilation, automated logic tests, static/integration checks, and manual SimHub runtime checks. Never claim SimHub runtime verification unless the plugin was actually loaded and exercised in SimHub.

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
