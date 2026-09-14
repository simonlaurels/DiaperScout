# Data Foundation Plan

## Status

Proposed implementation plan derived from the architecture, specification,
community and implementation documents on 30 August 2026.

## Sources of truth reviewed

The plan follows `architecture/domain-model.md`, `database-model.md`,
`entity-reference.md`, `knowledge-architecture.md`, `workflow-architecture.md`,
`editorial-architecture.md`, `api-architecture.md`, `authentication-and-roles.md`,
`media-and-evidence.md`, `discovery-task-system.md`; the product specification
documents; the implementation architecture, data-access, technology-stack,
configuration and testing documents; and the community and world documentation.

## Model

The first migration establishes six connected areas:

1. **Atlas reference data**: Manufacturer, Brand, Product, ProductVariant,
   SizeVariant, PackType and ProductIdentifier. Product facts are kept at their
   documented level. A product is not identified by a barcode.
2. **Retail reference data**: Country, Retailer and Location. There is no
   permanent Product-to-Location stock table.
3. **Community**: User, ExplorerProfile, Backpack and saved product/location
   entries. Contributor remains derived from submitted observations.
4. **Observation and evidence**: Observation records a time-bound report and is
   immutable after submission; EvidenceItem records provenance and metadata for
   supporting material. Media binaries remain outside PostgreSQL.
5. **Editorial/provenance**: EditorialDecision records a moderator decision
   against an observation. It does not let an observation update canonical data
   directly.
6. **Discovery**: KnowledgeGap and DiscoveryTask preserve the documented gap to
   evidence workflow. A task can be linked to observations, never directly to an
   Atlas mutation.

Stable/reference data are manufacturers, brands, product hierarchy, countries,
retailers and locations. Community-generated data are users, explorer profiles,
backpacks, observations, evidence and saved entries. Editorial and discovery
records are workflow/provenance data.

## Important invariants

* A Product has ProductVariants; a ProductVariant has SizeVariants; a
  SizeVariant has PackTypes; identifiers belong to PackTypes.
* Pack GTINs are globally unique when populated.
* A Location belongs to one Retailer and one Country.
* An Observation must identify a subject through either a known Product or a
  CandidateProductName; it must always have an author and observation time.
* Retail availability is derived from retail observations, never persisted as
  current stock.
* Submitted observation content cannot be changed through the aggregate API;
  workflow state and editorial decisions are separate.
* Historical/provenance records restrict deletion. User erasure is an
  anonymisation workflow, not cascade deletion.

## Intentional initial scope

The initial database establishes the complete persistent shape and constraints;
it does not implement authentication, media upload, editorial UI, Atlas
projection, full-text/geospatial search or automatic discovery-task generation.
Those require separate, documented application workflows.

## Reconciliation required

Older community/world documents describe a Scout, Scout Callsign and Scout Pins.
The newer architecture, API and solution-structure documents expressly prohibit
a Scout identity, role, endpoint or identifier. This implementation follows the
newer User -> Explorer model and does not create Scout persistence. If callsigns
or pins return, they need an explicit architecture decision defining them as
Explorer presentation/recognition data rather than a role or identity.

`Brand` is called a text product attribute in `product-attributes.md` but a
first-class reference/Atlas resource in the architecture and database model.
The latter is followed: Brand is a reference entity linked to Manufacturer and
Product; Product retains no duplicative brand text.
