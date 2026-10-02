# Production PWA reliability — 2 October 2026

## Outcome and scope

The proven multi-replica routing defect is remediated with ACA cookie affinity. The corrected image passed eight real interactive journeys with **two** Web replicas, including both replicas hosting circuits. The final operating policy is one warm Web replica for the current commercial-validation stage. Final one-replica acceptance also passed8/8 journeys; all captured configuration-preservation checks pass.

Three application reliability gaps were fixed: startup depended on body-end JavaScript for visibility; Atlas buffered its initial document behind API loading; proposal controls accepted input before hydration/draft restoration and could erase that input. Circuit rejection now presents an explicit recovery choice rather than automatically discarding the page.

This does **not** establish physical iPhone acceptance. The iOS27.0.1 black-screen symptom remains classification **C: insufficient physical evidence to distinguish native pre-document launch, document/asset delay, and circuit failure**. Static document startup defects were independently reproduced and corrected. No new product feature, database migration or paid resource was introduced.

## Evidence inspected before changing production

Read [Scan/Observation/Atlas report](scan-observation-atlas.md), its retained [WebKit transport failure](observation-atlas-evidence/production-webkit-transport-failure.json), previous iOS/PWA investigation and app-experience reports, shared Web Data Protection report/implementation, deployment strategy/operations, document/startup/manual Blazor/enhanced navigation/reconnect/passkey/service-worker code and deployment/provisioning scripts.

Live read-only baseline: Web `diaperscout-web-vnet--app-icon-f9673ac`, Single revision,100% latest traffic, one ready replica/no restart,0.5vCPU/1GiB. External Auto ingress8080, HTTPS only, unchanged diaperscout.app certificate. Effective Web min0/max10, no explicit rule, no affinity. API internal ingress, min0/max10/default HTTP scaler. Configuration/metadata are retained in [evidence](pwa-reliability-evidence/); credentials are redacted. The original root-cause/proposal/cost/rollback was committed as8cf8032 **before production-affecting changes**. The latest user task explicitly authorized justified reliability infrastructure remediation.

### Why four Web replicas existed

This was actual HTTP autoscaling, not a minimum of four, customer count, CPU/memory scaling or a guess about platform maintenance:

- System events say `http-scaler` changed the observations revision to3 at15:11:39 UTC and4 at15:11:55 on1 October.
- Metrics show856 completed Web requests in minute15:12 around the recorded browser smoke burst. Pages load multiple assets and create circuits; HTTP demand differs from number of people.
- Replicas remained4 through the stabilization period. Events report “all metrics below target”:3 at15:19,2 at15:21,1 at15:23.

See [events](pwa-reliability-evidence/scale-events.json) and [metrics](pwa-reliability-evidence/metrics.json). No explicit rule means ACA's default HTTP rule: effective min0/max10, concurrency target10; default scale-down stabilization/cooldown300s. The one-minute request metric does not expose precise sub-minute scaler input or the origin of every request. No exact concurrency calculation or assertion that all856 requests were our test is made. Explicit rescale events establish the trigger. Burst load and subsequent scale-in were unnecessary for this project's current stateful Web operating model.

Measured transient working sets were approximately160–250MB and the sampled per-container CPU peak approximately0.209vCPU against0.5 allocated. This limited evidence supports the present single-replica decision, not a general capacity guarantee.

## Root causes and supported remediation

### In-memory connection state was routed without affinity

The retained failure at15:15:51 occurred during the four-replica period: WebSocket failed; `/_blazor`404; LongPolling “No Connection with that ID”; then “Circuit host not initialized”. SignalR negotiation creates connection state on a particular process. Blazor component/circuit state also belongs to that process. With multiple replicas and no affinity, related HTTP/transport/reconnect requests can reach a different process.

A deterministic new regression runs two independent Kestrel Web hosts with mutually readable protected data. A connection negotiated on A gets WebSocket404 on B using the same token, and opens successfully on A. This separates shared cryptography from in-memory routing and reproduces the captured symptom without manufacturing more production failures.

