# Authentication & Roles

## Purpose

This document describes how DiaperScout identifies contributors, assigns responsibility and protects the integrity of the Atlas.

Authentication establishes identity.

Roles define responsibility.

Permissions exist to safeguard the Atlas rather than to create hierarchy.

The architecture encourages stewardship by allowing contributors to earn greater responsibility through sustained, positive participation.

---

# Philosophy

DiaperScout is a community of explorers and custodians.

Every contributor can improve the Atlas.

Not every contributor should have the same responsibilities.

Greater responsibility is earned through demonstrated stewardship rather than automatically granted.

The objective is to create a trustworthy community capable of maintaining the Atlas for many years.

---

# Identity

Authentication answers one question:

> **Who is this contributor?**

Authentication should provide:

- secure sign-in;
- secure session management;
- account recovery;
- identity verification where appropriate.

Authentication should remain independent from editorial and business logic.

---

# Responsibility

Authorisation answers a different question:

> **What responsibilities does this contributor have?**

Permissions should always reflect responsibility rather than status.

Every permission should exist because it protects the Atlas or enables meaningful contribution.

---

# Community Roles

## Visitor

Visitors may:

- browse the Atlas;
- search Products;
- explore Retailers;
- scan recognised Products.

Visitors cannot submit observations or participate in community workflows.

---

## Scout

Scouts contribute evidence to the Atlas.

Responsibilities include:

- discovering Products;
- recording Retail observations;
- submitting correction requests;
- completing Scout Tasks.

Scouts improve the Atlas through evidence.

They do not modify canonical knowledge directly.

---

## Trusted Scout

Trusted Scouts have demonstrated consistent, high-quality contribution.

Recognition is based upon stewardship rather than popularity.

Trusted Scouts may receive:

- increased editorial confidence;
- recommendations for moderator promotion;
- additional community responsibilities.

Trusted Scout is recognition of trust, not authority.

---

## Moderator

Moderators perform editorial responsibilities on behalf of the community.

Responsibilities include:

- reviewing observations;
- evaluating evidence;
- publishing canonical knowledge;
- maintaining editorial consistency;
- protecting the integrity of the Atlas.

Moderators remain accountable to the editorial architecture.

They do not bypass it.

---

## Administrator

Administrators maintain the DiaperScout platform.

Responsibilities include:

- infrastructure;
- deployments;
- platform security;
- configuration;
- operational monitoring;
- moderator management;
- verified manufacturer management.

Administrators are custodians of the platform.

They are not automatically custodians of editorial knowledge.

Operational responsibility should remain separate from editorial responsibility wherever practical.

---

## Verified Manufacturer

Verified Manufacturers contribute official observations.

Examples include:

- Product Specifications;
- official imagery;
- packaging changes;
- regional information.

Verification establishes identity.

It does not grant editorial authority.

Manufacturer observations enter the same editorial workflow as every other observation.

---

# Trust

Trust reflects the long-term quality of a contributor's participation.

Trust should increase through:

- accepted observations;
- accurate discoveries;
- successful Scout Tasks;
- consistently valuable contribution.

Trust should decrease through:

- rejected observations;
- malicious behaviour;
- repeated low-quality submissions;
- abuse of community systems.

Trust should reward stewardship rather than activity alone.

---

# Recommendation

The architecture may recommend contributors for additional responsibility.

Recommendations may consider:

- trust;
- quality of evidence;
- consistency;
- editorial judgement;
- long-term contribution.

Recommendations support human decision making.

They never replace it.

---

# Suspension

Where necessary, contributor accounts may be suspended.

Suspension exists to protect the Atlas from:

- malicious activity;
- repeated abuse;
- persistent low-quality contribution;
- deliberate misinformation.

Suspension should be proportionate.

Where appropriate, contributors should have the opportunity to rebuild trust over time.

The objective is protecting the Atlas rather than punishing contributors.

---

# Principle of Least Privilege

Contributors should possess only the permissions required to fulfil their responsibilities.

Additional permissions should be granted deliberately.

Limiting privilege:

- reduces accidental mistakes;
- improves security;
- reinforces architectural boundaries.

---

# Separation of Responsibility

Operational authority and editorial authority should remain separate.

Administrators maintain the platform.

Moderators curate the Atlas.

Verified Manufacturers provide authoritative observations.

Scouts explore the real world.

Each responsibility contributes to the Atlas without replacing another.

---

# Architectural Consequences

This model results in several important characteristics.

- Responsibility is earned through contribution.
- Editorial authority remains independent from operational authority.
- Manufacturer verification establishes identity rather than privilege.
- Trust reflects stewardship rather than popularity.
- The Atlas is protected through responsibility rather than restriction.

---

# Evolution

As the DiaperScout community grows, new responsibilities may emerge.

Future roles should extend this model rather than replace it.

The architecture should continue to encourage exploration, stewardship and transparency.

The objective is not simply to manage permissions.

The objective is to cultivate a community capable of preserving a trustworthy Atlas.

---

# Relationship to Other Documents

This document defines how DiaperScout establishes identity and responsibility.

Related documents describe the architecture from complementary perspectives.

- **Editorial Architecture** explains how editorial responsibilities are exercised.
- **Scout Task System** describes how Scouts contribute to maintaining the Atlas.
- **Workflow Architecture** explains how contributors interact with the system.
- **Backend Services** defines the services responsible for authentication and authorisation.

Together these documents describe how DiaperScout protects the Atlas through earned responsibility, clear boundaries and community stewardship.