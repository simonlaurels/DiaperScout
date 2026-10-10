# Future rebuild and validation runbook

**Future execution only, requiring new deployment authorization.** Source and infrastructure configuration are recoverable; discarded production data/accounts are not. These steps are an ordered reconstruction recipe based on live inventory and tracked source, not a rehearsed infrastructure deployment. Azure names, API availability, image provenance and CLI syntax must be revalidated at rebuild time. Do not call it a tested one-command restore.

## 1. Select source and validate locally

Use main containing `425bb728e6cefbff99fa5c94b517433b88cd0507` (merge `5173d15`). Create a fresh isolated checkout; preserve existing worktrees. Record exact chosen SHA. Run the full commands in [local-development.md](local-development.md). Review migrations and deployment context. Dockerfiles copy the context; `.dockerignore` excludes bin/obj/tests/.git but not every possible local diagnostic/credential file. Build from a **clean tracked-only checkout**, excluding this evidence directory if unnecessary. Never build from a dirty worktree containing operator receipts/secrets.

## 2. Recreate dedicated network/database

Create a dedicated resource group in `uksouth`, Basic ACR (admin auth disabled), PerGB2018 Log Analytics/30-day retention, and VNet `10.20.0.0/16` with `snet-containerapps` `10.20.0.0/23` delegated to `Microsoft.App/environments` and `snet-private-endpoints` `10.20.2.0/27`. Recheck subnet sizing/current provider requirements. Use the live settings in `environment.json`, `postgres.json`, `apps.json` as references, not ARM deployment templates.

Example future commands (Bash placeholders are deliberately not credentials):

```bash
az group create -n "$RG" -l uksouth
az acr create -g "$RG" -n "$ACR" --sku Basic --admin-enabled false
az monitor log-analytics workspace create -g "$RG" -n "$LOGS" -l uksouth --retention-time 30
az network vnet create -g "$RG" -n "$VNET" --address-prefixes 10.20.0.0/16
az network vnet subnet create -g "$RG" --vnet-name "$VNET" -n snet-containerapps --address-prefixes 10.20.0.0/23 --delegations Microsoft.App/environments
az network vnet subnet create -g "$RG" --vnet-name "$VNET" -n snet-private-endpoints --address-prefixes 10.20.2.0/27
```

Create PostgreSQL Flexible Server18, Burstable B1ms,32GiB Premium_LRS,HA disabled,7-day backups, with newly generated credentials supplied through a secure operator workflow. Create application database `diaperscout`. Create its private endpoint with group ID `postgresqlServer` in the private-endpoint subnet; link private DNS zone `privatelink.postgres.database.azure.com` to the VNet, registration disabled, and associate the endpoint DNS zone group. **Do not reuse old public firewall IP rules.** The observed production setting enabled public access; prefer a reviewed private-only rebuild, or explicitly approve any public exception. Verify the canonical server FQDN resolves to the new endpoint IP from inside the environment.

Create external Consumption Container Apps environment attached to the delegated subnet and Log Analytics. Example:

```bash
az containerapp env create -g "$RG" -n "$ENV" -l uksouth \
  --enable-workload-profiles --infrastructure-subnet-resource-id "$APP_SUBNET_ID" \
  --logs-workspace-id "$LOG_WORKSPACE_ID" --logs-workspace-key "$LOG_WORKSPACE_KEY"
```

Workspace shared key is a credential: obtain securely for this operation, suppress output/logging, never commit it. The generated environment domain/static IP and managed-group name will differ. Do not recreate managed LB/IP manually and do not add NAT unless a newly verified requirement needs it.

## 3. Build immutable artifacts and migrate

```bash
az acr build -r "$ACR" -t "diaperscout-api:$SHA" -f Dockerfile.api .
az acr build -r "$ACR" -t "diaperscout-web:$SHA" -f Dockerfile.web .
az acr build -r "$ACR" -t "diaperscout-migrations:$SHA" -f Dockerfile.migrations .
az acr repository show -n "$ACR" --image "diaperscout-api:$SHA" --query digest -o tsv
az acr repository show -n "$ACR" --image "diaperscout-web:$SHA" --query digest -o tsv
az acr repository show -n "$ACR" --image "diaperscout-migrations:$SHA" --query digest -o tsv
```

Record digests; deploy `LOGIN_SERVER/repository@sha256:...`. Build uses .NET10 SDK/runtime; migrations Dockerfile installs EF tool10.0.0. Check compatibility at rebuild time. Inspect an idempotent SQL migration script before execution; never apply historical destructive Down scripts.

Create a manual migration job in the new environment with managed identity/AcrPull and connection secret reference `pg-connection`; image is the migration bundle, no command override, no retries, bounded timeout. Connection template:

```text
Host=<canonical-server>.postgres.database.azure.com;Port=5432;Database=diaperscout;Username=<new-admin-or-app-role>;Password=<secure-new-value>;Ssl Mode=VerifyFull
```

Use a secret parameter/template outside Git for its value. Start the job only during the newly authorized rebuild, inspect exit code/history and migration table, then remove the migration runner when done. No migrations were run by shutdown preparation. Separate runtime least-privilege database credentials from migration credentials where practical.

## 4. Identities, authentication key storage and secrets

Create API/Web with new system-assigned identities and grant each AcrPull scoped to the new ACR. When bootstrapping identities, use a reviewed temporary neutral image or ARM deployment with controlled startup, configure registry identity and grants, wait for propagation, then set the actual digest. Never enable development auth in production.

