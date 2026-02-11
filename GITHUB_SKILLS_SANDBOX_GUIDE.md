# GitHub Skills Sandbox Guide

This branch is dedicated to Codex Skills experiments.

Branch Purpose:
- Test GitHub PR comment workflows.
- Test CI fix workflows.
- Test iterative commits without touching stable branches.
- Provide a safe branch for trial edits before merging to stable branches.

Branch:
- `codex/skills-sandbox-20260211`

Suggested test flow:
1. Open a PR from this branch to `main`.
2. Leave a few review comments on the PR.
3. Ask Codex to use GitHub-related skills to address those comments.
4. Add a small intentional test failure and ask Codex to repair CI.

Rollback test branch:
1. Return local repo to `main`:
   - `git checkout main`
2. Delete local sandbox branch:
   - `git branch -D codex/skills-sandbox-20260211`
3. Delete remote sandbox branch:
   - `git push origin --delete codex/skills-sandbox-20260211`
