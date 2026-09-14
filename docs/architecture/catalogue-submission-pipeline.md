# Catalogue Submission Pipeline

**Status:** Accepted design for v1.0  
**Scope:** DiaperScout catalogue and editorial workflow  
**Related architecture:** Canonical catalogue, editorial authority, product provenance

## 1. Purpose

DiaperScout v1.0 is intentionally focused on one core proposition:

> **DiaperScout is the best online catalogue of diapers.**

The initial product should provide a high-quality, trustworthy online catalogue of diaper products, with useful product information and legitimate destinations where visitors can buy those products.

To support that goal, catalogue entries must be researched, verified, and editorially approved before becoming part of the canonical catalogue.

The catalogue workflow must also support future expansion to public Explorer and manufacturer submissions without requiring a separate workflow.

This document defines that workflow.

## 2. v1.0 Scope

The v1.0 website focuses on:

- Product catalogue
- Product discovery and browsing
- Product search
- Product detail pages
- Retailer destinations
- Direct and affiliate outbound links
- Editorial catalogue management
- Product research and verification
- Catalogue provenance

The following are explicitly outside the v1.0 scope:

- Atlas
- Community observations
- Community submissions
- Location/sighting functionality
- Backpack
- User settings
- PWA functionality
- Barcode scanning
- Native mobile applications
- Sophisticated recommendation systems

Future versions may introduce these capabilities without changing the fundamental catalogue workflow defined here.

## 3. Submission Is Not a Canonical Product

A **Catalogue Submission** represents a proposed addition or change to the canonical catalogue.

A submission must not directly become a canonical `Product`.

The distinction is intentional:

    Catalogue Submission
            |
            | verification and editorial review
            v
    Canonical Catalogue

The canonical catalogue represents information that DiaperScout has chosen to publish.

A submission represents information that is being researched, verified, or reviewed.

This separation ensures that unverified community, manufacturer, or internal discoveries cannot directly alter canonical catalogue data.

## 4. Submission Sources

The pipeline must support multiple submission sources.

### v1.0

Moderator-created submissions are the primary entry mechanism.

### Future

The same pipeline may accept submissions from:

- Explorers
- Manufacturers
- Other trusted sources
- Automated discovery/import processes

The submission source must not require a different verification or publication workflow.

The source of a submission should be retained as provenance.

## 5. Submission Lifecycle

The initial workflow uses a deliberately small set of explicit states.

    Draft
      |
      v
    In Verification
      |
      v
    Ready for Review
      |
      v
    Approved
      |
      v
    Published

### Rejection and revision

A submission may be rejected:

    Ready for Review
            |
            v
        Rejected

A submission may also be returned for additional work:

    Ready for Review
            |
            v
      Needs Changes
            |
            v
      In Verification

### Future supersession

A published catalogue entry may eventually be superseded where a later canonical record replaces or materially corrects it.

This state should only be introduced when required by the implementation.

## 6. Verification Areas

Verification is divided into five primary areas.

### 6.1 Product Identity

Establish what the product actually is.

Typical checks include:

- Manufacturer
- Brand
- Product
- Product variant
- GTIN
- SKU
- Product identity/source

Where possible, product identity should be confirmed against authoritative sources.

### 6.2 Specifications

Verify the factual product attributes that DiaperScout presents.

Typical checks include:

- Size
- Manufacturer size
- Waist dimensions
- Pack quantity
- Packaging type
- Backing material
- Product type
- Other known specifications

Not every product will have every attribute available.

Unknown information must remain unknown rather than being guessed.

A missing optional specification does not automatically prevent publication.

### 6.3 Content and Rights

Establish what DiaperScout may legitimately display.

Typical checks include:

- Product description source
- Product description rights
- Product image source
- Product image permission
- Logo/brand asset permission where relevant
- Attribution requirements

DiaperScout should prefer original descriptions written from verified facts rather than unnecessarily reproducing manufacturer marketing copy.

Product imagery and other copyrighted assets must not be assumed to be reusable merely because they are publicly accessible.

### 6.4 Retail

Establish where the product can actually be purchased online.

Typical checks include:

- Retailer identified
- Product listing exists
- Product URL verified
- Product/variant corresponds to the destination
- Destination remains usable

A product may have multiple retailer listings.

A legitimate direct retailer destination remains valid even when no affiliate relationship exists.

### 6.5 Affiliate

Establish whether a retailer relationship can provide referral revenue.

Affiliate setup is deliberately treated as a commercial opportunity rather than a publication requirement.

Possible affiliate states include:

- Not investigated
- No affiliate programme
- Programme available
- Application required
- Application submitted
- Approved
- Configured
- Not applicable

Where an affiliate relationship exists, product-level destination/deep-link configuration should still be verified.

## 7. Reusable Verification Context

Not all verification work belongs to an individual product.

Some checks are reusable across an entire manufacturer, brand, or retailer relationship.

This is particularly important as the catalogue grows.

### 7.1 Manufacturer and Brand-Level Context

The following may apply across multiple products:

- Image usage permission
- Product photography permission
- Description/content permissions
- Logo/brand asset permissions
- Attribution requirements
- Manufacturer contact/permission records
- General product-data sources
- Brand guidelines

For example, if a manufacturer grants DiaperScout permission to use its product photography across its catalogue, a new product from that manufacturer should be able to inherit that permission.

The individual product may still record an exception where a particular asset has different terms.

### 7.2 Retailer-Level Context

The following may apply across multiple product listings:

- Affiliate programme
- Affiliate network
- Tracking configuration
- Deep-link mechanism
- General affiliate terms
- Affiliate relationship status
- Date last verified

Once a retailer's affiliate relationship is established, it should not need to be re-researched for every individual product.

Individual product destinations still require verification.

## 8. Inherited Verification

