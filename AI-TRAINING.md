# AI-TRAINING

Principles and constraints that every AI agent working on this repository must internalize.

## Mission

Help the human owner evolve a clean, educational demonstration of multiple authorization models on top of ASP.NET Core Identity + JWT + PostgreSQL, while keeping the knowledge base accurate.

## Core Constraints

1. **Truth over speed** – Never invent endpoints, tables, or behaviors that do not exist. If something is missing, say so and propose the minimal addition.
2. **Additive evolution** – New models (ABAC, ReBAC, …) must coexist with the existing RBAC surface.
3. **Docker is sacred** – The `docker compose up --build` path must continue to work after every change.
4. **Knowledge base is part of the product** – Code and the `*.md` memory files must stay in sync.
5. **Educational clarity** – Prefer obvious, well-commented code. This repo teaches; it is not a production template.

## Preferred Behavior

- When asked to “implement X”, first check DOMAIN-MEMORY and OPEN-QUESTIONS.
- When making a design choice, record it in DECISIONS.md.
- When finishing a feature, update the status table in PROJECT-MEMORY.md.
- Use the existing claim names and property names (`department`, `clearance`, etc.) instead of inventing new ones.

## Anti-Patterns to Avoid

- “I’ll just rewrite Program.cs from scratch.”
- Adding a second authentication scheme without discussion.
- Putting business logic inside controllers when a service already exists.
- Leaving temporary test files or debug endpoints in main.
- Updating only code and forgetting the knowledge base.

## Success Metric for an Agent Session

The next agent (or the human) can open SOURCE-OF-TRUTH + PROJECT-MEMORY and immediately understand what is true right now, without reading the entire git history.
