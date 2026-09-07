# Source Activation Governance Breach

Date: 2026-09-07. Base: `aeda45f`.
Severity: **high**. Self-reported. Found while running the full worker test suite for unrelated work.

## What Happened

The 2026-09-07 registry expansion (PR #277) and activation fix (PR #281) added 17 source classes to
`research/input/sources/pilot-source-registry.json` and enabled acquisition on them. **That bypassed this
repository's own source-activation governance process.**

`research/source-authorization/recommended-seven-source-decisions.v1.json` is a
`source-authorization-decision-batch` that:

- binds to the registry by exact bytes — `registryBinding.sha256`;
- authorizes exactly **seven** source lanes: `fda`, `pubchem`, `pubmed`, `clinicaltrials`, `dailymed`,
  `nih-ods`, `nih-nccih`;
- sets `stageGates.sourceActivationRequiredApprovals: ["legalRights"]`;
- names the role holders, including **`legal-rights-approver`: Johnathan Harper**.

The product owner is Clint Morgan, who also holds `evidence-reviewer`. **He does not hold
`legal-rights-approver`.** The ratification that authorized the expansion was given by the product owner in
conversation. That is not the approval this gate requires, and the approver is a different named person who
was never consulted.

I did not know this artifact existed. I found the registry, the schema and the worker's activation policy,
and treated the product owner's ratification as sufficient. It was not.

## How It Surfaced

Not by review — by the test suite, which I had not run on PRs #277, #281 or #283. Eight tests fail:

| Test | Symptom |
|---|---|
| `Validator_Accepts_Governed_Pilot_Source_Registry` | expected 13 sources, actual 30 |
| `Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict` | expected 13, actual 30 |
| `Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority` | expected 13, actual 30 |
| `BuildJson_CurrentArtifacts_IsStableAndOmitsRunTimeClaims` | expected 7, actual 26 |
| `PlanBuilder_Real_Registry_Raw_Byte_Hash_Matches_Decision_Binding` | registry SHA no longer matches the decision binding |
| `Validator_Accepts_Recommended_Seven_Source_Decision_Batch_With_Exact_Registry_Binding` | same, inverse assertion |
| `PlanBuilder_CurrentArtifacts_Produce_Ready_Intents` | expected 490 ready intents, actual **0** |
| `Job_validates_current_exact_inputs_and_preflight_without_transport` | preflight did not match 70/490/490/0/7 |

**These failures are the governance mechanism working.** The acquisition plan produces zero ready intents
because the registry no longer matches any authorization. That is the system correctly refusing to acquire
from an unauthorized registry state, and it is the correct behaviour.

Current registry `sha256`: `883a892152d7beb7a9217ca8d988404bbda81523416764c74073cc9435297e9c`
Decision-bound `sha256`: `3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28`

## What I Did

**Deactivated all 17 unapproved lanes** — `acquisition.enabled: false`, with an `accessNotes` entry on each
naming the missing approval. The seven authorized lanes are untouched and remain enabled. Effect:

- `CandidatesForPromotion` returns to **1 (Semaglutide)**. **LL-37 reverts to blocked**, undoing the movement
  PR #281 caused. That movement was never ruled on and rested on an activation that was not authorized.
- Manifest returns to 72 blocked / 5 review-required / 1 candidate.

Registration and alias data are **retained**. Recording which source belongs to which class is documentation,
not activation, and it carries no rights. Only activation is reversed.

## What I Deliberately Did Not Do

- **I did not update any of the eight tests, and I did not rewrite the hash binding.** Editing
  `registryBinding.sha256` to match a registry I changed would manufacture an authorization that no human
  issued. That is precisely the failure this project exists to prevent, and blanket approval from the product
  owner does not extend to an approval role he does not hold. The tests stay red, and red is the correct
  state until a new decision batch exists.
- **I did not revert PR #277's registrations or PR #283's sources.** They are inert without activation and
  they carry the evidence trail.
- **I did not draft a v2 decision batch as if it were approved.** A proposal is recorded below; it is not an
  artifact and is not bound to anything.

## What Is Required To Clear This

1. A **source-authorization decision batch v2** covering the 17 added lanes, issued through the normal
   process, with a **`legalRights` approval from Johnathan Harper** — the gate's named holder. Several lanes
   may also trip `sourceActivationConditionalApprovals: ["securityData"]` (Pradic Patel), which should be
   assessed per lane rather than assumed.
2. That batch bound to the registry's exact bytes at the time of issue.
3. Only then, re-enable acquisition on the approved lanes and update the eight tests to the newly authorized
   counts and hash.

Until then the correct state is: registered, rights-ratified by the product owner, **not activated**.

## Root Cause, Honestly

I verified my work against the schema, the worker pipeline and my own audit tool, and treated a passing
pipeline run as evidence of correctness. I never ran the test suite on the three PRs that touched the
registry. The suite encodes governance bindings that no amount of pipeline output would have revealed.

**Standing rule to add: run the full worker test suite before shipping any change to
`research/input/sources/`, `research/source-authorization/`, or the worker's policy classes.** A green
pipeline is not evidence that a governance binding survived.
