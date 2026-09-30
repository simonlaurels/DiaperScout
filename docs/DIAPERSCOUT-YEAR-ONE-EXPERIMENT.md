# DiaperScout Year One Experiment

**Experiment period:** 1 October 2026–30 September 2027

**Purpose:** Operating strategy and planning inventory

**Planning status:** KPI scorecard, measurement definitions and initial October roadmap to follow

## The question we are testing

Can DiaperScout become a genuinely useful, community-powered Explorer's Guide that at minimum sustains its operating costs and demonstrates credible potential for profitability, without imposing meaningful financial strain on its founder?

Year One is an opportunity to gather evidence through real use, useful contributions and properly measured commercial activity. It is not a commitment to complete every feature in this document. The annual endpoint is a review and decision point, not an automatic shutdown date or a binary profitability test.

Commercial validation should begin as soon as the first properly tracked revenue mechanism is live, while the Guide continues improving. Spending most of the year building and testing monetisation only at the end would leave too little evidence to answer the question fairly. Record when the product and each revenue mechanism actually enter the market so that results can be interpreted against their real exposure time.

## Identity and relationship to existing decisions

DiaperScout remains a community-powered explorer's guide to continence and absorbent products. Discovery Before Commerce, accuracy, dignity and trust govern how sustainability is pursued. Useful products and destinations deserve inclusion regardless of their affiliate status; commission must not displace usefulness in presentation or ranking.

This document connects the existing product direction to the Year One experiment. It does not replace the repository's authority order of World → Specification → Architecture → Code, or redefine implementation details maintained elsewhere.

The following references establish the boundaries for planning:

- [Project philosophy](world/01_Project_Philosophy.md), [core principles](world/02_Core_Principles.md), [canon](world/05_Canon.md) and [terminology](world/04_Terminology.md) define the Guide's identity. People are **Explorers**; contributing Explorers may be described as **Contributors**. Scout is not a user role or account type.
- [Product model](spec/product-model.md) and [data model principles](spec/data-model-principles.md) distinguish Product → Product Variant → Size Variant from Pack/Retail information and Community Observations.
- [Atlas First](architecture/decisions/ADR-0001%20-%20Atlas%20First.md) makes trustworthy knowledge the primary asset. The Atlas as a knowledge foundation is broader than the future Atlas map interface.
- [Catalogue submission pipeline](architecture/catalogue-submission-pipeline.md) establishes the v1 launch focus: verified catalogue entries, discovery, search, retailer destinations and editorial stewardship. Atlas map functionality, community submissions and observations, Backpack, PWA, scanning and native apps remain later capabilities; inclusion below does not make them v1 launch requirements.
- [V1 editorial catalogue entry](architecture/decisions/ADR-0011%20-%20V1%20Editorial%20Catalogue%20Entry.md) governs the narrowly authorised internal publishing capability and its audit/provenance requirements. Public contributions continue through verification and editorial review. Detailed authority belongs in the current ADR and [authorisation strategy](implementation/authorization.md), rather than being duplicated here.
- [Application structure and navigation](experience/UX-04-application-structure-and-navigation.md) and [community contributors](community/contributors.md) guide Explore, Products, Atlas, Backpack and Scan, anonymous exploration, meaningful recognition and private internal trust.

## Two ambitions

### Pathway to Profitability / sustainability

Establish a useful product, understand whether people discover and return to it, test credible revenue mechanisms, and learn whether income can cover deliberately lean operating costs. Profitability is an ambition to investigate, not an assumed result or a reason to compromise the Guide's independence.

### The Full Vision

Develop a distinctive Guide that people can use in the world: discover products, document local encounters, contribute trustworthy knowledge and carry a personal history of exploration. The mobile experience, community and personality are central to that destination.

> **The Full Vision is the destination; Pathway to Profitability earns the right to build it.**

This principle guides commitments of money and effort. It does not postpone every differentiating feature until profitability. Mix useful identity and community work into the experiment so that people encounter something worth returning to and contributing to. Some work earns its place through differentiation or delight rather than a direct revenue metric.

## Planning inventory

This inventory groups work by purpose. It is not an ordered delivery schedule, a promise that everything ships in Year One, or a replacement for existing specifications. Product Manager and Retail Manager are existing foundations to evolve, not new projects to restart.

### Pathway to Profitability

