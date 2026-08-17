# Authorization

## Purpose

This document defines how DiaperScout implements authorisation within the production application.

Authentication establishes the identity of a User.

Authorisation determines what that authenticated User is permitted to do.

The implementation must reflect the architectural distinction between:

* User;
* Explorer;
* Contributor;
* Community Trust;
* Moderator;
* Administrator;
* Verified Manufacturer.

In particular:

> **Contributor and Community Trust are not authorisation roles.**

---

# Authorisation Principles

DiaperScout follows these principles:

* deny by default;
* least privilege;
* server-side enforcement;
* explicit privileged access;
* policy-based authorisation;
* separation of authentication from authorisation;
* separation of editorial and operational responsibilities;
* no privilege derived solely from contribution volume;
* no privilege derived automatically from Community Trust.

Client-side checks improve user experience but are never considered security controls.

---

# Identity

The authenticated technical identity is represented by the application `User`.

The user-facing identity is the Explorer.

Conceptually:

```text id="5ng8g7"
Authenticated User
       │
       ▼
    Explorer
       │
       └── may contribute
```

Authentication establishes the User identity before authorisation is evaluated.

---

# Authorisation Roles

The initial privileged roles are:

```text id="9n9az8"
Moderator
Administrator
VerifiedManufacturer
```

These represent genuine security boundaries.

No other role should be introduced without an explicit architectural reason.

---

# Explorer

Explorer is **not** an authorisation role.

An authenticated User is permitted to perform ordinary Explorer actions according to the application's policies.

Examples include:

* browse the Atlas;
* search Products;
* explore Locations;
* save Products;
* save Locations;
* maintain their Backpack;
* create Observations;
* submit Evidence.

These permissions should normally be available to authenticated Users without assigning an `Explorer` role claim.

---

# Contributor

Contributor is **not** an authorisation role.

A User becomes a Contributor through submitting information to DiaperScout.

The application may derive whether a User has contribution history from persisted data.

This must not result in a role such as:

```text id="h6g4e1"
Contributor
TrustedContributor
```

being assigned to the User.

Contribution history is domain data.

It is not an access-control mechanism.

---

# Community Trust

Community Trust is an internal community signal.

It must not be implemented as:

* a role;
* a permission;
* a claim granting privileged access;
* a public reputation score.

For example, the application must not implement:

```text id="w7x6fs"
CommunityTrust >= 100
        ↓
Moderator
```

or:

```text id="xckq9c"
TrustedContributor
        ↓
Editorial permissions
```

Community Trust may be consumed by application workflows where appropriate, but authorisation remains explicit.

---

# Moderator

The Moderator role represents editorial authority.

Moderators may:

* review Observations;
* review Evidence;
* request additional Evidence;
* record Editorial Decisions;
* manage editorial workflow;
* publish accepted knowledge where the workflow permits.

Moderator permissions must be enforced server-side.

A User must be explicitly granted the Moderator role.

Community Trust may support a recommendation for moderation but never grants the role automatically.

---

# Administrator

The Administrator role represents operational authority.

Administrators may:

* manage platform configuration;
* manage User access;
* manage privileged roles;
* manage operational settings;
* access administrative tooling;
* perform platform maintenance.

Administrator permissions must be tightly restricted.

Administrators should not automatically receive editorial authority.

Where an individual legitimately requires both responsibilities, both permissions should be explicitly granted.

---

# Verified Manufacturer

The Verified Manufacturer responsibility identifies an authenticated manufacturer or authorised representative whose identity has been verified.

This may be implemented through explicit account or organisation state rather than necessarily using a conventional user role.

Verified Manufacturer permissions may include:

* submit official Product information;
* submit official media;
* submit manufacturer documentation;
* provide official correction information.

Verified Manufacturer access does **not** permit:

* direct modification of canonical Atlas data;
* bypassing editorial review;
* approving its own submissions;
* modifying community observations.

---

# Policies

ASP.NET Core policy-based authorisation should be used for privileged capabilities.

Examples include:

```text id="j9gn9f"
RequireModerator
RequireAdministrator
RequireVerifiedManufacturer
```

Policies should represent capabilities rather than simply exposing role checks throughout application code.

