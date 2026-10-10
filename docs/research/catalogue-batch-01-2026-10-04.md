# DiaperScout catalogue research — batch 01

Research date: **4 October 2026**. Repository inspected at `ce30d408e48403dda493ca5eafc785bc0b70b20b`.

**Research and staging only. Twenty discovery candidates considered: four READY, eight NEEDS RESEARCH and eight CONFLICT.** Two existing products were also checked and excluded from the new-product shortlist. No catalogue records, production data, images, application code or domain model were changed. No import or deployment is authorised by this report.

READY applies only to the explicitly selected sellable pack below, not every size or pack in its range. It means sufficient product/pack evidence for a reviewed catalogue proposal. Image rights remain flagged; unpublished-record duplicate checks and separate import approval are still required. The companion [staging JSON](catalogue-batch-01-2026-10-04.json) is a research manifest, not an import payload.

## 1. Methodology and provenance boundary

[DiapStash's catalogue](https://diapstash.com/catalog/) was used to discover names only. Brand searches identified TENA, MoliCare, ABENA, Seni, Attends and NorthShore leads. No descriptions, images, specifications, identifiers, taxonomy, lifecycle tags or structured catalogue data from DiapStash form the evidence for any proposed fact here.

Independent research followed product → manufacturer specifications → exact size/count → identifier → retailer/market corroboration → revision/lifecycle → existing DiaperScout coverage. Searches combined brand/product, size, pack count, EAN/barcode, known GTIN and country. Manufacturer documents were read as documents, not accepted from search snippets. ABENA packaging tables and HARTMANN's bag/carton table were also rendered and visually checked.

Source observations below distinguish **opened page**, **browser**, **downloaded PDF**, and **indexed lead only**. An opened web-tool page can be a cached representation; its crawl date is not a publication date. Prices and stock are observations, not promises. A search result that could not be opened cannot establish a proposed canonical fact. Blocked retailer pages were not bypassed. Authority searches produced ABENA AccessGUDID leads, but individual device pages were not successfully verified; no GS1 licence-owner or GUDID claim is used to make an entry READY.

GTIN strings retain leading zeroes. Check-digit validation uses the same alternating 3/1 rule and allowed 8/12/13/14-digit lengths as `RetailGtin.Normalise`. Passing a checksum establishes syntax, not ownership or pack identity. Every promoted identifier needs a source explicitly tying it to the exact sellable unit. Manufacturer article numbers, PZN, retailer SKU and ASIN remain separate identifiers.

## 2. Existing architecture and proposed mapping

The current implementation in `src/DiaperScout.Domain/Model.cs` represents the business node as **Manufacturer**, followed by **Brand**, **Product**, **ProductVariant**, **SizeVariant**, **PackType**, and **ProductIdentifier**. `Product.Family` is nullable text, not a separate family entity. Product types include `Tape`, `PullUp`, `AllInOne` and others; pack types are `Bag`, `Box`, `Case`.

Size records support the manufacturer's printed designation, separate waist/hip centimetres, measurement basis, and manufacturer-stated absorbency with method/source. Keep `M4`, `M2`, `M3` as printed designations when researching ABENA. Never turn a drops rating into millilitres. Inch measurements require the existing conversion workflow and preservation of original source/basis, rather than invented manufacturer centimetres.

Retail offers belong to the existing Retailer/RetailerProductListing architecture and its verification process. A commercial bundle does not create a manufacturer variant. There is no dedicated canonical market entity to populate: preserve market evidence with the researched pack and provenance. No new model is proposed.

For READY products, use one ordinary required variant record unless genuine manufacturer-defined alternatives are subsequently established. Its editorial display label should follow the existing catalogue convention; it must not claim a newly invented manufacturer's variant. Different absorbency product names here are separate proposed products pending editorial grouping, not colour variants or packaging variants. Optional family text can remain null; do not force the discovery site's grouping into the model.

Image source records already distinguish source URL, permission status/evidence and visibility. Permission must remain `Unknown` until supported; source availability does not justify public visibility. The domain's actual enum names are `PermissionGranted` and `PermissionNotRequired` for permissions that allow visibility.

## 3. Existing catalogue and duplicate protection

The public [DiaperScout products page](https://diaperscout.app/products) showed seven product-variant rows: **Crinklz Original / Thrust Vector**, and **NorthShore MEGAMAX** in Black, Blue, Pink, Purple, Tie-Dye and White. This is two covered products, not seven unrelated products. Comparing the complete public result list with the six shortlisted brands/manufacturers and names found no other represented ranges.

The Original detail page showed S/M/L/XL and a selected XL bag of 15; the MEGAMAX variant rows already represent the colour distinction. These two discovery leads are **DUPLICATE / ALREADY COVERED at product level**, not proposed additions. This does not establish that every barcode or market pack is already covered; future enrichment should attach to those products after exact-pack matching.

Read-only manual lookups at [Scan](https://diaperscout.app/scan) returned **“We don’t have this one yet”** for `4052199297002`, `4052199303413`, `7332152207383`, `5900516695750`, `7322541407432` and `5713571000168`. Names/manufacturers/brands and visible variants were also checked against the complete public result list; READY sizes/counts therefore had no visible product to match. No contribution or discovery was submitted.

**Limit:** public absence is not proof of absence from drafts, review submissions, hidden or historical catalogue records. Before any import, an editor must repeat manufacturer/brand/name/variant/size/count/GTIN checks against those records, including zero-padded equivalent GTINs. Existing records should be enriched rather than duplicated. The public web host's `/api/v1/products` returned 404; no production database access was attempted to work around it.

## 4. Twenty candidates and outcomes

| ID | Independently researched candidate | Outcome | Exact scope / reason |
|---|---|---|---|
| C01 | TENA ProSkin Slip Plus | NEEDS RESEARCH | Current UK M article 710607, 30 pieces; exact revision/market GTIN mapping needs verification. |
| C02 | TENA ProSkin Slip Super | CONFLICT | Same M/30 article and EAN, but official 70–122 cm hips versus retailer 73–122 cm. |
| C03 | TENA ProSkin Slip Maxi | NEEDS RESEARCH | Current UK M/24 article 712400; retailer barcode lead needs opened, exact current-pack evidence. |
| C04 | TENA Slip Active Fit Maxi | NEEDS RESEARCH | M/24 barcode found; competing capacity leads need primary technical/revision evidence. |
| C05 | TENA ProSkin Pants Super | NEEDS RESEARCH | Current UK M/12 article 793565; older retailer EAN not linked confidently to this article/revision. |
| C06 | TENA ProSkin Flex Maxi | CONFLICT | M/22 EAN independently verified; retailer labels fit as hips and fastening as adhesive, manufacturer says waist and hooks. |
| C07 | MoliCare Premium Elastic 6 drops | **READY** | **Medium, one bag of 30 only.** |
| C08 | MoliCare Premium Elastic 8 drops | CONFLICT | UK manufacturer bag 26 versus US manufacturer “bag 78” using the same M EAN. |
| C09 | MoliCare Premium Elastic 9 drops | CONFLICT | Manufacturer M hips 85–120 cm versus retailer 90–120 cm for the same bag EAN. |
| C10 | MoliCare Premium Elastic 10 drops | **READY** | **Medium, one bag of 14 only.** |
| C11 | ABENA Slip Premium, level 4 | CONFLICT | M4/21 manufacturer brief versus same-barcode Australian retailer's pull-up description. |
| C12 | ABENA Pants Premium, level 2 | CONFLICT | M2/15 manufacturer Rothwell 1900 ml versus retailer structured 1800 ml. |
| C13 | ABENA Pants Premium, level 3 | CONFLICT | M3/15 manufacturer bag EAN differs from hygi listing despite matching article number. |
| C14 | Seni Super Plus | NEEDS RESEARCH | M/30 EAN lead has retail unit ambiguity (one bag versus three bags); US packs differ. |
| C15 | Seni Super Quatro | CONFLICT | Current manufacturer's M 75–110 cm versus retailer 80–110 cm; pack revision needs matching. |
| C16 | Seni Active Super | **READY** | **Medium, one pack of 10 only; European evidence.** |
| C17 | Attends Slip Regular 10 | NEEDS RESEARCH | M/26 EAN lead; Regular/Air Comfort code/naming needs current primary confirmation. |
| C18 | Attends Pull-Ons 8 | **READY** | **Medium, one bag of 16 only.** |
| C19 | NorthShore Supreme | NEEDS RESEARCH | Current product confirmed; exact current bag/case UPC ownership and pack revision unverified. |
| C20 | NorthShore GoSupreme | NEEDS RESEARCH | Current distinct Max/GoSupreme/Lite ranges confirmed; exact colour/size/pack GTIN unverified. |

## 5. READY proposals — exact sellable packs

### C07 — MoliCare Premium Elastic 6 drops, Medium, bag 30

**Identity:** PAUL HARTMANN AG → MoliCare → Premium Elastic range → Premium Elastic 6 drops. Proposed product type `Tape`; one ordinary variant. **Printed size Medium/M; hips 85–120 cm; bag 30; EAN-13 `4052199297002`; manufacturer article 165272.** Current [UK manufacturer assortment][H6] explicitly joins Medium, 30 pieces and that EAN. The [manufacturer logistics table][HPDF], dated **September 2023**, separately lists bag versus carton identifiers; its corresponding case is three bags/90 pieces, EAN `4052199297019`. Do not attach the case EAN to the bag.

Specs: elastic side panels, reclosable hook fasteners, breathable material and wetness indicator are supported by manufacturer material. No numeric manufacturer-tested capacity is staged. Range research found S, M, L and XL; **only M/30 is proposed**, with no inherited identifier for other sizes. [Medi-Inn][R6] independently associates the bag EAN with M/30 and 85–120 cm. Its listing displayed unavailable status, so this is retail provenance, not a claim of current stock.

Market: UK manufacturer presence and German retailer evidence. US M/30 article 165272 appeared in a [Genesis retailer search lead][US6], but that page could not be opened; US GTIN and live availability remain unverified. Imagery: manufacturer packshot on H6; permission Unknown, no asset copied. Open questions: current UK buying destination/stock, image permission, hidden-record deduplication. None changes the verified UK bag identity.

### C10 — MoliCare Premium Elastic 10 drops, Medium, bag 14

**Identity:** PAUL HARTMANN AG → MoliCare → Premium Elastic → Premium Elastic 10 drops. Proposed type `Tape`; one ordinary variant. **Medium/M; hips 85–120 cm; bag 14; EAN-13 `4052199303413`; article 165672.** Current [UK assortment][H10] explicitly ties this EAN to one Medium bag of 14; [German manufacturer page][H10DE] and [manufacturer table][HPDF] corroborate the pack structure/fit. S/M/L/XL exist in manufacturer evidence; no sibling packs are proposed.

Elastic panels/reclosable hook tapes, breathable material and wetness indicator are supported. Millilitres are left null. [Easy Care Solutions][R10] offers **four bags of 14**, SKU `165672 x 4 Packs`, with EAN `4052199301556`. The manufacturer table confirms this is the carton identifier. It is separate 56-piece case evidence, not another barcode for the 14-piece bag. Retail site's “Unit Count 14” describes its inner bag; the offer title is four bags. Preserve both facts in retail notes and review whether the destination actually supplies the sealed manufacturer case before creating any case offer.

Market: UK manufacturer and UK retailer case offer; DE manufacturer evidence. US retailer leads were found for other sizes but do not verify this exact Medium pack. Imagery: H10 manufacturer Medium packshot, permission Unknown. Open questions: approved imagery, a verified single-bag UK retail destination, hidden-record deduplication. A now-404 Incontinence Choice URL is excluded from live offers.

### C16 — Seni Active Super, Medium, pack 10

**Identity:** TZMO SA → Seni → Active range → Active Super. Proposed type `PullUp`; one ordinary variant. **Medium; manufacturer waist/hip basis 80–110 cm; pack 10; EAN-13 `5900516695750`.** The [global manufacturer page][SA] establishes the exact name, size/count and fit. [DryOnline Germany][RSA] explicitly links that EAN to Medium/10 and 80–110 cm. Retail SKU `5512` is not the barcode. Manufacturer's wider range is S/M/L/XL, each shown as 10; other sizes are research context only.

Specs: breathable pull-up with elastic waist, standing gathers, wetness indicator and latex-free construction. No numeric manufacturer capacity is staged: the retailer's 1400 ml lacks a verified manufacturer test/method tie. European identity is supported; **UK and US exact-pack availability/GTIN were not established**. Do not equate the US “Seni Active” naming with “Active Super” without evidence. Retail page displayed a German offer but opened representation can be cached; recheck price/stock before any live offer.

Imagery: manufacturer's product photographs on SA, permission Unknown; no images copied. Open questions: UK/US exact-pack coverage, image licence, hidden-record deduplication. Broader commercial coverage is optional evidence, not permission to invent regional packs.

### C18 — Attends Pull-Ons 8, Medium, bag 16

**Identity:** Attends (manufacturer/brand name shown by product sources; Attindas group affiliation supported by [official terms][AT]) → Attends → Pull-Ons range → Pull-Ons 8. Proposed type `PullUp`; one ordinary variant. **Medium; manufacturer's waist fit 80–110 cm; bag 16; EAN-13 `7332152207383`; UK article 207406.** [Official UK shop][A8] establishes article, Medium, 16 and fit. Independently opened [Redcare France][RA8] explicitly shows Medium/16 and this EAN. Its BE03130978 product code is not an EAN.

Specs: breathable pull-up, elastic waist, standing leak protection and wetness indicator supported by the official product source. Do not map a disposal tape into a wearing fastener. Millilitres, exact factory/legal manufacturing entity, and colour remain unstaged where not verified. The UK source's generic Pull-Ons size table is not proof that every size exists at absorbency 8: only M/16 is proposed.

Market: official UK shop showed **in stock, £19.50 excluding VAT** when inspected; eligibility for any VAT exemption is not assumed. Redcare France displayed **currently unavailable** despite showing the product/barcode. No US equivalence established. Manufacturer image source A8 is legitimate provenance but not a reuse licence: [terms][AT] require express consent for redistribution/commercial use. Permission remains Unknown; approval/evidence is required before displaying that image. Final editor should select/reuse the appropriate Attends business record and verify its legal-entity detail if needed; do not invent a factory.

## 6. NEEDS RESEARCH — precise blockers

| Candidate | Independent facts / useful leads | What must be obtained before READY |
|---|---|---|
| C01 Plus | [Current UK page][TPLUS]: M 710607/30, hips 70–122 cm; ComfiStretch/DuoLock. Older regional barcode leads include 7322541802145 and 7322540646726. | Opened manufacturer packaging/label or trustworthy exact current UK pack evidence tying a GTIN to this revision. Different regional EANs are not automatically an error, and must not be collapsed. Older fit/page leads need revision resolution. |
| C03 Maxi | [Current UK page][TMAXI]: M 712400/24, hips 70–122 cm. [Pharmacy lead][RMAXI] links 7322541802633 to M/24; a separate carton lead is 7322541802640 for 3×24. | Actual exact-pack page/document verification, UK revision mapping and retailer fit discrepancy lead (73–122 versus 70–122). Lead pages failed direct opening; barcode is not promoted from snippets. |
| C04 Active Fit Maxi | [Official page][TAF] establishes plastic-backed adhesive-tab product, M 710949/24, hips 73–122. [Kiwisto][RAF] gives 7322540789171 and 3155 ml ISO/Rothwell. UK indexed lead gives 3270 ml. | Manufacturer current technical sheet/pack revision, exact UK retail evidence and resolution of competing capacity leads. The 3270 claim is a lead, not a verified conflict finding; neither value is staged as manufacturer capacity. |
| C05 Pants Super | [Official page][TPANTS]: M 793565/12, waist 80–110; S/L/XL also 12. [Superdrug][RTPANTS] shows EAN 7322540574814, but no longer available online. | Exact current 793565/12 GTIN. Superdrug does not provide sufficient explicit count/revision linkage; no count is inferred from unit price. No claim that older packs are globally discontinued. |
| C14 Super Plus | [Global manufacturer][SP]: M 75–110 cm, packs 10/30, breathable tab brief. [ProfiCleanShop][RSP] shows M/30 EAN 5900516803582, but “units per sales unit” says three bags. [US manufacturer][SPUS] M is 10/25, not European 10/30. | Selected-unit clarification, primary packaging EAN mapping and exact UK/US retailer/GTIN evidence. Keep 30, 90 and US 25 separate; do not transpose European identifiers to US bags. |
| C17 Slip Regular 10 | Current official range exists. M/26 EAN 7332152207680 and case 4×26 EAN 7332152207697 appear in [Swiss distributor lead][RA10]; article 207697 is associated with Regular and Air Comfort naming in official search results. | Opened current manufacturer M pack sheet/label, independent exact pack EAN and clarification whether Air Comfort is rename/region/another product. UK M shop and Swiss page could not be successfully read. US equivalents unknown. |
| C19 Supreme | [Current official product][NS] confirms plastic-backed tab brief, M/L/XL, rear waist elastic, leak guards. [US marketplace lead][RNS] suggests White M/15 UPC 893947012331; [Zoro lead][ZNS] suggests M/60 UPC 893947012317. | Current manufacturer GTIN/UPC for selected colour, size and bag/case; verify range revision, fit and retailer authorisation/pack scope. Zoro's unavailable/discontinued listing is not manufacturer lifecycle evidence. UK importing marketplace pages do not establish a UK pack. |
| C20 GoSupreme | [Current official range][NG] distinguishes GoSupreme Max, GoSupreme and GoSupreme Lite. [GoSupreme product][NG8] shows cloth-like backing and S through 3XL. | Exact selected colour/size/count/GTIN, current size fit and bag/case evidence. Do not collapse Max/Lite into packaging versions or borrow a MEGAMAX identifier. UK coverage remains unverified. |

**Promising candidates specifically blocked by missing/unverifiable exact-pack GTIN:** C01, C03, C05, C17, C19, C20. C14 also needs pack-unit/EAN mapping; C04 has retailer EAN evidence but lacks revision/specification confidence. Checksum-valid leads do not bypass these blockers.

## 7. CONFLICT — no winner silently selected

| Candidate | Evidence A | Evidence B | Required resolution |
|---|---|---|---|
| C02 Super M/30 | [UK current][TSUPER] and [Polish official EAN table][TSUPERPL]: article 711208, EAN 7322541802268, hips 70–122. | [123Drogisterij][RSUPER]: same article/EAN/count, hips 73–122 and different numeric capacity claims. | Obtain dated technical/pack label confirming intended revision. Do not average measurements or infer capacity from drops. |
| C06 Flex Maxi M/22 | [UK manufacturer][TFLEX]: waist 71–102, hook fastening. | Browser-opened [Redcare France][RFLEX]: exact EAN 7322541407432/M22, but calls the same fit hips and mentions adhesive attachments. | Verify fit basis and fastening/revision; request correction/label evidence. Numeric range agreement does not remove basis disagreement. |
| C08 Elastic 8 M | [UK source][H8] and [manufacturer table][HPDF]: bag 26, EAN 4052199297248; carton 78 has 4052199297255. | [US manufacturer][H8US]: “1 Bag of 78 Briefs” with EAN 4052199297248; selling unit says case. | Manufacturer confirmation of US selling unit/EAN. Do not reinterpret its incorrect-looking field without confirmation. |
| C09 Elastic 9 M/26 | [Manufacturer table][HPDF]: hips 85–120; [UK source][H9] confirms bag EAN 4052199297309. | [Kiwisto][R9]: same EAN/26, hips 90–120. | Dated pack/technical label or manufacturer confirmation, then update provenance. |
| C11 Slip M4/21 | [Manufacturer sheet][AM4PDF], dated 27 June 2026: brief with tapes, hips 70–110, Rothwell 3600 ml, bag 5713571000168, carton four bags 5713571000175. | [House of Nappies][RAM4]: same bag EAN/21/fit but describes pull-up use. | Establish/correct retailer style error and any regional packaging difference. Exact GTIN is independently verified, but candidate remains held for material style conflict. |
| C12 Pants M2/15 | [Manufacturer sheet][AM2PDF], 27 June 2026: hips 80–110, Rothwell 1900 ml, bag 5713571000885, six-bag carton 5713571000892. | [123Drogisterij][RAM2]: same article/EAN/15, description 1900 ml but structured capacity 1800 ml. | Manufacturer/retailer revision clarification. Leave disputed capacity out of canonical proposal until reviewed. |
| C13 Pants M3/15 | [Manufacturer sheet][AM3PDF], 27 June 2026: hips 80–110, Rothwell 2400 ml, bag 5713571000908; six-bag carton 5713571000915. | [hygi][RAM3]: article 1000021324, 15-piece page, EAN 5713571000847. | Confirm selected hygi pack/size and identify why EAN differs. Do not accept neighboring-size EAN or retailer feed as overriding primary sheet. |
| C15 Super Quatro M | [Current manufacturer][SQ]: M 75–110 cm. | [1plusHygiene][RSQ]: M/10 EAN 5900516803391, 80–110 cm, retailer ISO11948 3700 ml. | Match precise packaging revision and fit. Older EAN 5900516692858 is a separate lead, not an alias automatically assigned to this pack. |

Conflicts remain in staging with their evidence, not imported. Strong primary evidence is preserved; “CONFLICT” does not mean its checksum is bad or that the manufacturer is necessarily wrong.

## 8. Historical and rejected material

**No one of the 20 candidates was proved globally discontinued.** Current manufacturer presence supports researching them; it does not prove stock in every country. A dated 2023 document supports pack structure, not current availability on its own.

Historical naming follow-up: [ABENA's own packaging explanation][ARENAME] confirms Abri-Form → ABENA Slip and Abri-Flex → ABENA Pants. These old discovery names require conversion-sheet/pack-level research, not new duplicate Current products and not blanket deletion of old identifiers. This source does not establish that every old pack has vanished from sale. Mark old packaging as historical/replaced only when the exact conversion is independently established.

Rejected evidence, rather than invented REJECT product outcomes:

- DiapStash data beyond candidate names: excluded by the brief.
- ASIN/internal SKU as GTIN: excluded; Supreme marketplace SKU is not barcode proof.
- Countrywide staging-domain pages: excluded as production retail destinations.
- A now-404 MoliCare URL: excluded as a current offer; an indexed historical description is not live stock.
- User-review page whose Supreme heading contained MEGAMAX details: excluded for identity mismatch.
- Ubuy/Desertcart international-import pages, user reviews and generic search snippets: discovery/market leads only, not canonical packaging or UK-stock proof.

## 9. Exact-pack identifier ledger

All following independently evidenced numeric identifiers passed GTIN-13 check digits. Only the four READY bags are proposed for addition. Others are retained as evidence under held candidates or outer-pack context.

| Candidate | Exact unit | GTIN-13 | Evidence | Disposition |
|---|---|---|---|---|
| C07 | M, bag 30 | 4052199297002 | H6 + HPDF + R6 | READY selected pack |
| C07 | M, case 3×30 = 90 | 4052199297019 | HPDF explicit carton column | Evidence only; not proposed |
| C10 | M, bag 14 | 4052199303413 | H10 + H10DE + HPDF | READY selected pack |
| C10 | M, case 4×14 = 56 | 4052199301556 | HPDF + R10 | Evidence only; case delivery/offer review |
| C16 | M, pack 10 | 5900516695750 | SA identity/count + RSA exact EAN | READY selected pack |
| C18 | M, bag 16 | 7332152207383 | A8 identity/count + RA8 exact EAN | READY selected pack |
| C02 | M, bag 30 | 7322541802268 | TSUPERPL + RSUPER | CONFLICT fit |
| C06 | M, pack 22 | 7322541407432 | RFLEX exact EAN/count + TFLEX | CONFLICT fit basis/fastener |
| C08 | M, bag 26 / disputed US 78 | 4052199297248 | H8/HPDF versus H8US | CONFLICT unit |
| C09 | M, bag 26 | 4052199297309 | H9 + HPDF + R9 | CONFLICT fit |
| C11 | M4, bag 21 | 5713571000168 | AM4PDF + RAM4 | CONFLICT style |
| C11 | M4, carton 4×21 = 84 | 5713571000175 | AM4PDF p2 | Evidence only |
| C12 | M2, bag 15 | 5713571000885 | AM2PDF + RAM2 | CONFLICT capacity |
| C12 | M2, carton 6×15 = 90 | 5713571000892 | AM2PDF p2 | Evidence only |
| C13 | M3, bag 15 | 5713571000908 | AM3PDF p2 | CONFLICT retail identifier |
| C13 | M3, carton 6×15 = 90 | 5713571000915 | AM3PDF p2 | Evidence only |
| C13 | Claimed M3 page; mapping disputed | 5713571000847 | RAM3 | CONFLICT; no exact pack assigned |
| C15 | M, pack 10 | 5900516803391 | RSQ | CONFLICT revision/fit |

No barcode-symbol artwork was decoded. “EAN-13” here is the source's EAN designation and 13-digit GTIN data, not a claim to have physically inspected every printed symbol. A zero-prefixed 14-digit presentation of the same EAN must be considered equivalent for duplicate review, rather than used to create another pack.

## 10. Market differences, images and retail

**Market differences:** MoliCare 8's conflicting US unit field is a publication blocker. Seni Super Plus has different European and US manufacturer pack counts (European M 10/30; US M 10/25 and a US Regular size). NorthShore's US site distinguishes current GoSupreme ranges; its old 2022 brochure must not define the entire current range. ABENA's renamed packaging has new article/barcode conversion evidence; identical naming/absorbency alone does not establish identifier continuity.

UK presence is manufacturer evidence for READY MoliCare and Attends, not proof that a US pack shares their GTIN. For Seni Active Super, confirmed European sources are recorded without claiming UK stock. US retailer and authority searches were attempted; results that were inaccessible, generic, marketplaces or for another exact size are not promoted. An unverified country means unknown, not unavailable.

**Image ledger:** H6/H10, SA and A8 have legitimate manufacturer imagery sources. Source URLs are recorded, but all four have **Unknown permission**, no copied asset and no approved image. Final downloadable resolution and suitability for a specific packaging revision remain to be checked after permission is established. ABENA pages exposed a missing-image placeholder during inspection; their media centre is a follow-up route, not an automatic licence. TENA/HARTMANN/Seni retailer images are also not automatically reusable. Attends terms explicitly reserve redistribution/commercial rights. Use an already approved DiaperScout generic image if editorially appropriate, or obtain documented permission; do not assume a generic image has already been approved by this task.

| Retail source | Pack / market | Evidence and current-status limitation |
|---|---|---|
| Medi-Inn R6 | MoliCare 6 M/30, DE | Exact EAN, SKU 98583; unavailable in opened representation. |
| Easy Care Solutions R10 | MoliCare 10 M, four bags of 14, UK | Case EAN and bundle title verified; review sealed case versus reseller bundle. No single-bag offer inferred. |
| DryOnline RSA | Seni Active Super M/10, DE | Exact EAN, SKU 5512; offer present. Cached representation, recheck price/stock. |
| Official Attends shop A8 | Pull-Ons 8 M/16, UK | Article 207406, in stock, £19.50 ex VAT on inspected page. No affiliate eligibility established. |
| Redcare RA8 | Pull-Ons 8 M/16, FR | Exact EAN; explicitly unavailable when opened in browser. |
| Redcare RFLEX | Flex Maxi M/22, FR | Exact EAN; €38.89 and in stock in browser, but candidate held for specifications. |
| Lyreco Norway | Flex Maxi M/22, NO | Selected one pack versus carton three; SKU 310136, NOK299 including VAT, stock80 in browser. Technical accordion did not expose a barcode; not identifier evidence. |
| Superdrug RTPANTS | TENA Pants Super M, UK | EAN shown; no longer available online. Current 12-pack article mapping unresolved. |
| House of Nappies RAM4 | Slip M4/21, AU | EAN confirms pack, but style conflict prevents promotion of its specs. |
| hygi RAM3 | Pants M3/15 page, DE | Identifier conflict; no verified offer mapping. |

No affiliate arrangements, link rewriting, retailer imports or live stock integration were assumed or created. Public price is not approval for a commission relationship. Future listings should use the existing commerce verification architecture.

## 11. Review decisions and follow-up priority

1. Review the **four selected READY bags** and proposed business/product grouping. Recheck private/historical duplicates before creation; refresh evidence if packaging changed.
2. Obtain imagery permission or select an approved generic asset. No product photograph in this report is approved for reuse.
3. Ask HARTMANN to clarify MoliCare 8 US case/EAN fields, and compare current MoliCare 9 pack fit. No message has been sent.
4. Resolve ABENA M3's hygi barcode and M2's capacity discrepancy; verify Slip M4 retailer style/revision. Keep manufacturer sheets as evidence, not a silent override.
5. Obtain dated TENA pack/technical evidence for refreshed Slip range, Pants Super article793565 and Active Fit Maxi capacity. Clarify Flex fit basis/fastener wording.
6. Obtain current NorthShore manufacturer barcode lists by colour/size/pack and Attends Regular/Air Comfort identity documentation. These are valuable next research targets, not speculative imports.

## 12. Source register and audit trail

All sources accessed/searched **2026-10-04**. “Opened” means page content was inspected through the web tool; cached data/stock limitations apply. “Browser” means rendered page content inspected through the public browser. “Lead” means indexed content only, blocked/error on direct verification, and not accepted as canonical evidence. PDF dates below are printed document dates, not search-engine dates.

| ID | Source / inspection | Scope |
|---|---|---|
| H6 | [HARTMANN UK Elastic 6][H6], opened | Current assortment Medium/30/EAN |
| H8 | [HARTMANN UK Elastic 8][H8], indexed official content + HPDF | UK bag evidence; re-open before resolving US conflict |
| H8US | [HARTMANN US Elastic 8][H8US], opened | Disputed case/bag/EAN |
| H9 | [HARTMANN UK Elastic 9][H9], opened | Assortment |
| H10 | [HARTMANN UK Elastic 10][H10], opened | Exact M14/EAN, manufacturer packshot |
| H10DE | [HARTMANN DE Elastic 10][H10DE], opened | Fit and pack evidence |
| HPDF | [HARTMANN logistics PDF][HPDF], downloaded/read/rendered p1; September2023 | Separate consumer/case EAN columns |
| R6 | [Medi-Inn][R6], opened | M30 EAN/fit/unavailable |
| R9 | [Kiwisto Elastic9][R9], opened | Same EAN, conflicting fit |
| R10 | [Easy Care Solutions][R10], opened | Four14 case offer/EAN |
| US6 | [Genesis][US6], lead | US M30 ordering lead only |
| TPLUS | [TENA Plus UK][TPLUS], opened | Current shape/fit/article/pack |
| TSUPER | [TENA Super UK][TSUPER], opened | Current fit/article/pack |
| TSUPERPL | [TENA Super Poland][TSUPERPL], opened | Official EAN/size/count |
| RSUPER | [123Drogisterij Super][RSUPER], opened | Same EAN, conflicting fit |
| TMAXI | [TENA Maxi UK][TMAXI], opened | Current fit/article/pack |
| RMAXI | [Apotheke am Theater][RMAXI], lead | M24 EAN; not promoted |
| TAF | [TENA Active Fit UK][TAF], opened | Current product/size/count/specs |
| RAF | [Kiwisto ActiveFit][RAF], opened | Exact EAN, retailer capacity |
| TPANTS | [TENA Pants UK][TPANTS], opened | Current UK article/12-pack |
| RTPANTS | [Superdrug][RTPANTS], opened | EAN, unavailable; count unresolved |
| TFLEX | [TENA Flex UK][TFLEX], opened | Waist basis/hooks/M22 |
| RFLEX | [Redcare Flex][RFLEX], browser | EAN M22; fit/fastener discrepancy |
| AM4 / AM4PDF | [ABENA UK M4][AM4] browser / [technical sheet][AM4PDF] downloaded/read/rendered p2 | Printed27 June2026; exact bag/carton/specs |
| AM2 / AM2PDF | [ABENA UK M2][AM2] browser / [technical sheet][AM2PDF] downloaded/read/rendered p2 | Printed27 June2026; exact bag/carton/specs |
| AM3 / AM3PDF | [ABENA UK M3][AM3] browser / [technical sheet][AM3PDF] downloaded/read/rendered p2 | Printed27 June2026; exact bag/carton/specs |
| RAM4 | [House of Nappies][RAM4], opened | Exact bag EAN; style conflict |
| RAM2 | [123Drogisterij M2][RAM2], opened | Capacity conflict |
| RAM3 | [hygi M3][RAM3], opened | EAN conflict |
| ARENAME | [ABENA packaging explanation][ARENAME], opened | Historical names/conversion route |
| SP / SPUS | [Seni global Plus][SP] / [Seni US Plus][SPUS], opened | Different market pack tables |
| RSP | [ProfiCleanShop][RSP], opened | M30 EAN with sales-unit ambiguity |
| SQ / RSQ | [Seni Poland Quatro][SQ] / [1plusHygiene][RSQ], opened | Fit/revision disagreement |
| SA / RSA | [Seni global Active Super][SA] / [DryOnline][RSA], opened | Manufacturer identity/count plus retailer EAN |
| A8 / RA8 | [Attends UK shop][A8], opened / [Redcare][RA8], browser | M16 identity/count and independent EAN |
| AT | [Attends terms][AT], opened | Attindas affiliation; image reuse permission required |
| RA10 | [Sana Swiss][RA10], lead | Regular10 bag/case EAN leads only |
| NS / RNS / ZNS | [NorthShore Supreme][NS], opened / [Medistoreweb][RNS] and [Zoro][ZNS], leads | Current product vs unverified UPCs |
| NG / NG8 | [NorthShore range][NG] / [GoSupreme detail][NG8], opened | Current range distinction, GTIN missing |

PDF byte fingerprints (SHA-256) preserve which versions were inspected; source PDFs remain in D: research scratch, not vendored into the repository:

| Document | SHA-256 |
|---|---|
| HPDF | `ab752210eb736d0cf62c686577915796aa1c0dbfcc1d60e9834672687ee429c2` |
| AM4PDF | `b43c0a5cf88398c57a879b0e960176a296659e9bfb6a8f697ddafdaa6e73c0ef` |
| AM2PDF | `edc7a6ed4b7f1f8c1bbe8e59dfd940149e141c7b738cdb6d36ed0fe42f4c66c1` |
| AM3PDF | `23f7b4bd32e0239a52988f280d8230813e2f830720aa52afb43bad8d263dacf4` |

### Validation

**185 data consistency checks passed**, covering 20 unique candidate IDs, allowed outcomes, exact-pack source references, positive counts, GTIN syntax/check digits, no conflicting assignment of a READY barcode, four READY exact bags, unknown image permissions and disabled production/import flags. All 18 independently evidenced ledger identifiers pass their check digits; 49 source references resolve. Data checks do not certify source truth or resolve conflicts. Documentation-only change: application build/test suite was not run; no runtime code changed.

[H6]: https://www.hartmann.info/en-gb/products/continence-management/all-in-one/moderate-incontinence/molicare%C2%AE-premium-elastic-6-drops
[H8]: https://www.hartmann.info/en-gb/products/continence-management/all-in-one/severe-incontinence/molicare%C2%AE-premium-elastic-8-drops
[H8US]: https://www.hartmann.info/en-us/our-products/incontinence-management/briefs/severe-incontinence/molicare-premium-elastic-8d
[H9]: https://www.hartmann.info/en-gb/products/continence-management/all-in-one/very-severe-incontinence/molicare%C2%AE-premium-elastic-9-drops
[H10]: https://www.hartmann.info/en-gb/products/continence-management/all-in-one/very-severe-incontinence/molicare%C2%AE-premium-elastic-10-drops
[H10DE]: https://www.hartmann.info/de-de/produkte/inkontinenzversorgung/inkontinenz-slips/schwerste-inkontinenz/molicare%C2%AE-premium-elastic-10-tropfen
[HPDF]: https://www.hartmann.info/-/media/country/website/archive/incontinence/doc/sales-folder-relaunch-mc-premium-elastic-klinik-hires_0865536-018492.pdf?rev=4fdfcf64f3714b8a9e53082b50453d93&sc_lang=de-de
[R6]: https://medi-inn.de/products/molicare-premium-elastic-6-tropfen-gr-m-85-120-cm-hueftumfang
[R9]: https://www.kiwisto.de/MoliCare-Premium-Elastic-9-Tropfen-Medium-90-120-cm/1655720
[R10]: https://www.easycaresolutions.co.uk/molicare-premium-elastic-10-drops-medium-4-packs-of-14/
[US6]: https://www.genesishcp.com/products/molicare-premium-elastic-6-drops
[TPLUS]: https://www.tena.co.uk/professionals/products/unisex/slip-incontinence-briefs-with-tabs/tena-proskin-slip-plus/
[TSUPER]: https://www.tena.co.uk/professionals/products/unisex/slip-incontinence-briefs-with-tabs/tena-proskin-slip-super/
[TSUPERPL]: https://www.tena.pl/profesjonalisci/produkty/uniwersalne/tena-protective-underwear-pieluchomajtki-z-rzepami/tena-slip-proskin-super-m-l/
[RSUPER]: https://www.123drogisterij.nl/tena-slip-super-medium-1
[TMAXI]: https://www.tena.co.uk/professionals/products/unisex/slip-incontinence-briefs-with-tabs/tena-proskin-slip-maxi/
[RMAXI]: https://internet-apotheke-freiburg.de/shop/tena-slip-18914864
[TAF]: https://www.tena.co.uk/professionals/products/unisex/slip-incontinence-briefs-with-tabs/tena-slip-active-fit-maxi/
[RAF]: https://www.kiwisto.de/Tena-Slip-Active-Fit-Maxi-Medium-Windeln-mit-Folie/710949
[TPANTS]: https://www.tena.co.uk/professionals/products/unisex/incontinence-pants/tena-proskin-pants-super/
[RTPANTS]: https://www.superdrug.com/toiletries/incontinence/incontinence-pants/tena-pants-super-medium/p/618153
[TFLEX]: https://www.tena.co.uk/professionals/products/unisex/belted-briefs/tena-flex-maxi-belted-incontinence-briefs/
[RFLEX]: https://www.redcare-pharmacie.fr/hygiene-et-sante/BE04105714/tena-flex-maxi-m.htm
[AM4]: https://www.abena.co.uk/product-catalogue/incontinence/abena/all-in-one-brief-abri-form-delta-form/briefs-abena-slip-premium/p-9200-pm029458/v-1000021287
[AM4PDF]: https://cdn-abena.azureedge.net/assets/Global/Datasheets/en-GB/1000021287_en-GB.pdf
[AM2]: https://www.abena.co.uk/product-catalogue/incontinence/abena/pull-up-abri-flex/pants-abena-pants-premium/p-9200-pm029650/v-1000021323
[AM2PDF]: https://cdn-abena.azureedge.net/assets/Global/Datasheets/en-GB/1000021323_en-GB.pdf
[AM3]: https://www.abena.co.uk/product-catalogue/incontinence/abena/pull-up-abri-flex/pants-abena-pants-premium/p-9200-pm029650/v-1000021324
[AM3PDF]: https://cdn-abena.azureedge.net/assets/Global/Datasheets/en-GB/1000021324_en-GB.pdf
[RAM4]: https://www.houseofnappies.com.au/abena-slip-premium-m4-medium-waist-70-110cm-unisex
[RAM2]: https://www.123drogisterij.nl/abena-pants-m2
[RAM3]: https://www.hygi.de/abena-pants-m3-premium-1-packung-15-stueck-pd-217213
[ARENAME]: https://www.abenaprivat.dk/viden/ny-emballage
[SP]: https://en.seni-global.com/en/product/seni-super-plus-en
[SPUS]: https://seni-usa.com/en_US/product/seni-super-plus-briefs
[RSP]: https://www.proficleanshop.de/seni-super-plus-gr-m-inkontinenzslip-mit-klebeklettverschluessen-1-beutel-30-stueck
[SQ]: https://seni.pl/pl/produkt/seni-super-quatro
[RSQ]: https://www.1plushygiene.de/de/seni-super-quatro-medium/14064.html
[SA]: https://en.seni-global.com/en/product/seni-active-super-en
[RSA]: https://www.dryonline.de/seni-active-super-pants-medium
[A8]: https://shop.attends.co.uk/shop/pull-ons-8-medium
[RA8]: https://www.redcare-pharmacie.fr/hygiene-et-sante/BE03130978/attends-pull-ons-8-medium.htm
[AT]: https://www.attends.co.uk/terms-and-services
[RA10]: https://sana.swiss/en/attends-slip-regular-3148
[NS]: https://www.northshorecare.com/adult-diapers/adult-diapers-with-tabs/northshore-supreme-tab-style-briefs
[RNS]: https://medistoreweb.com/products/northshore-supreme-incontinence-tab-style-briefs-for-men-and-women-white-medium-pack-15
[ZNS]: https://www.zoro.com/northshore-supreme-briefs-60pk-1231/i/G010914295/
[NG]: https://www.northshorecare.com/adult-diapers/adult-pull-ups/gosupreme-product-line
[NG8]: https://www.northshorecare.com/adult-diapers/adult-pull-ups/northshore-gosupreme-pull-on-underwear
