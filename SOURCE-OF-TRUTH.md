# SOURCE-OF-TRUTH

> Canonical facts about this repository. AI agents must treat this file as the highest-priority source of truth. When any other document conflicts with this file, this file wins.

## Project Identity

| Field | Value |
|-------|-------|
| **Name** | Authorization |
| **Owner** | 0x-mhmdnzri |
| **Repo** | https://github.com/0x-mhmdnzri/Authorization |
| **Purpose** | Educational / demonstration .NET Web API that implements multiple authorization models (RBAC first, then ABAC, ReBAC, MAC, etc.) |
| **Language** | C# |
| **Framework** | ASP.NET Core |
| **Target Framework** | `net10.0` |
| **Database** | PostgreSQL 17 |
| **ORM** | EF Core 10 + Npgsql |
| **Identity** | ASP.NET Core Identity (custom `ApplicationUser` + `ApplicationRole`) |
| **AuthN** | JWT Bearer |
| **AuthZ (current)** | Classic RBAC via `[Authorize(Roles = "...")]` |
| **Container** | Fully Dockerized (`docker-compose.yml`) |
| **OpenAPI** | Built-in ASP.NET Core OpenAPI (`/openapi/v1.json`) |

## Current Implemented Scope (as of last update)

- **RBAC only** is fully working.
- Roles seeded: `Admin`, `Manager`, `User`.
- Default admin user: `admin@authorization.local` / `Admin123!` (role: Admin).
- Custom properties already present on user for future models: `Department`, `ClearanceLevel`.
- Role hierarchy field exists (`ParentRoleId`) but is not yet used in authorization logic.

## Explicit Non-Goals (for now)

- No frontend.
- No IdentityServer / OpenIddict yet (but Identity tables are designed so they can be shared later).
- No production hardening (secrets in compose, no rate limiting, no HTTPS enforcement in container, etc.).
- No unit / integration tests yet.

## Key Paths

```
/
├── docker-compose.yml
├── Authorization-Models-Scenarios.md   # Theory for all models
├── src/
│   └── Authorization.API/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   └── RbacController.cs
│       ├── Data/ApplicationDbContext.cs
│       ├── Models/
│       │   ├── ApplicationUser.cs
│       │   └── ApplicationRole.cs
│       ├── Services/TokenService.cs
│       ├── DTOs/AuthDtos.cs
│       ├── Program.cs
│       └── Dockerfile
└── (this knowledge base)
```

## Running the Project

```bash
docker compose up --build
```

- API: http://localhost:8080
- OpenAPI: http://localhost:8080/openapi/v1.json
- PostgreSQL: localhost:5432 (user=`auth_user`, pass=`auth_password`, db=`authorization_db`)

## Default Credentials

- Email: `admin@authorization.local`
- Password: `Admin123!`
- Role: `Admin`

## Stack Versions (locked in csproj)

- Microsoft.AspNetCore.* 10.0.x
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
- TargetFramework: net10.0