For example:

```text id="8a9o5j"
[Authorize(Policy = Policies.ModerateObservations)]
```

is preferred to scattering direct role checks throughout endpoints and services.

---

# Policy Definitions

The initial policy set should include capabilities such as:

### ModerateObservations

Allows an authorised User to review community Observations.

### PublishAtlas

Allows an authorised User to approve publication of canonical knowledge.

### ManageUsers

Allows an authorised User to manage User access.

### ManagePlatform

Allows an authorised User to manage platform configuration.

### SubmitManufacturerInformation

Allows an authorised Verified Manufacturer to submit official manufacturer information.

Additional policies should be introduced when a genuine capability boundary exists.

---

# Policy Composition

Where a capability requires multiple conditions, policies may combine:

* authenticated identity;
* role;
* organisation verification;
* resource ownership;
* workflow state.

For example:

```text id="3r1d1p"
Authenticated User
       +
Verified Manufacturer
       +
Authorised Organisation
       ↓
SubmitManufacturerInformation
```

The exact implementation belongs in the Application/API layers.

---

# Resource-Based Authorization

Some operations depend on the specific resource being accessed.

Examples include:

* editing an Explorer's own Backpack;
* modifying a user's own draft Observation;
* accessing private media;
* performing an editorial action on a specific Observation.

Resource-based authorisation should be used where ownership or resource state matters.

Example conceptual flow:

```text id="y2y9km"
Request
  ↓
Authenticated User
  ↓
Load Resource
  ↓
Authorization Handler
  ↓
Allow / Deny
```

The resource must be loaded and checked server-side.

---

# Ownership

Users should normally be allowed to modify only resources they own.

Examples include:

* their Backpack;
* their private Collections;
* their draft Observations;
* their account settings.

Ownership should not be inferred from client-supplied identifiers alone.

The server must establish ownership from the authenticated identity.

---

# Editorial Authorization

Editorial operations require explicit Moderator authority.

Examples include:

* accepting an Observation;
* rejecting an Observation;
* requesting additional Evidence;
* publishing canonical information;
* recording an Editorial Decision.

A Contributor must not be able to approve their own submission.

A Verified Manufacturer must not be able to approve its own manufacturer submission.

---

# Self-Approval Prevention

The authorisation and editorial workflow must prevent self-approval.

For example:

```text id="0qg1l6"
Contributor
   ↓
Observation
   ↓
Moderator Review
   ↓
Editorial Decision
```

The system must detect and prevent inappropriate approval paths.

Where necessary, resource-level policies should consider the submitting User as well as the Moderator role.

---

# Administrator Separation

Administrative authority and editorial authority are separate.

An Administrator should not automatically be treated as a Moderator.

This separation reduces the risk of operational privileges being used to bypass editorial controls.

Where dual responsibility is required, the User must explicitly possess both capabilities.

---

# API Enforcement

Authorisation must be enforced at the API/Application boundary.

The API must never rely on:

* hidden UI elements;
* disabled buttons;
* client-side route protection;
* JavaScript checks;
* PWA state.

A malicious client must receive an appropriate authorisation failure even when it bypasses the normal user interface.

---

# UI Enforcement

The Web application should hide or disable unavailable actions where appropriate.

This improves usability.

For example:

```text id="4w2h5v"
Moderator
  → show editorial controls

Explorer
  → show contribution controls
```

However, UI enforcement is supplementary.

The server remains authoritative.

---

# Authentication Requirement

Protected policies require an authenticated User.

Anonymous visitors may access only explicitly public capabilities.

Examples of public capabilities may include:

* viewing published Products;
* viewing public Locations;
* searching public Atlas information.

The exact public/private boundary is defined by the relevant feature specification.

---

# Denial Behaviour

Unauthenticated requests should normally receive an authentication challenge where appropriate.

Authenticated Users without sufficient permission should receive an authorisation failure.

The application must not reveal sensitive information merely because a User lacks access.

Resource existence may itself be sensitive and should be considered when designing protected endpoints.

---

# Claims

Claims should contain only information necessary for authentication and authorisation.

Do not use claims as a general-purpose storage mechanism for:

