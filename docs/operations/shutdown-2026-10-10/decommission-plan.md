# Ordered decommission plan — approval required

**No commands below have been executed.** User approval must name the production scope and accept production data, images in ACR, logs and authentication-key loss. PostgreSQL/catalogue loss and no backup are already accepted. Keep all external integrations/domain/DNS/R2 and subscription-wide resources.

## Preconditions and stop conditions

- Re-read live resource/group inventories, locks, identities, role assignments, private DNS links and storage consumers immediately before execution. This inventory can drift. Abort if unexpected/shared resources or consumers appear; exclude them from deletion pending clarification.
- Registry additional identities resolve to the dedicated bootstrap job plus two not-found/reference-missing directory results; reconfirm consumers before including ACR. Blob/Key Vault scope appears dedicated to Web but permission-scoped key metadata read was denied. Do not add permissions merely to complete inventory. Confirm that only DiaperScout consumes them before deletion.
- Verify GitHub main includes425bb72, documentation PR is retained, local run instructions work and external credentials can be re-obtained from kept providers/password manager. Do not read or export Azure secret values into the repo.
- Record deletion timestamp and cost baseline; no production deployment, migration/job execution or database export needed.
- Keep `NetworkWatcherRG`, `NetworkWatcher_uksouth`, `DefaultResourceGroup-SUK`, subscription, tenant/Entra identities and unrelated role assignments. Do not cancel the whole subscription.

## Approved execution sequence

1. Delete `diaperscout-web-vnet`, then `diaperscout-api-vnet`, then manual job `ds-bootstrap-admin`. This removes writers and sessions. Wait for each deletion and inventory the environment for other apps/jobs before proceeding.
2. Delete parent environment `diaperscout-prod-env-vnet` (including its DiaperScout managed certificate). Allow Azure to clean `ME_diaperscout-prod-env-vnet_rg-diaperscout-prod_uksouth`, `capp-svc-lb` and `capp-svc-lb-ip`. Recheck subscription-wide resources until completed. Do not delete managed LB/IP first or remove deny assignments to bypass platform management. [Microsoft managed networking guidance](https://learn.microsoft.com/en-us/azure/container-apps/custom-virtual-networks?tabs=workload-profiles-env).
3. Delete `pe-diaperscout-prod-pg`; confirm its NIC disappears. Delete `diaperscout-prod-pg` permanently without export/backup; this ends database compute/storage and discards accounts/catalogue/observations/passkeys. Existing provider backup-retention behavior is not a new backup operation; do not promise immediate physical erasure or recovery.
4. Delete `link-vnet-diaperscout-prod`, private DNS zone `privatelink.postgres.database.azure.com`, then `vnet-diaperscout-prod`, after confirming no additional links/endpoints/delegated resources. Private DNS is Azure-only; **Cloudflare DNS stays unchanged**.
5. Delete ACR `diaperscoutprod` only after consumer verification. All app image digests/history become unavailable; future images are rebuilt from preserved Git/Dockerfiles. Do not delete external credentials/Entra identities.
6. Delete dedicated `dsprodwebkeys21a5de71` storage and `ds-prod-webkeys-21a5de71` Key Vault after consumer verification. Explicitly accept loss of old authentication key-ring/session continuity. Delete the account rather than individually soft-deleting blobs and leaving a billable account. Vault purge protection90 days means no immediate purge or same-name recreate; use a new name in an early rebuild. [Microsoft soft-delete behavior and billing](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview).
7. Delete `diaperscout-prod-logs` after recording completion evidence. Re-list `rg-diaperscout-prod`; delete the empty group only after it is demonstrably empty. Verify the managed group is absent or empty; if residue remains, inspect parent deletion state before separately approving cleanup. Never apply a blanket wildcard/group deletion to unknown resources.
8. Re-run subscription-wide inventory and compare with this snapshot. Expected survivor is Network Watcher; empty default group retained. Record any residue by full ID and meter; do not claim completion while DiaperScout billable resources remain.

## Billing verification

Save read-only daily ActualCost query before/after, grouped ResourceId, covering at least7 days on either side of deletion. Use `cost-query.json` with adjusted date window and Cost Management query endpoint. POST here is a read-only reporting query, not a cloud mutation.

Check after24–48 hours, after72 hours if lag remains, and on the next invoice. Compare **usage dates**, not arrival dates: late postings for pre-deletion use are expected. Verify no new post-deletion usage for database, registry, private endpoint/DNS, LB/IP, apps/jobs, storage/vault/logs. Recent zero rows are not final proof. Investigate new usage after deletion against exact resource IDs, then recheck lagged data. Costs are estimates until invoiced; credits, support, commitments and some purchases may need separate billing review. [Microsoft cost-data guidance](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/understand-cost-mgt-data).

Keep domain renewal payment and all free-tier integrations untouched. No DNS pointing change or maintenance landing page is included: the site will fail at its old Azure origin after deletion. Any desired landing page/DNS change needs separate user instruction.

Approval requested: execute the named sequence above, subject to revalidated dedicated ownership; skip and report any shared/uncertain resource. This document does not itself grant approval.