Microsoft explicitly requires affinity for Blazor Server on ACA. ACA cookie affinity is supported for this Single-revision/HTTP app. It covers negotiation, fallback transports and reconnect routing, but is best effort if a replica disappears. A backplane would not move Blazor component memory or eliminate affinity requirements; there is no application broadcast-hub requirement here. No Redis/Azure SignalR/new infrastructure was added.

The physical phone's red banner alone does not prove its precise exception without remote Inspector logs. Remediation is justified by the independently captured routing defect, supported requirements, local two-host reproduction and successful live two-replica validation.

### Proposal input was overwritten during hydration

The first real two-replica matrix passed4/8 journeys. All negotiations200 and WebSockets carried frames; there were no connection-ID failures or browser page errors. Four failures were at Continue-to-pack.

A single bounded [production diagnostic](pwa-reliability-evidence/proposal-hydration-diagnostic.json) identifies the separate cause: brand input was filled at964ms and changed at969ms; by996ms the brand DOM value was empty while the product input was being filled. The form then reported the brand/name missing. Global startup readiness was already true from Scan, but the inserted proposal component had not completed hydration.

Proposal inputs/step/submit controls now remain disabled until the first interactive render completes its asynchronous draft restoration, then enable immediately. Tests hold both framework and draft-module loading independently and verify entered brand/product survive subsequent transitions. All existing validation, authentication, moderation and canonical submission behavior is preserved. No delay/retry or draft-storage feature was added.

### Startup visibility and Atlas response buffering

Explore is static and does not await the API. Startup markup was already static, but CSS hid it until body-end app.js set standalone detection, after framework loading. Both Chromium and WebKit reproduce the hidden old startup while the framework script is withheld. With external CSS absent, the old document also lacks the critical cream background/layout.

A small `StartupHead.razor` precedes external resources: standalone detection plus critical cream/light/startup CSS. Guide/logo/title remain static, independent of circuit success. `Atlas.razor` now streams its initial document while its API request is pending; Products already streamed. A held-API regression verifies branded first content is visible before releasing the API, then transitions to real interactivity without a permanent cover.

Manual Blazor startup still waits for DOMContentLoaded and complete server component state, and readiness still comes from actual circuit opening on interactive pages. The chrome observer safely handles temporarily absent main content during streaming. Enhanced navigation restores app attributes/chrome. Existing8s slow/45s failure disclosure and explicit startup reload remain; there is no fake percentage or artificial delay.

Production Chromium/WebKit checks with a dark preference and intentionally unexecuted framework/app scripts show cream/light styling and visible Guide/brand. [WebKit screenshot](pwa-reliability-evidence/production-startup-webkit.png), [Chromium screenshot](pwa-reliability-evidence/production-startup-chromium.png), [static results](pwa-reliability-evidence/production-static-startup.json). This proves document behavior, not iOS's native launch screen before HTML arrives.

### Honest recovery and observability

Reconnect rejection no longer automatically reloads, and visibility changes no longer add app-level retry loops. Failed/rejected/resume-failed sessions disclose the state and offer explicit Retry/Resume/Reload. A rejected session warns that unsaved changes may be lost. Framework bounded reconnect behavior is retained; failures/red UI are not hidden.

Production startup logs successful shared-key readiness. A scoped circuit handler logs connection up/down with a12-hex SHA256 fingerprint of the random circuit ID; ACA supplies replica/revision metadata. No raw circuit/reconnect identifier, connection token, user identity or form data is logged. This provides replica evidence using the existing logging workspace.

No hub timeout overrides exist: framework defaults apply (15s keepalive/handshake,30s client/server timeout). ACA HTTP ingress documents240s request timeout and WebSocket support. The conservative SW navigation response deadline20s is unchanged. None of these were raised to mask a routing defect.

## Before/after configuration and deployment

| Setting | Before | Bounded validation | Final policy |
| --- | --- | --- | --- |
| Web replicas | effective0–10 |2–2 |1–1 |
| Web HTTP rule | default target10 |same |same, max1 prevents scale-out |
| Web affinity |none |sticky |sticky |
| Revision mode/traffic |Single/latest100% |same |same |
| Web CPU/memory |0.5vCPU/1GiB |same |same |
| API replicas/ingress |0–10/internal |unchanged |unchanged |
| Web DP/identity/network/domain |existing configuration |unchanged |unchanged |

