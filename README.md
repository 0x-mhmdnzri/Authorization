# Authorization

.NET 10 Web API demonstrating multiple authorization models (RBAC, ABAC, MAC, ReBAC, RAdAC, etc.) using:

- ASP.NET Core Identity (custom tables based on Identity)
- EF Core + PostgreSQL
- JWT Authentication
- Fully Dockerized

## Project Structure

```
Authorization/
├── docker-compose.yml
├── src/
│   └── Authorization.API/
│       ├── Controllers/
│       │   ├── AuthController.cs      # Register / Login / Me
│       │   └── RbacController.cs      # RBAC endpoints (first model)
│       ├── Data/
│       │   └── ApplicationDbContext.cs
│       ├── DTOs/
│       ├── Models/
│       │   ├── ApplicationUser.cs
│       │   └── ApplicationRole.cs
│       ├── Services/
│       │   └── TokenService.cs
│       ├── Dockerfile
│       ├── .dockerignore
│       ├── Program.cs
│       └── appsettings.json
└── Authorization-Models-Scenarios.md  # Theory & scenarios for all models
```

## Quick Start with Docker

```bash
docker compose up --build
```

- API: http://localhost:8080
- OpenAPI: http://localhost:8080/openapi/v1.json
- PostgreSQL: localhost:5432 (user: `auth_user` / pass: `auth_password` / db: `authorization_db`)

### Default Admin

- Email: `admin@authorization.local`
- Password: `Admin123!`
- Role: `Admin`

## Implemented so far

### 1. RBAC (Role-Based Access Control)

- Custom `ApplicationUser` + `ApplicationRole` on top of Identity tables
- Roles: Admin, Manager, User (seeded)
- JWT contains role claims
- Endpoints under `/api/rbac/*` protected by `[Authorize(Roles = "...")]`

**Key endpoints:**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/auth/register | Anonymous | Register + default User role |
| POST | /api/auth/login | Anonymous | Login → JWT |
| GET | /api/auth/me | Bearer | Current user + roles |
| GET | /api/rbac/roles | Admin | List roles |
| POST | /api/rbac/roles | Admin | Create role |
| POST | /api/rbac/assign-role | Admin | Assign role to user |
| GET | /api/rbac/admin-only | Admin | Example protected |
| GET | /api/rbac/manager-area | Manager,Admin | Example protected |

## Next models (planned)

- ABAC (Attribute-Based)
- ReBAC (Relationship-Based)
- MAC, DAC, RuBAC, PBAC, RAdAC, PAC, CBAC

## Local development (without Docker)

1. Start PostgreSQL and update `appsettings.Development.json` connection string
2. `dotnet ef migrations add Initial --project src/Authorization.API`
3. `dotnet run --project src/Authorization.API`

## Notes

- Identity tables are customized (Users, Roles, UserRoles, ...)
- Ready to share the same Identity store with Duende IdentityServer / OpenIddict later if needed
- Migrations run automatically on startup (MigrateAsync + seed)

