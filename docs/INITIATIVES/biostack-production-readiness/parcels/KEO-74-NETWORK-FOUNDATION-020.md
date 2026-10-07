# Parcel: KEO-74-NETWORK-FOUNDATION-020

## Goal

Produce a fail-safe, review-only Azure network foundation for the merged source-acquisition jobs in PR #231 without cutting over or changing any current BioStack workload.

## Initiative

BioStack production readiness / KEO-74 governed source acquisition.

## Project Track

Azure infrastructure.

## Wave

Infrastructure prerequisite.

## Branch

`codex/keo74-network-foundation-20260726`

## Worktree

`D:\Repos\BioStack-keo74-network-foundation-020-20260726`

## Dependencies

- PR #231 merged source-acquisition jobs and their final-state private ACR validation.
- Existing `biostackmissionctrlacr` in `biostack-rg`.
- An operator-provisioned trusted self-hosted Linux x64 runner with the required VNet/DNS reachability remains external to this parcel.

## Integration Surfaces

- `infra/azure/source-acquisition-jobs.bicep` deployment-time private-network resource IDs.
- Existing ACR transition from Basic to Premium while both current access paths remain intact.

## Security Gate

Independent infrastructure review required before any deployment.

## Allowed Files

- `infra/azure/source-acquisition-acr-transition.bicep`
- `infra/azure/source-acquisition-network-foundation.bicep`
- `infra/azure/source-acquisition-network-foundation.parameters.example.json`
- `infra/azure/verify-source-acquisition-network-foundation.ps1`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-74-NETWORK-FOUNDATION-020.md`
- `research/routing-events/keo-74-network-foundation-20260726.json`

## Forbidden

- Deploying or writing any Azure resource, role assignment, ACR content, app, environment, job, traffic rule, DNS record outside this template, or source endpoint.
- Migrating, changing, deleting, or rehosting current Container Apps, managed environments, or jobs.
- Disabling ACR public network access or its admin user during this transition.
- Provisioning self-hosted runner compute.
- Adding credentials, secrets, fixed principal IDs, role changes, or live-source calls.

## Out of Scope

Deployment, cutover, ACR credential removal, ACR public-access disablement, app/job migration, source acquisition, Blob resources, roles, runner compute, and production qualification.

## Existing Patterns To Follow

- `infra/azure/source-acquisition-jobs.bicep` consumes existing network resource IDs and remains unchanged.
- `infra/azure/source-acquisition-role.bicep` demonstrates narrow Azure modules; this parcel creates no roles.

## Contract

The foundation creates a new VNet, a subnet delegated to `Microsoft.App/environments`, a separate private-endpoint subnet, a new workload-profile Container Apps environment with no workloads, an ACR `registry` private endpoint, the exact `privatelink.azurecr.io` zone, its `default` zone group, and a registration-disabled VNet link.

The existing ACR is updated only to Premium while explicitly retaining `publicNetworkAccess: Enabled` and `adminUserEnabled: true`. The template fails closed if those two properties are not already present or if the location/subscription boundary differs.

The template emits the exact parameters required later by `source-acquisition-jobs.bicep`:

- `containerAppsEnvironmentId`
- `containerAppsInfrastructureSubnetId`
- `privateEndpointSubnetId`
- `privateDnsVnetId`
- `acrPrivateEndpointResourceId`
- `acrPrivateDnsZoneResourceId`
- `acrPrivateDnsVnetLinkResourceId`

## Required Tests

- Compile both Bicep templates.
- Run deterministic preservation/shape verification.
- Run an Azure `what-if` against only subscription `909e0322-c3c0-4bce-ae53-b3d2ed735bd4` if permissions and provider validation permit.

## Acceptance Criteria

- No current app, environment, job, role, traffic path, or registry content is declared or changed.
- ACR Premium transition preserves public network access and admin-user access.
- Infrastructure and private-endpoint subnets are separate and parameterized.
- Private DNS uses exactly `privatelink.azurecr.io`, group `registry`, and a registration-disabled VNet link.
- All required downstream resource IDs are outputs.
- Verification is deterministic and contains no credentials or secrets.

## Verification

```powershell
rtk proxy pwsh -NoProfile -File infra/azure/verify-source-acquisition-network-foundation.ps1
rtk proxy pwsh -NoProfile -File infra/azure/verify-source-acquisition-network-foundation.ps1 -RunWhatIf
rtk proxy az bicep build --file infra/azure/source-acquisition-acr-transition.bicep
rtk proxy az bicep build --file infra/azure/source-acquisition-network-foundation.bicep
rtk git diff --check
```

## Evidence Required

- Subscription state and VNet address-space audit summary.
- Existing ACR non-secret transition properties.
- Bicep compile and deterministic verifier results.
- Bounded `what-if` result or exact blocker.
- Independent review outcome.

## Bounded verification evidence

Executed at `2026-07-26T16:46:07Z` without deployments or other Azure writes:

- Both Bicep files compiled with Azure CLI Bicep `0.41.2`; only the CLI's newer-version notice was emitted.
- The offline verifier passed with nine root resources and all seven network resource-ID outputs required by `source-acquisition-jobs.bicep`.
- Live prerequisite checks passed: the exact subscription is Enabled; the ACR remains Basic, public network access Enabled, and admin user enabled; the enabled subscription contains zero VNets.
- The full foundation `what-if` succeeded with eight additive creates: one new managed environment, one VNet, two separate subnets, one ACR private endpoint, one DNS zone group, the exact private DNS zone, and one VNet link. Existing apps, environments, jobs, and unrelated resources were reported `Ignore`.
- Because Azure reports the nested ACR module as `Ignore` in the parent `ResourceIdOnly` result, a separate full-payload `what-if` ran directly against the transition module. Its only delta path was `sku.name` from Basic to Premium; `publicNetworkAccess` remained `Enabled`, `adminUserEnabled` remained `true`, and audited ancillary policies remained unchanged.
- Independent infrastructure re-review passed after the verifier was tightened to require exactly eight allowlisted `Create` changes and reject every unexpected non-ignored resource or change type. Both Bicep compiles, canonical routing schema, staged diff validation, and the direct ACR `sku.name`-only delta passed.
- No credentials, registry content, source payloads, or secret-bearing resource properties were queried or saved.

## Collision Risk

Medium. The new files do not edit #231, but their outputs become deployment inputs to its jobs template. Deployment sequencing and address-space ownership require operator review.

## Audit and example values

Read-only Azure CLI queries on 2026-07-26 confirmed subscription `909e0322-c3c0-4bce-ae53-b3d2ed735bd4` is Enabled and contains zero VNets. The example therefore proposes `10.74.0.0/16`, with `10.74.0.0/23` for the delegated Container Apps infrastructure subnet and `10.74.2.0/24` for private endpoints. These remain examples: the operator must re-audit peered/on-premises address ownership before deployment because subscription inventory alone cannot prove global non-overlap.

The existing `biostackmissionctrlacr` is in `biostack-rg`, region `eastus`, SKU Basic, with public network access Enabled and admin user enabled. The current BioStack managed environments have no VNet integration. This parcel creates a parallel environment and never changes them.

## Cost-bearing changes

If deployed, this parcel adds recurring cost for ACR Premium, a Container Apps workload-profile environment and its networking, and an ACR private endpoint. Private DNS is also billable at a smaller rate. No cost is incurred by compilation, read-only queries, or `what-if`. Exact prices are intentionally not hard-coded and require an operator estimate for the target region and retention period.

## PR Notes

- What changed: added a fail-safe transitional network foundation and offline/live-read-only verification.
- Why: PR #231 requires private ACR/DNS/VNet IDs that the current subscription does not have.
- Risk: CIDR ownership beyond Azure subscription inventory and recurring Azure cost require operator review.
- Verification: compile, deterministic checks, and bounded Azure `what-if`.
- Evidence: this parcel and its routing event.

## Session Handoff

- Starting commit: `53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`
- Ending commit: unchanged; no commit authorized.
- Files changed: the six Allowed Files listed above.
- Commands run: read-only Azure subscription/VNet/ACR/environment queries, Bicep compile, verifier, `what-if` if feasible, and git checks.
- Tests passed: both Bicep compiles; deterministic verifier; full foundation `what-if`; direct ACR preservation `what-if`; JSON parsing. Final tracked-diff validation is required after staging because these six files began untracked.
- Tests failed: none. One initial local verifier invocation exposed command-line parsing for the VNet count; the script was corrected to parse bounded JSON and the final runs passed.
- Decisions needed: final address-space ownership, cost approval, runner provisioning, and deployment authorization.
- Blockers: no trusted VNet-connected self-hosted runner is provisioned by this parcel; final-state ACR disablement remains a later cutover gate.
- Next safe action: publish the reviewed code-only parcel; deployment remains blocked on address-space ownership, cost approval, runner provisioning, and explicit authorization.
- Do not touch: current apps/environments/jobs, roles, ACR access settings beyond the preserved transition, registry content, sources, or secrets.

## Stop-and-Report Rule

Any deployment, access disablement, current-workload change, role change, runner provisioning, source call, or contract change requires a new explicit authorization and parcel amendment.
