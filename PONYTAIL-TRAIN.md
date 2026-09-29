# PONYTAIL-TRAIN

How to keep the PONYTAIL protocol itself useful and up-to-date.

## When to Update This Protocol

- After a painful multi-agent hand-off (something was lost).
- When a new class of task repeatedly needs the same missing context.
- When the human owner requests a change in agent behavior.

## Training Checklist

1. Identify the friction point (missing fact, wrong assumption, forgotten update).
2. Decide whether the fix belongs in:
   - SOURCE-OF-TRUTH / PROJECT-MEMORY (fact),
   - TASK-PROTOCOL or AI-TRAINING (behavior),
   - or PONYTAIL itself (process).
3. Make the smallest clarifying change.
4. Optionally add a Yield entry describing the improvement.

## Anti-Patterns

- Turning PONYTAIL into a full project-management system.
- Writing long narrative logs.
- Duplicating information that already lives in DECISIONS or DOMAIN-MEMORY.

## Current Training Focus

- Ensure every structural code change is accompanied by a knowledge-base update.
- Keep OPEN-QUESTIONS honest; do not silently decide them.
