# Authorization

.NET 10 Web API demonstrating multiple authorization models (RBAC, ABAC, MAC, ReBAC, RAdAC, etc.) using:

- ASP.NET Core Identity (custom tables based on Identity)
- EF Core + PostgreSQL
- JWT Authentication
- Fully Dockerized
- **Git Flow** strictly followed

## Git Flow Strategy

```
main          ← production-ready, tagged releases
  ↑
develop       ← integration branch
  ↑
feature/*     ← one feature branch per authorization model
```

### Branch naming convention
- `feature/01-rbac-core`
- `feature/02-abac`
- `feature/03-mac`
- `feature/04-dac`
- `feature/05-pbac-policy`
- `feature/06-pbac-purpose`
- `feature/07-radac`
- `feature/08-rebac`
- `feature/09-pac`
- `feature/10-cbac`
- `feature/11-rubac`

### Workflow for each model
1. `git checkout develop`
2. `git checkout -b feature/0X-model-name`
3. Implement + tests + migration
4. Commit with conventional commits (`feat(rbac): ...`, `fix(...)`, `docs(...)`)
5. Push & open PR → develop
6. After review/merge → delete feature branch
7. When ready for release: `git checkout main && git merge develop && git tag vX.Y.Z`

## Project Structure

```
Authorization/
├── docker-compose.yml
├── .gitignore
├── README.md
├── Authorization-Models-Scenarios.md
└── src/
    └── Authorization.API/
        ├── Controllers/
        │   ├── AuthController.cs
        │   └── RbacController.cs
        ├── Data/
        │   ├── ApplicationDbContext.cs
        │   └── Migrations/
        ├── DTOs/
        ├── Models/
        ├── Services/
        ├── Dockerfile
        ├── Program.cs
        └── appsettings.json
```

## Quick Start with Docker

```bash
docker compose up --build
```

- API: http://localhost:8080
- OpenAPI: http://localhost:8080/openapi/v1.json
- PostgreSQL: localhost:5432 (`auth_user` / `auth_password` / `authorization_db`)

### Default Admin
- Email: `admin@authorization.local`
- Password: `Admin123!`
- Role: `Admin`

## Implemented Models

### ✅ 1. RBAC – Role-Based Access Control (`feature/01-rbac-core`)

- Custom `ApplicationUser` + `ApplicationRole`
- Seeded roles: Admin, Manager, User
- JWT includes role claims
- Full CRUD for roles + assignment
- Protected endpoints with `[Authorize(Roles = "...")]`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/auth/register | Anonymous | Register + default User role |
| POST | /api/auth/login | Anonymous | Login → JWT |
| GET | /api/auth/me | Bearer | Current user + roles |
| GET | /api/rbac/roles | Admin | List roles |
| POST | /api/rbac/roles | Admin | Create role |
| POST | /api/rbac/assign-role | Admin | Assign role |
| DELETE | /api/rbac/remove-role | Admin | Remove role |
| GET | /api/rbac/admin-only | Admin | Demo protected |
| GET | /api/rbac/manager-area | Manager,Admin | Demo protected |


### ✅ 2. ABAC – Attribute-Based Access Control (`feature/02-abac`)

Decisions based on attributes of **Subject**, **Resource**, **Action** and **Environment**.

- New tables: `Resources`, `AbacPolicies`
- Policy engine evaluates: same department, business hours, clearance level, sensitivity
- Seeded sample policies and resources
- Finance demo user: `finance@authorization.local` / `Finance123!`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/abac/resources | Admin | Create resource |
| GET | /api/abac/resources | Bearer | List resources |
| POST | /api/abac/policies | Admin | Create ABAC policy |
| GET | /api/abac/policies | Admin | List policies |
| POST | /api/abac/evaluate | Bearer | Evaluate access (core ABAC) |
| GET | /api/abac/resources/{id}/content | Bearer | Protected content (403 if denied) |



### ✅ 3. MAC – Mandatory Access Control (`feature/03-mac`)

System-enforced access based on security labels (Bell-LaPadula model).

- **No Read Up**: subject clearance must be ≥ resource classification
- **No Write Down**: subject clearance must be ≤ resource classification
- Owners cannot override the policy (unlike DAC)

Uses existing `ClearanceLevel` (User) and `Sensitivity` (Resource).

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | /api/mac/levels | Anonymous | List security levels + ranks |
| GET | /api/mac/my-clearance | Bearer | Current user clearance |
| POST | /api/mac/evaluate | Bearer | Evaluate Read/Write decision |
| GET | /api/mac/resources/{id}/read | Bearer | Read under MAC (403 if denied) |
| POST | /api/mac/resources/{id}/write | Bearer | Write under MAC (403 if denied) |



### ✅ 4. DAC – Discretionary Access Control (`feature/04-dac`)

Resource **owner** decides who gets access (classic ACL model).

- Owner always has full control
- Owner (or user with Share) can grant/revoke permissions
- Permissions: Read, Write, Delete, Share (or Full)
- Optional expiration on grants

New table: `ResourcePermissions`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/dac/grant | Bearer | Grant access (owner/Share) |
| POST | /api/dac/revoke | Bearer | Revoke access (owner only) |
| GET | /api/dac/resources/{id}/acl | Bearer | List ACL (owner) |
| POST | /api/dac/evaluate | Bearer | Evaluate permission |
| GET | /api/dac/resources/{id}/content | Bearer | Access content under DAC |



