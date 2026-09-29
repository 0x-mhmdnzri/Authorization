# Authorization

.NET 10 Web API demonstrating **11 authorization models** + a **Next.js admin panel**.

- ASP.NET Core Identity · EF Core · PostgreSQL · JWT (Access + Refresh + Renew)
- Fully Dockerized (`api` + `db` + `admin-web`)
- Git Flow

## Quick Start

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| **Admin panel** | http://localhost:3000 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| OpenAPI | http://localhost:8080/openapi/v1.json |
| PostgreSQL | localhost:5432 (`auth_user` / `auth_password` / `authorization_db`) |

### Demo accounts

| Email | Password | Notes |
|-------|----------|-------|
| `god@authorization.local` | `God123!` | **GOD** – full access to every section |
| `admin@authorization.local` | `Admin123!` | Admin + all section permissions |
| `finance@authorization.local` | `Finance123!` | ABAC demo user |

## Admin panel (`src/admin-app`)

Next.js 15 App Router + NextAuth wrapping backend tokens.

- Menu is **fully server-side rendered** from `GET /api/menu`
- Each section shows which authorization method(s) protect it (RBAC, ABAC, MAC, …)
- GOD / higher managers assign per-section **read/write** (DAC + RBAC)

### Local UI only

```bash
cd src/admin-app
cp .env.example .env.local
npm install && npm run dev
```

Requires API on `:8080`.

### Auth endpoints (stateless)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/login` | Access (15m) + Refresh (7d) |
| POST | `/api/auth/refresh` | Rotate both tokens |
| POST | `/api/auth/renew` | New access, extend refresh |
| POST | `/api/auth/revoke` | Logout / revoke refresh |
| GET | `/api/auth/me` | Current user |
| GET | `/api/menu` | SSR menu for current user |
| GET/POST | `/api/admin/users` | User admin |
| POST | `/api/admin/users/section-permissions` | Grant section R/W |

## Project structure

```
src/
├── Authorization.API/     # .NET 10 API – all 11 models
└── admin-app/             # Next.js admin panel
```

## Implemented authorization models

| # | Model | Branch | Status |
|---|-------|--------|--------|
| 01 | RBAC | `feature/01-rbac-core` | ✅ |
| 02 | ABAC | `feature/02-abac` | ✅ |
| 03 | MAC | `feature/03-mac` | ✅ |
| 04 | DAC | `feature/04-dac` | ✅ |
| 05 | PBAC-Policy | `feature/05-pbac-policy` | ✅ |
| 06 | PBAC-Purpose | `feature/06-pbac-purpose` | ✅ |
| 07 | RAdAC | `feature/07-radac` | ✅ |
| 08 | ReBAC | `feature/08-rebac` | ✅ |
| 09 | PAC | `feature/09-pac` | ✅ |
| 10 | CBAC | `feature/10-cbac` | ✅ |
| 11 | RuBAC | `feature/11-rubac` | ✅ |
| 12 | Admin panel | `feature/12-admin-app` | ✅ |

Full theory: [Authorization-Models-Scenarios.md](Authorization-Models-Scenarios.md)  
Admin backlog: [BACKLOG-ADMIN-APP.md](BACKLOG-ADMIN-APP.md)

## Git Flow

```
main ← production
  ↑
develop ← integration
  ↑
feature/* ← one branch per feature
```

## Notes

- Migrations + seed run automatically on API startup
- CORS allows `http://localhost:3000` for the admin panel
- Identity tables customized (`Users`, `Roles`, …)
