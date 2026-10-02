# iOS installed-PWA safe-area research — 2 October 2026

Research only: no application/runtime/infrastructure changes or deployment made for this investigation.

## Physical evidence

The supplied physical screenshot `IMG_2735.PNG` is 1290×2796. The status-area corner median is RGB(250,245,236), exactly **#faf5ec**. The left edge of the artwork begins at screenshot pixel y=177. This distinguishes the native-looking top strip from the correctly proportional rendered portrait. It does not alone prove whether that area is outside the DOM viewport.

Current implementation has `viewport-fit=cover`, standalone manifest, `apple-mobile-web-app-status-bar-style=black-translucent`, a fixed inset-zero welcome and an absolute proportional-cover portrait. Welcome panel/html/body use **#b7e7ed** only once welcome is active. The pre-framework `StartupHead`, startup fixed overlay, document theme meta and manifest theme/background still use **#faf5ec**. That exact colour correspondence is evidence worth testing, not proof of a root cause.

## Established platform guidance

[WebKit's safe-area guidance](https://webkit.org/blog/7929/designing-websites-for-iphone-x/) explicitly supports extending visual backgrounds to display edges while selectively padding important content away from sensor housings/home indicators. Unsafe areas are not forbidden to images. Safe insets describe geometry, not contrast: making a button blue does not make an occluded button operable.

[Apple's archived meta-tag reference](https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariHTMLRef/Articles/MetaTags.html) documents `black-translucent` for drawing below the status bar. It is older guidance, not a guarantee about iOS 27.0.1 native presentation.

The supplied [2021 article](https://itnext.io/make-your-pwas-look-handsome-on-ios-fd8fdfcd5777) combines these settings with `min-height:calc(100% + env(safe-area-inset-top))` and four-sided padding. Its author describes developing the height workaround on iOS 12. Applying that whole-document workaround does not establish a solution for the present screenshot; extra document height cannot necessarily paint outside a native web view, and can introduce unwanted scroll.

## Current primary-source evidence

- [WebKit bug 301994](https://bugs.webkit.org/show_bug.cgi?id=301994) remains **REOPENED**. Later comments describe a standalone-only native strip and reduced DOM viewport. A WebKit maintainer confirmed recurrence on **iOS 27 beta** on 4 August 2026. This is materially newer evidence than the article. It does not prove that the same defect affects this device's 27.0.1 build.
- [WebKit bug 316008](https://bugs.webkit.org/show_bug.cgi?id=316008), reported June 2026 and still **NEW**, reports incorrect `vh`/`lvh` sizing when a Home Screen app was installed without the translucent status metadata. The reporter says later metadata updates do not update the installation. This is a reporter claim with a supplied testcase, not a confirmed universal requirement to reinstall. DiaperScout's earlier deployment used `default`, so retained installation behavior is a plausible second factor.
- [WebKit bug 305546](https://bugs.webkit.org/show_bug.cgi?id=305546) describes native status-background changes after soft navigation. Its linked [draft PR 60377](https://github.com/WebKit/WebKit/pull/60377) is open, not a shipped fix. The code proposal resets stale scroll tracking after back/forward-cache restoration. Blazor enhanced navigation warrants explicit testing, but this evidence does not equate all enhanced navigation with a BFCache restoration.
- [Current WebKit LocalFrameView.cpp](https://raw.githubusercontent.com/WebKit/WebKit/main/Source/WebCore/page/LocalFrameView.cpp), in `fixedContainerEdges`, samples fixed containers and can prefer a previously recorded edge colour when a new container is viewport-sized. This gives a specific mechanism compatible with DiaperScout's full-screen cream startup being replaced by the full-screen blue welcome. The current main branch is not proof of the exact engine binary on the phone.

Safari 27's published WebKit feature notes were checked; no specific resolution of the native-strip issue was found there. Community claims about mandatory blur or deprecated tags were not treated as an established fix.

## Working diagnosis and next validation

The strongest application-related hypothesis is **native edge-colour sampling retains startup cream**, potentially compounded by an older installed icon retaining status-bar configuration. The screenshot's exact cream match supports this hypothesis. It is not yet a proved defect in CSS padding, Blazor, service-worker caching or infrastructure.

Use Safari remote Web Inspector on the actual installed app to capture only non-sensitive geometry/settings: `screen.height`, `innerHeight`, `documentElement.clientHeight`, `visualViewport.height/offsetTop/scale`, device-pixel ratio, computed top/bottom safe insets, computed html/body/welcome background colours, welcome bounding rectangle and the status-bar meta value. Capture once during static startup and once after readiness. No auth cookies/tokens, protected state or contribution draft data are needed.

Compare a genuinely fresh, separate test installation against the existing installation without deleting the current app or its drafts. A controlled non-production A/B should compare blue root background from first paint versus a cream-to-blue change, and replacement of full-screen fixed startup/welcome panels. This tests colour sampling separately from viewport reservation. If the native strip is outside the measured viewport, do not keep adding CSS height or negative offsets to the production welcome. Prefer matching its sampled background with a scoped, first-paint approach once proven, preserving startup readiness and recovery.

A complete four-sided control-inset check is also needed in landscape: the welcome currently has fixed 18px horizontal card spacing. This is separate from the portrait top-strip issue and should not be confused with its cause.

No installed iOS simulator test was possible here: `xcrun simctl` is unavailable. Playwright WebKit and manually emulated standalone/safe insets cannot reproduce the native Home Screen container or establish install-time settings.
