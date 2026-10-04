# Installed PWA Backpack

## Capabilities and implementation

1. The original Backpack was a public coming-soon page. Existing data supported Explorer names, passkeys, sign-out, author-linked observations/shops/proposals, local product recovery drafts and editorial catalogue drafts. Personal draft/discovery/settings destinations were added following explicit authorization to implement the mockup's menus. User/ExplorerProfile have no reliable member creation date.
2. Installed mode shows the large illustrated canvas backpack in the reference's sunlit cabin scene, a stitched live ID, hanging real passkeys, the gear shortcut, two-by-two Continue journey/My discoveries/Passkeys/Account settings grid, Sign out and Go to Atlas. All menus have working destinations. Existing fonts, bottom navigation and safe areas remain in use. Standard browser/desktop Backpack keeps the original presentation.
3. The Explorer ID reads the existing authenticated Explorer projection through a private, no-store web proxy returning only displayName. Subject, email and internal identity IDs are excluded. Profileless accounts show a neutral Explorer label; request failures show a recoverable status.
4. Registered passkeys appear as illustrated hanging keys with luggage tags containing the real friendly name. Every key links to management; multiple keys scroll inside the small ring area. Dates and removal are shown on the dedicated Passkeys destination. No device model is inferred.
5. Zero keys has a positive invitation in the Passkeys menu. One/multiple keys are independently represented without fake records; email sign-in remains available.
6. Passkeys opens `/backpack/passkeys`, an alias of the existing management page. Add/removal preserve the existing WebAuthn ceremonies, confirmation, ownership, recent-sign-in and antiforgery checks. No last-key policy changed; email fallback remains available.
7. Sign-out uses the existing cookie-clearing route directly. Anonymous access remains public and offers sign-in/account creation.
8. Release solution build: passed, 0 warnings, 0 errors (15.32 seconds). Domain tests: 61 passed, 0 failed, 0 skipped. Final targeted integration/browser run: 42 passed, 0 failed, 0 skipped, 2 minutes 40 seconds. Covers BackpackBrowserTests, PasskeyApiTests, PasskeyWebTests, PasskeyBrowserTests and PwaBrowserTests (including iOS). New browser cases verify real identity, zero/one/three keys, cancellation reaching the existing registration ceremony, safe removal including the last key, sign-out, anonymous access, private projection, navigation return and bottom-nav clearance. Existing native registration/sign-in tests also pass. Initial run was blocked by Docker startup; after recovery 39 passed and three new tests failed due to missing email test data. Correcting that fixture made all cases pass; no security requirement was weakened. Full unrelated integration suite was not run.
9. Inspected full-page installed renders at 390px Chromium (zero/three keys) and 430px WebKit (one key). Checked ID/name wrapping, key tags/details, zero state, scrolling, no horizontal overflow, hidden global header, existing active navigation and bottom clearance. Evidence is in `backpack-evidence/`, using explicitly test-only records. The design is a simpler illustration than the approved mockup, preserving the physical backpack/stitched ID metaphor. Only the approved mockup attachment was supplied; a separate current-screen screenshot was not available, so the original markup was inspected instead. This is browser emulation, not physical iPhone testing or a claim of exact visual parity.
10. Deliberately omitted: fabricated member dates, statistics, saved items, social profiles, email-change authority and new character artwork. Draft counts, discoveries, profile editing and device draft preferences are now implemented with real data. No database migration or production change was introduced.

## Files

