#!/usr/bin/env bash
# Apply the documented low-traffic Blazor policy without replacing images, env,
# secrets, identity, networking or API configuration. See implementation report.
set -euo pipefail
resource_group=rg-diaperscout-prod
web_app=diaperscout-web-vnet
mode=$(az containerapp show -g "$resource_group" -n "$web_app" --query properties.configuration.activeRevisionsMode -o tsv)
if [[ "$mode" != Single ]]; then
    echo 'Cookie affinity requires Single revision mode. Review configuration before applying.' >&2
    exit 1
fi
az containerapp ingress sticky-sessions set -g "$resource_group" -n "$web_app" --affinity sticky --output none
scale=$(az containerapp show -g "$resource_group" -n "$web_app" --query 'properties.template.scale.[minReplicas,maxReplicas]' -o tsv)
if [[ "$scale" != $'1\n1' ]]; then
    az containerapp update -g "$resource_group" -n "$web_app" --min-replicas 1 --max-replicas 1 --output none
fi
az containerapp show -g "$resource_group" -n "$web_app" \
    --query '{revision:properties.latestReadyRevisionName,scale:properties.template.scale,affinity:properties.configuration.ingress.stickySessions}' -o json
