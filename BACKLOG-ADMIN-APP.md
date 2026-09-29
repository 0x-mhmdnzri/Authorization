# Backlog – feature/12-admin-app

**Status: ALL PHASES COMPLETE ✅**

| Phase | Description | Status |
|-------|-------------|--------|
| A | Backend: Access/Refresh/Renew, GOD, Menu API, Admin users | ✅ |
| B | Next.js scaffold + NextAuth | ✅ |
| C | SSR menu + method badges | ✅ |
| D | Admin pages (Users, Roles, all model lists, Profile) | ✅ |
| E | Dockerfile, docker-compose `admin-web`, CORS, README | ✅ |

## Run everything

```bash
docker compose up --build
```

- Admin: http://localhost:3000  
- API: http://localhost:8080  
- Login: `god@authorization.local` / `God123!`
