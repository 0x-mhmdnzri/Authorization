# TASK-PROTOCOL

How any AI agent must approach work in this repository.

## 1. Bootstrap (every new session)

1. Read `SOURCE-OF-TRUTH.md`.
2. Skim `PROJECT-MEMORY.md` and `OPEN-QUESTIONS.md`.
3. If the task touches architecture or domain, also read the relevant memory file.
4. Confirm current branch is `main` (or the branch named in the task).

## 2. Before Writing Code

- Restate the goal in one sentence.
- List the files you expect to touch.
- Check whether the change affects any of the knowledge-base documents; if yes, plan to update them in the same change set.
- Prefer the smallest possible change that satisfies the request.

## 3. Implementation Rules

- Keep Docker working: after structural changes, the project must still start with `docker compose up --build`.
- Do not remove existing Identity customization.
- New authorization models must be additive; do not break the current RBAC endpoints.
- Prefer explicit, readable code over clever abstractions (this is an educational repo).
- Update `DECISIONS.md` when you make a non-trivial architectural choice.

## 4. After Implementation

- Update the relevant knowledge files (PROJECT-MEMORY status table, DOMAIN-MEMORY endpoints, etc.).
- If you added a new public endpoint, document it in DOMAIN-MEMORY and README.
- Leave the working tree in a state that another agent can continue from.

## 5. Communication

- When uncertain about an open question, surface it instead of inventing an answer.
- Prefer committing related documentation together with the code change.

## 6. Forbidden Actions (unless explicitly requested)

- Rewriting the entire project structure.
- Replacing Identity with a custom auth system.
- Adding heavy frameworks (MediatR, MassTransit, etc.) without a clear need.
- Changing the default admin credentials without updating SOURCE-OF-TRUTH and README.
