# Production PWA reliability — 2 October 2026

## Investigation and proposal recorded before production changes

Starting source: f9673ac, branch fix/product-submission-recovery. Read the Scan/Observation/Atlas report and retained transport failure, PWA/startup reports, shared Data Protection implementation/report, deployment strategy, deployment/operations, current document/startup/worker/manual Blazor/reconnect/passkey code. The user explicitly authorizes justified reliability infrastructure remediation in this task.

### Live baseline and cause

Web latest/ready revision `diaperscout-web-vnet--app-icon-f9673ac`, Single revision mode, 100% latest traffic, one currently ready replica (no restart). Resources 0.5 vCPU/1 GiB. Web minReplicas omitted (effective 0), max10, no explicit rule (default HTTP concurrency target10), polling30s/cooldown300s, no sticky sessions. External Auto HTTP ingress port8080, HTTPS only, existing custom domain/certificate. API internal ingress, min0/max10/default HTTP rule; its configuration must remain unchanged. Baseline JSON is retained in pwa-reliability-evidence; credential values are redacted.

Four was real autoscaling, not four customers or a minimum of four. Platform events explicitly say **http-scaler** changed the observations revision to3 at15:11:39 UTC and4 at15:11:55 on1 October. Metrics show856 requests in minute15:12 during the recorded browser smoke burst, four replicas through15:19, then events “all metrics below target”:3 at15:19,2 at15:21,1 at15:23. Browser tests load many assets and circuits; the scaler counts HTTP demand, not humans. One-minute metrics do not expose the precise sub-minute concurrency input, so no exact concurrency or attribution of every request to the test is claimed. The default threshold10 and stabilization explain why a small test burst creates extra replicas that persist after the burst. This was not CPU/memory scaling or platform maintenance: the explicit http-scaler events identify the trigger.

The retained WebKit failure at15:15:51 fell within the four-replica period: negotiate/transport sequence followed by WebSocket failure and LongPolling404 “No Connection with that ID”, then “Circuit host not initialized”. Connection IDs and circuits belong to a server process; shared cryptographic keys cannot supply another process's in-memory connection. No affinity allows negotiate and transport/reconnect requests to reach different replicas. This is a supported-architecture violation and explains the captured failure. The physical phone's later red banner alone cannot prove its exact exception without a remote trace; fixing this independently proven routing defect is justified.

Data Protection baseline still uses SystemAssigned MI, isolated `DiaperScout.Web.Production`, protected Blob keys.xml and versionless Key Vault wrapping key. Every non-development Web process does a protect/unprotect readiness roundtrip **before listening**. A ready replica therefore establishes cloud key-ring read/unwrap capability, not just process liveness. Additional cross-process proof will use protected SSR descriptor key IDs and ready replicas during the bounded two-replica validation. No cloud permissions/key material will be changed or exposed.

### Supported decision and rollout

Enable ACA cookie affinity on this Single/HTTP Web app. Microsoft specifically requires it for Blazor Server on ACA; it covers HTTP negotiation, fallback transports and reconnect. It is best effort if a replica disappears, not circuit migration. No broadcast hub requirement exists here: adding a Redis backplane does not distribute Blazor component memory or remove affinity requirements.

Operate this commercial-validation Web at **min1/max1**, preserving0.5vCPU/1GiB, existing image-only process and all other settings. Existing measured working sets ~160–250MB and modest CPU during the burst do not justify multiple servers for this stage. One warm Web avoids scale-to-zero launch latency and request-burst scale-in killing in-memory contributor circuits. Affinity remains enabled as a correctness requirement for rolling overlap, maintenance and future expansion; max1 alone is not the routing fix. This accepts single-instance availability/capacity limits; no claim of high availability. API remains min0.

Before settling at1/1, deploy the tested image briefly at **min2/max2 with affinity**, run a bounded Chromium/WebKit browser/standalone interactive matrix without saving data, record both ready replicas, affinity-cookie presence (never values), protected descriptor key IDs and per-replica request evidence. This deliberately validates the routing fix with two processes, rather than hiding the bug behind max1. Then settle at1/1 using the same immutable image and validate again. Any failures are retained; no rerun-until-green. No production load-generation test.

Cost: one warm Consumption Web0.5vCPU/1GiB can incur idle/active compute charges where previously Web could reach0; this is the deliberate launch/session reliability tradeoff. It avoids sustained extra replicas after test bursts. Brief second-replica validation adds only its bounded compute time. No new paid resource, Redis, backplane or API always-on cost. Monthly cost is usage/grant dependent; no unsupported price estimate.

