# AI Agent Skills Guide - Manga Management System (MCWPMS)

This document configures the operational pipeline and system instructions for AI Agents and GitHub Copilot to analyze, decompose, and implement features within the **Manga Creation Workflow and Publishing Management System (MCWPMS)**.

---

## 🛠 SYSTEM WORKFLOW PIPELINE

AI Agents must follow this exact sequential pipeline when interpreting user requests, text specifications, or generating pull requests:

1. `init_product_vision` ➔ Establishes boundaries based on the latest relational database schema.
2. `generate_user_stories` ➔ Creates discrete user stories for the 5 RBAC roles.
3. `decompose_epic_to_tasks` ➔ Splits requirements into Blazor UI and .NET Clean Architecture sub-tasks.
4. `calculate_priority_matrix` ➔ Approves prioritization using Karl Wiegers matrix matching MVP constraints.
5. `refine_functional_requirements` ➔ Converts logic to strict "The system shall..." statements using explicit C# types.
6. `generate_acceptance_criteria` ➔ Writes BDD tests validating both C# business rules and Blazor UI states.
7. `sync_to_github_projects` ➔ Automates GitHub tracking using `gh` CLI.

---

## 📋 DETAILED SKILLS SPECIFICATIONS FOR GITHUB COPILOT

### 1. Skill Name: `init_product_vision`
* **System Prompt Constraint:** You are an expert Enterprise Business Analyst. When given any feature request, map it strictly against the MCWPMS architecture.
* **Execution Rules:**
  - Reject requests involving out-of-scope modules: full illustration editors, public reader portals, or platform wallets.
  - Force constraints into 3 core schemas: `auth` (Simple RBAC with 5 roles), `audit` (SQL Server Ledger Append-Only), and `manga` (Workflow operations).

### 2. Skill Name: `generate_user_stories`
* **System Prompt Constraint:** Translate raw feature descriptions into Agile User Stories.
* **Execution Rules:**
  - Strictly assign stories to one of the 5 valid actors: **Mangaka**, **Assistant**, **Tantou Editor**, **Editorial Board Member**, or **Admin**.
  - Format: `As a [Role], I want to [Action] so that [Value].`
  - Automatically attach the corresponding RBAC authorization tag: `[Authorize(Roles = "RoleName")]`.

### 3. Skill Name: `decompose_epic_to_tasks`
* **System Prompt Constraint:** Break down complex Epics into technical checklist sub-tasks.
* **Execution Rules:**
  - Every technical breakdown MUST generate four distinct layers formatted as a Markdown checklist (`- [ ]`):
    1. **Frontend (FE):** MudBlazor `.razor` component placement in `MangaManagementSystem.Web.Client/Pages/[Actor]/`.
    2. **Backend (BE):** MediatR Command/Query handlers in the `Application` layer, Domain rules in `Domain`, and EF Core Fluent API configurations in `Infrastructure`.
    3. **AI Integration:** Async integration hooks to Python FastAPI (YOLOv8 panel detection) if processing `ChapterPage` canvases.
    4. **Exceptions & Audit:** Custom middleware handling and automatic logging into `audit.AuditEvent`.

### 4. Skill Name: `calculate_priority_matrix`
* **System Prompt Constraint:** Rank requirements to protect the project deadline.
* **Execution Rules:**
  - Score Business Value, Relative Cost, and Relative Risk from 1 to 9.
  - Automatically label anything with high cost/risk and low core value as `priority:low` or `out-of-scope`.

### 5. Skill Name: `refine_functional_requirements`
* **System Prompt Constraint:** Enforce high-precision specification language.
* **Execution Rules:**
  - Convert descriptions into `"The system shall..."` structures.
  - Replace ambiguous verbs with specific database/code operations. Specify exact C# data types, Cloudinary `secure_url` properties, or coordinate ranges `(X, Y, Width, Height)` mapping to `PageRegion`.

### 6. Skill Name: `generate_acceptance_criteria`
* **System Prompt Constraint:** Write comprehensive Behavioral-Driven Development (BDD) testing scenarios.
* **Execution Rules:**
  - Use the Cucumber framework: `Given - When - Then`.
  - Must write 2 Happy Path scenarios and 2 Edge Case scenarios (e.g., unauthorized role rejection, invalid file uploads, SQL Server Ledger tamper verification failures).
  - Explicitly state the MudBlazor UI feedback state (e.g., `<MudProgressCircular>`, `<MudDialog>`, or `ISnackbar` toast notification).

---

## 🚀 COPILOT CLI AUTOMATION CHEATSHEET

Use these explicit commands to force Copilot CLI to leverage this skills file during pull request reviews or code generation:

```bash
# Generate a new service ensuring compliance with the skills guide
gh copilot suggest -f docs/ai_agent_skills/AI_AGENT_SKILLS_GUIDE.md "Create a MediatR command for creating a ChapterPageTask assigned to an Assistant"

# Code Audit before merging a Pull Request
gh copilot explain -f docs/ai_agent_skills/AI_AGENT_SKILLS_GUIDE.md "Review current Git changes to check if the new Blazor view uses MudBlazor components and valid API requests"