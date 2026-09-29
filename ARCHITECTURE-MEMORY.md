# ARCHITECTURE-MEMORY

## High-Level Architecture

```
┌─────────────────┐     JWT      ┌──────────────────────┐
│   Client /      │ ──────────►  │  Authorization.API   │
│   Postman /     │              │  (ASP.NET Core 10)   │
│   Frontend      │ ◄──────────  │                      │
└─────────────────┘   JSON       │  Controllers         │
                                 │  ├─ AuthController   │
                                 │  └─ RbacController   │
                                 │                      │
                                 │  Services            │
                                 │  └─ TokenService     │
                                 │                      │
                                 │  Identity + EF Core  │
                                 │  └─ ApplicationDbContext
                                 └──────────┬───────────┘
                                            │
                                            ▼
                                 ┌──────────────────────┐
                                 │   PostgreSQL 17      │
                                 │  (Users, Roles,      │
                                 │   UserRoles, ...)    │
                                 └──────────────────────┘
```

## Layers (Current)

| Layer | Responsibility | Location |
|-------|----------------|----------|
| **API / Presentation** | HTTP endpoints, authorization attributes, DTO mapping | `Controllers/`, `DTOs/` |
| **Application / Services** | Token generation, future policy evaluation | `Services/` |
| **Domain / Models** | User, Role, future Resource, Relation, Policy | `Models/` |
| **Infrastructure** | EF Core, Identity stores, PostgreSQL | `Data/`, Identity packages |
| **Hosting** | DI, middleware pipeline, seeding, Docker | `Program.cs`, `Dockerfile`, `docker-compose.yml` |

## Key Design Choices

### Identity Customization
- `ApplicationUser` extends `IdentityUser` with domain fields useful for multiple models (`Department`, `ClearanceLevel`).
- `ApplicationRole` extends `IdentityRole` and already has `ParentRoleId` for future hierarchical RBAC.
- Table names customized: `Users`, `Roles`, `UserRoles`, etc.

### Authentication
- Symmetric JWT (HMAC-SHA256).
- Claims injected: roles + custom attributes.
- Token lifetime: 8 hours (hard-coded in `TokenService`).

### Authorization (RBAC)
- Pure declarative: `[Authorize(Roles = "Admin")]` etc.
- No custom `IAuthorizationHandler` yet.
- Role checks happen at the framework level after JWT validation.

### Data Access
- Single `ApplicationDbContext` inheriting `IdentityDbContext<ApplicationUser, ApplicationRole, string>`.
- Auto-migrate + seed on every startup (demo convenience).

### Future Extension Points (already prepared in comments)

```csharp
// In ApplicationDbContext – planned tables
// public DbSet<Permission> Permissions { get; set; }
// public DbSet<RolePermission> RolePermissions { get; set; }
// public DbSet<Resource> Resources { get; set; }
// public DbSet<RelationTuple> RelationTuples { get; set; } // ReBAC
```

## Request Pipeline Order

1. Exception handling (default)
2. HTTPS redirection
3. Authentication (JwtBearer)
4. Authorization
5. Controllers

## Docker Topology

- `api` service builds from `src/Authorization.API/Dockerfile`.
- Depends on healthy `db` (Postgres healthcheck).
- Shared bridge network `auth-network`.
- Named volume for Postgres data.

## Open Questions Affecting Architecture

See `OPEN-QUESTIONS.md`. Main ones:
- Central PDP vs. per-controller handlers for ABAC/ReBAC?
- Keep everything in one project or split into Authorization.Core / .Infrastructure later?