### ✅ 5. PBAC – Policy-Based Access Control (`feature/05-pbac-policy`)

Central **Policy Decision Point**. Policies are managed centrally and can combine roles, attributes, context and DAC grants.

New table: `Policies`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/pbac/policies | Admin | Create central policy |
| GET | /api/pbac/policies | Admin | List policies |
| POST | /api/pbac/evaluate | Bearer | Central evaluate (PDP) |
| GET | /api/pbac/resources/{id}/content | Bearer | Content via PBAC |



### ✅ 6. PBAC-Purpose – Purpose-Based Access Control (`feature/06-pbac-purpose`)

Access is allowed only for an explicitly stated and permitted **purpose**.

Tables: `Purposes`, `ResourcePurposes`, `PurposeAccessLogs`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/purpose/purposes | Admin | Create purpose |
| GET | /api/purpose/purposes | Bearer | List purposes |
| POST | /api/purpose/assign | Admin | Assign purpose to resource |
| GET | /api/purpose/resources/{id}/purposes | Bearer | Purposes of a resource |
| POST | /api/purpose/evaluate | Bearer | Evaluate with purpose |
| GET | /api/purpose/resources/{id}/content?purpose=CODE | Bearer | Content (purpose required) |
| GET | /api/purpose/logs | Admin | Access logs |

Seeded purposes: TREATMENT, RESEARCH, BILLING, AUDIT



### ✅ 7. RAdAC – Risk-Adaptive Access Control (`feature/07-radac`)

Access adapts to **real-time risk** vs **operational need**.

- Risk factors: device, location, time, auth strength, behavior
- Decisions: Allow / AllowWithConstraints / Deny
- Tables: `RiskPolicies`, `RiskAssessmentLogs`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/radac/policies | Admin | Set risk thresholds |
| GET | /api/radac/policies | Admin | List policies |
| POST | /api/radac/evaluate | Bearer | Full risk evaluation |
| GET | /api/radac/resources/{id}/content | Bearer | Content under RAdAC |
| GET | /api/radac/logs | Admin | Assessment logs |



### ✅ 8. ReBAC – Relationship-Based Access Control (`feature/08-rebac`)

Zanzibar-style relation tuples. Access is determined by relationships.

Table: `RelationTuples`  
Format: `objectType:objectId#relation@subject`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/rebac/tuples | Bearer | Write relation tuple |
| DELETE | /api/rebac/tuples | Bearer | Delete tuple |
| GET | /api/rebac/tuples/{type}/{id} | Bearer | List tuples for object |
| POST | /api/rebac/check | Bearer | Check permission via relations |
| GET | /api/rebac/resources/{id}/content | Bearer | Content via ReBAC |
| POST | /api/rebac/share | Bearer | Share document with user |

Supports: direct user relations, group#member, parent inheritance for view.



### ✅ 9. PAC – Privileged Access Control (`feature/09-pac`)

Just-In-Time privileged elevation – no standing admin rights.

Tables: `PrivilegeDefinitions`, `PrivilegeElevationRequests`, `PrivilegedActionLogs`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/pac/privileges | Admin | Define privilege |
| GET | /api/pac/privileges | Bearer | List privileges |
| POST | /api/pac/request | Bearer | Request JIT elevation |
| POST | /api/pac/decide | Admin/Manager | Approve/Deny |
| GET | /api/pac/my-elevations | Bearer | My active elevations |
| GET | /api/pac/check/{code} | Bearer | Check active privilege |
| POST | /api/pac/execute | Bearer | Execute privileged action (logged) |
| POST | /api/pac/revoke/{id} | Bearer/Admin | Revoke elevation |
| GET | /api/pac/pending | Admin/Manager | Pending requests |
| GET | /api/pac/logs | Admin | Action audit log |

Seeded: PROD_DB_ADMIN, K8S_ADMIN, SECRET_READ



### ✅ 10. CBAC – Context-Based Access Control (`feature/10-cbac`)

Access depends on **environmental context**: time, network, device, location, auth strength.

Tables: `ContextPolicies`, `ContextEvaluationLogs`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/cbac/policies | Admin | Create context policy |
| GET | /api/cbac/policies | Admin | List policies |
| POST | /api/cbac/evaluate | Bearer | Evaluate with context |
| GET | /api/cbac/resources/{id}/content | Bearer | Content under CBAC |
| GET | /api/cbac/logs | Admin | Evaluation logs |

Conditions: business hours, days of week, trusted network, managed device, country allow/block, min auth method, anomalous location.


## Planned (one feature branch each)

- [x] ABAC (`feature/02-abac`)
- [x] MAC (`feature/03-mac`)
- [x] DAC (`feature/04-dac`)
- [x] PBAC-Policy (`feature/05-pbac-policy`)
- [x] PBAC-Purpose (`feature/06-pbac-purpose`)
- [x] RAdAC (`feature/07-radac`)
- [x] ReBAC (`feature/08-rebac`)
- [x] PAC (`feature/09-pac`)
- [x] CBAC (`feature/10-cbac`)
- [ ] RuBAC

## Local development

```bash
# Restore & migrate
dotnet restore src/Authorization.API
dotnet ef database update --project src/Authorization.API

# Run
dotnet run --project src/Authorization.API
```

## Notes

- Identity tables customized (`Users`, `Roles`, `UserRoles`, ...)
- Migrations auto-applied on startup
- Ready for Duende IdentityServer / OpenIddict integration later
