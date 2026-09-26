# Authorization

.NET 10 Web API demonstrating multiple authorization models (RBAC, ABAC, MAC, ReBAC, RAdAC, etc.) using:

- ASP.NET Core Identity (custom tables based on Identity)
- EF Core + PostgreSQL
- JWT Authentication
- Fully Dockerized
- **Git Flow** strictly followed

## Git Flow Strategy

```
main          ← production-ready, tagged releases
  ↑
develop       ← integration branch
  ↑
feature/*     ← one feature branch per authorization model
```

### Branch naming convention
- `feature/01-rbac-core`
- `feature/02-abac`
- `feature/03-mac`
- `feature/04-dac`
- `feature/05-pbac-policy`
- `feature/06-pbac-purpose`
- `feature/07-radac`
- `feature/08-rebac`
- `feature/09-pac`
- `feature/10-cbac`
- `feature/11-rubac`

### Workflow for each model
1. `git checkout develop`
2. `git checkout -b feature/0X-model-name`
3. Implement + tests + migration
4. Commit with conventional commits (`feat(rbac): ...`, `fix(...)`, `docs(...)`)
5. Push & open PR → develop
6. After review/merge → delete feature branch
7. When ready for release: `git checkout main && git merge develop && git tag vX.Y.Z`

## Project Structure

```
Authorization/
├── docker-compose.yml
├── .gitignore
├── README.md
├── Authorization-Models-Scenarios.md
└── src/
    └── Authorization.API/
        ├── Controllers/
        │   ├── AuthController.cs
        │   └── RbacController.cs
        ├── Data/
        │   ├── ApplicationDbContext.cs
        │   └── Migrations/
        ├── DTOs/
        ├── Models/
        ├── Services/
        ├── Dockerfile
        ├── Program.cs
        └── appsettings.json
```

## Quick Start with Docker

```bash
docker compose up --build
```

- API: http://localhost:8080
- OpenAPI: http://localhost:8080/openapi/v1.json
- PostgreSQL: localhost:5432 (`auth_user` / `auth_password` / `authorization_db`)

### Default Admin
- Email: `admin@authorization.local`
- Password: `Admin123!`
- Role: `Admin`

## Implemented Models

### ✅ 1. RBAC – Role-Based Access Control (`feature/01-rbac-core`)

- Custom `ApplicationUser` + `ApplicationRole`
- Seeded roles: Admin, Manager, User
- JWT includes role claims
- Full CRUD for roles + assignment
- Protected endpoints with `[Authorize(Roles = "...")]`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/auth/register | Anonymous | Register + default User role |
| POST | /api/auth/login | Anonymous | Login → JWT |
| GET | /api/auth/me | Bearer | Current user + roles |
| GET | /api/rbac/roles | Admin | List roles |
| POST | /api/rbac/roles | Admin | Create role |
| POST | /api/rbac/assign-role | Admin | Assign role |
| DELETE | /api/rbac/remove-role | Admin | Remove role |
| GET | /api/rbac/admin-only | Admin | Demo protected |
| GET | /api/rbac/manager-area | Manager,Admin | Demo protected |

## Planned (one feature branch each)

- [ ] ABAC
- [ ] MAC
- [ ] DAC
- [ ] PBAC (Policy)
- [ ] PBAC (Purpose)
- [ ] RAdAC
- [ ] ReBAC
- [ ] PAC
- [ ] CBAC
- [ ] RuBAC

## Local development

```bash
# Restore & migrate
dotnet restore src/Authorization.API
dotnet ef database update --project src/Authorization.API

# Run
dotnet run --project src/Authorization.API
```

## Notes

- Identity tables customized (`Users`, `Roles`, `UserRoles`, ...)
- Migrations auto-applied on startup
- Ready for Duende IdentityServer / OpenIddict integration later
