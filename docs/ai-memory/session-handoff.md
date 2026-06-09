# OpenCode Session Handoff

## Latest Task Summary

Set up the persistent OpenCode memory workflow for this repo: lightweight memory docs, AGENTS guidance, agent prompts, command shortcuts, and a safe config baseline.

## Files Changed By This Setup

- `docs/ai-memory/project-memory.md`
- `docs/ai-memory/current-task.md`
- `docs/ai-memory/decisions.md`
- `docs/ai-memory/session-handoff.md`
- `docs/ai-memory/token-rules.md`
- `AGENTS.md`
- `.opencode/agents/context-keeper.md`
- `.opencode/agents/token-auditor.md`
- `.opencode/agents/task-planner.md`
- `.opencode/agents/bug-fixer.md`
- `.opencode/agents/reviewer.md`
- `.opencode/commands/start-task.md`
- `.opencode/commands/handoff.md`
- `.opencode/commands/update-memory.md`
- `.opencode/commands/compact-context.md`
- `opencode.json`

## How To Continue

- Read this file first on the next session.
- Then read `AGENTS.md`, `docs/ai-memory/project-memory.md`, and `docs/ai-memory/current-task.md`.
- Use `/start-task` to begin new work with only the smallest relevant files.
- Update this file and `docs/ai-memory/current-task.md` after major progress or before switching tasks.

## What Not To Do

- Do not scan the whole repo unless necessary.
- Do not modify application source code as part of memory setup.
- Do not touch secrets, provider keys, appsettings secrets, or environment files.
- Do not rewrite the docs folder or delete existing docs.
- VERIFIED 2026-06-09: IDs are integer (`role_id SMALLINT`, `user_id INT`, `series_id BIGINT`), NOT GUID. Do not introduce `Guid`/`uniqueidentifier` IDs unless the team migrates the schema first.

## Notes

- OpenCode plugin support was not added here because safe availability was uncertain; enable it later only after confirming it is supported in your local setup.
- Existing unrelated app source changes were left untouched.
