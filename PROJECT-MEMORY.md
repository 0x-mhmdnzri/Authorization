# PROJECT-MEMORY

High-level persistent memory for AI agents working on this repository.

## Vision

Build a single, clean, Dockerized .NET 10 Web API that demonstrates the major authorization models side-by-side so developers (and AI agents) can compare them practically.

Models planned (in order of implementation priority):

1. **RBAC** – Role-Based Access Control ← **DONE**
2. **ABAC** – Attribute-Based Access Control
3. **ReBAC** – Relationship-Based Access Control (Zanzibar-style)
4. **MAC** – Mandatory Access Control (clearance + classification)
5. **RuBAC** – Rule-Based (time / IP / device conditions)
6. **PBAC / CBAC / RAdAC / PAC** – as extensions or policy layers

Full theoretical scenarios live in `Authorization-Models-Scenarios.md`.

## Current Status (Snapshot)

| Area | Status | Notes |
|------|--------|-------|
| Project skeleton | ✅ | net10.0, Docker, OpenAPI |
| Identity + JWT | ✅ | Custom user/role, claims in token |
| RBAC endpoints | ✅ | Full CRUD-ish for roles + protected examples |
| Seed data | ✅ | Admin / Manager / User roles + default admin |
| ABAC | ❌ | User already has Department + ClearanceLevel |
| ReBAC | ❌ | DbContext has comments for RelationTuple |
| Tests | ❌ | None yet |
| Logging structure | ⚠️ | Basic ASP.NET Core logging only |
| Performance tuning | ❌ | Not started (demo scale) |

## Guiding Principles

1. **One model at a time** – finish RBAC cleanly before starting ABAC.
2. **Keep Identity tables** – do not replace ASP.NET Core Identity; extend it.
3. **Docker-first** – every change must still work with `docker compose up --build`.
4. **Educational clarity over production perfection** – code should be readable and annotated.
5. **Prepare for hybrids** – real systems combine models; design tables and services with that in mind.

## Important Invariants

- JWT always contains: `sub`, `email`, `role` claims + custom `department`, `clearance`, `firstName`, `lastName`.
- Migrations run automatically on startup (`MigrateAsync` + seed).
- All RBAC-protected endpoints live under `/api/rbac/*`.
- Auth endpoints live under `/api/auth/*`.

## Recent History (High Level)

- Project created with RBAC baseline using Identity + JWT.
- Knowledge-base files added for AI agents (this set of docs).

## Next Logical Steps (Priority Order)

1. Add structured logging + request correlation.
2. Add a simple test project (xUnit + WebApplicationFactory).
3. Implement ABAC policy evaluation (start with attributes already on User).
4. Introduce a Policy Decision Point (PDP) abstraction so later models can plug in.
5. Add ReBAC with a simple relation-tuple table + check API.
