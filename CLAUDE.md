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
npx prettier --write .                      # formatting (.prettierrc); no ESLint configured
```

Health check (includes DB connectivity): `GET http://localhost:5062/api/health`. OpenAPI document is mapped only in Development.

## Architecture

Clean Architecture in `backend/src`, dependencies pointing inward: `Api → Application, Infrastructure`; `Infrastructure → Application → Domain`.

- **Domain** — entities only, no package dependencies. All entities derive from `BaseEntity` (`Guid Id`, `CreatedAt`, `UpdatedAt`).
- **Application** — use cases/DTOs and `IApplicationDbContext` (exposes `DbSet<T>`s + `SaveChangesAsync`). Application code depends on this interface, not on the concrete DbContext. Services are registered in `AddApplication()`.
- **Infrastructure** — `EmployeeManagerDbContext` implements `IApplicationDbContext`; registered in `AddInfrastructure()`, which also maps the interface to the same scoped context instance. Its `SaveChangesAsync` override stamps `CreatedAt`/`UpdatedAt` (UTC) for every `BaseEntity` — don't set these manually.
- **Api** — controllers go in `Controllers/` (none yet); `Program.cs` reads connection string `ConnectionStrings:EmployeeManagerDb` (throws if missing) and CORS origins from `Cors:AllowedOrigins`. Dev values live in `appsettings.Development.json`; `appsettings.json` leaves them empty.

### Persistence conventions

- Snake_case DB naming is applied globally via `UseSnakeCaseNamingConvention()` — write C# names in PascalCase; raw SQL (e.g. check constraints) must use snake_case column names.
- Entity mapping lives in `IEntityTypeConfiguration<T>` classes under `Infrastructure/Persistence/Configurations/`, picked up automatically by `ApplyConfigurationsFromAssembly`. Adding a new entity means: entity in Domain, `DbSet` on both `IApplicationDbContext` and the DbContext, a configuration class, then a migration.
- Migrations live in `Infrastructure/Persistence/Migrations`.

### Employee domain rules

`Employee` has an optional self-referencing `ManagerId` (null = top-level manager). Enforced in the DB: unique email, check constraint `ck_employees_manager_not_self`, and `DeleteBehavior.Restrict` on the manager FK (a manager with direct reports can't be deleted until reports are reassigned).

### Front end

`src/app` is split into `core/` (singletons, e.g. `ApiService`), `shared/`, and `features/`. `ApiService` prefixes requests with `environment.apiUrl` — `http://localhost:5062/api` in development (direct call, relies on API CORS), `/api` in production. Uses `inject()` rather than constructor injection.
