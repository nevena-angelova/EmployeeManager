# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

ASP.NET Core Web API (.NET 10) + EF Core 10 (Npgsql) + PostgreSQL 18 back end; Angular 22 front end (zoneless, standalone, Vitest via `@angular/build:unit-test`).

## Commands

Run from the repo root unless noted.

```bash
# Database (local Postgres, role/db/password all "employee_manager")
docker compose up -d

# Back end
dotnet build backend/EmployeeManager.slnx
dotnet run --project backend/src/EmployeeManager.Api          # http://localhost:5062
dotnet test backend/EmployeeManager.slnx                      # IntegrationTests need the Postgres above
dotnet test backend/tests/EmployeeManager.UnitTests --filter "FullyQualifiedName~<TestName>"   # single test

# EF Core migrations (dotnet-ef is pinned in dotnet-tools.json)
dotnet tool restore
dotnet ef migrations add <Name> \
  --project backend/src/EmployeeManager.Infrastructure \
  --startup-project backend/src/EmployeeManager.Api \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project backend/src/EmployeeManager.Infrastructure \
  --startup-project backend/src/EmployeeManager.Api

# Front end (in frontend/employee-manager-client)
npm install
npm start                                   # http://localhost:4200
npm run build
npm test                                    # Vitest
npm test -- --include <path/to/file.spec.ts>   # single spec file
npx prettier --write <changed files>        # formatting (.prettierrc); no ESLint configured
```

CI (`.github/workflows/ci.yml`, on pushes and PRs to `main`) runs the back-end build + tests against a `postgres:18` service container and the front-end `npm ci`, build and tests.

Health check (includes DB connectivity, anonymous): `GET http://localhost:5062/api/health`. OpenAPI document is mapped only in Development.

Gotchas:
- The connection string only exists in `appsettings.Development.json`, and the `Development` environment comes from the launch profile. If you run the API with `--no-launch-profile` (e.g. a second instance with `--urls http://localhost:5063` when 5062 is taken), set `ASPNETCORE_ENVIRONMENT=Development`. Otherwise every DB call fails with database `''`.
- Some existing scaffold files (`main.ts`, `index.html`, `app.config.ts`, `environments/*`) aren't Prettier-formatted yet. Running `prettier --write .` or `src` reformats them, so format only the files you changed.

## Architecture

Clean Architecture in `backend/src`, dependencies pointing inward: `Api → Application, Infrastructure`; `Infrastructure → Application → Domain`.

- **Domain** — entities only, no package dependencies. All entities derive from `BaseEntity` (`Guid Id`, `CreatedAt`, `UpdatedAt`).
- **Application** — use cases/DTOs and `IApplicationDbContext` (exposes `DbSet<T>`s + `SaveChangesAsync`). Application code depends on this interface, not on the concrete DbContext.
  - Use cases are grouped by feature folder, e.g. `Employees/OrgChart/` with an `IXxxService` + `XxxService` pair, `XxxDto` records, and pure helper classes.
  - Keep logic in pure static helpers (e.g. `OrgChartBuilder`) so it can be unit-tested without a DB. The UnitTests project only references Domain + Application.
  - Services that need the DB are tested in `tests/EmployeeManager.IntegrationTests` against the real DbContext. Each test class's `TestDatabase` creates and drops a throwaway `employee_manager_test_*` database on the local server (or `EMPLOYEE_MANAGER_TEST_DB`). Not SQLite: it can't translate some of the projections. `BeforeFirstSave` simulates a concurrent request winning a race.
  - Register services in `AddApplication()`.
