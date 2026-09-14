# ADR-0011 — V1 Editorial Catalogue Entry

## Status

Accepted

## Context

The community pathway into the Atlas remains Observation, Evidence, Editorial Review and publication. Version 1 also needs a controlled internal desktop workflow for an assigned Moderator to enter verified canonical catalogue records directly.

ADR-0003 rejected direct Moderator product editing. This ADR narrowly supersedes that decision for the internal v1 catalogue-entry capability only.

## Decision

An explicitly assigned Moderator may create or change canonical catalogue records through internal editorial tooling when protected by the server-side `PublishAtlas` policy.

The capability is not available to Explorers, Contributors, Community Trust, Verified Manufacturers, or Administrators solely by virtue of their administrative authority. It does not introduce another identity or role.

Each direct catalogue mutation must be atomic and retain immutable audit and provenance information: acting User, time, action, affected canonical identifiers, submitted or before/after payload, source references, rationale, and correlation identifier where available.

Community and manufacturer discoveries continue to use the normal Observation → Evidence → Editorial Review → Atlas pathway. This decision does not permit them to create canonical records directly.

## Consequences

- The initial catalogue can be maintained through accountable internal editorial work without building a general CMS.
- `PublishAtlas` remains an explicit capability rather than a client-side UI convention.
- Administrator and editorial responsibilities remain separate.
- Direct catalogue changes remain explainable even when they are not the result of an Observation.
- Production catalogue writes remain disabled until production authentication and authority configuration are available.
