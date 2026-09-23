# Authorization Models: Full Implementation Scenarios

This document provides complete, practical scenarios for implementing the major access control / authorization models. Each section includes:

- Clear definition
- Core principles
- Real-world scenario
- Implementation approach (with pseudo-code / architecture)
- Strengths & weaknesses
- When to use it
- Hybrid notes (most production systems combine multiple models)

**Models covered:**
- DAC – Discretionary Access Control
- MAC – Mandatory Access Control
- RBAC – Role-Based Access Control
- RuBAC – Rule-Based Access Control
- ABAC – Attribute-Based Access Control
- PBAC – Policy-Based Access Control
- CBAC – Context-Based / Claims-Based Access Control
- ReBAC – Relationship-Based Access Control
- RAdAC – Risk-Adaptive Access Control
- PAC – Privilege Access Control (treated here as fine-grained privilege management, often overlapping with PBAC/ABAC)

> **Note:** PBAC appears twice in the original request; it is covered once. PAC is less standardized in literature and is treated as a privilege-centric approach that can sit on top of other models.

---

## 1. DAC – Discretionary Access Control

### Definition
The owner of a resource has full discretion to grant or revoke access to other users. Access decisions are decentralized and based on the identity of the subject (user/process).

### Core Principles
- Resource ownership is explicit.
- Owners can change Access Control Lists (ACLs) or permissions at any time.
- Classic examples: Unix/Linux file permissions (`chmod`, `chown`), Windows NTFS ACLs.

### Scenario: Shared Document Platform (like Google Drive / Dropbox)
A user uploads a file. By default only they can access it. They can later share it with specific users or groups with “view”, “comment”, or “edit” rights. They can also make it public or revoke access at any time.

### Implementation Outline

```text
Resource {
  id
  owner_id
  acl: List[{subject_id, permissions: Set[read|write|delete|share]}]
}

function canAccess(user, resource, action):
  if user.id == resource.owner_id:
    return true
  for entry in resource.acl:
    if entry.subject_id == user.id and action in entry.permissions:
      return true
  return false

function share(resource, target_user, permissions):
  assert caller is owner or has "share" permission
  update or add ACL entry
```

### Strengths
- Extremely flexible and intuitive for end users.
- Low administrative overhead for small systems.

### Weaknesses
- Hard to enforce organization-wide policies.
- Prone to privilege creep and accidental oversharing.
- Difficult to audit at scale (“who can still access this file?”).

### Best Used When
Personal file sharing, consumer applications, small teams where owners should control their own data.

---

## 2. MAC – Mandatory Access Control

### Definition
Access decisions are enforced by the system according to a central security policy. Neither the resource owner nor the user can override the policy. Subjects and objects are labeled with security clearances / classifications.

### Core Principles
- Labels (e.g., Top Secret, Secret, Confidential, Unclassified).
- Classic models: Bell-LaPadula (confidentiality – “no read up, no write down”), Biba (integrity).
- Used in military, intelligence, and highly regulated environments.

### Scenario: Classified Intelligence System
Every document is labeled with a classification level. Every user has a clearance level. A user with “Secret” clearance can never read a “Top Secret” document, even if the document owner tries to share it. Writing is also restricted to prevent information leakage.

### Implementation Outline

```text
Subject {
  clearance: Level   // e.g. 1=Unclassified ... 4=TopSecret
  compartments: Set[string]
}

Object {
  classification: Level
  compartments: Set[string]
}

function canRead(subject, object):
  return subject.clearance >= object.classification
         and object.compartments ⊆ subject.compartments

function canWrite(subject, object):
  return subject.clearance <= object.classification   // no write-down
         and subject.compartments ⊆ object.compartments
```

### Strengths
- Strongest protection against unauthorized disclosure.
- Policy is centrally enforced and cannot be bypassed by users.

### Weaknesses
- Very rigid; high administrative cost.
- Poor usability for collaborative / commercial systems.
- Difficult to adapt to dynamic business needs.

### Best Used When
Military, government classified systems, high-assurance environments where information flow control is critical.

---

## 3. RBAC – Role-Based Access Control

### Definition
Permissions are assigned to roles; users are assigned to roles. A user gains all permissions of the roles they hold.

### Core Principles
- Separation of users ↔ roles ↔ permissions.
- Supports hierarchy (role inheritance) and constraints (separation of duties).
- NIST RBAC model is the standard reference.

### Scenario: Hospital Information System
Roles: Doctor, Nurse, Receptionist, Lab Technician, Administrator.  
A Doctor role has permissions to view patient records, write prescriptions, and order tests. A Nurse can view records and update vitals but cannot prescribe. Users are assigned one or more roles based on their job function.