- `src/DiaperScout.Web/Components/Pages/Backpack.razor`
- `src/DiaperScout.Web/Components/App.razor`
- `src/DiaperScout.Web/Services/PasskeyWebEndpoints.cs`
- `src/DiaperScout.Web/wwwroot/css/backpack-pwa.css`
- `src/DiaperScout.Web/wwwroot/js/backpack.js`
- `src/DiaperScout.Web/wwwroot/js/passkeys.js`
- `src/DiaperScout.Web/wwwroot/pwa/backpack-cabin.png`
- `tests/DiaperScout.Api.IntegrationTests/BackpackBrowserTests.cs`
- `src/DiaperScout.Application/BackpackAccount.cs`
- `src/DiaperScout.Infrastructure/BackpackAccount.cs`
- `src/DiaperScout.Infrastructure/ServiceCollectionExtensions.cs`
- `src/DiaperScout.Domain/Model.cs` (validated existing-profile rename)
- `src/DiaperScout.Api/BackpackAccountEndpoints.cs`
- `src/DiaperScout.Api/Program.cs`
- `src/DiaperScout.Web/Services/BackpackAccountWebEndpoints.cs`
- `src/DiaperScout.Web/Program.cs`
- `src/DiaperScout.Web/Components/Pages/BackpackJourney.razor`
- `src/DiaperScout.Web/Components/Pages/BackpackDiscoveries.razor`
- `src/DiaperScout.Web/Components/Pages/BackpackSettings.razor`
- `src/DiaperScout.Web/Components/Pages/Passkeys.razor`
- `src/DiaperScout.Web/Components/Shared/BackpackMenuIcon.razor`
- `src/DiaperScout.Web/wwwroot/js/backpack-personal.js`
- `src/DiaperScout.Web/wwwroot/js/contribution-draft.js`
- `tests/DiaperScout.Api.IntegrationTests/BackpackAccountTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/BackpackPersonalBrowserTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/PasskeyBrowserTests.cs` (installed page-heading assertion)
- This handover.

Baseline: GitHub branch `fix/product-submission-recovery`, commit `ffad7d0`, fetched and confirmed current before editing. No deployment/push performed for this task.

Final repeat Release build after all changes: 0 warnings, 0 errors, 4.51 seconds.

## Visual revision following feedback

The initial flat CSS backpack did not match the approved composition and was replaced. The revision uses a reference-based raster illustration with live HTML identity and security controls. Re-inspected the actual rendered zero/one/three-key screens at 390px Chromium and 430px Chromium/WebKit, including viewport previews with closed tags and full-page open-tag evidence. The name is tested to fit within the ID holder, the artwork must load, and the add action opens its panel before reaching the unchanged WebAuthn flow. Final revised browser run: 4 passed, 0 failed, 0 skipped, 28 seconds. A prior run caught enhanced navigation replacing the opened add panel; explicitly disabling enhanced navigation for its local anchors and preventing fragment navigation fixed it. Final Release solution build: 0 warnings, 0 errors, 6.60 seconds. Prior 42 security/PWA and 61 domain results apply to the unchanged underlying authentication implementation. Nothing was pushed or deployed.

Final one-key preview: `backpack-evidence/backpack-preview-1-430-chromium.png`. The name and key label belong to an explicit test account. WebKit emulation reports no native passkey support; its fallback warning is preserved rather than hidden for screenshots. Chromium provides a native-capable preview. Physical iPhone validation remains outside this local rendering check.

## Completed menu workflows

- Continue journey merges actual unexpired owner-matched device drafts with the signed-in editor's own Draft/NeedsChanges catalogue submissions. Local recovery keeps its existing 30-minute expiry and unchanged submission wizard. Server drafts resume the existing catalogue editor; editorial permissions are checked before returning them. The server count is exact, with the latest 50 records displayed. Corrupt, expired and foreign-owner device drafts are excluded. Listing does not extend expiry or delete records.
- My discoveries shows the latest 50 own product observations, public shop additions and product proposals. Atlas links use real places. Proposals show actual moderation status and do not claim publication. All queries filter by the authenticated actor before projection.
- Account settings displays the actual private sign-in email and permits renaming an existing Explorer profile, retaining the existing 100-character limit and unique-name constraint. A profileless account is not silently given a new profile. Email/sign-in authority cannot be edited. The device preference can disable new product recovery copies; existing copies retain their expiry and are not deleted. Security links reach existing passkeys and sign-out.
- Private API/proxy responses are `no-store`, self-scoped and authenticated. Name changes require the existing antiforgery header/token; no account identifier supplied by the browser chooses the target user. Draft matching uses a digest of the existing draft-owner claim rather than printing an identity identifier on the card.

Final validation: six Backpack API/browser cases passed initially (48 seconds). Broader relevant suite: 53 passed, one failed out of 54 (3 minutes 49 seconds); the failure expected the old mobile global-header Passkeys link. Replacing that obsolete assertion with the actual page heading preserved native WebAuthn coverage. After adding owned editorial draft recovery, the final targeted run passed all eight Backpack/native-passkey cases (1 minute 3 seconds), including that corrected case. All 61 domain tests passed again (2 seconds). Full unrelated integration suite was not repeated. Final build result is recorded below.