- **Infrastructure** — `EmployeeManagerDbContext` is an `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and implements `IApplicationDbContext`; registered in `AddInfrastructure()`, which also maps the interface to the same scoped context instance. Its `SaveChangesAsync` override stamps `CreatedAt`/`UpdatedAt` (UTC) for every `BaseEntity` — don't set these manually. Adding or removing a `ProjectMember` also counts as an update to its (tracked) project.
- **Api** — controllers go in `Controllers/`. Every endpoint requires a signed-in user (fallback authorization policy) unless it opts out with `[AllowAnonymous]`. They are thin `[ApiController]`s with primary-constructor injection of Application services, and they declare `[ProducesResponseType]` so the OpenAPI doc is accurate. `Program.cs` reads connection string `ConnectionStrings:EmployeeManagerDb` (throws if missing) and CORS origins from `Cors:AllowedOrigins`. Dev values live in `appsettings.Development.json`; `appsettings.json` leaves them empty.

### Persistence conventions

- Snake_case DB naming is applied globally via `UseSnakeCaseNamingConvention()` — write C# names in PascalCase; raw SQL (e.g. check constraints) must use snake_case column names.
- Entity mapping lives in `IEntityTypeConfiguration<T>` classes under `Infrastructure/Persistence/Configurations/`, picked up automatically by `ApplyConfigurationsFromAssembly`. Adding a new entity means: entity in Domain, `DbSet` on both `IApplicationDbContext` and the DbContext, a configuration class, then a migration.
- Migrations live in `Infrastructure/Persistence/Migrations`.

### Authentication and roles

ASP.NET Core Identity (Identity Core only, no cookies) stores users and roles; the API authenticates with JWT bearer tokens.

- `ApplicationUser` (`Infrastructure/Identity`) is a sign-in account linked 1:1 to an `Employee` via `EmployeeId` (cascade delete). The user name and email are the employee's email.
- Three roles, defined in `Application/Auth/Roles`: `Employee`, `Manager` and `Admin`. Each user has exactly one. The seeder makes anyone with direct reports a Manager, and `DevelopmentDataSeeder.AdminEmail` an Admin. Roles are stored, not derived, so they don't change by themselves when the hierarchy changes. An admin sets them when creating/editing an employee; the new role is in the token from the user's next sign-in.
- Role-restricted endpoints use `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` or `ManagerOnly` (used by the project write endpoints).
- Tokens use short claim names (`sub` = user id, `email`, `role`); `MapInboundClaims` is off, so read them with `JwtRegisteredClaimNames` / `JwtOptions.RoleClaimType`.
- JWT settings live in the `Jwt` section (`Issuer`, `Audience`, `SigningKey`, `ExpiryMinutes`). `SigningKey` is only set in `appsettings.Development.json`; the API refuses to start without a key of 32+ bytes.
- Identity tables are renamed to snake_case (`users`, `roles`, `user_roles`, ...) in `IdentityTablesConfiguration`/`ApplicationUserConfiguration`, since Identity's explicit table names bypass the naming convention. The base `OnModelCreating` runs before `ApplyConfigurationsFromAssembly`.
- Failed logins count towards Identity's lockout (5 failures → 5 minutes).

`POST /api/auth/login` (anonymous) takes `{ email, password }` and returns `{ accessToken, expiresAt, user }`; wrong credentials or lockout give 401. `GET /api/auth/me` returns the current user (`employeeId`, `email`, `firstName`, `lastName`, `jobTitle`, `role`).

### Employee domain rules

`Employee` has an optional self-referencing `ManagerId` (null = top-level manager). Enforced in the DB: unique email, check constraint `ck_employees_manager_not_self`, and `DeleteBehavior.Restrict` on the manager FK (a manager with direct reports can't be deleted until reports are reassigned).

The DB does **not** prevent longer manager cycles (A → B → A), so any code that walks the hierarchy must guard against them. `OrgChartBuilder` tracks the current path.

### Endpoints

`GET /api/employees/org-chart[?rootId={guid}]` (`EmployeesController` → `OrgChartService`):
- Returns the hierarchy as a nested tree: an array of top-level managers, each with recursive `directReports`.
- Each node has `id`, `firstName`, `lastName`, `jobTitle`, `email` and `directReports`.
- Every level is sorted by first name, then last name.
- `rootId` returns a one-element array for that subtree. An unknown id gives 404, and a malformed GUID gives 400.
- All employees are loaded in a single `AsNoTracking` projection and the tree is assembled in memory.

Admin-only employee management (`EmployeesController` → `IEmployeeManagementService`, implemented in `Infrastructure/Employees/EmployeeManagementService` because it needs `UserManager`):
- `GET /api/employees` (sorted by name), `GET /api/employees/{id}`, `POST` (creates the employee **and** their `ApplicationUser` with the given `password` and `role`, in one transaction), `PUT /{id}` (keeps the account's email/user name and role in step), `DELETE /{id}` (the account cascades).
- Rule violations come back as an `EmployeeResult` that the controller maps to 400 `ValidationProblem` (errors keyed by camelCase field), 404, or 409 `ProblemDetails`. Delete gives 409 for an employee with direct reports or for your own account; an admin can't remove their own Admin role.
- Manager changes are checked by the pure `ManagerAssignmentRules` (manager exists, isn't the employee, isn't in their subtree).
- JSON dictionary keys are camelCased globally (`DictionaryKeyPolicy`) so data-annotation errors also use `email`, not `Email`.

Projects (`ProjectsController` → `ProjectService`, in Application since it only needs `IApplicationDbContext`; controller helpers shared via `Controllers/ControllerExtensions`):
- A `Project` (`Name` unique per owner, case-insensitive; optional `Description`) is owned by the manager who created it, and has `ProjectMember`s. Deleting the owner or a member employee cascades.
- `GET /api/projects/managed` (`ManagerOnly`) lists your own projects; `GET /api/projects/mine` the ones you're on; `GET /{id}` is 404 unless you own it or are on it.
- `POST`, `PUT /{id}` (replaces the members) and `DELETE /{id}` are `ManagerOnly` and owner-only (404 for someone else's project). Results reuse `EmployeeResult`.
- A save that loses a race (a member added twice, or a member or the project deleted meanwhile) gives 409; a duplicate name still gives the 400 name error.
- New members must be the owner's direct reports (pure `ProjectMemberRules`, errors under `memberIds`). Existing members who moved to another manager stay until removed, and come back with `isDirectReport: false`.

Monthly hour allocations (`AllocationsController` → `AllocationService`, in Application; `month` query param is `yyyy-MM`, defaults to the current UTC month, malformed → 400 keyed `month`):
- A `HourAllocation` is an employee's hours on a project for one month (`Month` is always the 1st, `Hours` positive, numeric(5,1); 0 hours = no row). Unique per employee/project/month. It references the project and employee, not the membership, so past months survive a member leaving; deleting either cascades.
- `GET /api/allocations/team` (`ManagerOnly`) returns the manager's projects (columns), direct reports (rows) with the projects they can be given hours on, their hours on the manager's projects and `otherHours` on other managers' projects.
- `PUT /api/allocations/team` (`ManagerOnly`) replaces the month's hours for the manager's direct reports on the manager's projects (left out or 0 = removed). Past months give 409, as does losing a race. Errors are keyed `allocations` (not a direct report, duplicate item) or `allocations.{employeeId}`.
- Rules (pure `AllocationRules`, `AllocationMonth`): hours ≥ 0 in steps of 0.5, only on the manager's projects the report is on (or already has hours on that month), and each report's total including `otherHours` ≤ 8h × weekdays in the month. The current and later months are editable; "current" comes from the injected `TimeProvider` (integration tests use `FixedTimeProvider`).
- `GET /api/allocations/team/statistics?year=yyyy` (`ManagerOnly`; defaults to the current UTC year, malformed → 400 keyed `year`) returns a year of the manager's direct reports' planned (allocated) and logged hours on the manager's projects: 12 `months` (Jan–Dec, each with `capacityHours`, `planned`, `logged`, and `otherPlanned`/`otherLogged` on other managers' projects), plus `projects` and `members`, each with 12 `months` of `planned`/`logged` and year totals. The sums are done by the pure `AllocationStatisticsBuilder`.
- `GET /api/allocations/mine` (everyone) returns your hours that month on every project you're on or have allocated or logged hours on, each with `loggedHours` (the month's sum of your daily `TimeEntry`s) and a `totalLoggedHours`. The team endpoint likewise gives each report `logged` (per manager's project) and `otherLoggedHours`. Logged hours are informational: they don't count towards capacity.
- Removing a member in `ProjectService.UpdateAsync` also deletes their hours on that project from the current month on.

Daily time entries (`TimeEntriesController` → `TimeEntryService`, in Application; `date` query param is `yyyy-MM-dd`, defaults to today in UTC, malformed → 400 keyed `date`):
- A `TimeEntry` is the hours an employee actually spent on a project on one day, logged by the employee themself (unlike `HourAllocation`, which is a manager's monthly plan). `Hours` positive and ≤ 24, numeric(3,1); 0 hours = no row. Unique per employee/date/project. Like allocations it references the project and employee, not the membership, so entries survive a member leaving (they aren't removed when a member is); deleting either cascades.
- `GET /api/time-entries/mine` (everyone) returns the day's hours on every project you're on or already have hours on that day, the total, `isEditable`, `maxHours`, and `week`: the daily totals Monday–Sunday of that week.
- `PUT /api/time-entries/mine` replaces your hours for the day (left out or 0 = removed). Days after today give 409, as does losing a race. Errors are keyed `entries`.
- Rules (pure `TimeEntryRules`, `TimeEntryDay`): hours ≥ 0 in steps of 0.5, each project once, only on projects you're on (or already have hours on that day), day total ≤ 24. Today and any earlier day are editable.

The Development seeder (`DevelopmentDataSeeder`) creates 15 employees under one CEO, Margaret Hayes, adds the admin Grace Turner under her (a separate step, so existing DBs get her too), then the roles, then an account for every employee without one, then a few sample projects, then current-month hours for their members, then their time entries on the five weekdays before today. Every seeded account's password is `DevelopmentDataSeeder.DevelopmentPassword`; emails are `first.last@example.com` (e.g. `grace.turner@example.com` is the Admin, `margaret.hayes@example.com` a Manager, `ethan.clarke@example.com` an Employee). Each step is skipped if its data exists, so an existing dev DB gets accounts on the next run.

### Front end

`src/app` is split into `core/` (singletons, e.g. `ApiService`), `shared/`, and `features/`. `ApiService` prefixes requests with `environment.apiUrl` — `http://localhost:5062/api` in development (direct call, relies on API CORS), `/api` in production. `ApiService.get(path, params?)` takes optional query params; `post`/`put(path, body)` send JSON, and `delete(path)`. Uses `inject()` rather than constructor injection.

