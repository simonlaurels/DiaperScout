# DiaperScout Launch Baseline

**Baseline date:** 1 October 2026  
**Recorded:** 2 October 2026  
**Experiment period:** 1 October 2026 – 30 September 2027  
**Status:** Immutable launch snapshot

## Purpose

This document records the starting position of DiaperScout at the beginning of its one-year commercial-validation experiment.

It is a historical baseline, not a living status document. Figures in this document should not be updated as DiaperScout changes. Future measurements should be recorded as separate dated reviews and compared with this baseline.

DiaperScout entered the experiment with substantial product functionality already implemented, but effectively no external adoption, no revenue, and some known production reliability work still outstanding.

---

## 1. Catalogue

At baseline, the canonical catalogue contains:

- **2 current products**
- **7 product variants**
- **11 displayed size entries**
- **2 products retail-matching ready**
- **0 products blocked from retail matching**
- **2 catalogue warnings**
- **206 submissions in the Submission Queue**

Both catalogue warnings are for **missing primary images**.

### Published/current products

| Product | Brand | Manufacturer | Variants | Sizes | Status |
| --- | --- | --- | ---: | ---: | --- |
| MEGAMAX | NorthShore | NorthShore | 6 | 7 | Current |
| Original | Crinklz | Thrust Vector | 1 | 4 | Current |

The six MEGAMAX variants are:

- Black
- Blue
- Pink
- Purple
- Tie-Dye
- White

Crinklz Original has one meaningful product variant.

### Submission pipeline

The Submission Queue contains **206 submissions**.

These provide an initial pool of catalogue candidates and should not be interpreted as 206 published products.

---

## 2. Retail and commerce

At baseline, DiaperScout contains:

- **1 canonical retailer**
- **1 verified retailer**
- **0 discovered retailers**
- **0 retailers needing review**

The sole retailer is **NRU**, verified and last updated on 1 October 2026.

The initial manually tested retail listing connects:

**NorthShore MEGAMAX → Black → Medium → 10-piece Bag → NRU**

This establishes the initial end-to-end canonical pack → retailer listing → Where to Buy workflow.

### Commerce architecture

The commerce plugin architecture is operational and separates:

- retail discovery;
- pricing;
- availability;
- affiliate resolution.

Awin support has been extracted into a provider plugin.

At baseline, this architecture is primarily enabling infrastructure rather than a mature automated retail network. Manual canonical retailer/listing management is operational.

---

## 3. Accounts and registration

At baseline:

- **2 accounts exist**
- **2 accounts are active**
- **1 Administrator account**
- **1 Explorer account**
- **0 known external users**
- **New public registrations are closed**

Both accounts were created on 27 September 2026 and belong to the founder.

The external-user count is therefore derived from the observed account list rather than a separate analytics metric.

DiaperScout begins the experiment with effectively **zero registered public adoption**.

---

## 4. Traffic baseline

Cloudflare analytics were exported on 2 October 2026.

**Measurement window:** 2 September 2026 – 1 October 2026

Recorded totals:

- **11,991 requests**
- **47,948,499 bytes served** (~47.95 MB / ~45.7 MiB)
- **5,559,239 bytes cached** (~5.56 MB)
- **1,428 summed daily unique visitors**

The `1,428` figure is the sum of Cloudflare's daily unique-visitor counts and **must not be interpreted as 1,428 distinct monthly users**, because the same visitor may be counted on multiple days.

The traffic period primarily covers development, testing, automated/background traffic and pre-launch activity. It should not be treated as evidence of meaningful public adoption.

On 1 October 2026, the exported dataset records **1 request and 1 daily unique visitor**.

Accordingly, meaningful public traffic/adoption at baseline is considered effectively zero.

The raw Cloudflare exports should be retained alongside or separately from this baseline for future reference.

---

## 5. Revenue and commercial position

At baseline:

- **Revenue:** £0
- **Affiliate revenue:** £0
- **Paying customers:** 0
- **Established manufacturer relationships:** 0
- **Verified canonical retailers:** 1

DiaperScout is intentionally operating as a low-cost commercial-validation experiment. Significant expenditure and premature business infrastructure are being avoided until there is evidence of genuine demand or commercial viability.

### Manufacturer outreach

Initial manufacturer outreach has begun.

Thrust Vector GmbH was contacted regarding Crinklz on 30 September 2026, requesting permission to use official product/packaging imagery and manufacturer-supplied descriptions/specifications.