Create StorageV2 Standard_LRS (HTTPS/TLS1.2, anonymous blobs/shared-key access disabled) and private `web-production` container,90-day versioning/soft-delete policy as observed. Create an RBAC Key Vault with purge protection90 days and **software RSA3072** `web-production` wrapping key; choose a new globally unique name while old vault is retained. Grant Web Storage Blob Data Contributor at container scope and Key Vault Crypto Service Encryption User at key scope. `ops/provision-web-data-protection.sh` is the historical implementation, but it hardcodes old names/subscription: adapt/review before execution, never run it unchanged. A separate authorized crypto officer creates the key; remove temporary grants afterwards. Wait for RBAC propagation.

Restore external-provider credentials from kept Cloudflare/Resend/Geoapify accounts/password manager into Azure secret slots with the names in `apps.json`; do not recover them from Git. Generate a new random internal shared secret and use the identical value only in API/Web. A newly created database requires new credentials. Ignore unused legacy `resend-api-key` unless a new requirement needs it. Source preservation is not credential backup.

Apply [configuration-template.json](configuration-template.json) via an operator-reviewed Azure deployment specification, translating `settings` to container env name/value and `secretReferences` to env name/secretRef; `secretsToProvision` is a name checklist only. It is intentionally **not** an ARM/YAML deployable file. Replace every `<...>` placeholder. Actual live app configuration including scaling/ingress/identity is in `apps.json`.

## 5. Deploy and validate before public DNS

Deploy internal API at8080,0.5CPU/1GiB,min0/max10; external Web at8080,0.5CPU/1GiB,min0/max2,cooldown600s,sticky affinity,Single revision/latest100%. Set HTTPS-only ingress. Web receives the newly generated internal API FQDN. Production Web Data Protection requires exact application name `DiaperScout.Web.Production`, credential-free Blob URI and **versionless** Key Vault URI. Health checks must be added/reviewed deliberately; the observed apps did not return explicit custom probes. Do not accidentally replace environment/secret/identity settings with an image-only update.

Before assigning a public domain, verify API/Web health and startup logs, private DNS/VerifyFull TLS from an in-environment connection, migration completion, registry pulls, managed-identity Blob writes/key wrapping and successful shared-key readiness. Verify no Development auth/catalogue seeding. Run read-only database probe with `SHOW ssl`/`pg_stat_ssl` for the current connection and check canonical host/SslMode without printing credentials. Existing tracked `ops/ContributionAuthProbe` demonstrates a read-only in-environment database probe; extend a separately reviewed probe if TLS details are required.

### Fresh administrator bootstrap — unresolved reconstruction step

The deployed manual job uses old `diaperscout-api:bootstrap-diagnostic` and `--bootstrap-admin`, but current tracked API source contains **no `--bootstrap-admin` handler**. Do not assume rebuilding Dockerfile.api reproduces that custom image or start it as a bootstrap job: it may just start the web server. Existing catalogue population tooling expects an already-active administrator. Before a future public rebuild, implement/review a one-time least-privilege administrator creation path using the current User/profile/role/audit model, validate it on a disposable database, and run it only with separately authorized chosen administrator identity. Never enable Development mode in production to obtain seeded privileges. This gap is documented rather than inventing a working command.

Once bootstrap is resolved, validate fresh sign-in/magic links/Resend, passkeys matching `diaperscout.app` HTTPS origin, session key sharing across processes, catalogue/search/product detail, admin writes, exact-pack retail destinations, scanner camera/manual fallback, upload/moderation, Geoapify selection/attribution, observation/Atlas, Backpack and installed-PWA reconnect/offline/resume. R2 bucket survives but no old database associations survive; repopulate via reviewed manifests/editorial workflows only, not an assumed restoration. New account/passkey enrollment is expected.

Only after validation and explicit DNS authorization: update relevant Cloudflare origin record/domain verification for the new ingress (retain R2 and Resend DNS), bind custom domain/managed certificate, confirm issuance/renewal and TLS, then smoke test public/installed-PWA routes on desktop/mobile Chromium/WebKit and a physical iPhone. DNS/domain preservation during shutdown does not authorize a later DNS mutation.

## Historical pitfalls to preserve

- Private Link DNS + canonical hostname + VerifyFull; stale NAT allowlist is not a connectivity strategy.
- Shared Data Protection solves cryptographic sharing, not in-memory Blazor circuit portability; retain sticky sessions. Scale-to-zero cold starts and scale-in can terminate sessions; use `ops/apply-web-reliability-policy.sh` only after reviewing the selected warm/scaled policy. Older1/1 policy is historical; live shutdown snapshot is0/2.
- Web key RBAC propagation and versionless wrapping key URI; managed identities get new IDs after recreation. Operator metadata-read denial is not evidence Web cannot use its own scoped role.
- PWA service worker caches/static artwork and explicit reconnect/reload behavior; verify physical-device startup, not just browser screenshots.
- Browser tests need two collection workers and specific hydration readiness; private `_blazorInputFileNextFileId` marker is framework-upgrade-sensitive. Keep real assertions/timeouts.
- Avoid blindly restoring mutable catalogue IDs/grouping from manifests after a blank database. Historical receipts include successful and failed tests/deploys; use latest validated sources rather than treating every receipt as a pass.
- Historical runtime warnings for HTTP8080/TLS termination and missing libgssapi are documented, not permission to weaken TLS.

Read [PWA reliability](../../implementation/pwa-production-reliability.md), [Geoapify deployment/TLS](../../implementation/geoapify-scan-discovery.md), [deployment strategy](../../implementation/deployment-strategy.md) and source Dockerfiles/ops scripts. The Azure rebuild, administrator bootstrap and physical-iPhone acceptance require future work/authorization; none was executed here.
