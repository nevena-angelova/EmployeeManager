# EmployeeManager

Employee management system.

| Layer     | Technology                         |
|-----------|------------------------------------|
| Back-end  | ASP.NET Core Web API (.NET 10)     |
| ORM       | Entity Framework Core 10 (Npgsql)  |
| Database  | PostgreSQL 17                      |
| Front-end | Angular 20                         |

## Structure

```
EmployeeManager/
├── backend/
│   ├── EmployeeManager.slnx
│   ├── src/
│   │   ├── EmployeeManager.Api/             # Web API: controllers, Program.cs, config
│   │   ├── EmployeeManager.Application/     # Use cases, interfaces, DTOs
│   │   ├── EmployeeManager.Domain/          # Entities, domain logic (no dependencies)
│   │   └── EmployeeManager.Infrastructure/  # EF Core DbContext, configurations, migrations
│   └── tests/
│       └── EmployeeManager.UnitTests/
├── frontend/
│   └── employee-manager-client/             # Angular app (core/, shared/, features/)
└── docker-compose.yml                       # Local PostgreSQL
```

Dependencies point inward: `Api → Application, Infrastructure`, `Infrastructure → Application → Domain`.

## Getting started

Prerequisites: .NET 10 SDK, Node.js 20.19+ (or 22.12+), Docker (or a local PostgreSQL).

```bash
docker compose up -d                                  # start PostgreSQL
dotnet run --project backend/src/EmployeeManager.Api  # API on http://localhost:5062
cd frontend/employee-manager-client && npm install && npm start   # UI on http://localhost:4200
```

Health check: `GET http://localhost:5062/api/health`

## EF Core migrations

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project backend/src/EmployeeManager.Infrastructure \
  --startup-project backend/src/EmployeeManager.Api \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project backend/src/EmployeeManager.Infrastructure \
  --startup-project backend/src/EmployeeManager.Api
```
