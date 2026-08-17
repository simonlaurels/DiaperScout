# Authentication & Roles

## Purpose

This document describes how DiaperScout identifies users, assigns responsibility and protects the integrity of the Atlas.

Authentication establishes identity.

Roles define security boundaries.

Community Trust provides an internal signal about the reliability of community contributions.

Permissions exist to safeguard the Atlas and enable meaningful contribution rather than to create hierarchy.

---

# Philosophy

DiaperScout is a community of Explorers and custodians.

Every Explorer can improve the Atlas.

Not every user should have the same responsibilities or permissions.

Greater responsibility may be earned through demonstrated stewardship, but contribution history and Community Trust do not automatically grant privileged access.

The objective is to create a trustworthy community capable of maintaining the Atlas for many years.

---

# Identity

Authentication answers one question:

> **Who is this user?**

Authentication should provide:

* secure sign-in;
* secure session management;
* account recovery;
* identity verification where appropriate.

Authentication should remain independent from editorial and business logic.

The technical identity is represented by a **User**.

The user-facing identity is an **Explorer**.

These terms describe the same authenticated person from different architectural perspectives.

---

# Explorer

An Explorer is a person using DiaperScout.

An Explorer may:

* browse the Atlas;
* search Products;
* explore Locations;
* discover Products;
* save Products and Locations;
* maintain a Backpack;
* submit community contributions.

Being an Explorer does not imply that the person has contributed to DiaperScout.

Exploration is available without contribution.

---

# Contributor

A Contributor is an Explorer who submits information to DiaperScout.

Contributor is not a security role, account type or rank.

It describes participation.

A Contributor may submit:

* Observations;
* Evidence;
* correction requests;
* Product discoveries;
* Location information;
* other approved community contributions.

A User does not need a special permission role simply because they become a Contributor.

The system determines whether a particular action is permitted based on the capabilities and security boundaries required for that action.

---

# Roles

Roles represent genuine security boundaries.

A role should exist only when it grants permissions that cannot safely be represented through ordinary user participation or contribution history.

The initial privileged roles are:

* Moderator
* Administrator
* Verified Manufacturer

Additional roles should not be introduced without a clear security or operational requirement.

---

# Moderator

Moderators perform editorial responsibilities on behalf of the community.

Responsibilities include:

* reviewing Observations;
* evaluating Evidence;
* publishing canonical knowledge;
* maintaining editorial consistency;
* protecting the integrity of the Atlas;
* resolving appropriate editorial disputes.

Moderators remain accountable to the editorial architecture.

They do not bypass the editorial workflow simply because they possess moderation permissions.

Moderator access should be explicitly granted.

Community Trust may inform recommendations for moderation, but it does not automatically create a Moderator account.

---

# Administrator

Administrators maintain the DiaperScout platform.

Responsibilities include:

* infrastructure;
* deployments;
* platform security;
* configuration;
* operational monitoring;
* user and access management;
* moderator management;
* verified manufacturer management.

Administrators are custodians of the platform.

They are not automatically custodians of editorial knowledge.

Operational responsibility should remain separate from editorial responsibility wherever practical.

Administrator access should be tightly controlled and granted according to the principle of least privilege.

---

# Verified Manufacturer

A Verified Manufacturer is an authenticated organisation or representative whose manufacturer identity has been verified.

Verified Manufacturers may contribute official information such as:

* Product Specifications;
* official imagery;
* packaging changes;
* regional information;
* other manufacturer-sourced information.

Verification establishes identity.

It does not grant editorial authority.

Manufacturer contributions enter the same appropriate editorial workflow as community contributions.

A Verified Manufacturer should not be able to directly modify canonical knowledge solely because its identity has been verified.

---

# Community Trust

Community Trust is an internal signal representing an Explorer's demonstrated reliability as a Contributor.

Community Trust may consider:

* accepted Observations;
* accurate discoveries;
* quality of Evidence;
* successful resolution of Discovery Tasks;
* consistency;
* correction history;
* long-term contribution;
* malicious or abusive behaviour;
* repeated low-quality submissions.

Community Trust should reflect stewardship rather than activity volume.

---

# Trust Is Not a Role

Community Trust does not create a new account role.

There is no:

* Trusted Explorer role;
* Trusted Contributor role;
* trust rank;
* public trust level.

An Explorer with high Community Trust remains an Explorer and Contributor.

Their trust may influence internal workflows, but it does not automatically grant privileged permissions.

---

# Trust Is Not Authority

Community Trust does not allow an Explorer or Contributor to declare information authoritative.

A highly trusted Contributor may produce strong evidence.

That evidence must still pass through the appropriate editorial process.

The principle is:

> **Trust helps evaluate contribution. It does not replace evidence or editorial review.**

---

# Trust and Recommendations

Community Trust may be used to support recommendations for additional responsibility.

Recommendations may consider:

* Community Trust;
* quality of Evidence;
* consistency;
* editorial judgement;
* long-term contribution;
* demonstrated stewardship.

