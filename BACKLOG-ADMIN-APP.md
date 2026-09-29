# Backlog – feature/12-admin-app

Branch: `feature/12-admin-app` (from `develop`)  
Goal: Next.js admin panel + complete org access architecture with all authorization models labeled in UI.

---

## Phase A – Backend foundation (CURRENT)

| ID | Task | Status |
|----|------|--------|
| A1 | RefreshToken entity + DbContext + migration | 🔄 in progress |
| A2 | TokenService: Access (15m) + Refresh (7d) + Renew | ⬜ |
| A3 | AuthController: login/register return both tokens; POST /refresh; POST /renew; POST /revoke | ⬜ |
| A4 | ApplicationUser.IsGod + seed single GOD user | ⬜ |
| A5 | MenuSection + SectionPermission models (read/write per section) | ⬜ |
| A6 | Seed default menu sections + GOD full access | ⬜ |
| A7 | GET /api/menu – SSR-ready menu from DB filtered by user permissions | ⬜ |
| A8 | Admin endpoints: create user, assign section permissions (GOD / higher manager) | ⬜ |
| A9 | Update AuthDtos + OpenAPI | ⬜ |

---

## Phase B – admin-app scaffold

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
| D2 | Users management (create, list, assign roles & section perms) | RBAC + DAC | ⬜ |
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
