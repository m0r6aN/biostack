#!/usr/bin/env bash
# Ledger gate G3: apply health probes (container-app-probes.json) and a replica floor to the
# live API and web Container Apps. Merges into the current spec, so nothing else changes.
#
# Usage: infra/azure/apply-container-app-probes.sh <resource-group> <api-app> <web-app> [min-replicas] [--dry-run]
#   min-replicas defaults to 1 (no scale-to-zero, so no cold start; ~1 always-on replica per app is billed).
#   Pass 0 to allow scale-to-zero (cheaper; first request after idle waits for a cold start).
set -euo pipefail

rg="${1:?resource group}"; api="${2:?api app name}"; web="${3:?web app name}"; min="${4:-1}"; dry="${5:-}"
here="$(cd "$(dirname "$0")" && pwd)"
probes="$here/container-app-probes.json"

apply() {
  local app="$1" key="$2" spec patch
  spec="$(az containerapp show -n "$app" -g "$rg" -o json)"
  # Only properties.template is sent: every container keeps its image/env/resources and gains the
  # probes (first container = the app); scale keeps its rules with the new floor.
  patch="$(jq --argjson cfg "$(jq ".$key" "$probes")" --argjson min "$min" '
    {properties: {template: (.properties.template
      | .containers |= (to_entries | map(if .key == 0 then .value + {probes: $cfg.probes} else .value end))
      | .scale = ((.scale // {}) + {minReplicas: $min, maxReplicas: ([(.scale.maxReplicas // 1), ($min | if . < 1 then 1 else . end)] | max)})
    )}}' <<<"$spec")"
  if [[ "$dry" == "--dry-run" ]]; then
    echo "--- $app (dry run) ---"; jq '.properties.template | {probes: .containers[0].probes, scale}' <<<"$patch"; return
  fi
  local file; file="$(mktemp --suffix .yaml)"; printf '%s\n' "$patch" > "$file"   # JSON is valid YAML
  az containerapp update -n "$app" -g "$rg" --yaml "$file" -o none
  rm -f "$file"
  az containerapp show -n "$app" -g "$rg" --query "{probes: properties.template.containers[0].probes[].type, minReplicas: properties.template.scale.minReplicas, latestReady: properties.latestReadyRevisionName}" -o json
}

apply "$api" api
apply "$web" web
