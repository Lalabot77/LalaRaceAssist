# Agent Entry Point

Start with [Docs/Project_Index.md](Docs/Project_Index.md). It is the canonical documentation map and entry point for this repo.

Required operating order:
- Obey [Docs/Internal/CODEX_CONTRACT.txt](Docs/Internal/CODEX_CONTRACT.txt) as mandatory policy.
- Use [Docs/Internal/Architecture_Guardrails.md](Docs/Internal/Architecture_Guardrails.md) for subsystem boundaries and ownership.
- When working from an explicit task, use [Docs/Internal/CODEX_TASK_TEMPLATE.txt](Docs/Internal/CODEX_TASK_TEMPLATE.txt).
- Read the relevant `Docs/Subsystems/*.md` files before editing subsystem code or subsystem docs.
- Treat [Docs/Internal/Code_Snapshot.md](Docs/Internal/Code_Snapshot.md) as orientation only; it is non-canonical.

Working rules:
- Prefer subsystem-local edits over widening central file responsibilities.
- Do not widen ownership boundaries unless the task explicitly requires it.
- Keep documentation aligned with the final repo state, including [Docs/RepoStatus.md](Docs/RepoStatus.md) when docs or behavior change.
- Maintain changelog split discipline: `CHANGELOG.md` is user-facing release history; `Docs/Internal/Development_Changelog.md` is the ongoing internal development log.
