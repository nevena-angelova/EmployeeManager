---
name: update-claude-md
description: Bring CLAUDE.md up to date with the work done in this session and on this branch — new or changed commands, conventions, endpoints, features and gotchas. Use before ending a session, after finishing a feature, or when asked to update CLAUDE.md or the project docs.
---

# Update CLAUDE.md

CLAUDE.md is the project guide every new session starts from. Keep it accurate and short: it should hold what someone can't easily work out from the code.

## 1. Collect what changed

From the repo root:

```bash
base=$(git merge-base HEAD main)
git log --oneline "$base"..HEAD
git diff --stat "$base"
git status --short
```

Add what this conversation knows that the diff doesn't show: decisions made, problems hit and how they were solved, commands that turned out to be needed. Read the parts of the diff you need to understand a change. Don't guess from file names.

## 2. Compare with CLAUDE.md

Read CLAUDE.md in full, then go through each section:

- **Stack**: new packages, frameworks or version changes.
- **Commands**: new scripts, flags or setup steps. Commands that changed or no longer work.
- **Gotchas**: anything that went wrong this session and would trip up the next one.
- **Architecture / Persistence conventions**: new layers, patterns, entities or rules.
- **Employee domain rules**: new constraints or invariants.
- **Endpoints**: new or changed routes, parameters, status codes and response shapes.
- **Front end**: new features, routes, components, services or conventions.

Also check what's already there: remove or correct anything the changes made untrue.

## 3. Edit

- Change only what's out of date, missing or wrong. Don't rewrite sections that are still accurate.
- Match the existing style: short bullets, plain words, code in backticks, no marketing tone.
- Leave out what's obvious from the code, one-off details, and history ("we changed X to Y"). Describe how things are now.
- Before running a command you add, check that it works, or say in the summary that you didn't check it.
- If nothing needs changing, say so and leave the file alone.

## 4. Report

Summarise the edits as a short list: section → what was added, changed or removed. Don't commit. The user decides when to commit.
