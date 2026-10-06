# Explorer ID storyboard restoration

This presentation pass follows the supplied onboarding storyboard and the later Guide/Fox character sheets. Fox is a small, treasured stuffed toy: bead eyes, stitched smile and seams, a worn ear, patched paw and green scarf. Fox is stationary and secondary; the Guide supplies all actions. Wanderer provides a personal setting: the Guide writes the tag at the camper’s wooden dinette, then hands it to the Explorer from the same cozy interior.

## Comparison before UI changes

| Storyboard moment | Loss in the deployed version | Presentation correction |
| --- | --- | --- |
| Create your Explorer ID | Existing map pose and a plain card offered no tangible personal ID. | A bespoke scene of the Guide offering a blank Explorer tag at her desk, with Fox placed beside her belongings. |
| Tell us about you | Form-only composition lost the Guide/Fox vignette and preparation story. | A bespoke scene of the Guide seated inside Wanderer writing the tag, below the two fields; name/nickname wording and real form behavior retained. |
| Check your email | Generic envelope symbol replaced letter delivery and landscape. | A bespoke postbox/envelope scene; clear inbox instructions, resend and correction paths. No fake inbox launch via a compose-mail link. |
| Email verified | Folded straight into passkey setup, losing the confirmation pause. | An illustrated owl/outpost moment and Continue link, displayed only from existing server-verified onboarding state. |
| Add a passkey | Plain name card and text-only security explanation lost the equipment story. | A bespoke Guide attaching an Explorer tag to a packed Backpack; optional/recommended copy and native flow retained. |
| Device confirmation | Actual native system prompt already matched the intended behaviour. | Unchanged; no simulated Face ID or device-security dialog. |
| Explorer ID ready | Minimal drawn key/tag, no send-off and no completion checklist. | Bespoke brass key/luggage-tag art with real name overlaid, only when the existing server state confirms a passkey; Guide handing the tag toward the Explorer from inside Wanderer, and a truthful progress list. Skipping shows the personal ID and email-sign-in reminder without a key. |

## Scope

Only onboarding Razor markup, scoped CSS, presentation bindings and the browser test for the additional confirmation step change. The verification and passkey endpoints, profile lifecycle, credentials, cookie/claim construction, request/consumption semantics, rate limits, CSRF, recent-authentication policy and production configuration remain unchanged. `step=passkey` selects a visible screen only; it grants no verified/authenticated state.

Welcome, global navigation, Backpack and unrelated product screens are untouched. Art is delivered as optimised WebP with transparency and explicit image dimensions. Dynamic personal names are normal accessible HTML text, not embedded in generated pixels.

## Artwork provenance

Eight final assets were created using the built-in `image_gen` tool, using the supplied canonical sheets and storyboard as appropriate. [Prompt set](onboarding-art-prompts.json) records the final generation/edit prompts. Fox remains a stationary plush, with stitched features. The corrected brass key has teeth on one side and a smooth opposite edge. Superseded outdoor-writing and double-sided-key images are excluded from the product assets.

Final assets are under `src/DiaperScout.Web/wwwroot/images/onboarding/`: `welcome.webp`, `details-van.webp`, `email.webp`, `verified.webp`, `passkey.webp`, `key-v2.webp`, `ready-van.webp`, `id.webp`. These are transparent 768 × 512 WebP images. Source generation originals remain outside the repository; all product references point to repository assets. Names remain live text rather than fictional names printed into artwork.

## Validation and release status

Release solution build passed with zero warnings and errors. All 15 focused onboarding checks passed, including three installed-app browser scenarios: Chromium with a registered passkey, Chromium skipping it, and WebKit without passkey support. These retain real email verification, fresh-page/reload behavior, virtual WebAuthn registration, cancellation and transient-error recovery, server-backed completion, Backpack consistency, sign-out and subsequent email sign-in. Visible completion artwork loaded successfully; the physical key and passkey checklist item appeared only after actual registration.

Mobile screenshots were reviewed for introduction, details, email waiting, email confirmation, passkey setup and both completion paths. The writing/handover scenes use Wanderer’s interior. Text fits the real tag artwork and the browser checks found no horizontal overflow at 375/390 px. Native device prompts remain device-owned and are not replaced by illustration or a simulated dialog. The final art browser checks ran against isolated test databases, not production.

This presentation pass was released as an image-only Web update. The earlier authentication release remains the unchanged API baseline. The prior 380-test / 24-production-check release results describe that baseline, not a rerun for this art pass.


## Production release — 5 October 2026

Runtime commit `1635a6b` was pushed to `fix/product-submission-recovery`. ACR build `db2g` succeeded from its committed source archive. Only Web was deployed: revision `diaperscout-web-vnet--art-1635a6b` is Healthy, Provisioned, Running and receives 100% traffic.

Web image: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:4e26430299abcfbde94b67bd080a99c5b2a08a68d020e2a9365de7e263db738e`.

[Configuration comparison](explorer-id-art-evidence/config-comparison.json) confirms preservation of application identities, environment, configuration and container template except Web image/revision suffix. API remains on its previous image/revision with internal ingress; Web remains public. No database migration, deployment job, secret, networking or scale change was made.

[Production validation](explorer-id-art-evidence/production-smoke.json) passed 36 checks: mobile Chromium/WebKit public pages, required-input validation, catalogue/product detail, Explore, Atlas, Scan, sign-in and Backpack entry points, anonymous rejection for private onboarding, plus delivery and SHA-256 verification of all eight illustration files. Browser checks reported no page errors. Screenshots confirm the bespoke welcome and Wanderer writing scene are live. No registration emails or synthetic production records were created.

[Recent console-log summary](explorer-id-art-evidence/log-summary.json) found no TLS/DNS/database failure or exception lines in the sampled Web/API logs. This is a sampled check, not continuous monitoring. Authenticated registration/passkey/skip/email-returning flows were verified in isolated tests; native-device authentication and the authenticated production completion screen were not re-exercised during this presentation deployment.

Rollback is a Web image-only update to `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:191755436b06b3c7418c25a0fa5cd825c4923ca4190d3e6ed44cdbcc5f27ae2c`.
