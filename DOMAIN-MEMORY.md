# DOMAIN-MEMORY

Domain knowledge for the Authorization project.

## Core Domain Concepts

### Subject (User)
- Represented by `ApplicationUser`.
- Identity properties: Id, Email, UserName, PasswordHash, ...
- Domain properties already present:
  - `FirstName`, `LastName`
  - `Department` (string) – useful for ABAC
  - `ClearanceLevel` (string) – useful for MAC / ABAC
  - `IsActive`
  - `CreatedAt`

### Role
- Represented by `ApplicationRole`.
- Extends Identity role with `Description`, `CreatedAt`, `ParentRoleId` (for hierarchy).
- Current seeded roles: `Admin`, `Manager`, `User`.

### Permission (not yet materialized)
- Currently implicit inside `[Authorize(Roles = "...")]` attributes.
- Future: explicit Permission entity + RolePermission mapping for richer RBAC / PBAC.

### Resource (not yet materialized)
- Will be needed for DAC, ReBAC, fine-grained ABAC.
- Planned shape roughly: Id, Type, OwnerId, Classification, Attributes...

### Relation (ReBAC)
- Planned: RelationTuple (object, relation, subject) – Zanzibar style.

### Policy
- Planned central concept for PBAC / ABAC evaluation.

## Authorization Models – Current Mapping

| Model | Status | How it is (or will be) realized |
|-------|--------|---------------------------------|
| **RBAC** | Implemented | Identity Roles + `[Authorize(Roles)]` + role claims in JWT |
| **ABAC** | Prepared | User attributes already exist; need policy engine |
| **MAC** | Prepared | ClearanceLevel on user; need classification on resources + Bell-LaPadula style checks |
| **ReBAC** | Planned | RelationTuple table + graph check API |
| **RuBAC** | Planned | Rules evaluated at runtime (time, IP, device) |
| **PBAC** | Planned | Central PDP that can consume roles + attributes + relations |
| **DAC** | Planned | Owner + ACL on resources |
| **RAdAC / PAC** | Future | Risk engine + JIT privilege |

## Current Endpoints (RBAC surface)

### Auth (`/api/auth`)
| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| POST | /register | Anonymous | Create user + default "User" role |
| POST | /login | Anonymous | Issue JWT |
| GET | /me | Bearer | Current user + roles + attributes |

### RBAC (`/api/rbac`)
| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| GET | /roles | Admin | List roles |
| POST | /roles | Admin | Create role |
| POST | /assign-role | Admin | Assign role to user |
| DELETE | /remove-role | Admin | Remove role from user |
| GET | /users/{id}/roles | Admin | Get roles of a user |
| GET | /admin-only | Admin | Demo protected endpoint |
| GET | /manager-area | Manager,Admin | Demo protected endpoint |
| GET | /user-area | Authenticated | Demo for any logged-in user |

## Domain Rules Currently Enforced

1. New users automatically receive the `User` role.
2. Only `Admin` can manage roles and assignments.
3. JWT contains both role claims and attribute claims (`department`, `clearance`).
4. Inactive users (`IsActive = false`) cannot log in.
5. Email is unique and used as username.

## Glossary

- **PEP** – Policy Enforcement Point (currently the `[Authorize]` attributes).
- **PDP** – Policy Decision Point (not yet extracted; logic is inside framework + future services).
- **PIP** – Policy Information Point (UserManager, future attribute stores).
- **PAP** – Policy Administration Point (future admin UI / endpoints for policies).