Enable affinity as the correctness fix, then validate with two actual processes **before** adopting max1. One warm Web avoids Web scale-to-zero launch latency and request-burst scale-in terminating contributor circuits. Current load does not justify multiple always-on servers. This deliberately accepts single-instance availability/capacity limits; it is not high availability. Affinity is retained for supported future scale and temporary platform overlap. ACA replica targets are not absolute guarantees during maintenance.

Azure mutations were limited to:

1. Web ingress sticky-sessions set tosticky.
2. ACR Web builds/immutable Web image updates and bounded Web2/2 validation revisions.
3. Final Web min1/max1 using the identical corrected image.

Source8cf8032: ACR db15/digest`sha256:1a5b1287d0175d4888484e05871f7202d137b926562a9fc9cae4cea656b7a635`, revisionreliability-2-8cf8032. This version exposed the proposal race during validation.

Corrected source2b7e35f: ACR db16/digest`sha256:5e86abc43fb532bd0ba5c90371c657683d951fe8f9787ff99e2e4438879a569f`, validated revisionreliability-2-2b7e35f; final revisionreliability-1-2b7e35f. Source/app code was fully tested before each image. Deployment used the established Web-only ACR/immutable-image process. No migration/API deployment occurred.

`ops/apply-web-reliability-policy.sh` records a repeatable Web-only1/1+sticky policy, refuses non-Single mode, and preserves images/env/secrets/identity/networking. Normal image-only deployments preserve the operating policy.

### Data Protection and preserved infrastructure

Existing SystemAssigned Web MI, application isolation `DiaperScout.Web.Production`, Blob `web-production/keys.xml`, versionless Key Vault `web-production`, scoped Blob Data Contributor/KV Crypto Service Encryption User grants and managed-identity-only credential provider remain unchanged. Both corrected validation replicas independently logged protect/unprotect readiness before listening; all fresh protected descriptors use verified shared key`3ab78896-4823-4ed8-bca6-8809146fbb0b`. Existing Blob encrypted-key storage, no shared-key/anonymous access, HTTPS/TLS minimum and Key Vault RBAC/purge protection remain.

The CLI identity's Key Vault key-read attempt returned ForbiddenByRbac. No grant was added or bypassed. The Web MI's real startup checks and common protected descriptor key establish operational ring read/unwrap health; successful use of an existing key does not claim a new key was written at every startup. Existing encryption/rotation configuration and scoped write permission are preserved.

API image, min0/max10, configuration/secret references and ingress are compared after deployment. PostgreSQL FQDN remains`diaperscout-prod-pg.postgres.database.azure.com`; its pre-existing network settings and approved Private Link/subnet remain unchanged. The existing VerifyFull connection secret reference is not changed. No database/secret/network/TLS mutation was made. Web VNet/environment/certificate, DP storage/vault/roles are compared before/after. All15 captured before/after preservation assertions pass; see [comparison](pwa-reliability-evidence/configuration-preservation.json). Connection secret contents were not retrieved; the existing API connection reference is identical and no API/secret mutation occurred.

Service worker remains network-authoritative for navigation and caches only its sealed nine static/offline assets, versionv2. No dynamic documents, API/auth/circuit data are cached. Authentication/passkey implementation, observation security and catalogue services are unchanged.

## Verification and retained failures

Full final suite: **266 passed**,215 integration/API/browser +51 Domain;0 failed/0 skipped. Release: **0 warnings/0 errors**. Diff check clean. [Final tests](pwa-reliability-evidence/tests-final.log), [Release](pwa-reliability-evidence/release-final.log).

Production log review found only the two pre-existing hosting warnings, also present on baselinef9673ac: Docker `ASPNETCORE_URLS=http://+:8080` overrides the base-image HTTP_PORTS setting; HTTPS redirection middleware cannot discover a local HTTPS port behind ACA TLS termination. Existing ingress remains HTTPS-only/certificate unchanged. Neither is a circuit/key failure; no fail/unhandled/cryptographic error headers were observed for the corrected revisions. These warnings were explained and retained in [runtime evidence](pwa-reliability-evidence/explained-existing-runtime-warnings.json), rather than suppressed or addressed through unrelated TLS/proxy changes.

