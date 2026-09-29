# Backlog – feature/12-admin-app

Branch: `feature/12-admin-app` (from `develop`)  
Goal: Next.js admin panel + complete org access architecture with all authorization models labeled in UI.

---

## Phase A – Backend foundation ✅ DONE

See previous commits. Access/Refresh/Renew, GOD, Menu API, Admin users API.

---

## Phase B – admin-app scaffold ✅ DONE

| ID | Task | Status |
|----|------|--------|
| B1 | Create `src/admin-app` (Next.js 15 App Router, TypeScript, Tailwind) | ✅ |
| B2 | NextAuth credentials provider wrapping backend access/refresh/renew | ✅ |
| B3 | Env config (`.env.example`) | ✅ |
| B4 | Auth pages: login | ✅ |
| B5 | Protected layout shell + middleware | ✅ |

---

## Phase C – SSR Menu + Shell ✅ DONE (included in scaffold)

| ID | Task | Status |
|----|------|--------|
| C1 | Server Component calls GET /api/menu | ✅ |
| C2 | Sidebar fully SSR from menu payload | ✅ |
| C3 | Method badges on each nav item | ✅ |
| C4 | Read-only indicator when `canWrite=false` | ✅ |

---

## Phase D – Admin pages (per section) ⬅️ NEXT

| ID | Task | Methods labeled | Status |
|----|------|-----------------|--------|
| D1 | Dashboard / Overview | CBAC,RBAC | ✅ (basic) |
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

## How to run admin-app locally

```bash
# Terminal 1 – API
docker compose up --build
# or: dotnet run --project src/Authorization.API

# Terminal 2 – Admin UI
cd src/admin-app
cp .env.example .env.local
npm install
npm run dev
```

Open http://localhost:3000 — login with `god@authorization.local` / `God123!`
