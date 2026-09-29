# PERFORMANCE-PLAYBOOK

This is a demonstration project. Performance work is low priority until the feature set stabilizes.

## Current Characteristics

- Single process, single database.
- Very small data volume (seeded roles + a few users).
- JWT validation is cheap (symmetric key).
- Identity role checks are in-memory after the user is loaded.

## Known Potential Bottlenecks (future)

| Area | Risk | Mitigation idea |
|------|------|-----------------|
| Role / claim lookup on every request | Low now | Cache user roles in the token (already done) |
| Future ReBAC graph checks | Medium | Use a dedicated engine or materialize / cache paths |
| Future complex ABAC policies | Medium | Compile policies, cache decision results with short TTL |
| EF Core tracking | Low | Use `AsNoTracking()` for read-only queries |
| Auto-migrate on every startup | Acceptable for demo | Move to explicit migration job in production |

## Checklist for Later Optimization

- [ ] Add response caching for pure GET authorization metadata where safe.
- [ ] Measure JWT generation / validation under load.
- [ ] Consider Redis for distributed session / policy decision cache if the service is scaled.
- [ ] Review N+1 queries when ReBAC or multi-attribute ABAC is added.
- [ ] Enable EF Core compiled queries for hot paths.

## Rule for Agents

Do **not** optimize for performance unless the task explicitly asks for it or a measurable problem is reported. Prefer clarity and correctness.