Visual validation re-inspected the actual 390/430px installed renders with all four menus, gear shortcut, live draft count, zero/one/multiple real keys and scrolling. The final test account has one actual owner-matched recovery copy; the mockup's example count was not hard-coded. `backpack-all-menus-390.png` is full-page evidence; `backpack-menus-lower-390.png` shows the menu area after scrolling, with Sign out/Go to Atlas fully clear of the unchanged fixed navigation. No claim of physical-device testing or exact pixel parity is made. No commit, push or deployment performed.

Final Release solution build for the completed menu workflows: passed, 0 warnings, 0 errors, 9.68 seconds. `git diff --check` passed.

Artwork: `src/DiaperScout.Web/wwwroot/pwa/backpack-cabin.png`, generated with the built-in imagegen tool using the supplied mockup as reference. No Guide/Fox artwork was regenerated. The illustration itself contains no account data, lettering or UI.

## Production release — 4 October 2026

Following explicit user approval to commit and deploy, fetched GitHub again and confirmed the branch had not advanced. Committed and pushed the implementation as `a7dfaf875a037e8bb7f4336a4e5216821f0a1760`. API and Web images were built from an immutable archive of that revision (ACR runs `db1t` and `db1u`). API was verified Healthy/Running/Provisioned with 100% traffic before Web deployment.

Read-only production browser screenshot review caught an inherited CSS specificity conflict in the new signed-out view: the installed page padding overrode the guest heading clearance. Corrected that one selector and added a geometric assertion to production smoke checks. Committed/pushed `aecf707`; replacement Web build `db1v` uses an immutable archive of that commit. API code is unchanged from `a7dfaf8`. Repeat Release solution build: 0 warnings, 0 errors, 30.08 seconds. Repeat Backpack browser regressions: 4 passed, 0 failed, 0 skipped, 24 seconds.

Final runtime revisions: API `diaperscout-api-vnet--backpack-a7dfaf8`, Web `diaperscout-web-vnet--backpack-aecf707`. Immutable image digests and prior rollback images are recorded in `backpack-evidence/config-comparison.json`. Configuration, identity and the complete template except image/revision suffix compare equal with the pre-release snapshot. No migrations, production data changes, authentication configuration changes or infrastructure changes were made.

Production evidence is under `backpack-evidence/production-smoke.*` and `production-backpack-*.png`. Checks use anonymous sessions, installed-mode emulation, Chromium 390×844 and WebKit 430×932, no service worker cache and zero production writes. Authenticated personal flows were verified with isolated automated test accounts; no production account was created or modified. Physical iPhone testing is not claimed. Restore the recorded prior API/Web digests to roll back without database changes.

Final live smoke result: both browser sessions passed at 08:27 UTC, zero browser errors, anonymous private-account requests redirected to sign-in (302), new assets available, guest heading clear of the sign-in card, installed navigation correct, no horizontal overflow, and Explore/Products/Atlas healthy. Both production revisions are Healthy/Provisioned and receive 100% traffic.

Final artwork prompt:

> Create a production illustration asset for the DiaperScout Backpack mobile screen, based closely on the TOP SCENE of this reference. Output portrait 1024x1152 illustration only, NO UI, NO text, NO lettering, NO status bar, NO navigation, NO buttons, NO humans or fox characters. Keep the reference composition: warm sunlit explorer cabin, softly painted blue-sky window and foliage upper left, wooden wall and softly blurred map upper right, wooden table bottom; a large olive canvas explorer backpack with rounded flap, rich tactile fabric stitching, brown leather straps and buckles, brass compass pin, two small gold star pins. Backpack fills lower two thirds, front facing, centred. Place a clearly rectangular EMPTY cream paper Explorer ID insert inside a stitched brown leather holder on front of bag, face parallel to image plane: holder spans x28%-72%, y52%-73%; blank cream interior x31%-69%, y55%-70%, absolutely no text or avatar, as real HTML text will be overlaid there. Bag top handle at y29%, bottom at y96%. Upper left x5%-48%, y7%-28% soft pale empty space for real heading. A small brass attachment ring at right side of bag, no hanging characters or existing keys. Preserve mockup's warm detailed storybook illustration, softly hand-painted fabric/leather, not photorealistic and not flat vector. Do not reproduce lower account cards. No invented UI or account data.
