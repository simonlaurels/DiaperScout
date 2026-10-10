# Shared Web Data Protection

The production Web replicas previously wrote independent, unencrypted key rings to container-local storage. That prevents replicas from consistently decoding authentication cookies, antiforgery tokens and Blazor protected state. The installed-PWA recovery does not establish that this defect caused the physical iPhone failure.

Web now uses the official Azure Blob and Key Vault Data Protection providers, with its existing system-assigned managed identity. Production requires all three nonsecret settings:

```
DataProtection__ApplicationName=DiaperScout.Web.Production
DataProtection__BlobUri=https://dsprodwebkeys21a5de71.blob.core.windows.net/web-production/keys.xml
DataProtection__KeyVaultKeyUri=https://ds-prod-webkeys-21a5de71.vault.azure.net/keys/web-production
```

The application discriminator must match `DiaperScout.Web.{EnvironmentName}`. Development without Azure settings uses `DiaperScout.Web.Development`. API protection is separate and unchanged. Production accepts only credential-free HTTPS Azure endpoints and a versionless wrapping-key URI. Missing or invalid settings stop startup. Before serving requests, each replica protects and decrypts a readiness value through the configured ring; inaccessible storage or wrapping keys stop startup without an ephemeral fallback.

## Storage and access

Resources are dedicated to production Web in `rg-diaperscout-prod`, UK South. `ops/provision-web-data-protection.sh` records the provisioning commands.

Resource provisioning requires a management identity authorized to create these resources and scoped role assignments. Key creation additionally requires Key Vault Crypto Officer access on this dedicated vault. Grant it temporarily if needed, wait for RBAC propagation, create the key, and remove the grant. The script's key-list check fails closed on access errors and never replaces an existing key. The optional cloud probe requires temporary Blob Data Contributor access on the container and Crypto Service Encryption User on the wrapping key; remove both grants after verification. Do not grant these roles subscription-wide.

- `dsprodwebkeys21a5de71`: Standard LRS Blob storage; HTTPS required, minimum TLS 1.2, shared-key authorization and anonymous access disabled. Private `web-production` container. Blob versioning and blob/container soft deletion retain recovery data for 90 days.
- `ds-prod-webkeys-21a5de71`: RBAC vault, purge protection, 90-day soft deletion. RSA-3072 `web-production` wrapping key, operations wrap/unwrap. Key ring XML is envelope-encrypted before upload.
- Web principal `2ed34980-da90-483e-8937-fe94e9c5236f`: Storage Blob Data Contributor scoped to that container; Key Vault Crypto Service Encryption User scoped to that key. No secrets, storage-account keys or developer credential fallback.

Data Protection rotates its own keys every 90 days. Rotate the vault wrapping key periodically by creating a new version under the same key name. Retain every old wrapping-key version while any ring entry references it: do not delete, disable or expire those versions. The versionless configuration uses the current version for new ring entries; existing entries retain their specific wrapping-key version.

## Migration and recovery

Old container-local rings are not imported. The stable application discriminator and new shared ring invalidate existing cookies and protected state once. Users may need to sign in again or explicitly reload an old open tab; registered passkeys and application data remain intact. New sessions survive replica replacement and later compatible deployments.

Keep the discriminator and both storage URIs stable across releases. Roll back only to an image supporting this provider configuration; rolling back to the previous local-ring image reintroduces the defect. Recover accidental Blob deletion using its retained versions or soft deletion, preserving the wrapping keys. Do not generate a replacement wrapping key name as a recovery shortcut. Monitor the retained versions' capacity/cost and vault audit events.

No networking, affinity, replica counts, API scaling, PostgreSQL or PWA service-worker policy is changed. This fixes shared cryptographic state; it does not resolve the independently known SignalR routing/affinity risk. Physical iPhone startup remains a separate validation step.

## Validation

Regressions cover fail-closed configuration, environment isolation, Azure repository/encryptor registration, cross-instance/restarted-provider decoding, purpose separation and encrypted persisted XML. Offline crypto tests use a certificate-backed shared directory, not a claim that Azure permissions were exercised. Production startup readiness and storage inspection provide the cloud-specific evidence.

On 1 October 2026, all **225 regressions passed** (187 integration/browser and 38 domain/application); the final Release build passed with **zero warnings/errors**. One browser regression needed an explicit circuit-ready wait before clicking a prerendered control; the subsequent full suite passed.

The real Azure probe (`ops/DataProtectionProbe`) also passed: a new independent provider decrypted the first provider's payload, a different application name failed decryption, and persisted ring XML contained encrypted secrets with no plaintext master key. Probe and provisioning roles were temporary and removed. Its safe metadata is in `data-protection-evidence/cloud-probe.json`; the key identifier is metadata, not key material.

ACR build **db10** succeeded. Web image `diaperscout-web:shared-keys-20261001`, digest `sha256:4fd96351bf8b8c24e4b2f652be7968137e44ede9a0d86afa44e20523ce2c89c3`, deployed as **diaperscout-web-vnet--shared-keys-20261001**, active, provisioned, running and healthy. Startup exercises the key ring under the actual Web managed identity, independently of the CLI probe identity.

All three replicas observed after autoscaling were running and ready with zero restarts; each must complete its real managed-identity protect/decrypt check before listening. See `data-protection-evidence/replicas.json` and `revision.json`. No scale settings were changed to obtain this observation.

All **16 production browser observations** passed: WebKit and Chromium, browser and simulated standalone modes, Explore/Products/Scan/sign-in. Startup covers dismissed, error banners stayed hidden, and no console/page errors or failing Blazor transport responses were observed in that run. Twelve fresh public SSR fetches produced 12 protected component descriptors, all referencing shared key `3ab78896-4823-4ed8-bca6-8809146fbb0b`. See `data-protection-evidence/production-smoke.json`.

Before/after comparisons confirmed unchanged Web identity, scaling and ingress, and an unchanged API image/revision/scaling configuration, including API minimum replicas **0**. See `data-protection-evidence/infrastructure-preservation.json`. A clean smoke does not prove that the separate intermittent SignalR affinity failure is eliminated.

Physical iPhone iOS 27.0.1 validation is still required: launch the installed Home Screen app, verify the splash clears and catalogue interactions work, then repeat after closing the app. Sign in again if an old cookie is rejected. These browser checks emulate standalone detection; they do not emulate the complete iOS Home Screen lifecycle.

Provider configuration follows [Microsoft's Data Protection guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0).
