# Production retirement preparation — 10 October 2026

Status: **preparation only; Azure deletion approval pending**. Azure was queried read-only. No Azure deployment, stop, configuration change, deletion, secret-value read or database query was performed. The user accepts loss of the entire production PostgreSQL database/catalogue and requires no backup. This overrides historical recommendations to preserve the Atlas for this retirement only.

## Phase results

1. Live refs verified: main `2cf6e071442559660e7ad8d9c01209a914b997cb` was an ancestor of `fix/integration-browser-regressions` at `425bb728e6cefbff99fa5c94b517433b88cd0507`. [PR #1](https://github.com/simonlaurels/DiaperScout/pull/1) consolidated 60 commits/760 files using a merge commit, `5173d15ad0fe011a780cae75754c15696700148a`. No force push or branch deletion. Integration review examined final readiness/resource changes and verified exact-head local validation receipts: 361/361 integration twice, 67/67 domain, 58/58 related, zero-warning/error Release build. Two Cloudflare Pages checks passed; no tracked GitHub Actions workflows or Actions runs. User explicitly permits free Pages deployments. This was an integration review, not a fresh independent audit of every historical commit. Windows only; macOS unvalidated.
2. Live inventory and costs saved in sanitized JSON alongside this file. Networking and remaining configuration are recorded below. Rebuild instructions are in [rebuild.md](rebuild.md). No secret values committed.
3. Fresh local verification on the validated binaries: 76/76 integration tests, zero failed/skipped, 1m59s. Filter: `FullyQualifiedName~WebDataProtectionTests|FullyQualifiedName~Passkey|FullyQualifiedName~CatalogueSubmission`. Disposable `postgres:18.3`, migrated synthetic database, local API/web/browser hosts; production Azure is not used by these fixtures. [Local development instructions](local-development.md). Full interactive Aspire startup was not rerun, so the token prompt and local HTTPS configuration remain explicit setup requirements.
4. [Ordered decommission and billing plan](decommission-plan.md) prepared. **Do not execute until the user approves its named scope.**

Existing original, retailer and integration worktrees/untracked files were preserved. An additional isolated `shutdown-planning` worktree holds this documentation. Fresh validation adds its log/TRX only; nothing was cleaned, stashed or pruned. Synced `sources/` remained read-only.

## Actual deployed inventory

Subscription `21a5de71-5404-4671-8511-e5808a114028`, Pay-As-You-Go; all regional production resources are UK South. The subscription-wide resource list shows 18 resource entries (including child entries), with no other application stack visible. This is not proof that externally connected/shared consumers do not exist.

| Resource/group | Actual purpose and retirement dependency |
| --- | --- |
| `rg-diaperscout-prod` | Production resource group; use individual ordered deletion, not a blanket group deletion initially |
| `diaperscout-api-vnet` | Internal HTTPS ingress, port 8080; system identity; 0.5 CPU/1 GiB; min0/max10, cooldown300s, polling30s |
| `diaperscout-web-vnet` | Public HTTPS ingress, port 8080; `diaperscout.app` SNI managed certificate; system identity; 0.5 CPU/1 GiB; sticky affinity; min0/max2, cooldown600s, polling30s |
| `ds-bootstrap-admin` | Manual job, 300s timeout, zero retries; API `bootstrap-diagnostic` image, `--bootstrap-admin`, `pg-connection` reference; do not execute during inventory |
| `diaperscout-prod-env-vnet` | External Consumption environment; default domain `mangograss-ae5d4035.uksouth.azurecontainerapps.io`; static ingress IP `51.132.50.148`; Log Analytics logging |
| `ME_diaperscout-prod-env-vnet_rg-diaperscout-prod_uksouth` | Azure-managed group whose `managedBy` is this environment; contains `capp-svc-lb` and `capp-svc-lb-ip`. Delete parent environment and verify cleanup; never manually remove managed load balancer first |
| `vnet-diaperscout-prod` | `10.20.0.0/16`; `snet-containerapps` `10.20.0.0/23`, delegated `Microsoft.App/environments`; `snet-private-endpoints` `10.20.2.0/27`. Neither subnet has an attached NSG/route table in the read |
| `diaperscout-prod-pg` | PostgreSQL18, Burstable Standard_B1ms; 32GiB Premium_LRS/P4, 120 IOPS, auto-grow; no HA, 7-day non-georedundant backup policy; Ready |
| `pe-diaperscout-prod-pg` / `nic-pe-diaperscout-prod-pg` | Approved Private Link connection, `postgresqlServer` group, private-endpoint subnet; private IP `10.20.2.4` |
| `privatelink.postgres.database.azure.com` / `link-vnet-diaperscout-prod` | Private DNS A record `diaperscout-prod-pg` → `10.20.2.4`, VNet link |
| `diaperscoutprod` | Basic ACR, admin credentials disabled, public network enabled; apps use system identity + AcrPull. No ACR tasks returned |
| `diaperscout-prod-logs` | PerGB2018, retention30 days, no daily quota (`-1`); remove after final operational evidence |
| `dsprodwebkeys21a5de71` | Standard_LRS StorageV2; HTTPS/TLS1.2; shared-key auth disabled; network default Allow; Blob versioning and blob/container soft-delete90 days |
| `ds-prod-webkeys-21a5de71` | RBAC Key Vault, purge protection enabled, retention90 days; versionless `web-production` key reference. Operator key-metadata read returned Forbidden; no permission changes attempted |
| `NetworkWatcherRG/NetworkWatcher_uksouth` | Subscription-level potentially shared resource: **keep** |
| `DefaultResourceGroup-SUK` | Empty in returned resource inventory: **keep** |

No NAT gateway remains in the live resource list. Historical NAT/public-IP costs must not be treated as existing deletion targets. No deletion locks were returned.

### Database networking and TLS

Canonical host: `diaperscout-prod-pg.postgres.database.azure.com`. Public network access is still **Enabled**, although application access uses Private Link. Five firewall rules remain: `MacTempMigration`, `ContainerApps-NAT`, `Simon-PC`, `TempAdminAccess`, `ContainerApps`. Do not infer a recreated NAT gateway from the stale rule name or copy these personal/outdated IP rules into a rebuild.

Historical deployed database verification recorded **VerifyFull/TLS1.3** ([evidence](../../implementation/geoapify-discovery-evidence/database-verification.log)); current API/job retains the `pg-connection` reference. Secret contents were deliberately not retrieved: current secret TLS mode is **not independently reverified**. A rebuild must use canonical-host `Ssl Mode=VerifyFull`, DNS resolving privately, and a fresh runtime TLS check. Never use a private IP/privatelink hostname as the connection host or disable certificate verification to make networking work.

### Images and configuration

Current API: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-api@sha256:5042a81533cdb8897cdb75e51702e048803ecf86d067dfeeb399cc911716bcc9`.

Current Web: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:02a8d4eb5ec918687d4dc7eee66dcb1fb2c1d0211e18b3f64744c9b38c606623`.

Do not assume these correspond to the newly merged Git commit; no build provenance lookup was performed. Future rebuilds should build new immutable images from a selected Git SHA. Image history disappears when ACR is deleted, while Dockerfiles/source remain in GitHub.

API environment/reference names are fully recorded in `apps.json`. Secrets present: `resend-api-key` (legacy, not selected), `resend-api-key-v2` (selected), `pg-connection`, `production-internal-secret`, `r2-access-key-id`, `r2-secret-access-key`, `geoapify-api-key`. Web has `production-internal-secret`; job has `pg-connection`. **Names only, no values.** Re-obtain external integration credentials from their retained providers or a password manager; the Azure copy will disappear. No credential recovery is promised by source preservation.

Nonsecret current API settings: R2 enabled, bucket `diaperscout-prod-images`, endpoint `https://e5feb375654ff90ed3e02537d3586405.r2.cloudflarestorage.com`; Resend sender `hello@diaperscout.app`; magic-link URL `https://diaperscout.app/signin/magic-link`; editorial writes true. Web points to the internal API FQDN, uses `DiaperScout.Web.Production`, Blob `https://dsprodwebkeys21a5de71.blob.core.windows.net/web-production/keys.xml`, and versionless Key Vault URI `https://ds-prod-webkeys-21a5de71.vault.azure.net/keys/web-production`. A new environment gets different hostnames; update templates accordingly. No explicit custom container probes were returned.

Web identity `2ed34980-da90-483e-8937-fe94e9c5236f`: AcrPull, Storage Blob Data Contributor scoped to `web-production`, Key Vault Crypto Service Encryption User scoped to `keys/web-production`. API identity `86504b9b-a2ca-4772-8960-a90e717bb000`: AcrPull. Three additional AcrPull principal IDs are in `roles.json`: `bb2a6be4-0fd1-463a-acc4-0111b4dbe4de` resolves to `ds-bootstrap-admin`; directory reads for `134d711e-1521-42d2-a6c1-02c5a155c035` and `642a6dca-a4ce-4e6c-b88c-1bc35236726b` returned not-found/reference-missing errors. These appear stale but the error alone does not prove absence of every external registry consumer. No Entra identities, subscription-level permissions or external provider credentials are deletion targets.

## Measured costs

Read-only Cost Management query: ActualCost/PreTaxCost, daily, grouped ResourceId, 1 October00:00 UTC through 11 October00:00 UTC, run on 10 October. Returned dates1–10 October, GBP, no next page. Total **£15.7688209598875 (£15.77)**. This is estimated accrued pre-tax cost, not a monthly forecast or final invoice. Recent-day data may be partial.

| Resource | GBP accrued |
| --- | ---: |
| Managed load balancer | 4.3295 |
| PostgreSQL | 4.2353 |
| Web | 2.4259 |
| ACR | 1.7795 |
| Private endpoint | 1.7284 |
| Managed public IP | 0.8642 |
| API | 0.2776 |
| Private DNS | 0.1163 |
| Key Vault | 0.0091 |
| Bootstrap job | 0.0030 |
| Blob Storage | 0.0002 |
| Log Analytics | 0.0000 |

Support plans, Marketplace purchases, reservations and account-level commitments were not separately inventoried. Check invoices before claiming zero total Azure bill. [Microsoft billing-data guidance](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/understand-cost-mgt-data) explains latency and excluded charges. Historical £11.18 launch-period cost is a different time window and must not replace this result.

## Preserved services and consequences

Keep GitHub source/branches/worktrees, domain registration/renewal, Cloudflare DNS, Cloudflare R2/bucket/objects/tokens, Resend/domain verification, Cloudflare Pages and other free-tier integrations (including Geoapify and configured optional commerce providers). Free-tier status/quotas are not independently audited; no provider configuration was changed.

With DNS untouched, deleting Azure will leave the production domain pointing at a missing origin until a separately authorized DNS change. Resend DNS records and R2 data must remain. R2 objects survive but database associations/accounts/passkeys/observations/catalogue do not. Deleting Blob/Key Vault removes authentication key material; no session continuity is promised on rebuild. The production database deletion waiver does not silently waive loss of unrelated shared storage: scope verification remains mandatory.
