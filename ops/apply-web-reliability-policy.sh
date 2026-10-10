#!/usr/bin/env bash
# Web-only scale-to-zero policy. Never replace configuration/secrets/identity/API.
# See docs/implementation/web-scale-to-zero.md. --rollback-warm restores 1/1.
set -euo pipefail
resource_group=rg-diaperscout-prod
web_app=diaperscout-web-vnet
api_version=2025-07-01
min=0 max=2 cooldown=600
case "${1:-}" in
    '') ;;
    --rollback-warm) min=1; max=1; cooldown=300 ;;
    *) echo "Usage: $0 [--rollback-warm]" >&2; exit 2 ;;
esac
command -v jq >/dev/null
snapshot=$(mktemp)
body=$(mktemp)
trap 'rm -f "$snapshot" "$body"' EXIT
az containerapp show -g "$resource_group" -n "$web_app" -o json > "$snapshot"
if [[ $(jq -r '.properties.configuration.activeRevisionsMode' "$snapshot") != Single ]]; then
    echo 'Cookie affinity requires Single revision mode. Review configuration before applying.' >&2
    exit 1
fi
if [[ $(jq -r '.properties.configuration.ingress.stickySessions.affinity' "$snapshot") != sticky ]]; then
    az containerapp ingress sticky-sessions set -g "$resource_group" -n "$web_app" --affinity sticky --output none
fi
id=$(jq -r '.id' "$snapshot")
# Stable ARM API exposes cooldownPeriod; installed containerapp CLI has no flag.
# GET using the same API before PATCH, retain every other template field and rule.
az rest --method get --url "https://management.azure.com${id}?api-version=${api_version}" -o json > "$snapshot"
if ! jq -e --argjson min "$min" --argjson max "$max" --argjson cooldown "$cooldown" \
    '.properties.template.scale | .minReplicas == $min and .maxReplicas == $max and .cooldownPeriod == $cooldown' "$snapshot" >/dev/null; then
    suffix="scale-policy-$(date -u +%Y%m%d%H%M%S)"
    jq --arg suffix "$suffix" --argjson min "$min" --argjson max "$max" --argjson cooldown "$cooldown" \
        '{properties:{template:.properties.template}} | .properties.template.scale.minReplicas=$min | .properties.template.scale.maxReplicas=$max | .properties.template.scale.cooldownPeriod=$cooldown | .properties.template.revisionSuffix=$suffix' \
        "$snapshot" > "$body"
    # A new immutable revision needs a fresh suffix. Configuration is not in PATCH.
    az rest --method patch --url "https://management.azure.com${id}?api-version=${api_version}" --body "@$body" --output none
    # PATCH can return 202 before the resource reflects its new template.
    # Bounded control-plane polling; never send application keepalive requests.
    completed=false
    for ((attempt=1; attempt<=30; attempt++)); do
        az containerapp show -g "$resource_group" -n "$web_app" -o json > "$snapshot"
        if jq -e --arg revision "${web_app}--${suffix}" --argjson min "$min" --argjson max "$max" --argjson cooldown "$cooldown" \
            '.properties | .provisioningState == "Succeeded" and .latestRevisionName == $revision and .latestReadyRevisionName == $revision and .template.scale.minReplicas == $min and .template.scale.maxReplicas == $max and .template.scale.cooldownPeriod == $cooldown and .configuration.ingress.stickySessions.affinity == "sticky"' "$snapshot" >/dev/null; then
            completed=true; break
        fi
        sleep 10
    done
    if [[ "$completed" != true ]]; then
        echo 'Scaling revision did not become ready within the bounded deployment wait. Inspect ACA; no application retries were attempted.' >&2
        exit 1
    fi
fi
az containerapp show -g "$resource_group" -n "$web_app" \
    --query '{revision:properties.latestRevisionName,ready:properties.latestReadyRevisionName,scale:properties.template.scale,affinity:properties.configuration.ingress.stickySessions}' -o json