At baseline, a response is pending and no manufacturer relationship or permission should be considered established.

---

## 6. Azure infrastructure and cost

The launch-period Azure Cost Management snapshot recorded:

- **Actual cost:** £11.18
- **Displayed forecast:** £12.27

The snapshot covers approximately 2 September – 1 October 2026 and is **not a steady-state monthly cost figure**.

The largest recorded service costs were:

| Service | Cost |
| --- | ---: |
| NAT Gateway | £3.60 |
| Azure Database for PostgreSQL | £2.39 |
| Load Balancer | £2.18 |
| Azure Container Apps | £1.40 |
| Virtual Network | £0.95 |

A substantial portion of the measurement period predates the final infrastructure cleanup.

The legacy NAT Gateway and its public IP have since been removed after dependency testing demonstrated that fixed outbound IP was not required.

Consequently, this cost snapshot should be treated as the historical launch-period baseline rather than a prediction of future steady-state Azure spend.

### Production architecture

At baseline, production uses:

- Azure Container Apps
- Azure Database for PostgreSQL Flexible Server
- Azure Container Registry
- Azure Private Link for PostgreSQL
- Azure Private DNS
- Azure Blob Storage for shared Data Protection keys
- Azure Key Vault for Data Protection key wrapping
- Cloudflare R2 for object/image storage
- Resend for transactional email

PostgreSQL connections use `Ssl Mode=VerifyFull`.

The production database is accessed over Private Link while retaining the canonical PostgreSQL hostname for TLS hostname verification.

API scale-to-zero remains enabled.

Legacy Container Apps resources and the obsolete NAT infrastructure have been removed.

---

## 7. Product capabilities at baseline

DiaperScout is already substantially functional at the beginning of the experiment.

Implemented capabilities include:

- public product catalogue and search;
- product variants;
- size variants;
- canonical pack/sellable-item identity;
- GTIN/barcode identity;
- Product Details;
- Where to Buy;
- canonical retailer management;
- canonical retail listings;
- retailer verification;
- commerce plugin architecture;
- affiliate-resolution architecture;
- catalogue submission and moderation;
- existing-product management;
- passwordless authentication/passkeys;
- installable PWA;
- branded standalone startup experience;
- camera barcode scanning;
- manual GTIN entry;
- known-barcode exact-pack resolution;
- unknown-product public contribution workflow;
- authenticated physical-product observations;
- physical commercial locations;
- optional observed shelf pricing;
- Atlas map and structured location fallback;
- exact product/variant/size/pack context through the physical discovery journey.

The PWA mobile navigation at baseline is:

**Explore → Products → Scan → Atlas → Backpack**

Backpack is not yet implemented as a substantive feature.

---

## 8. Physical-world workflow

The intended physical workflow at baseline is:

**Scan physical barcode → resolve exact canonical pack → record where it was found → authenticate if necessary → select/create physical shop → optionally record price → save observation → appear in Atlas**

For an unknown GTIN:

**Scan → unknown product → Add Product → Product → Pack → Review → authenticate → submit → moderation queue**

An unknown barcode does not automatically create canonical catalogue data.

Moderators can resolve a submitted GTIN against an existing canonical pack where appropriate, avoiding duplicate products.

Observations are evidence that a contributor saw an exact product at a particular public commercial location and time. They are not permanent inventory or stock claims.

---

## 9. Production quality and testing

Immediately following implementation of the Scan / Observation / Atlas workflow, the automated suite recorded:

- **255 tests passed**
- **0 failed**
- **0 skipped**
- **Release build: 0 warnings, 0 errors**

Production smoke testing covered public pages, responsive layouts and the newly deployed functionality.

However, automated success did not mean physical-device acceptance was complete.

---

## 10. Known launch-day defects and limitations

### P0 — Blazor multi-replica / SignalR reliability

The principal known production defect at baseline is intermittent failure of the Blazor InteractiveServer connection/circuit when multiple Web replicas are active.

Production evidence has included:

- WebSocket transport failure;
- `/_blazor` HTTP 404;
- LongPolling reporting `No Connection with that ID`;
- `Circuit host not initialized`;
- multiple healthy Web replicas;
- no session affinity/sticky-session configuration observed.

Four ready Web replicas were observed during production validation.

Shared Data Protection has already been implemented using Azure Blob Storage and Key Vault and is functioning correctly. Shared cryptographic keys do not, by themselves, make in-memory SignalR connection/circuit state portable between replicas.