### Implementation Outline

```text
Role {
  name
  permissions: Set[Permission]
  parent_roles: List[Role]   // for hierarchy
}

User {
  roles: Set[Role]
}

function hasPermission(user, permission):
  for role in user.roles (including inherited):
    if permission in role.permissions:
      return true
  return false
```

### Strengths
- Easy to understand and administer.
- Scales well for organizations with stable job functions.
- Excellent auditability (“who has the Doctor role?”).

### Weaknesses
- Role explosion when fine-grained or contextual needs appear.
- Static – does not natively handle “only during business hours” or “only for patients in my department”.

### Best Used When
Enterprise applications with well-defined job functions, most SaaS products as the baseline model.

---

## 4. RuBAC – Rule-Based Access Control

### Definition
Access is granted or denied according to a set of predefined rules/conditions evaluated at request time. Rules are usually global or resource-scoped and independent of (or layered on top of) roles.

### Core Principles
- Rules are condition → effect (Allow/Deny).
- Common conditions: time of day, IP range, device type, day of week, previous actions.
- Often combined with RBAC (“Doctor role AND during working hours”).

### Scenario: Corporate VPN + File Share
Rule: “Allow access to the Finance share only between 08:00–18:00 on weekdays AND from corporate IP ranges OR company-managed devices.”

Even if a user has the Finance role, access is denied outside those conditions.

### Implementation Outline

```text
Rule {
  id
  condition: Expression   // e.g. time.between("08:00","18:00") AND ip.in(corporate_ranges)
  effect: Allow | Deny
  priority: int
}

function evaluate(request):
  matching = sort rules by priority that match request
  for rule in matching:
    if rule.effect == Deny:
      return Deny
  return Allow if any Allow rule matched else Deny
```

### Strengths
- Simple to express temporal, location, and environmental constraints.
- Complements RBAC very well.

### Weaknesses
- Can become a large unordered list of rules that is hard to reason about.
- Limited expressiveness compared to full ABAC/PBAC.

### Best Used When
You need time/location/device constraints on top of an existing RBAC system.

---

## 5. ABAC – Attribute-Based Access Control

### Definition
Access decisions are based on attributes of the subject, the resource, the action, and the environment. Policies are Boolean expressions over these attributes.

### Core Principles
- Attributes can be anything: department, clearance, sensitivity, time, location, device health, etc.
- Policies are usually written in a policy language (XACML, ALFA, Cedar, Rego, etc.).
- Highly dynamic and fine-grained.

### Scenario: Multi-tenant SaaS Analytics Platform
Policy examples:
- A user can view a dashboard if `user.department == dashboard.owner_department` AND `user.clearance >= dashboard.sensitivity` AND `environment.time` is within business hours AND `device.is_managed == true`.

### Implementation Outline (Policy Decision Point style)

```text
Request {
  subject: {id, department, clearance, roles, ...}
  resource: {id, owner_department, sensitivity, type, ...}
  action: string
  environment: {time, ip, device_posture, location, ...}
}

Policy: "permit if subject.department == resource.owner_department 
              and subject.clearance >= resource.sensitivity
              and environment.time.hour between 8 and 18"
```

Architecture typically follows the XACML reference:
- Policy Enforcement Point (PEP)
- Policy Decision Point (PDP)
- Policy Administration Point (PAP)
- Policy Information Point (PIP)

### Strengths
- Extremely flexible and fine-grained.
- Context-aware by design.
- Scales better than pure RBAC for complex requirements.

### Weaknesses
- Attribute sprawl and policy complexity.
- Harder to answer “who can currently access X?” (requires enumeration or simulation).
- Higher runtime cost if policies are complex.

### Best Used When
Cloud, multi-tenant, Zero Trust, or any environment where access depends on rich context.

---

## 6. PBAC – Policy-Based Access Control

### Definition
A centralized approach where all authorization logic is externalized into versioned, governable policies. PBAC can consume roles, attributes, relationships, and context. It is more of an architectural pattern than a pure decision model.

### Core Principles
- Policies live outside individual applications.
- Policy-as-code, versioning, testing, and audit trails.
- One Policy Decision Point can serve many services.

### Scenario: Large Bank / Enterprise Platform
All microservices call a central authorization service. Business policies such as:
- “Customer support agents can view account details only for customers in their region and only after MFA if the account balance > $50k”
- “Traders can execute orders only during market hours and only on instruments they are licensed for”

are written once and enforced everywhere.

