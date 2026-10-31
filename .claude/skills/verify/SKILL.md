---
name: verify
description: Check that the EmployeeManager branch builds, passes its tests and is formatted — builds and tests the .NET back end and the Angular front end, and checks the changed front-end files with Prettier.” Use before committing, before opening a PR, after a refactor. Pass `backend`, `frontend` or `all` to force a scope.
---

# Verify

Report whether the current branch builds, passes its tests and is formatted. This skill **checks only**: it doesn't fix failures, reformat files or commit. After the report, offer fixes.

## 1. Work out the scope

If the argument is `backend`, `frontend` or `all`, use it. Otherwise look at what changed on this branch, from the repo root:

```bash
base=$(git merge-base HEAD main)
{ git diff --name-only "$base"; git ls-files --others --exclude-standard; } | sort -u
```

- Any path under `backend/`, or `dotnet-tools.json` → back end.
- Any path under `frontend/` → front end.
- Nothing changed, only other files changed (docs, `.claude/`), or you're on `main` → run `all`.

Keep the list of changed `frontend/employee-manager-client/` files for step 4.

## 2. Back end

```bash
dotnet build backend/EmployeeManager.slnx
dotnet test backend/EmployeeManager.slnx --no-build
```

- If the build fails, skip the tests and report the first compiler errors (file:line + message), not the whole log.
- The unit tests reference only Domain + Application and don't need Postgres or a running API.

## 3. Front end

Run these in `frontend/employee-manager-client`. If `node_modules` is missing, run `npm install` first.

```bash
npm run build
npm test -- --watch=false
```

Pass `--watch=false` every time. Otherwise Vitest may stay in watch mode and never exit.

## 4. Formatting (front end only)

Check **only** the changed front-end files from step 1 that still exist. Strip the `frontend/employee-manager-client/` prefix, because Prettier runs from that folder:

```bash
npx prettier --check --ignore-unknown <changed files>
```

Never run Prettier on `.` or `src`. Scaffold files (`main.ts`, `index.html`, `app.config.ts`, `environments/*`) aren't formatted yet and would show up as false failures. If no front-end files changed, skip this step.

Use `--check`, not `--write`. Reformatting is a fix, so offer it after the report.

## 5. Report

Run the independent steps even when an earlier one fails, so one run shows every problem. The exception is a failed build, which skips the tests on that side.

End with a short table:

| Check | Result |
|---|---|
| Back-end build | ✅ / ❌ / skipped |
| Back-end tests | ✅ 42 passed / ❌ 2 failed of 42 / skipped |
| Front-end build | … |
| Front-end tests | … |
| Prettier (n changed files) | … |

Under the table, list each failure with its location and the key line of output: failing test names, compiler errors, unformatted files. Say why any check was skipped. Don't call the branch green unless every check that ran passed.