* Community Trust;
* contribution counts;
* dynamic reputation;
* Product permissions;
* large domain objects.

Dynamic domain information should be retrieved from the appropriate application service.

---

# Role Assignment

Privileged roles must be explicitly assigned.

Role assignment should be restricted to appropriately authorised administrators.

The system must record:

* who granted the role;
* which role was granted;
* when it was granted;
* when it was removed where applicable.

Role changes are security-sensitive operations and should be auditable.

---

# Verified Manufacturer Assignment

Manufacturer verification should record sufficient provenance to establish:

* the organisation;
* the representative;
* verification status;
* verification date;
* responsible administrator or process;
* relevant verification evidence.

Verification state should not be confused with editorial approval.

---

# Revocation

Privileged access must be revocable.

Revocation applies to:

* Moderator;
* Administrator;
* Verified Manufacturer access.

Revocation should take effect promptly.

Existing sessions should not retain privileged access indefinitely after revocation.

---

# Session Security

Authorisation depends on the authenticated session being valid.

The application must:

* protect authentication cookies/tokens;
* enforce appropriate expiration;
* revoke or invalidate access when required;
* protect against session fixation;
* avoid storing sensitive authorisation state in client-controlled storage.

---

# Background Processing

Background services must not assume that the initiating User remains authorised.

A background process should operate using an explicit service identity or trusted internal mechanism.

Where an operation requires User-level authorisation, that decision should be made before the work is queued.

---

# Events

Events should not be treated as implicit authorisation.

For example:

```text id="wy6j9h"
ObservationAccepted
```

does not mean that every consumer may now modify the Product.

Each consuming service remains responsible for its own authority boundaries.

---

# Auditing

Security-sensitive authorisation events should be auditable.

Examples include:

* role granted;
* role revoked;
* manufacturer verification;
* administrative access;
* editorial permission changes;
* failed privileged operations where useful.

Audit records should contain enough information to investigate security incidents without unnecessarily storing personal information.

---

# Testing

Authorisation tests must verify both positive and negative cases.

Every privileged capability should test:

* anonymous User;
* authenticated ordinary Explorer;
* Contributor;
* Moderator;
* Administrator;
* Verified Manufacturer;
* inappropriate combinations of roles;
* resource ownership;
* self-approval prevention.

Tests should verify that bypassing the UI cannot bypass server-side security.

---

# Security Boundaries

The resulting security model is:

```text id="z74vki"
Anonymous
   │
   └── Public Atlas

Authenticated User
   │
   ├── Explorer capabilities
   ├── Personal Backpack
   └── Contributions

Moderator
   └── Editorial capabilities

Administrator
   └── Platform capabilities

Verified Manufacturer
   └── Official manufacturer submissions
```

Community Trust exists alongside this model:

```text id="svk9fs"
Contributor
     │
     ▼
Community Trust
     │
     └── Internal workflow signal
```

It does not create another permission tier.

---

# Anti-Patterns

The following are explicitly prohibited unless the architecture is formally changed.

## Scout Role

There is no `Scout` role.

## Trusted Explorer Role

There is no automatic trusted-user permission tier.

## Contributor Role

Contributor is not an authorisation role.

## Trust-Based Permissions

Community Trust must not directly grant permissions.

## Client-Only Authorization

Client-side checks are never sufficient.

## Administrator Equals Moderator

Administrative access does not automatically grant editorial authority.

## Manufacturer Equals Editor

Manufacturer verification does not grant editorial authority.

## Self Approval

Users must not approve their own contributions.

## Hidden Security Through UI

Removing a button is not authorisation.

---

# Relationship to Other Documents

This document defines the implementation of authorisation.

Related documents include:

* **Authentication & Roles** — defines identity and responsibility boundaries.
* **Domain Model** — defines the underlying domain concepts.
* **API Architecture** — defines how protected capabilities are exposed.
* **Backend Services** — defines service ownership.
* **Workflow Architecture** — defines workflow transitions.
* **Security** — defines broader application security requirements.
* **Testing Strategy** — defines testing requirements.

Together these documents establish a secure, explicit and least-privilege authorisation model for DiaperScout.