Recommendations support human decision making.

They never automatically grant privileged roles.

For example, an Explorer with a strong contribution history may be recommended for Moderator consideration.

The actual Moderator role must still be explicitly granted.

---

# Permissions

Permissions should reflect the minimum access required for a responsibility.

Examples include:

| Capability                               | Explorer | Moderator | Administrator | Verified Manufacturer |
| ---------------------------------------- | -------: | --------: | ------------: | --------------------: |
| Browse Atlas                             |        ✓ |         ✓ |             ✓ |                     ✓ |
| Search Products                          |        ✓ |         ✓ |             ✓ |                     ✓ |
| Save Products / Locations                |        ✓ |         ✓ |             ✓ |                     ✓ |
| Submit Observations                      |        ✓ |         ✓ |             ✓ |                     ✓ |
| Submit Evidence                          |        ✓ |         ✓ |             ✓ |                     ✓ |
| Review Evidence                          |        — |         ✓ |             — |                     — |
| Publish Canonical Knowledge              |        — |         ✓ |             — |                     — |
| Manage Editorial Decisions               |        — |         ✓ |             — |                     — |
| Manage Platform Configuration            |        — |         — |             ✓ |                     — |
| Manage User Access                       |        — |         — |             ✓ |                     — |
| Submit Official Manufacturer Information |        — |         — |             — |                     ✓ |

The exact permission set is defined by the authorisation implementation.

This table describes architectural intent rather than the final implementation mechanism.

---

# Principle of Least Privilege

Users and roles should possess only the permissions required to fulfil their responsibilities.

Additional permissions should be granted deliberately.

Limiting privilege:

* reduces accidental mistakes;
* improves security;
* reinforces architectural boundaries;
* limits the impact of compromised accounts.

Privileged roles should be separated wherever practical.

---

# Separation of Responsibility

Operational authority and editorial authority should remain separate.

Administrators maintain the platform.

Moderators curate the Atlas.

Verified Manufacturers provide authoritative source information.

Explorers discover and contribute information.

Contributors provide Evidence and Observations.

Community Trust helps the system assess contribution reliability.

Each responsibility contributes to the Atlas without replacing another.

---

# Editorial Authority

No community contribution should directly modify canonical knowledge.

The normal path is:

```text
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Atlas
```

The same principle applies to Verified Manufacturer contributions.

Verification establishes the source.

It does not bypass editorial review.

---

# Suspension

Where necessary, User accounts may be suspended.

Suspension exists to protect the Atlas and the wider community from:

* malicious activity;
* repeated abuse;
* persistent low-quality contribution;
* deliberate misinformation;
* attempts to manipulate Community Trust;
* other violations of DiaperScout's rules.

Suspension should be proportionate.

Where appropriate, contributors should have the opportunity to rebuild trust over time.

The objective is protecting the Atlas rather than punishing contributors.

---

# Privacy

Authentication and authorisation systems should minimise the personal information exposed to other users.

Public contribution should use the Explorer's chosen public identity.

Internal information such as:

* authentication details;
* Community Trust;
* moderation history;
* administrative information;

must not be exposed as public profile information unless explicitly required by another approved feature.

---

# Architectural Consequences

This model results in several important characteristics.

* `User` is the technical identity.
* `Explorer` is the canonical user-facing identity.
* `Contributor` describes contribution activity rather than a role.
* Community Trust is an internal signal rather than a permission level.
* Privileged roles represent genuine security boundaries.
* Editorial authority remains independent from operational authority.
* Manufacturer verification establishes identity rather than privilege.
* Trust reflects stewardship rather than popularity.
* The Atlas is protected through responsibility and evidence rather than arbitrary hierarchy.
* No `Scout` role or `Scout` identity exists within the production architecture.

---

# Evolution

As the DiaperScout community grows, new responsibilities may emerge.

Future roles should be introduced only where they represent a genuine security, editorial or operational boundary.

New responsibilities should not automatically become roles.

The architecture should continue to encourage:

* exploration;
* stewardship;
* transparency;
* evidence-based contribution;
* least privilege;
* separation of responsibility.

The objective is not simply to manage permissions.

The objective is to cultivate a community capable of preserving a trustworthy Atlas.

---

# Relationship to Other Documents

This document defines how DiaperScout establishes identity, responsibility and authorisation.

Related documents describe the architecture from complementary perspectives.

* **Editorial Architecture** explains how editorial responsibilities are exercised.
* **Discovery Task System** describes how Explorers may contribute to maintaining the Atlas through structured investigations.
* **Workflow Architecture** explains how users and contributors interact with the system.
* **Backend Services** defines the services responsible for authentication and authorisation.
* **Community Contributions** describes the community contribution model.
* **Terminology** defines the canonical language used throughout the project.

Together these documents describe how DiaperScout protects the Atlas through clear security boundaries, evidence, stewardship and community contribution.