Auth (`core/auth/`):
- `AuthService` holds the session (`LoginResult`) in a signal, persisted to `localStorage`; `currentUser` is a signal and `accessToken()` returns null (and signs out) once the token has expired.
- `authInterceptor` adds `Authorization: Bearer` to requests under `environment.apiUrl` only. On a 401 for a request that carried a token, it signs out and navigates to `/login?returnUrl=...`.
- `authGuard` protects routes; add `canActivate: [authGuard]` to every new feature route. Admin-only routes add `adminGuard` (non-admins go to `/org-chart`).
- The router uses `withComponentInputBinding()`, so query/route params bind to component `input()`s (e.g. `Login.returnUrl`).

Conventions:
- Component classes have no `Component` suffix and files are named like `org-chart.ts|html|scss` (the Angular 20+ style, as with `App`).
- Use signals, `input()`/`computed()`, the `@if`/`@for` control flow, and `rxResource` for loading data.
- Feature pages are lazy-loaded with `loadComponent` in `app.routes.ts`.
- Specs sit next to their files. The app is zoneless, so specs use `await fixture.whenStable()` and `componentRef.setInput()`. Use `provideHttpClientTesting()` for services, and a stubbed service for pages.
- Colours are CSS custom properties (`--color-*`, `--line-0`…`--line-4`, `--shadow-dialog`) defined in `styles.scss`. Use those tokens instead of hard-coded colours. The type is Overpass (Google Fonts, linked in `index.html`).
- The visual language is a transit map: each team (everyone under someone who reports to a top-level manager) has a line colour. `shared/team-lines.ts` (`teamLines`/`lineColour`) assigns them, and both the org chart and the Employees table use it so the colours agree. Set `--line` on an element and its children pick it up.
- Projects on the Hours page have their own colours (`--project-0`…`--project-5` with `-tint` pairs, via `projectColour` in `features/allocations/hour-rail.ts`), separate from the team `--line-*`s. On the Hours page the grid's planned hours are small boxed fields (plain figures once the month has passed) with "N h logged" under them; colour lives in the capacity rails (`hourRail`), which take the grid's spare width: a thick planned rail over a thin logged one on the same scale, explained by one legend in the header. The tints are used by Daily hours.
- The app is light-only (`color-scheme: light`) and deliberately ignores the system dark mode.

