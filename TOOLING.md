# TOOLING

Everything an agent or developer needs to build, run, and inspect this project.

## Prerequisites

- Docker + Docker Compose (recommended path)
- OR .NET 10 SDK + local PostgreSQL 17

## Primary Path – Docker

```bash
# From repository root
docker compose up --build
```

Services:
- `authorization-api` → http://localhost:8080
- `authorization-db` → localhost:5432

Useful commands:

```bash
# Rebuild only API
docker compose up --build api

# View logs
docker compose logs -f api

# Stop and remove containers (keep volume)
docker compose down

# Full reset (including DB volume)
docker compose down -v
```

## Local Development (without Docker)

1. Start PostgreSQL and create database `authorization_db` with user/password matching `appsettings.Development.json`.
2. Update connection string if needed.
3. From `src/Authorization.API`:

```bash
dotnet restore
dotnet ef migrations add Initial --if needed
dotnet run
```

Migrations are applied automatically on startup, so manual `dotnet ef database update` is optional.

## Useful .NET Commands

```bash
# Build
dotnet build src/Authorization.API

# Run
dotnet run --project src/Authorization.API

# Add a migration (when schema changes)
dotnet ef migrations add <Name> --project src/Authorization.API

# List packages
dotnet list src/Authorization.API package
```

## OpenAPI / Exploration

- JSON: http://localhost:8080/openapi/v1.json
- Use any OpenAPI client (Postman, Insomnia, Bruno, Swagger UI if added later).

## Default Test Credentials

```
POST /api/auth/login
{
  "email": "admin@authorization.local",
  "password": "Admin123!"
}
```

## Environment Variables (Docker)

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__DefaultConnection` | Postgres connection |
| `Jwt__Key` | Symmetric signing key (≥ 32 chars) |
| `Jwt__Issuer` / `Jwt__Audience` | Token validation |
| `ASPNETCORE_ENVIRONMENT` | Development / Production |
| `ASPNETCORE_URLS` | Listening addresses |

## Package Snapshot (see .csproj)

- Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.0
- Microsoft.AspNetCore.OpenApi 10.0.12
- Microsoft.EntityFrameworkCore.Design 10.0.0
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
