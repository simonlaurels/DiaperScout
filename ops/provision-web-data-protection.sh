#!/usr/bin/env bash
# Dedicated production Web key storage; does not change existing network or scaling resources.
set -euo pipefail
subscription=21a5de71-5404-4671-8511-e5808a114028
resource_group=rg-diaperscout-prod
account=dsprodwebkeys21a5de71
vault=ds-prod-webkeys-21a5de71
container=web-production
key=web-production
base="/subscriptions/$subscription/resourceGroups/$resource_group"
storage_scope="$base/providers/Microsoft.Storage/storageAccounts/$account/blobServices/default/containers/$container"
vault_scope="$base/providers/Microsoft.KeyVault/vaults/$vault"
az account set --subscription "$subscription"
principal=$(az containerapp show -g "$resource_group" -n diaperscout-web-vnet --query identity.principalId -o tsv)
test -n "$principal"
az provider register -n Microsoft.Storage --wait
az provider register -n Microsoft.KeyVault --wait
az storage account create -g "$resource_group" -n "$account" -l uksouth --sku Standard_LRS --kind StorageV2 --https-only true --min-tls-version TLS1_2 --allow-blob-public-access false --allow-shared-key-access false --output none
az storage account blob-service-properties update -g "$resource_group" --account-name "$account" --enable-versioning true --enable-delete-retention true --delete-retention-days 90 --enable-container-delete-retention true --container-delete-retention-days 90 --output none
az rest --method put --url "https://management.azure.com$storage_scope?api-version=2023-05-01" --body '{"properties":{"publicAccess":"None"}}' --output none
az keyvault create -g "$resource_group" -n "$vault" -l uksouth --enable-rbac-authorization true --enable-purge-protection true --retention-days 90 --output none
az role assignment create --assignee-object-id "$principal" --assignee-principal-type ServicePrincipal --role 'Storage Blob Data Contributor' --scope "$storage_scope" --output none
az role assignment create --assignee-object-id "$principal" --assignee-principal-type ServicePrincipal --role 'Key Vault Crypto Service Encryption User' --scope "$vault_scope/keys/$key" --output none
# Provision with a separately authorized crypto officer. Never replace an existing key.
# RBAC propagation can take minutes: wait before this step if a new grant was assigned.
key_exists=$(az keyvault key list --vault-name "$vault" --query "[?name=='$key'] | length(@)" -o tsv)
if [ "$key_exists" = 0 ]; then
    az keyvault key create --vault-name "$vault" --name "$key" --kty RSA --size 3072 --ops wrapKey unwrapKey --output none
fi
az keyvault key show --vault-name "$vault" --name "$key" --query key.kid -o tsv
# Remove any temporary provisioning grant afterwards. No operator grant is needed by Web.