| Area | Work to consider |
| --- | --- |
| Business foundations | Establish a business bank account; research business registration and act when justified or required; undertake a trademark search and consider eventual protection when justified. Account suitability, legal requirements, costs and timing remain research and decision work. |
| Manual retailer flow | Complete and validate **Add business → Add listing → Where to Buy**, evolving Retail Manager and its relationship to Product Manager. Verify that destinations correspond to the intended product/variant and remain useful. Direct, non-affiliate destinations remain valid. |
| Retailer/plugin architecture | Design an incremental retailer/plugin approach supporting Where to Buy listings, stock information and affiliate integration. Preserve separation between product facts, retail offers and local observations; avoid building an extensive integration framework before a concrete need is demonstrated. |
| Affiliate research and integrations | Research programme eligibility, relevant retailers, terms, attribution, reporting, costs, data access and permitted content before choosing implementation order. An Amazon plugin followed by eBay is a possibility, conditional on research, not a committed sequence or an assertion of programme access. |
| Attribution and reporting | Implement and validate affiliate click tracking and programme reporting so that commercial results can be reconciled and interpreted. Do not assume every outbound retailer click is an affiliate click or a purchase. |
| Analytics and discoverability | Establish the compact KPI instrumentation, improve SEO and useful catalogue entry discoverability, and use observed searches and acquisition sources to guide improvements. |
| Launch and relationships | Launch useful slices, undertake community outreach, and build relationships with retailers and manufacturers. Obtain and retain permission/provenance for images, descriptions and other assets where needed; public availability is not permission to reuse content. |
| Cost discipline | Monitor operating costs, establish affordable budget guardrails and review new recurring commitments before adopting services or APIs. |

Stock supplied by a retailer integration must remain distinguishable from an Explorer's dated observation. A local sighting is evidence of an encounter, not a guarantee of current availability. Affiliate status must remain metadata about a useful destination, consistent with the catalogue pipeline's affiliate policy.

### Full Vision

| Area | Work to consider |
| --- | --- |
| Mobile-first Guide | Make core discovery and contribution journeys useful on mobile web, then evolve PWA capabilities where they support actual use. |
| Explore / welcome experience | Create a personality-rich home and welcome experience with community discoveries and useful contribution tasks. Preserve the Guide's friendly, respectful voice and the established Explore destination. |
| Barcode product discovery | Allow scanning to identify an existing product or propose a new one through the moderation/submission queue. A scan or imported lookup is evidence to verify, not automatic publication of canonical facts. |
| Local product observations | Support scanning and observations of packs found at local businesses and specific Locations, retaining relevant product/variant, place, time and provenance. |
| Atlas map | Present local observations and Changing Places toilets. Research appropriate sources, permissions, verification and freshness for facility information before implementation; do not imply guaranteed access or availability. |
| Community stewardship | Evolve contribution, reputation/internal trust, moderation, provenance, duplicate handling and correction processes. Recognition should value useful knowledge; internal trust must not become public scores, competitive ranks or automatic publishing authority. |
| Explorer identity and Backpack | Evolve discoveries, contribution history, saved work and useful tasks around an Explorer's journey. Preserve anonymous browsing and proportionate sign-in at the point needed for attribution or persistence; avoid making a public social profile the centre of the Guide. |
| Native iPhone/iOS app | Consider much later, after mobile web/PWA and actual usage justify the additional development and maintenance commitment. It is not a prerequisite for validating the product. |

### Foundations supporting both ambitions

- **Catalogue quality:** Run a verification, enrichment and approval campaign for imported candidates. Check identity, Product Variants, Size Variants and GTINs; resolve duplicates; add permitted images and useful destinations; retain source provenance; and retire invalid candidates. Missing facts should remain unknown. Discontinued but genuine products retain historical value and should not be discarded merely because they are no longer sold.
- **Contribution governance:** Make submission, evidence, review, correction and moderation responsibilities understandable and manageable. Extend the existing editorial pipeline rather than creating a parallel route that bypasses it. Keep internal catalogue work distinguishable from community demand and contribution evidence.
- **Production basics:** Maintain proportionate backups, recovery verification, health visibility and basic production operations in line with the existing [backup and recovery](implementation/backup-and-recovery.md), [observability](implementation/observability.md) and [operations](architecture/deployment-and-operations.md) guidance. This inventory does not claim those capabilities are all already implemented.
- **Privacy and measurement quality:** Collect only data genuinely useful to operating the product or evaluating the experiment. Exclude internal/test traffic where practical and keep known testing, bots and supportive activity from being mistaken for independent demand.
- **Lean infrastructure:** Keep recurring infrastructure deliberately small and understandable. New services need a concrete benefit proportionate to cost and maintenance effort.

## Measurement philosophy

Define a small Year One scorecard next. The candidate areas below are a selection pool, not a requirement to create dozens of metrics or dashboards. Establish reliable baselines before setting evidence-based targets; there are no arbitrary growth targets in this strategy.

| Candidate area | Possible measures |
| --- | --- |
| Reach and return | Unique visitors, product views/searches, acquisition source and returning visitors. |
| Useful discovery | Search-to-product engagement, Where-to-Buy usage, outbound retailer/affiliate clicks and searches with no useful result. |
| Community and stewardship | Registered Explorers, active contributors, submissions/observations, approval rate and moderation backlog. |
| Commercial sustainability | Monthly operating cost, affiliate clicks, attributable affiliate revenue, other revenue and cost coverage. |

Measure capabilities when they are live and instrumented. A deferred community feature does not produce meaningful evidence of zero community demand. Registration alone is not evidence of contribution, and clicks alone are not evidence of purchases or satisfaction. Use qualitative feedback about usefulness and uncertainty alongside the small quantitative scorecard.

The agreed cost-coverage relationship is:

```text
Cost coverage (%) = monthly DiaperScout revenue / monthly DiaperScout operating cost × 100
```

Track AI/development spend separately from recurring operating cost. Also track cumulative personal cash investment so that the real cost of the experiment remains visible. Product hosting sustainability and the affordability of discretionary development are separate questions; cost coverage alone cannot answer both.

Useful commercial measures may include click-through rate (CTR), attributed conversion rate, earnings per click and revenue per product view. Affiliate conversion means:

```text
Attributed conversion rate (%) = reported qualifying purchases / tracked affiliate clicks × 100
```

This is attributed conversion, not a complete picture of all purchases influenced by DiaperScout. Programme attribution windows, reporting delays, consent choices, blocked tracking, cross-device journeys and cancelled or disqualified purchases may affect results. Programme-specific definitions and access must be researched rather than assumed. Reconcile periods and eligibility rules before comparing programmes or interpreting trends.

The next scorecard task must settle event definitions, denominators, time windows, data sources, exclusions and reporting limitations. In particular, define what constitutes a useful search result, an active contributor and an approval rate; distinguish reported, approved and paid revenue; decide how costs are allocated to months; and represent unavailable data and zero-cost denominators explicitly rather than reporting misleading percentages. CTR needs an explicit eligible view/impression denominator, not an unspecified count of visits.

Internal clicks, test purchases, development activity, bots and friends clicking simply to help must not inflate the apparent commercial case. Exclude or label known activity where practical, acknowledge what cannot be separated, and avoid collecting invasive identifiers merely to make measurement appear exact.

## Financial guardrails

The experiment must fit within limited personal discretionary funds. Low recurring cost is a hard design constraint, and continued operation must not depend on a financially uncomfortable subsidy from Simon.

New paid infrastructure, services and APIs need a clear purpose, an understood recurring or usage-based cost, and an affordable limit before commitment. Budget guardrails should make it possible to reduce or pause discretionary work when circumstances change. This strategy does not invent a spending allowance or assume future revenue.

Revenue is evidence, not automatic permission to spend the same amount. Consider building a small project reserve before reinvestment, then make deliberate decisions about operating needs, resilience and growth. The reserve amount and reinvestment policy remain planning decisions. Review recurring product costs, discretionary AI/development spend and cumulative personal investment together without conflating them.

## How to operate the experiment

**Ship slices that generate evidence.** A trustworthy, useful increment can teach us something before the vision is complete. Manual retailer listings can validate utility and outbound interest before plugins. Mobile web can validate journeys before native apps. Published catalogue pages can build search visibility while the Atlas map remains unfinished. A tracked revenue mechanism can test commercial potential while community features develop.

**Connect work to a reason.** Where useful, ask: “Which KPI or evidence is this expected to move?” The answer may concern discovery, quality, contribution, sustainability or an uncertainty to resolve. Full Vision work can also be justified by identity, differentiation and delight without pretending every improvement has a direct measurable revenue effect.

**Keep the pace sustainable.** Set a realistic weekly time allowance alongside the money budget. Faster AI implementation should create room to choose better, finish useful slices and reclaim time; it should not expand scope until every available evening is consumed. Unfinished work may roll forward rather than turning every week into a deadline. Moderation, maintenance and recovery work also consume that allowance.

**Protect privacy and trust.** The product domain can be sensitive. Avoid unnecessary profiling, inferred personal circumstances or detailed behavioural histories. Analytics, affiliate tracking, local observations and public contribution history should each have a clear purpose, proportionate detail and retention, and appropriate access. Preserve anonymous exploration and avoid exposing sensitive location patterns. Privacy restraint applies even when collecting more data would be technically easy.

**Learn throughout the year.** Review evidence as usable data accumulates and adjust scope and spending accordingly. Record important changes and their reasons so that the annual review can distinguish product changes, data-quality changes and genuine shifts in use. Do not wait until September to discover that tracking was absent or a commercial hypothesis was never tested.

## The Year One review

On 30 September 2027, review usefulness, community evidence, commercial trajectory, costs, founder capacity and time actually spent in-market. Interpret results in light of the launch dates, catalogue quality, outreach, measurement reliability and the period during which revenue mechanisms were operational.

Crossing 100% cost coverage in one month does not by itself establish durable profitability. Being below it on the review date does not automatically make the experiment a failure. Look at trajectory and the quality of the evidence, including whether the product received a fair in-market test.

Possible outcomes, without ranking or a predetermined preference, are:

- Continue and invest further where evidence supports it.
- Continue cheaply where utility or community value exists but commercial evidence remains uncertain.
- Change direction based on observed use and what people find valuable.
- Wind down if a fair in-market test shows little demand and continued subsidy is not justified.

The review should produce an explicit decision and rationale. September is a decision point, not a guillotine; the calendar alone does not determine the Guide's future.

## Next planning step

Define the compact Year One KPI scorecard and its measurement definitions. Then use those KPIs and this inventory to create the initial October roadmap, taking account of dependencies, budget, available time and valuable differentiating work. The roadmap is the next planning task and is deliberately not specified here.