Coverage includes static startup before scripts/styles/circuit, Chromium/WebKit standalone, held API/Atlas streaming, explicit lost-session behavior, proposal hydration/draft restoration, isolated genuine auth continuation/submission/observation/Atlas flows, passkeys, conservative worker caching/offline and encrypted shared DP/application isolation. Production simulation does not prove physical iOS camera/passkey/background behavior.

Live matrix: two rounds ×Chromium/WebKit ×browser/simulated standalone,8 sessions per operating configuration. Each traverses Explore→Scan→unknown lookup→proposal product/pack/review→existing sign-in→Atlas→Scan lookup. Each waits at least35s with live WebSocket frames before another server-handled action. No authentication completion or final submission/observation save is performed. Diagnostic values remain in disposable client forms/session drafts; **zero production data writes**. Affinity cookies are Secure/HttpOnly; only value hashes are retained. Cookie hashes are opaque and are not used to count/map replicas.

- Corrected2/2 matrix: **8/8 passed**,08:17:35–08:23:05 UTC. All negotiations200, no failed transport/page errors/red UI, all dwell intervals≥35001ms. Both replicas hosted logged circuit connections and independently passed DP readiness. Descriptors share the expected production key. [Results](pwa-reliability-evidence/production-two-replicas.json), [circuit logs](pwa-reliability-evidence/final-two-circuit-logs.json), [replicas](pwa-reliability-evidence/final-two-replicas.json), [per-pod traffic](pwa-reliability-evidence/final-two-pod-requests.json).
- Final1/1 matrix: **8/8 passed**,08:29:21–08:34:24 UTC. All negotiations200, no failed transport/page errors/red UI, real form transitions after≥35s, shared key unchanged. Final revision Healthy/Provisioned,100% traffic,1 ready replica/0 restarts. [Results](pwa-reliability-evidence/production-one-replica.json), [key/circuit logs](pwa-reliability-evidence/final-one-circuit-logs.json), [revision](pwa-reliability-evidence/final-revision-health.json), [replica](pwa-reliability-evidence/final-one-replica.json).

Retained failures were addressed explicitly, not removed or rerun until a lucky pass:

- Original four-replica production404/connection-ID evidence remains in the previous report.
- First smoke harness used an assertion helper absent from bundled Playwright core;8 attempts stopped on Explore before circuit testing. Corrected harness, retained [failure](pwa-reliability-evidence/production-two-replicas-harness-failure.json).
- First real matrix4/8 failure led to the diagnosed hydration fix; retained [journeys](pwa-reliability-evidence/production-two-replicas-first-journeys.json) and input trace.
- New artificial draft-module interception initially missed fingerprinted URLs and later worker-owned requests. Two failed full-suite logs are retained. Native routing now matches fingerprints and blocks workers **only in this artificial gate test**, per Playwright guidance; worker-enabled regression/production checks remain enabled. The final full suite is clean.
- First WebKit early screenshot timed out on fonts.ready because the test deliberately pauses document completion. Capture-only font waiting was disabled using the bundled screenshotter option; application fonts are untouched. [Capture failure](pwa-reliability-evidence/production-static-startup-capture-failure.json) remains; corrected early startup captures pass both engines.
- Pre-fix startup/streaming/proposal tests fail for their expected causes; baseline regression logs are retained.
- Initial configuration comparison used mismatched projections: the baseline selected four container fields, while final raw containers additionally exposed imageType/default probes. Matching the same baseline projection shows all captured settings unchanged; the initial schema-mismatch result is retained. No API or resource/env field changed.

### Cost, limitations and future scale

One warm Consumption Web0.5vCPU/1GiB can incur idle/active compute where Web previously reached0. Idle discount depends on real CPU/network/HTTP activity; active WebSocket sessions may incur active usage. It is not guaranteed free. This avoids unnecessary persistent extra replicas after bursts. The temporary2/2 target ran approximately07:38–08:25 UTC (including investigation, tests and rolling transitions), then settled1/1. It adds temporary compute; no new resource/backplane/always-on API was created. Monthly cost depends on usage/grants; no unsupported price estimate is given. Existing logging receives a small number of lifecycle events.