### Implementation Outline
Same PDP architecture as ABAC, but with strong emphasis on:
- Central policy repository
- Policy versioning & approval workflows
- Simulation / “what-if” analysis
- Consistent decision logging for compliance

### Strengths
- Governance and consistency across a large estate.
- Business teams can often understand / author policies (especially with natural-language or domain-specific languages).
- Combines the best of RBAC + ABAC + ReBAC under one roof.

### Weaknesses
- Requires investment in a policy platform.
- Can become a single point of failure or bottleneck if not designed carefully.

### Best Used When
Large organizations, regulated industries, or any multi-application environment that needs centralized authorization governance.

---

## 7. CBAC – Context-Based / Claims-Based Access Control

### Definition
Two related meanings:
1. **Context-Based**: Access depends heavily on environmental / situational context (time, location, device, threat level).
2. **Claims-Based** (Microsoft terminology): Access is based on claims (signed statements) carried in a security token (SAML, JWT, etc.). Closely related to ABAC.

### Scenario: Zero-Trust Corporate Network
A user authenticates and receives a token containing claims: `role=engineer`, `department=platform`, `device_managed=true`, `location=office`.  
Access to internal tools is granted only if the claims satisfy the policy **and** the current context (e.g., risk score is low, time is business hours).

### Implementation Notes
Usually implemented as a special case or layer on top of ABAC/PBAC where the primary attributes come from the identity token (claims) and are enriched with real-time context.

### Strengths
- Natural fit with modern identity protocols (OIDC, SAML).
- Strong support in Microsoft ecosystems (Azure AD, Entra ID).

### Weaknesses
- Depends on the quality and freshness of claims.
- Context enrichment still requires additional Policy Information Points.

---

## 8. ReBAC – Relationship-Based Access Control

### Definition
Access is determined by the existence (and type) of relationships between the subject and the resource in a graph. Classic example: Google Zanzibar.

### Core Principles
- Model the world as a graph of objects and relation tuples.
- Permissions are defined as graph reachability or specific relation paths.
- Excellent for hierarchical and collaborative ownership.

### Scenario: Collaborative Document / Project Management Tool (Notion / Google Docs style)
- User A is the `owner` of Document X → full control.
- User A shares Document X with Group Y as `editor`.
- User B is a `member` of Group Y → User B can edit Document X.
- Folder hierarchy: if a user is `viewer` of a parent folder, they inherit `viewer` on children (unless overridden).

### Implementation Outline (Zanzibar-style)

```text
RelationTuple {
  object: "document:123"
  relation: "editor"
  subject: "user:alice" | "group:eng#member"
}

// Permission definitions (in a schema)
// document#viewer = owner + editor + viewer + parent#viewer
// document#editor = owner + editor

function check(user, relation, object):
  return exists path in the relation graph from user to object via the allowed relations
```

Popular open-source implementations: OpenFGA, SpiceDB, Ory Keto, Permify.

### Strengths
- Extremely natural for collaborative, hierarchical, and multi-tenant ownership models.
- Fine-grained per-object permissions without role explosion.
- Efficient at scale with proper indexing / caching.

### Weaknesses
- Graph maintenance and consistency.
- Less natural for pure environmental conditions (time, risk, device) – usually combined with ABAC/PBAC.

### Best Used When
Document collaboration, social networks, project management, multi-tenant SaaS with hierarchical resources.

---

## 9. RAdAC – Risk-Adaptive Access Control

### Definition
Access decisions dynamically adapt based on a real-time calculation of **security risk** versus **operational need**. Higher risk may still be allowed if operational need is critical (or vice-versa).

### Core Principles
- Continuously compute a risk score from multiple factors (user behavior, device health, location, threat intelligence, authentication strength, etc.).
- Compare against policy thresholds that can be overridden by mission/operational criticality.
- Originates from NSA / NIST work on adaptable access control.

### Scenario: Military / Emergency Response System
A field operative needs access to sensitive intelligence.  
Normal risk threshold would deny the request because the device is not fully trusted and the network is untrusted.  
However, the operational need is classified as “critical” (life-threatening situation). The RAdAC engine allows a limited, time-boxed, audited access window.

### Implementation Outline

```text
function decide(request):
  risk_score = compute_risk(
    user_trust,
    device_posture,
    network_quality,
    location_anomaly,
    threat_intel,
    auth_strength,
    history
  )
  operational_need = assess_need(request.mission_criticality, role, context)

  if risk_score <= normal_threshold:
    return Allow
  if operational_need >= critical_threshold and risk_score <= max_acceptable:
    return Allow_with_constraints (time-box, extra logging, limited actions)
  return Deny
```