Features:
- `features/login/` (route `/login`) is a reactive-forms sign-in page. It only follows in-app `returnUrl`s (starting with a single `/`). The `App` header shows the user's name, role and a Sign out button when signed in.
- `features/employees/` (route `/employees`, Admins only, linked from the header nav) lists employees in a table with Add, View profile and Delete. Add opens `EmployeeForm` in a native `<dialog>` owned by the page. `EmployeeForm` is a plain reactive form (no dialog, no heading of its own) that shows the API's 400 errors under their fields; the password field only appears on create.
- `features/employees/employee-profile/` (route `/employees/:id`, Admins only) is an employee's profile: details, a link to their manager, and their direct reports. Its Edit button swaps the details for an inline `EmployeeForm`; there's no edit popup.
- `features/projects/` (route `/projects`, everyone, linked from the header nav) starts with "Daily hours" (`daily-hours/DailyHours`, only shown once "Projects I'm on" has loaded and isn't empty): the day from the `?date=yyyy-MM-dd` query param (helpers in `daily-hours/day.ts`; ‹ ›, Today), a Mon–Sun week strip with each day's total (past weekdays with nothing logged are flagged), and a reactive form with one hours input per project, a live total and Save. Future days are read-only; a 409 shows a banner with Reload. Below it the page lists "Projects I manage" (Managers, with a New project dialog) and "Projects I'm on". `project-detail/` (route `/projects/:id`) shows the members with their team colours; the owner gets Edit (inline `ProjectForm`) and Delete. `ProjectForm` offers the signed-in manager's direct reports, taken from the org chart (`org-chart-people.ts`), as member checkboxes.
- `features/allocations/` (route `/allocations`, everyone, nav link "Hours") shows a month chosen by the `?month=yyyy-MM` query param (pure helpers in `month.ts`; ‹ › navigate). Everyone sees "My hours"; Managers also get "Team hours", a reactive-form grid of direct reports × their own projects (an input only where the report is a member, "—" otherwise) with live row totals, including `otherHours` from other managers' projects, against the monthly capacity. Both sections show the month's logged hours (sum of daily time entries) beside the allocations: a Logged column in "My hours", and "N h logged" under each grid cell and row total. Past months are read-only; 400 errors keyed `allocations.{employeeId}` show under that row, and a 409 shows a banner with Reload. Managers also get a Statistics link to `allocations/statistics/` (route `/allocations/statistics`, `managerGuard`, which sends non-Managers to `/allocations`). That page shows the year from `?year=yyyy` (`year.ts`; ‹ ›): year totals, a CSS bar chart of planned vs logged per month against team capacity (pure `statistics-chart.ts`), and By month / By project / By report tables. Hour figures are formatted with `formatHours` (`hours.ts`).
- `features/org-chart/` (route `/org-chart`, which is also the default route) draws the org chart top-down.
  - `OrgChart` is the page. It handles the loading, error-with-retry and empty states, and starts the horizontally scrolling chart centred on the top of the tree.
  - `OrgChartBranch` is a recursive component that renders one card plus its reports. Each card collapses or expands its team.
  - The connector lines are pure CSS, using the nested `ul`/`li` `::before`/`::after` technique.
