# Backlog – feature/12-admin-app

Branch: `feature/12-admin-app` (from `develop`)  
Goal: Next.js admin panel + complete org access architecture with all authorization models labeled in UI.

---

## Phase A – Backend foundation ✅ DONE

| ID | Task | Status |
|----|------|--------|
| A1 | RefreshToken entity + DbContext + ensure tables | ✅ |
| A2 | TokenService: Access (15m) + Refresh (7d) + Renew | ✅ |
| A3 | AuthController: login/register both tokens; POST /refresh; /renew; /revoke | ✅ |
| A4 | ApplicationUser.IsGod + seed single GOD user | ✅ |
| A5 | MenuSection + SectionPermission models | ✅ |
| A6 | Seed default menu sections + Admin full access | ✅ |
| A7 | GET /api/menu – SSR-ready menu from DB | ✅ |
| A8 | Admin endpoints: create user, assign section permissions | ✅ |
| A9 | Update AuthDtos | ✅ |

### New credentials (seeded)
| User | Password | Notes |
|------|----------|-------|
| `god@authorization.local` | `God123!` | Only GOD (`IsGod=true`) |
| `admin@authorization.local` | `Admin123!` | Admin + all section perms |
| `finance@authorization.local` | `Finance123!` | Demo ABAC user |

### New API surface
- `POST /api/auth/login` → AccessToken + RefreshToken
- `POST /api/auth/refresh` → rotate tokens
- `POST /api/auth/renew` → new access, same refresh (extended)
- `POST /api/auth/revoke` → logout
- `GET /api/menu` → hierarchical menu for current user
- `GET /api/admin/users`
- `POST /api/admin/users`
- `POST /api/admin/users/section-permissions`
- `GET /api/admin/users/{id}/section-permissions`

---

## Phase B – admin-app scaffold ⬅️ NEXT

| ID | Task | Status |
|----|------|--------|
| B1 | Create `src/admin-app` (Next.js 15 App Router, TypeScript, Tailwind) | ⬜ |
| B2 | NextAuth credentials provider wrapping backend access/refresh/renew | ⬜ |
| B3 | Env config (API_URL, NEXTAUTH_SECRET, …) | ⬜ |
| B4 | Auth pages: login | ⬜ |
| B5 | Protected layout shell | ⬜ |

---

## Phase C – SSR Menu + Shell

| ID | Task | Status |
|----|------|--------|
| C1 | Server Component that calls GET /api/menu with access token | ⬜ |
| C2 | Sidebar rendered fully on server from menu payload | ⬜ |
| C3 | Method badges (RBAC / ABAC / …) on each nav item | ⬜ |
| C4 | Read-only vs write UI based on section permissions | ⬜ |

---

## Phase D – Admin pages (per section)

| ID | Task | Methods labeled | Status |
|----|------|-----------------|--------|
| D1 | Dashboard / Overview | — | ⬜ |
| D2 | Users management | RBAC + DAC | ⬜ |
| D3 | Roles & assignments | RBAC | ⬜ |
| D4 | Resources & ACL | DAC + MAC | ⬜ |
| D5 | ABAC policies | ABAC | ⬜ |
| D6 | Central policies (PBAC) | PBAC-Policy | ⬜ |
| D7 | Purposes | PBAC-Purpose | ⬜ |
| D8 | Risk policies & logs | RAdAC | ⬜ |
| D9 | Relation graph (ReBAC) | ReBAC | ⬜ |
| D10 | Privileged access (PAC) | PAC | ⬜ |
| D11 | Context policies | CBAC | ⬜ |
| D12 | Access rules | RuBAC | ⬜ |
| D13 | My access / profile | CBAC + claims | ⬜ |

---

## Phase E – Ops

| ID | Task | Status |
|----|------|--------|
| E1 | Dockerfile for admin-app | ⬜ |
| E2 | docker-compose service `admin-web` | ⬜ |
| E3 | CORS on API for admin-app origin | ⬜ |
| E4 | Update README + knowledge memory files | ⬜ |
| E5 | PR → develop | ⬜ |

---

## Locked decisions

- Folder: `src/admin-app`
- GOD: exactly one user (`IsGod = true`)
- Menu: dedicated endpoint, data from DB, fully SSR
- Auth: Access + Refresh + Renew from backend, wrapped in NextAuth
- Git Flow: feature branch from develop
