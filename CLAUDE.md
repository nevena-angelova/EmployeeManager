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
dotnet test backend/EmployeeManager.slnx
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

Health check (includes DB connectivity): `GET http://localhost:5062/api/health`. OpenAPI document is mapped only in Development.

Gotchas:
- The connection string only exists in `appsettings.Development.json`, and the `Development` environment comes from the launch profile. If you run the API with `--no-launch-profile` (e.g. a second instance with `--urls http://localhost:5063` when 5062 is taken), set `ASPNETCORE_ENVIRONMENT=Development`. Otherwise every DB call fails with database `''`.
- Some existing scaffold files (`main.ts`, `index.html`, `app.config.ts`, `environments/*`) aren't Prettier-formatted yet. Running `prettier --write .` or `src` reformats them, so format only the files you changed.

## Architecture

Clean Architecture in `backend/src`, dependencies pointing inward: `Api → Application, Infrastructure`; `Infrastructure → Application → Domain`.

- **Domain** — entities only, no package dependencies. All entities derive from `BaseEntity` (`Guid Id`, `CreatedAt`, `UpdatedAt`).
- **Application** — use cases/DTOs and `IApplicationDbContext` (exposes `DbSet<T>`s + `SaveChangesAsync`). Application code depends on this interface, not on the concrete DbContext.
  - Use cases are grouped by feature folder, e.g. `Employees/OrgChart/` with an `IXxxService` + `XxxService` pair, `XxxDto` records, and pure helper classes.
  - Keep logic in pure static helpers (e.g. `OrgChartBuilder`) so it can be unit-tested without a DB. The UnitTests project only references Domain + Application.
  - Register services in `AddApplication()`.
- **Infrastructure** — `EmployeeManagerDbContext` implements `IApplicationDbContext`; registered in `AddInfrastructure()`, which also maps the interface to the same scoped context instance. Its `SaveChangesAsync` override stamps `CreatedAt`/`UpdatedAt` (UTC) for every `BaseEntity` — don't set these manually.
- **Api** — controllers go in `Controllers/`. They are thin `[ApiController]`s with primary-constructor injection of Application services, and they declare `[ProducesResponseType]` so the OpenAPI doc is accurate. `Program.cs` reads connection string `ConnectionStrings:EmployeeManagerDb` (throws if missing) and CORS origins from `Cors:AllowedOrigins`. Dev values live in `appsettings.Development.json`; `appsettings.json` leaves them empty.

### Persistence conventions

- Snake_case DB naming is applied globally via `UseSnakeCaseNamingConvention()` — write C# names in PascalCase; raw SQL (e.g. check constraints) must use snake_case column names.
- Entity mapping lives in `IEntityTypeConfiguration<T>` classes under `Infrastructure/Persistence/Configurations/`, picked up automatically by `ApplyConfigurationsFromAssembly`. Adding a new entity means: entity in Domain, `DbSet` on both `IApplicationDbContext` and the DbContext, a configuration class, then a migration.
- Migrations live in `Infrastructure/Persistence/Migrations`.

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

The Development seeder (`DevelopmentDataSeeder`) creates 15 employees under one CEO, Margaret Hayes.

### Front end

`src/app` is split into `core/` (singletons, e.g. `ApiService`), `shared/`, and `features/`. `ApiService` prefixes requests with `environment.apiUrl` — `http://localhost:5062/api` in development (direct call, relies on API CORS), `/api` in production. `ApiService.get(path, params?)` takes optional query params. Uses `inject()` rather than constructor injection.

Conventions:
- Component classes have no `Component` suffix and files are named like `org-chart.ts|html|scss` (the Angular 20+ style, as with `App`).
- Use signals, `input()`/`computed()`, the `@if`/`@for` control flow, and `rxResource` for loading data.
- Feature pages are lazy-loaded with `loadComponent` in `app.routes.ts`.
- Specs sit next to their files. The app is zoneless, so specs use `await fixture.whenStable()` and `componentRef.setInput()`. Use `provideHttpClientTesting()` for services, and a stubbed service for pages.
- Colours are CSS custom properties (`--color-*`, `--shadow-card`) defined in `styles.scss`. Use those tokens instead of hard-coded colours.
- The app is light-only (`color-scheme: light`) and deliberately ignores the system dark mode.

Features:
- `features/org-chart/` (route `/org-chart`, which is also the default route) draws the org chart top-down.
  - `OrgChart` is the page. It handles the loading, error-with-retry and empty states, and starts the horizontally scrolling chart centred on the top of the tree.
  - `OrgChartBranch` is a recursive component that renders one card plus its reports. Each card collapses or expands its team.
  - The connector lines are pure CSS, using the nested `ul`/`li` `::before`/`::after` technique.