### Strengths
- Closest model to real-world human decision making under uncertainty.
- Supports dynamic risk posture (Zero Trust continuous verification).

### Weaknesses
- Complex to design reliable risk and need scoring functions.
- Requires high-quality real-time telemetry and threat data.
- Harder to explain and audit decisions.

### Best Used When
High-stakes environments (defense, critical infrastructure, emergency services) or advanced Zero Trust implementations that already have strong UEBA / risk engines.

---

## 10. PAC – Privilege Access Control

### Definition
PAC is less standardized. In practice it usually means **fine-grained management of privileges** (the actual rights/actions) rather than just roles or attributes. It focuses on the privilege lifecycle: just-in-time elevation, privilege inventory, least privilege enforcement, and privilege analytics.

Often implemented as a layer on top of RBAC/ABAC/PBAC (Privileged Access Management – PAM systems).

### Scenario: Just-in-Time Privileged Access for Production Systems
Engineers do not have permanent admin rights on production Kubernetes clusters.  
When they need elevated privileges:
1. They request a specific privilege for a limited time and with a business justification.
2. The request is approved (or auto-approved under policy).
3. Temporary credentials / tokens are issued with the exact privileges needed.
4. All actions are recorded and the privilege is automatically revoked after the time window.

### Implementation Notes
- Privilege inventory and classification.
- Just-in-time (JIT) and just-enough-privilege (JEP) workflows.
- Session recording / proxying for highly privileged sessions.
- Integration with existing RBAC/ABAC engines for the actual decision.

### Strengths
- Dramatically reduces standing privileges (major attack surface reduction).
- Strong audit trail for privileged activity.

### Weaknesses
- Adds friction for legitimate privileged work.
- Requires mature identity and approval workflows.

### Best Used When
Any environment that has privileged accounts (admins, DBAs, cloud operators) – essentially every non-trivial organization.

---

## Comparison Matrix

| Model   | Decision Basis              | Granularity     | Flexibility | Admin Complexity | Best Fit                          |
|---------|-----------------------------|-----------------|-------------|------------------|-----------------------------------|
| DAC     | Owner discretion / ACL      | Per-object      | High        | Low              | Personal / small team sharing     |
| MAC     | System labels & policy      | Hierarchical    | Very Low    | High             | Military / classified systems     |
| RBAC    | Roles                       | Coarse–Medium   | Medium      | Low–Medium       | Stable job functions              |
| RuBAC   | Predefined rules/conditions | Medium          | Medium      | Medium           | Time / location constraints       |
| ABAC    | Attributes + context        | Fine            | High        | High             | Dynamic, multi-tenant, Zero Trust |
| PBAC    | Centralized policies        | Any             | High        | High             | Large / regulated enterprises     |
| CBAC    | Claims + context            | Fine            | High        | Medium–High      | Token-based / Microsoft ecosystems|
| ReBAC   | Relationships (graph)       | Per-object      | High        | Medium–High      | Collaboration & hierarchies       |
| RAdAC   | Risk vs Operational Need    | Dynamic         | Very High   | Very High        | High-stakes / adaptive systems    |
| PAC     | Privilege lifecycle         | Fine            | High        | High             | Privileged Access Management      |

---

## Recommended Hybrid Architecture (Modern Best Practice)

Most mature systems do **not** pick a single model. A practical layered approach:

1. **Baseline** – RBAC for coarse job-function permissions.
2. **Ownership & Sharing** – ReBAC (or DAC-style ACLs) for per-object collaborative access.
3. **Context & Fine-grained rules** – ABAC / RuBAC / CBAC for environmental conditions.
4. **Governance** – PBAC as the central policy layer that orchestrates the above.
5. **Adaptive / High-risk** – RAdAC on top for continuous risk evaluation.
6. **Privileged paths** – PAC / JIT elevation for admin-level actions.

This combination gives you the simplicity of roles, the flexibility of attributes and relationships, centralized governance, and adaptive risk handling.

---

## Next Steps / Implementation Roadmap

1. Start with a clear inventory of resources, subjects, and the real access questions your system must answer.
2. Choose a baseline (usually RBAC + ReBAC or RBAC + ABAC).
3. Select a policy language / engine (Cedar, OpenFGA, OPA/Rego, XACML, custom).
4. Externalize authorization early (don’t embed decisions deep in business logic).
5. Add observability: decision logs, “explain” APIs, and simulation tools.
6. Iterate toward more dynamic models (ABAC → RAdAC) only when the business need justifies the complexity.

---

*Document created for the Authorization repository. Feel free to extend each section with concrete code samples in your preferred language or policy language.*
