# admin-app

Next.js 15 admin panel for the Authorization multi-model demo.

## Stack

- Next.js 15 (App Router) + TypeScript + Tailwind
- NextAuth (Credentials) wrapping backend Access / Refresh / Renew tokens
- Menu fully **server-side rendered** from `GET /api/menu`

## Setup

```bash
cd src/admin-app
cp .env.example .env.local
# edit API_URL / NEXTAUTH_SECRET
npm install
npm run dev
```

Open http://localhost:3000

Ensure the API is running (`docker compose up` or `dotnet run`) on port 8080.

## Demo logins

| Email | Password | Notes |
|-------|----------|-------|
| god@authorization.local | God123! | GOD – full access |
| admin@authorization.local | Admin123! | Admin |
| finance@authorization.local | Finance123! | Limited |

## Auth flow

1. Login → backend `POST /api/auth/login` → Access (15m) + Refresh (7d)
2. NextAuth stores tokens in JWT session
3. On access expiry, JWT callback calls `POST /api/auth/refresh`
4. Protected routes use `next-auth/middleware`
5. Admin layout (Server Component) loads menu via `GET /api/menu`