Future: before increasing max, measure concurrent circuits, CPU/memory and latency under representative load; keep Single mode, affinity and protected shared keys. Use a deliberate HTTP threshold rather than current default10, minimum capacity and controlled scale-in/deployment draining. Affinity cannot preserve lost in-memory circuits; distributed persisted component state/draft recovery must be designed/tested before claiming seamless replica replacement. Redis alone is insufficient. Framework reconnect retention is finite, and deployment/restart may still require explicit reload.

Rollback: restore immutable Web image `sha256:a4cf752d97de970779350e66114512d911772f945171f9b7990a965be578f647` with min1/max1 and sticky retained if application regression. There is no DB migration. Reverting to original min0/max10/no affinity reintroduces the proven defect and is only an emergency exact-baseline rollback, not a reliability solution. API, database hostname/VerifyFull TLS, Private Link/VNet, DP Blob/KV/roles and auth secrets remain untouched.

### Black screen — current classification C

No physical Web Inspector/first-paint timeline is available. Cannot distinguish native iOS pre-document launch blackness from document/asset delay or circuit failure. Explore is static and does not await the API. The manifest already specifies cream background/theme and conservative network-first navigation; no new dynamic caching is justified.

A separate application-side gap is definite: startup markup is static, but visibility depends on `data-standalone` set by body-end app.js, after the framework script. Default CSS hides it before that script. Plan: detect standalone in a tiny head script and inline only critical cream/light/startup layout, keeping Guide/brand static. Test in WebKit/Chromium with framework/app scripts withheld and external CSS unavailable, plus held API response on an already streaming catalogue route. Preserve DOMContentLoaded manual-start ordering, circuit readiness gating and existing8s/45s honest slow/failure handling. Atlas should stream its initial document while awaiting API. No fake progress or automatic reload loop.

Reconnect currently reloads automatically on rejected circuit and retries on visibility changes. Replace destructive automatic reload with explicit honest lost-session/reload UI; user-triggered retry/resume remains. Keep normal bounded framework reconnect attempts. Do not disguise red error UI.

## Primary references