The Container Apps scaling and routing configuration requires further investigation.

### Physical iPhone field test

The first genuine physical-iPhone field test occurred on 1 October 2026.

The installed PWA:

1. intermittently launched to a black screen instead of the branded startup experience;
2. eventually became usable;
3. successfully used the real phone camera to scan a physical barcode;
4. entered the unknown-product contribution journey;
5. subsequently lost the Blazor circuit and displayed the red reload/error interface before the contribution could be completed.

This means real-world barcode scanning was proven, but the complete physical Scan → Contribution/Observation → Atlas journey had **not yet passed physical-device acceptance** at baseline.

The SignalR/multi-replica problem and black-startup symptom are therefore launch-priority reliability work.

### Other known limitations

At baseline, DiaperScout intentionally does not provide:

- public product-photo contribution;
- automatic geocoding;
- automatic GPS location publication;
- offline contribution queues;
- global POI imports;
- permanent inventory/stock claims;
- observation editing;
- social features;
- ratings;
- badges;
- routing;
- recommendations.

These are deferred capabilities rather than launch defects.

---

## 11. Brand and product state

DiaperScout begins the experiment with an established visual/product identity centred around exploration and discovery.

The Guide is the primary human character and visual helper.

The product intentionally avoids adopting organised-Scouting terminology throughout the interface. DiaperScout is the product name; users and ordinary actions do not need to become "scouts" or "scouting".

A revised PWA/app icon based on a side-profile illustration of the Guide looking through binoculars was selected around launch. The icon uses a simplified map background and location pin and is intended as the v1 application icon.

---

## 12. Interpretation of the baseline

DiaperScout does **not** begin this experiment as an idea or prototype.

It begins with a functioning production platform, canonical catalogue model, moderation system, authentication, retail architecture, PWA, physical barcode scanning, contribution workflows and an Atlas.

However, it also begins with:

- effectively zero external users;
- £0 revenue;
- £0 affiliate revenue;
- two published/current products;
- one verified retailer;
- no established manufacturer relationships;
- public registration closed;
- a large candidate/submission backlog;
- an unresolved production reliability issue affecting physical field use.

This distinction matters.

The experiment is therefore primarily testing whether the existing technical/product foundation can be converted into:

1. a useful catalogue;
2. genuine external usage;
3. retailer/manufacturer participation;
4. repeat physical-world contributions;
5. meaningful Where to Buy usage;
6. affiliate/commercial activity;
7. enough value or traction to justify continued investment.

---

## 13. Future comparison points

Future reviews should be recorded separately rather than modifying this baseline.

Useful comparison metrics include:

- canonical products;
- meaningful variants;
- canonical packs;
- submission backlog;
- catalogue image/data completeness;
- verified retailers;
- active retail listings;
- external registered users;
- active contributors;
- observations;
- Atlas locations;
- scans;
- known vs unknown barcode resolution;
- public traffic;
- Where to Buy usage;
- affiliate clicks;
- affiliate revenue;
- total revenue;
- manufacturer relationships/permissions;
- Azure operating cost;
- production reliability;
- significant capabilities shipped.

Suggested checkpoints:

- **1 November 2026 — Month 1**
- **1 January 2027 — Quarter 1**
- **1 April 2027 — Half-year**
- **1 July 2027 — Month 9**
- **30 September 2027 — Final experiment review**

---

## Baseline summary

| Metric | 1 October 2026 |
| --- | ---: |
| Current canonical products | 2 |
| Product variants | 7 |
| Displayed size entries | 11 |
| Retail-matching-ready products | 2 |
| Catalogue warnings | 2 |
| Submission queue | 206 |
| Canonical retailers | 1 |
| Verified retailers | 1 |
| Registered accounts | 2 |
| Known external accounts | 0 |
| Public registration | Closed |
| Established manufacturer relationships | 0 |
| Revenue | £0 |
| Affiliate revenue | £0 |
| Paying customers | 0 |
| 30-day Cloudflare requests | 11,991 |
| 30-day data served | ~47.95 MB |
| 30-day data cached | ~5.56 MB |
| Launch-period Azure actual cost | £11.18 |
| Physical end-to-end acceptance | Not yet passed |
| Automated tests after Scan/Observation/Atlas | 255 passed / 0 failed / 0 skipped |
| Release build | 0 warnings / 0 errors |

---

**Baseline frozen:** 1 October 2026  
**Experiment ends:** 30 September 2027