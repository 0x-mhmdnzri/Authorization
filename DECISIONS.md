# DECISIONS (Architecture Decision Records)

Format: short ADR. Newest first.

---

## ADR-005 – Knowledge base for AI agents (2026-09-29)

**Status:** Accepted  
**Context:** Multiple AI agents will work on this repo. They need shared, up-to-date understanding.  
**Decision:** Create a set of markdown files at the root (SOURCE-OF-TRUTH, PROJECT-MEMORY, ARCHITECTURE-MEMORY, DOMAIN-MEMORY, DECISIONS, OPEN-QUESTIONS, KNOWLEDGE-INDEX, layer-map, TOOLING, etc.).  
**Consequences:** Agents can bootstrap quickly. These files must be kept in sync with code changes.

---

## ADR-004 – Auto-migrate + seed on startup

**Status:** Accepted (demo convenience)  
**Context:** Educational project; developers and agents should get a working system with zero manual steps.  
**Decision:** Call `Database.MigrateAsync()` and seed roles + admin user inside `Program.cs` on every startup.  
**Consequences:** Simple DX. Not suitable for production (use proper migration pipelines later).

---

## ADR-003 – Custom Identity tables + extra attributes on User/Role

**Status:** Accepted  
**Context:** We want to demonstrate multiple authorization models without throwing away Identity.  
**Decision:**  
- Extend `IdentityUser` → `ApplicationUser` with `Department`, `ClearanceLevel`, etc.  
- Extend `IdentityRole` → `ApplicationRole` with `Description` + `ParentRoleId`.  
- Rename tables to `Users`, `Roles`, `UserRoles`...  
**Consequences:** Ready for ABAC/MAC attributes and hierarchical RBAC. Identity can still be shared with IdentityServer later.

---

## ADR-002 – JWT with roles + custom claims

**Status:** Accepted  
**Context:** Need a simple, self-contained authentication mechanism for demos.  
**Decision:** Symmetric JWT (HMAC-SHA256), 8-hour lifetime, include role claims + `department` + `clearance` + name claims.  
**Consequences:** Stateless, easy to inspect. Secrets currently live in docker-compose / appsettings (acceptable for demo).

---

## ADR-001 – Start with classic RBAC using ASP.NET Core Identity

**Status:** Accepted  
**Context:** RBAC is the most widely understood model and the natural baseline.  
**Decision:** Implement RBAC first with Identity roles and declarative `[Authorize(Roles = "...")]`.  
**Consequences:** Fast delivery of a working baseline. Other models will be added incrementally on top of the same Identity store.
