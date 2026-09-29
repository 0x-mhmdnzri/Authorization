# Backlog – feature/12-admin-app

Branch: `feature/12-admin-app`

---

## Phase A – Backend ✅
## Phase B – Scaffold + NextAuth ✅
## Phase C – SSR Menu + badges ✅

---

## Phase D – Admin pages ✅ DONE

| ID | Page | Methods | Status |
|----|------|---------|--------|
| D1 | Dashboard | CBAC,RBAC | ✅ |
| D2 | Users (+ detail + create + section perms) | RBAC,DAC | ✅ |
| D3 | Roles & assignments | RBAC | ✅ |
| D4 | Resources & ACL | DAC,MAC | ✅ (list) |
| D5 | ABAC policies | ABAC | ✅ (list) |
| D6 | Central policies | PBAC-Policy | ✅ (list) |
| D7 | Purposes | PBAC-Purpose | ✅ (list) |
| D8 | Risk policies | RAdAC | ✅ (list) |
| D9 | Relations | ReBAC | ✅ (info) |
| D10 | Privileges | PAC | ✅ (list) |
| D11 | Context policies | CBAC | ✅ (list) |
| D12 | Access rules | RuBAC | ✅ (list) |
| D13 | My Access / profile | CBAC,RBAC | ✅ |

---

## Phase E – Ops ⬅️ NEXT

| ID | Task | Status |
|----|------|--------|
| E1 | Dockerfile for admin-app | ⬜ |
| E2 | docker-compose service `admin-web` | ⬜ |
| E3 | CORS on API for admin-app origin | ⬜ |
| E4 | Update README | ⬜ |
| E5 | PR → develop | ⬜ |

---

## Run

```bash
docker compose up --build   # API :8080
cd src/admin-app && cp .env.example .env.local && npm i && npm run dev  # :3000
```

Login: `god@authorization.local` / `God123!`
