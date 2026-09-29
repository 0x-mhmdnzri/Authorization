# KNOWLEDGE-INDEX

Index of all knowledge files intended for AI agents. Read in this order when onboarding.

## Core Memory (must read first)

| File | Purpose |
|------|---------|
| [SOURCE-OF-TRUTH.md](SOURCE-OF-TRUTH.md) | Highest-priority canonical facts. Wins all conflicts. |
| [PROJECT-MEMORY.md](PROJECT-MEMORY.md) | Vision, current status, principles, next steps. |
| [ARCHITECTURE-MEMORY.md](ARCHITECTURE-MEMORY.md) | Layers, pipeline, design choices, extension points. |
| [DOMAIN-MEMORY.md](DOMAIN-MEMORY.md) | Domain entities, models mapping, endpoints, rules. |
| [DECISIONS.md](DECISIONS.md) | Architecture Decision Records. |
| [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) | Unresolved design questions. |

## Structural & Operational

| File | Purpose |
|------|---------|
| [layer-map.md](layer-map.md) | Precise mapping of folders → responsibilities. |
| [TOOLING.md](TOOLING.md) | Build, run, Docker, migrations, useful commands. |
| [IDE-SETUP.md](IDE-SETUP.md) | Recommended IDE / extensions / settings. |
| [STRUCTURED-LOGGING.md](STRUCTURED-LOGGING.md) | Logging strategy (current + recommended). |
| [PERFORMANCE-PLAYBOOK.md](PERFORMANCE-PLAYBOOK.md) | Performance notes and future checklist. |

## Agent Protocols

| File | Purpose |
|------|---------|
| [TASK-PROTOCOL.md](TASK-PROTOCOL.md) | How agents should approach any task in this repo. |
| [AI-TRAINING.md](AI-TRAINING.md) | Training principles and constraints for agents. |
| [PONYTAIL.md](PONYTAIL.md) | Persistent Operational Notes & Yield Tracking protocol. |
| [PONYTAIL-TRAIN.md](PONYTAIL-TRAIN.md) | How to train / update the PONYTAIL process. |
| [GRAPHIFY.md](GRAPHIFY.md) | Graph representation of authorization models & relations. |

## Theory & Specs

| File | Purpose |
|------|---------|
| [Authorization-Models-Scenarios.md](Authorization-Models-Scenarios.md) | Full theoretical scenarios for every authorization model. |
| [README.md](README.md) | Human-oriented project overview (keep in sync). |

## How to keep this index healthy

- When you create a new knowledge file, add a row here.
- When you change a core decision, update SOURCE-OF-TRUTH + DECISIONS + relevant memory files.
- Prefer short, factual, up-to-date documents over long historical essays.
