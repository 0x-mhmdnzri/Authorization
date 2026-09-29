# PONYTAIL

**P**ersistent **O**perational **N**otes & **Y**ield **T**racking for **A**gent **L**earning

A lightweight protocol for keeping multi-agent (and human) work continuous and low-friction on this repository.

## Purpose

- Capture the “why” and the current mental model so the next agent does not re-discover everything.
- Record yields (what worked, what failed, what was learned) after non-trivial tasks.
- Keep a short, living tail of recent context.

## Location of State

- Long-term decisions → `DECISIONS.md`
- Current truth → `SOURCE-OF-TRUTH.md` + `PROJECT-MEMORY.md`
- Open items → `OPEN-QUESTIONS.md`
- Short-term session notes → (optional) create `PONYTAIL-NOTES.md` only when a long multi-step task is in progress; delete or archive when done.

## Protocol Steps

1. **Start of task**  
   Read the core memory files. If a `PONYTAIL-NOTES.md` exists, read it too.

2. **During task**  
   If the task spans multiple messages or is complex, append short bullet points to `PONYTAIL-NOTES.md` (goal, files touched, blockers).

3. **End of task**  
   - Update the permanent knowledge files.  
   - Add a short yield entry (see template below) if something non-obvious was learned.  
   - Remove or archive temporary notes.

## Yield Entry Template (append to DECISIONS or a yields section)

```markdown
### Yield – YYYY-MM-DD – <short title>
- **Tried**: …
- **Worked**: …
- **Failed / Surprising**: …
- **Carry forward**: …
```

## Rules

- Keep notes extremely short.
- Never let temporary notes become the source of truth; promote durable knowledge into the proper files.
- Prefer updating existing memory files over creating new ones.
