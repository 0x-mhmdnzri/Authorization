# layer-map

Precise mapping of physical folders / files to logical layers and responsibilities.

```
src/Authorization.API/
├── Controllers/                     ← Presentation / API layer
│   ├── AuthController.cs            # Authentication endpoints (register/login/me)
│   └── RbacController.cs            # RBAC management + protected demo endpoints
│
├── DTOs/                            ← Presentation contracts
│   └── AuthDtos.cs                  # Request/Response records for Auth + RBAC
│
├── Services/                        ← Application services
│   └── TokenService.cs              # JWT creation (ITokenService)
│
├── Models/                          ← Domain entities
│   ├── ApplicationUser.cs           # IdentityUser + domain attributes
│   └── ApplicationRole.cs           # IdentityRole + hierarchy fields
│
├── Data/                            ← Infrastructure / Persistence
│   └── ApplicationDbContext.cs      # IdentityDbContext + future tables
│
├── Program.cs                       ← Composition root + pipeline + seeding
├── appsettings.json                 ← Configuration
├── appsettings.Development.json
├── Dockerfile                       ← Container build
├── Authorization.API.csproj         ← Project + package references
└── Properties/launchSettings.json

Root/
├── docker-compose.yml               ← Orchestration (API + Postgres)
├── Authorization-Models-Scenarios.md# Domain theory
└── *.md knowledge files             ← AI agent memory layer
```

## Responsibility Summary

| Layer | Owns | Does NOT own |
|-------|------|--------------|
| Controllers | HTTP, status codes, `[Authorize]`, calling services | Business rules, DB access |
| DTOs | Shape of requests/responses | Validation beyond attributes, domain logic |
| Services | Token generation, future policy evaluation | Direct EF queries (prefer repositories later) |
| Models | Entity shape, navigation properties | Persistence configuration |
| Data / DbContext | Mapping, table names, migrations | Business decisions |
| Program.cs | DI registration, middleware order, seed | Feature logic |
