# Parcel: P0-PUBLIC-CONTENT-BOUNDARY-REPAIR-025

Status: backend and endpoint boundary verification passed; hosted frontend
verification pending.

## Objective

Repair the public content boundary so that absence of detected findings is not
presented as compatibility, and legacy individualized-action fields are not
rendered or returned through the public knowledge surface.

This parcel starts from `origin/main` commit
`53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c` on branch
`codex/p0-public-content-boundary-20260726`.

## Public-boundary contract

The public compound surface retains useful observational evidence:

- canonical identity, classification, regulatory status, and evidence tier;
- mechanism summaries, pathways, benefits, source references, and notes;
- reported `AvoidWith` caution signals and drug-interaction observations.

The public projection and card withhold fields that imply individualized action:

- `PairsWellWith`, compatible blends, and vial compatibility;
- recommended dose, dose range, maximum dose, frequency, timing, schedules,
  escalation steps, and tiered dosing;
- protein, carbohydrate, supplement, sleep, and exercise optimization fields;
- contextual product additions derived from knowledge-search recommendations.

The blend checker now treats zero returned overlap findings as `unknown`.
Source-reported pairing data may only appear as a caveated observation that
does not establish compatibility or safety.

The unauthenticated interaction-check endpoint uses a dedicated evidence-only
service path:

- raw `PairsWellWith` and `CompatibleBlends` metadata produces an `Unknown`
  interaction rather than a synergy or compatibility result;
- no remove-one or swap-one scenarios are calculated or serialized;
- an absent rule finding remains `Unknown`, not neutral or compatible;
- reviewed caution, interference, redundancy, and pathway evidence remains
  available as observational evidence.

Protocol suggestion rules retain the detected safety or uncertainty signal but
use observational language rather than instructions to remove, reduce, swap,
or add dosing details. Non-empty counterfactuals are reframed from their
structured score deltas; upstream recommendation text is never forwarded.

The public knowledge response retains its internal C# compatibility properties,
but JSON serialization ignores individualized-action fields entirely. They are
not emitted as empty strings or arrays. Frontend types treat those omitted
properties as optional and no public runtime path requires them.

## Legacy-data quarantine

This code change quarantines legacy prescriptive values at the public response
projection and public card. It deliberately does not rewrite or delete:

- in-repository seed and research content;
- `LocalKnowledgeSource` or `DatabaseKnowledgeSource` records;
- any deployed database or other live knowledge store.

Those stores contain legacy dose, schedule, optimization, pairing, and blend
values. A governed data inventory and migration decision is still required to
determine whether those values should remain restricted at rest, be moved to a
non-public evidence record, or be deleted. That decision cannot be made safely
in this repository-only parcel and no live data mutation is authorized.

## Scope and exclusions

The parcel changes only the public projection, public card, blend-summary
wording, public interaction-check projection, protocol-suggestion wording,
focused tests, and this evidence.

It does not modify the source-acquisition work in PR #230, package lockfiles,
the primary `D:\Repos\BioStack` checkout, Claude worktrees, live data,
or deployments.

## Verification

```powershell
rtk npm test -- src/__tests__/components/CompoundIntelligenceCard.test.tsx src/__tests__/components/ToolsDecisionSurfaceBoundary.test.ts
rtk test dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter FullyQualifiedName~BoundaryTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter FullyQualifiedName~KnowledgeEndpointsIntegrationTests --disable-build-servers
rtk git diff --check
```

Verification results:

- Application public-boundary tests: 20 passed, 0 failed, 0 skipped.
- Knowledge endpoint integration tests: 3 passed, 0 failed, 0 skipped.
- Endpoint evidence proves evidence tier/source references survive public
  serialization, individualized-action fields are absent, raw pairing metadata
  returns `Unknown`, and counterfactual/swap arrays are empty.
- Independent re-review passed after confirming all 16 individualized-action
  properties in the public DTO are omitted from JSON and the prior endpoint
  bypasses are closed.
- Frontend tests remain hosted-CI pending. The isolated dependency install did
  not complete and ended with `ENOTEMPTY` in the worktree-only `node_modules`;
  no lockfile changed.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories remain
  unchanged.

## Residual human decisions

1. Assign an owner and retention policy for the legacy prescriptive values at
   rest, including the live database.
2. Decide whether a future versioned C# contract should remove the ignored
   compatibility properties entirely. This parcel omits them from public JSON
   while avoiding an internal source-data migration.
3. Decide whether the `AvoidWith` and drug-interaction observations need
   structured provenance before any broader public use. They remain visible
   because hiding caution signals would reduce useful safety evidence.
