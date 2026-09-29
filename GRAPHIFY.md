# GRAPHIFY

Graph-oriented view of the authorization domain to help agents reason about relationships.

## Current Concrete Graph (RBAC)

```
(User) ──assigned──► (Role) ──implies──► (Permission*)
   │                    │
   │                    └── ParentRole (hierarchy, not yet enforced)
   │
   ├── Department (attribute)
   └── ClearanceLevel (attribute)

* Permissions are currently implicit in [Authorize(Roles)] attributes.
```

## Planned Graphs

### ABAC

```
(Subject attributes) + (Resource attributes) + (Environment) + (Action)
        │
        ▼
   Policy evaluation ──► Allow / Deny
```

### ReBAC (Zanzibar-style)

```
(User) ──member──► (Group) ──editor──► (Document)
(User) ──owner───► (Document)
(Folder) ──parent──► (Document)   // inheritance
```

Relation tuple shape (planned):

```
(object_type:object_id)#relation@subject_type:subject_id
```

### MAC

```
(User.ClearanceLevel)  ────── compares ──────  (Resource.Classification)
         │                                              │
         └──────── compartments / labels ───────────────┘
```

## How Agents Should Use This

- When designing a new model, first sketch the graph here (or in a PR description).
- Prefer explicit relation names that match industry vocabulary (owner, editor, viewer, member, parent, …).
- Keep the graph and the eventual database schema in sync.
