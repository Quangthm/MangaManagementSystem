# Git Workflow Rules

- **NEVER merge code directly into the `main` branch on GitHub**. Only the Team Leader has the permission to merge PRs into `main`.
- When asked to "merge", it means:
  1. Pull the latest `main` branch from the remote repository to the local machine.
  2. Merge the remote `main` branch into the local feature branch (e.g., `workspace` branch) to resolve conflicts locally.
  3. Run tests locally to ensure everything works fine.
  4. Create a **Pull Request (PR)** pushing only the feature branch changes to GitHub for the Team Leader to review.