Verification should distinguish between direct product verification and reusable verification inherited from another entity.

Useful states include:

### Verified

The specific product or relationship has been explicitly checked and confirmed.

### Inherited

The requirement is satisfied by an applicable manufacturer, brand, or retailer-level verification.

### Exception

A general inherited policy exists, but the specific product or asset has different requirements and requires individual treatment.

For example:

    Product image rights
        Inherited from manufacturer permission

or:

    Product image rights
        Exception — specific image has separate licensing terms

This avoids repeating the same administrative work for every product while preserving the ability to handle exceptions safely.

## 9. Provenance

Meaningful verification should retain provenance.

A verification record should be capable of recording information such as:

- Verification type
- Verification status
- Scope
- Source
- Source URL or reference
- Person who performed the verification
- Verification timestamp
- Notes
- Relevant permission/licence terms

Examples:

    GTIN
    Source: Manufacturer product page
    Verified by: Moderator
    Verified: 2026-09-12

    Image rights
    Source: Manufacturer permission email
    Scope: Current product catalogue photography
    Permission: Granted
    Verified by: Moderator
    Verified: 2026-09-12

This information should remain available for future catalogue maintenance and audit.

## 10. Publication Requirements

Publication should require the information necessary to make a trustworthy catalogue entry, but should not require every possible piece of data.

### Normally required

- Product identity verified
- Manufacturer verified
- Brand/product/variant verified
- Core specifications verified where available
- Content and image rights resolved
- At least one legitimate product destination
- Editorial approval

### Not necessarily required

- GTIN, where no reliable GTIN exists
- Waist measurement, where the manufacturer does not publish one
- Every possible product specification
- Multiple retailers
- Product photography
- Affiliate relationship

The catalogue should prefer accurate incomplete information over inaccurate completeness.

## 11. Affiliate Policy

Affiliate status must never determine whether a product or retailer is considered useful.

DiaperScout should follow this principle:

> **Usefulness determines the destination; affiliate status is metadata.**

If a retailer is the best or only useful destination but has no affiliate programme, it should still be listed.

Similarly, a retailer should not be ranked above a more useful destination solely because it provides a higher commission.

Affiliate configuration should therefore enhance a useful retailer relationship rather than distort catalogue recommendations.

## 12. Editorial Review

Before a submission creates or changes canonical catalogue data, it must undergo editorial review.

The reviewer should be able to see:

- Submitted product information
- Verification status
- Verification provenance
- Manufacturer/brand-level inherited permissions
- Retailer-level affiliate status
- Product-specific exceptions
- Outstanding unknown information
- Editorial notes
- Submission source

The reviewer then decides whether the submission is ready to become canonical catalogue data.

## 13. Publishing

Publishing is the point at which verified submission data becomes canonical catalogue data.

The operation should:

1. Confirm the submission is eligible for publication.
2. Apply the approved canonical catalogue changes atomically.
3. Record the editorial decision.
4. Record catalogue audit/provenance.
5. Mark the submission as `Published`.

A failed publication must not leave the canonical catalogue partially updated.

## 14. Future Public Submissions

Future public submissions must enter the same pipeline.

For example:

    Explorer
       |
       v
    Product Submission
       |
       v
    Candidate
       |
       v
    Verification
       |
       v
    Editorial Review
       |
       v
    Canonical Product

The submitter's claims must not be treated as canonical facts simply because they were submitted.

Public submissions provide discovery information and potential evidence.

Editorial verification remains responsible for canonical catalogue data.

The same principle applies to manufacturer submissions.

## 15. No Generic Workflow Engine

The catalogue pipeline should be implemented directly using explicit domain/application concepts.

Do not introduce a generic workflow engine or generic workflow framework for v1.0.

The workflow is currently well understood and narrow enough to model directly.

Avoid introducing abstractions such as:

- Generic workflow definitions
- Generic workflow instances
- Generic workflow transitions
- Generic workflow rules

If future DiaperScout functionality demonstrates a genuine need for multiple independent workflows, this decision can be revisited.

## 16. Relationship to Editorial Authority

Canonical catalogue publication is an editorial responsibility.

The existing `Moderator` authority and `PublishAtlas` capability model remain the basis for authorised canonical catalogue changes.

For v1.0:

- Moderator authority is assigned to an existing `User`.
- Administrator authority does not automatically grant catalogue publishing authority.
- Catalogue changes must be performed through Application-layer commands/services.
- API endpoints must not expose EF entities directly.
- Catalogue changes must retain immutable audit/provenance records.

This pipeline does not create a new `Editor`, `Catalogue Manager`, or `Scout` identity or role.

## 17. v1.0 Implementation Direction

The implementation should proceed incrementally:

1. Catalogue submission model and lifecycle
2. Verification/provenance model
3. Reusable manufacturer/brand verification context
4. Reusable retailer/affiliate context
5. Application-layer submission services
6. Moderator catalogue workflow
7. Public product catalogue
8. Retailer/product destinations
9. Catalogue population and research
10. Catalogue quality assurance
11. Production deployment

The first v1.0 implementation should prioritise the underlying pipeline rather than building a simplistic standalone "Add Product" form.

The eventual desktop workflow should guide the moderator through the pipeline and clearly show which checks are:

- Complete
- Inherited
- Outstanding
- Optional/unknown
- Exceptions

## 18. Design Principle

The catalogue should be built around one central principle:

> **Research first. Verify. Record where the information came from. Resolve rights. Establish useful buying destinations. Configure referral where possible. Review. Publish.**

This process is intended to make DiaperScout not merely a large catalogue, but a **trusted and useful catalogue**.

The long-term goal remains to expand from the online catalogue into the wider DiaperScout ecosystem.

For v1.0, however:

> **Build the best online catalogue of diapers first.**