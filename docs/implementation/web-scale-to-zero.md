# Web scale-to-zero policy — 2026-10-02

The user chose to restore Web scale-to-zero after the proven Blazor routing fix. **Sticky cookie affinity remains the correctness fix**; shared Data Protection protects cross-replica descriptors/authentication but does not share live circuit state. This supersedes the warm operating policy in [the reliability report](pwa-production-reliability.md), whose successful two-replica and warm one-replica results remain historical evidence.

| Setting | Current Web policy | Warm rollback |
|---|---|---|
| Minimum replicas | 0 | 1 |
| Maximum replicas | 2 | 1 |
| Cooldown | 600 seconds | 300 seconds |
| Polling interval | 30 seconds, unchanged | unchanged |
| Scale rules | `null`: existing default HTTP rule, target 10 | unchanged |
| Revision mode | Single | Single |
| Ingress affinity | sticky | sticky |

Max2 bounds burst scale-out while allowing modest headroom at current low traffic. The previous investigation recorded low per-replica CPU/memory and unnecessary scale-out to four instances from the default max10. Two actual replicas already passed the routing regression matrix with affinity. This cap is not a claim of load-tested capacity or high availability. Idle shutdown saves warm compute but restores Web cold-start latency. API cold-start latency remains independent; API min0/max10 is untouched. Active connections/traffic can delay zero; cooldown is not a circuit persistence guarantee and affinity cannot move an in-memory circuit after its replica exits.

## Deployment and preservation

Applied through `bash ops/apply-web-reliability-policy.sh`, using the supported stable ARM `2025-07-01` JSON Merge PATCH API because the installed ACA CLI has no cooldown flag. The script reads the live template using the same API, changes only min/max/cooldown and the required fresh revision suffix, and retains all other template fields. It omits non-versioned configuration, secrets, identity and networking from PATCH. It requires Single mode, ensures sticky affinity, waits a bounded time for revision readiness, and avoids creating a revision when the policy already matches.

Revision: `diaperscout-web-vnet--scale-policy-20261002092936`.
Image unchanged: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:5e86abc43fb532bd0ba5c90371c657683d951fe8f9787ff99e2e4438879a569f`.

[Before/after preservation checks](web-scale-to-zero-evidence/preservation.json) compare identical projections: all checks pass for Web configuration (including HTTPS-only ingress, custom-domain certificate, affinity, registries and secret references), identity, environment/profile, and every other template field. API configuration/template/identity/environment/profile also match exactly. Shared Data Protection application isolation, Blob/KV key references and managed identity are unchanged. No changes were submitted to API, VNet, PostgreSQL, Private Link, DNS, TLS, Blob, Key Vault or role assignments. Application code, startup artwork and conservative service-worker caching are unchanged.

ARM accepts `scale.cooldownPeriod=600`; [Microsoft's schema](https://learn.microsoft.com/en-us/rest/api/resource-manager/containerapps/container-apps/update?view=rest-resource-manager-containerapps-2025-07-01) exposes this field. [Scaling behavior](https://learn.microsoft.com/en-us/azure/container-apps/scale-app) distinguishes final-replica cooldown from other scale-in behavior. This is an approximately ten-minute idle policy, not an exact shutdown deadline.

## Validation

Azure recorded an actual KEDA `1 to 0` deactivation at09:45:16.2188031UTC and a fresh replica scheduled at09:47:04.4101612UTC, before the browser probes. See [idle scale events](web-scale-to-zero-evidence/idle-scale-events.json). The first zero transition occurred about12 minutes after the only two requests recorded in the09:33 minute; metric granularity and scaler cadence do not identify an exact600-second timer. The600-second field is persisted, and actual idle shutdown is proven.

The warm Chromium standalone journey passed, retained `acaAffinity` (Secure/HttpOnly/SameSite=None), and used shared key `3ab78896-4823-4ed8-bca6-8809146fbb0b`. It exercised Scan lookup, unsent proposal steps, interaction after35 seconds, passkey sign-in navigation and Atlas; no API write/authentication/submission was performed. See [browser evidence](web-scale-to-zero-evidence/production-warm-chromium.json).

The instrumented WebKit cold probe is pending the next zero observation. [PWA regressions](web-scale-to-zero-evidence/regression.log):11/11 passed. [Policy tests](web-scale-to-zero-evidence/policy-tests.log):4/4 passed, covering narrow template changes, idempotence, rollback and non-Single refusal. [Release build](web-scale-to-zero-evidence/release.log):zero warnings/errors. Bash syntax and browser-harness syntax also pass. The script was also run against production a second time, with [the same ready revision](web-scale-to-zero-evidence/policy-idempotent.json). Tests deliberately avoid application requests during the idle observation; control-plane reads do not keep Web warm. Browser validation will use real SignalR actions and WebSocket frame evidence, not just an HTTP200 or a hidden splash. Physical iPhone iOS27.0.1 standalone acceptance remains a user-device check; desktop WebKit with iPhone emulation and `navigator.standalone` is an approximation.

## Rollback

```bash
bash ops/apply-web-reliability-policy.sh --rollback-warm
```

This restores the proven **min1/max1 + sticky**, cooldown300s, retaining the existing rule/polling interval and the current verified image. Verify the newly ready revision reports those bounds and affinity, and repeat interactive smoke. It leaves API and shared keys unchanged. To restore scale-to-zero later, run the script without arguments. Normal Web image-only deployments must preserve this policy and affinity.