API still scales to0 and can delay the first barcode/Atlas backend request. Warm Web/static startup and normal loading states do not pretend the API is already awake. Deployments, process loss, prolonged background suspension and network loss can still end an in-memory circuit. Explicit recovery is honest; unsaved pre-auth work is not claimed durable. Saved production records are persisted by existing database paths; auth continuation draft lifetime remains30 minutes.

Before genuine multi-replica scale, measure concurrent circuits, CPU/memory and latency under representative load. Keep Single mode/affinity/protected shared keys, deliberate capacity/HTTP threshold, and controlled scale-in/deployment draining. Durable distributed component/draft state must be designed/tested before claiming seamless replica replacement. Redis alone does not move circuits. Review capacity/HA when real usage warrants it.

## Rollback

No DB rollback/migration is required. For an application regression, restore previous immutable Web image`sha256:a4cf752d97de970779350e66114512d911772f945171f9b7990a965be578f647` with min1/max1 and sticky retained. Confirm ready/100% traffic; recheck representative circuit/auth journeys. API, database, DP and networking remain untouched.

Exact baseline rollback to min0/max10/no affinity is technically possible, but reintroduces the proven routing defect and is not a reliability solution. Image rollback restores old startup/auto-reload/proposal race behavior; use only when its tradeoff is justified.

## Simon's physical iPhone27.0.1 acceptance

1. Force-close the installed PWA, then cold-launch its Home Screen icon; verify Guide/“Preparing your map…” instead of black. If black remains, record whether it precedes all web content and capture remote Inspector timing/error if available.
2. Open Scan and scan a real physical pack barcode; confirm camera/lookup works.
3. Known pack: open its exact product/pack and Record where found. Unknown pack: Add product and enter only genuine pack information; confirm fields/steps preserve it.
4. Authenticate with the existing passkey/sign-in flow and confirm the intended pack/proposal and draft return intact.
5. Submit the first genuine observation at a real public shop, or genuine unknown-pack proposal; wait for its actual saved/review receipt. Do not submit diagnostic/fake data.
6. For a saved observation, open Atlas and verify its real place/pack/date. Unknown proposals require moderation and do not immediately create Atlas observations.
7. Background/reopen promptly, then repeat after several minutes; check reconnection or explicit recovery rather than silent loss/loops. A stopped/expired circuit is not guaranteed to retain unsaved work.
8. Close/reopen and confirm the genuinely saved record persists. Report black screen/red UI and first remote console error if either remains.

## Primary references

- [ACA .NET Blazor affinity requirement](https://learn.microsoft.com/en-us/azure/container-apps/dotnet-overview)
- [ACA affinity eligibility/best-effort semantics](https://learn.microsoft.com/en-us/azure/container-apps/sticky-sessions)
- [ACA default scaling/stabilization](https://learn.microsoft.com/en-us/azure/container-apps/scale-app)
- [ASP.NET Core10 Blazor SignalR requirements](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0)
- [SignalR same-process requirement](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0)
- [Streaming rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/rendering?view=aspnetcore-10.0)
- [ACA ingress timeout/protocols](https://learn.microsoft.com/en-us/azure/container-apps/ingress-overview)
- [Consumption billing](https://learn.microsoft.com/en-us/azure/container-apps/billing)
- [Playwright interception/worker limitations](https://playwright.dev/dotnet/docs/network#missing-network-events-and-service-workers)


## Git delivery

Application commits8cf8032 and2b7e35f are local onfix/product-submission-recovery. GitHub SSH push returned Permission denied(publickey): the SSH agent has no identities and Keychain has no stored identity. No credential was requested/read in chat. Simon was asked to unlock locally with `ssh-add --apple-use-keychain ~/.ssh/id_ed25519`. Final report/evidence are committed locally; push remains pending that authentication step. Unrelated docs/baselines work was left untouched.