- [ACA .NET overview, Blazor requirement](https://learn.microsoft.com/en-us/azure/container-apps/dotnet-overview)
- [ACA affinity eligibility and best-effort semantics](https://learn.microsoft.com/en-us/azure/container-apps/sticky-sessions)
- [ACA default scaling and stabilization](https://learn.microsoft.com/en-us/azure/container-apps/scale-app)
- [ASP.NET Core10 Blazor SignalR affinity](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0)
- [ASP.NET Core10 SignalR same-process requirements](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0)
- [ASP.NET Core10 streaming rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/rendering?view=aspnetcore-10.0)

## Implementation, verification and physical acceptance

In progress. This proposal was saved before any production-affecting change.

Local causal checks: with the head startup fix removed and Atlas streaming removed, the new tests fail3/4 (both engines lose the critical background/layout; Atlas response headers time out while API is held). Existing Products streaming passes. With fixes all7 initial targeted cases passed. With actual CSS, both Chromium and WebKit explicitly report the old startup as hidden while the framework script is withheld. All4 early cases fail against the previous document and pass with the fix. Full regression:264 passed (213 integration/browser +51 Domain),0 skipped. Release:0 warnings,0 errors. git diff --check clean. Failure logs are retained as intentional pre-fix regressions, not unexplained production failures. Baseline failure logs are retained.

The CLI account has no Key Vault key data-plane read grant (ForbiddenByRbac). No permission was added. Web MI readiness and cross-replica SSR key IDs are the authoritative operational verification; CLI key-list access is not required or silently bypassed.


### Deployment checks before mutation

Changes are limited to static critical startup head, streamed Atlas initialization, guarded chrome observer, explicit reconnect failure UI, a non-secret successful key-ring readiness log, regression tests, a Web-only policy script and diagnostic reports/smoke script. Service worker, authentication/passkeys, canonical catalogue and observation/submission services are unchanged. Private Link, database network/FQDN, API image/scale/env/secret references and existing Web identity/keys were captured for after comparison. No new infrastructure resources are needed.


Timeout inspection: no application override for hub keepalive/handshake/client timeouts or disconnected circuit retention; framework defaults apply (15s keepalive/handshake,30s client/server timeout). ACA HTTP ingress documents240s request timeout and supports WebSockets. The existing SW navigation fetch has20s to receive a network response; the startup UI reports slow at8s and failed at45s. None were raised to hide routing failures. See [ACA ingress](https://learn.microsoft.com/en-us/azure/container-apps/ingress-overview) and [Consumption billing](https://learn.microsoft.com/en-us/azure/container-apps/billing). Idle reduced-rate eligibility also depends on actual CPU/network/HTTP activity; a warm Web is not guaranteed free.

The repository has no current Bicep/deployment configuration declaring Web replica limits; `ops/apply-web-reliability-policy.sh` records a repeatable Web-only policy and refuses non-Single mode. Image-only updates preserve it. Existing DP provisioning is preserved.


### First two-replica validation findings and justified follow-up

ACR db15 built source8cf8032 to immutable digest1a5b1287d0175d4888484e05871f7202d137b926562a9fc9cae4cea656b7a635. Web affinity enabled, image deployed briefly as reliability-2-8cf8032 with2/2; both replicas ready, zero restarts and independent successful managed-identity key-ring readiness logs.

The first smoke harness attempt incorrectly assumed bundled Playwright core exported @playwright/test expect. All8 attempts stopped on Explore before testing a circuit. Retained harness-failure JSON/log; corrected DOM assertions, not application or transport behavior.

The first actual journey matrix passed4/8. All negotiations200 and WebSockets carried frames; no page errors, connection-ID404, transport failure or red error UI. Four cases failed Continue-to-pack. These failures are retained in first-journeys evidence, not excluded from the report. Successful sessions used both ACA affinity cookie groups and the expected shared DP key.

A single bounded Chromium diagnostic then proved the distinct cause: input trace at964ms shows brand filled, change969ms; by996ms the brand DOM value is empty while the product is entered. The form then reports “Enter the brand and product name from the pack”, with no circuit error. Inputs were usable in prerendered enhanced-navigation HTML before component hydration finished. Global pwa readiness already true from Scan does not imply a newly inserted component is ready. This is not affinity failure.

Small additional application remediation: disable proposal inputs/step/submit controls until its first interactive render has completed async draft restoration, then enable immediately. Preserve all validation/security/canonical submission behavior. No delay, retries, new feature or auth change. New Chromium/WebKit tests hold framework loading and then draft-module loading, verify controls unavailable at both stages, then complete product/pack/review transitions with entered values preserved. Both fail before the fix (enabled prerendered controls); a new full suite/Release precedes replacement image deployment. Existing Scan already uses component readiness gating.


Cookie values are opaque: their hashes demonstrate presence/session tracking, **not a reliable mapping to a replica**. The first2-replica run generated4 distinct cookie-value hashes; it does not imply4 replicas. Per-pod Request metrics independently confirm traffic to both9vsng andvxnpb; protected SSR descriptors all use3ab78896-4823-4ed8-bca6-8809146fbb0b. For stronger final circuit proof, the follow-up image logs connection up/down with a12-hex SHA256 fingerprint of the random circuit ID and existing ACA replica/revision metadata. No raw reconnect identifier, connection token, user identity or form data is logged.

All11 targeted reliability cases now pass. The held draft-module regression must match fingerprinted JS URLs from the import map; an initially exact route failed to intercept the actual module and was corrected. New full266-case regression and Release validation are running before any replacement deployment. The confirmed production form failure and original harness failure remain retained separately from corrected final validation.


Follow-up suite failure explanation: the new WebKit module-gate test timed out despite pwa state ready and no browser/HTTP errors. Service-worker activation can take ownership of module requests before native Playwright routing intercepts them. An exact fingerprint pattern fix and document-completion wait alone did not make the gate reliable. The module-gate test now explicitly blocks workers, as [Playwright recommends for native request interception](https://playwright.dev/dotnet/docs/network#missing-network-events-and-service-workers). This applies only to the artificial interception test. Worker-enabled regressions and production smoke remain enabled, and production caching code is unchanged. Both failed suite logs are retained. No unsuccessful suite is described as passing.


Final follow-up validation before deployment: full266 tests passed (215 API/integration/browser +51 Domain),0 failed/0 skipped; Release0 warnings/0 errors; diff check clean. The module-gate tests are now deterministic while the real SW remains enabled in its own suite. Deployment will replace only the Web image on the temporary2/2 affinity configuration, then repeat all8 journeys on the corrected image before settling1/1.
