# OPEN-QUESTIONS

Questions that affect future design. AI agents should not invent answers; surface them when relevant.

## Architecture

1. **PDP design**  
   When we add ABAC / ReBAC / RuBAC, should we introduce a single central `IAuthorizationService` / Policy Decision Point, or keep model-specific handlers and attributes?

2. **Project structure**  
   Stay with a single `Authorization.API` project or eventually split into:
   - Authorization.Domain
   - Authorization.Application
   - Authorization.Infrastructure
   - Authorization.API

3. **Policy language**  
   For ABAC/PBAC: pure C# predicates, a simple DSL, or adopt something like Open Policy Agent / Cedar / ALFA later?

## Data Model

4. **Permission entity**  
   Introduce an explicit `Permission` table + many-to-many with Role, or continue relying on role names only?

5. **Resource modeling**  
   How generic should the Resource table be? (Type + Id vs. strongly-typed resources per domain.)

6. **ReBAC storage**  
   Simple RelationTuple table vs. integrating an existing engine (OpenFGA, SpiceDB, Permify) via HTTP?

## Security & Ops

7. **Secrets management**  
   When do we move JWT key and DB password out of docker-compose into proper secret stores?

8. **HTTPS / production readiness**  
   Keep HTTP-only for local demo or add proper TLS configuration?

9. **Testing strategy**  
   Priority of unit tests vs. integration tests (WebApplicationFactory) vs. contract tests?

## Product

10. **Order of next models**  
    Current proposed order: ABAC → ReBAC → MAC → RuBAC. Is this still optimal?

11. **Admin UI**  
    Pure API forever, or add a minimal Blazor / React admin later for role/permission/relation management?
