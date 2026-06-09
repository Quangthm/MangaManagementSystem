# Current Task

## Active Task

Set up persistent OpenCode memory and token-saving workflow for future sessions.

## Branch / Status

- Branch: `feature/UI` tracking `origin/feature/UI` marked gone.
- Pre-existing unrelated app source changes detected:
  - `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/RegisterPage.razor`
  - `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Shared/WorkspaceCard.razor`
- This setup should not modify application source code or secrets.

## Files Likely Relevant

- `AGENTS.md`
- `opencode.json`
- `docs/ai-memory/*.md`
- `.opencode/agents/*.md`
- `.opencode/commands/*.md`
- Context sources: `README.md`, `docs/context.md`, `docs/business-rules.md`, `docs/functional-requirements.md`, `docs/user-stories.md`, `docs/Skills/AI_AGENT_SKILLS_GUIDE.md`

## Already Tried

- Inspected repository docs and source project files enough to summarize architecture and rules.
- Confirmed no existing `opencode.json`, no existing `AGENTS.md`, and no existing `.opencode/` in this repo before setup.
- Fetched OpenCode config schema and confirmed `instructions`, `compaction`, `default_agent`, and `plugin` are valid top-level config fields.

## Next Safest Steps

- Use `docs/ai-memory/session-handoff.md` first in future sessions.
- Use `/start-task` before broad implementation work.
- Before code edits, identify exact targeted files and confirm ID types against the verified integer schema (`role_id SMALLINT`, `user_id INT`, `series_id BIGINT`) across model/DTO/repository/stored-procedure/route types.
- Do not touch appsettings, secrets, provider/API config, or application source unless explicitly requested.
