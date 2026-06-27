# Agent Directive: BioStack Lane G - Evidence-Bound Runtime Receipts

You are an expert .NET backend engineer, governance systems architect, and Keon Runtime integration specialist.

Your assignment is to implement Lane G from `docs/plans/biostack-backend-intelligence-master-plan.md`.

This is an implementation task. Focus only on making BioStack Runtime receipts meaningful, evidence-bound, actor-aware, and testable.

## Mission

BioStack already issues Keon Runtime receipts, but the current receipts are too thin. Many receipt calls use empty `EvidenceRefs`, hardcoded tenant/actor values, and a uniform commentary-only status. This means receipts prove that something happened, but not what evidence, policy, graph artifact, source, claim, or user context justified it.

Fix that.

The goal is to make receipts matter.

## Scope

Backend only.

Inspect and modify:

* `backend/src/BioStack.Api`
* `backend/src/BioStack.Application`
* `backend/src/BioStack.Infrastructure`
* `backend/src/BioStack.Domain`
* `backend/src/BioStack.Contracts`
* Related test projects under `backend/tests`

Do not modify frontend code unless required by backend contract changes, and avoid frontend work unless absolutely necessary.

## Required Outcomes

Implement a receipt evidence layer that ensures important BioStack decisions emit receipts with:

* Correct actor
* Correct tenant or user context
* Stable receipt class
* Meaningful effect status
* Non-empty evidence references when evidence exists
* Policy decision metadata when a policy gate is involved
* Source, knowledge, graph, claim, protocol, or user-observation references where applicable
* Tests proving the above

## Current Problems to Fix

### Problem 1: Empty EvidenceRefs

Find every `IssueReceiptAsync` call that passes:

`EvidenceRefs: []`

Replace this with evidence references derived from the actual decision inputs.

Examples of acceptable evidence refs:

* `knowledge-entry:{id}`
* `source:{id}`
* `source-intake:{id}`
* `staged-artifact:{id}`
* `transcript-candidate:{artifactId}`
* `evidence-claim:{id}` if present
* `relationship-edge:{id}` if present
* `compound-graph:{hash}` if present
* `protocol:{id}`
* `protocol-run:{id}`
* `profile:{id}`
* `check-in:{id}`
* `policy:{policyIdOrHash}`
* `safety-gate:{decisionId}`
* `collective-deliberation:{id}` if present

If the current code does not yet have a structured entity for one of these, use the most stable available existing identifier and clearly document the fallback.

### Problem 2: Hardcoded Tenant and Actor

Find receipt issuance paths using hardcoded actor or tenant values such as:

* `biostack-public`
* `biostack-system`

Replace these with values from the authenticated user context where possible.

Use existing abstractions such as:

* `ICurrentUserAccessor`
* profile ownership context
* user id
* profile id
* tenant/user context already available in the endpoint

System actors are allowed only for true system-initiated operations, such as offline worker jobs or scheduled precompute tasks. User-triggered operations must identify the acting user.

### Problem 3: No Receipt Taxonomy

Introduce stable receipt classes as constants or enum-like values.

Suggested families:

* `source.intake.received`
* `source.transcript.resolved`
* `source.candidate.staged`
* `source.review-state.changed`
* `source.artifact.promoted`
* `evidence.claim.created`
* `evidence.claim.updated`
* `evidence.contradiction.detected`
* `evidence.contradiction.resolved`
* `intelligence.substance-profile.generated`
* `intelligence.compatibility-matrix.rebuilt`
* `intelligence.graph-artifact.used`
* `deliberation.stack-review.completed`
* `safety.gate.triggered`
* `safety.warning.surfaced`
* `safety.unsafe-request.refused`
* `personalization.overlay.applied`
* `recommendation.rationale.generated`
* `protocol.phase-plan.generated`
* `monitoring.plan.generated`
* `user.dose-log.recorded`
* `user.outcome-observation.recorded`
* `export.care-team-summary.generated`
* `admin.override.performed`

Do not over-implement all families if the current code does not support them yet. Create the taxonomy and wire the classes that correspond to currently implemented behavior.

### Problem 4: Receipt Issuance Is Scattered

Introduce a centralized helper or service, for example:

`ReceiptEvidenceBuilder`

or

`RuntimeReceiptFactory`

It should help construct:

* actor
* tenant/profile context
* receipt class
* evidence refs
* policy refs
* protocol refs
* source refs
* correlation id
* effect status

Keep it simple. Do not build a giant framework. Build enough structure that future agents can extend receipt families without duplicating logic.

## Required Design Rules

1. No vanity receipts.
   Do not add receipts for page views or low-value UI actions.

2. A receipt is warranted only when one of these is true:

   * evidence is used
   * a policy is evaluated
   * a safety boundary is triggered
   * a knowledge artifact changes state
   * a user-facing intelligence output is generated
   * a user observation is recorded
   * an admin governance decision is made
   * an export/report is generated

3. Receipts must be deterministic enough to test.

4. If evidence exists, `EvidenceRefs` must not be empty.

5. If evidence is missing, the receipt should reflect the evidence gap explicitly rather than pretending evidence exists.

6. Preserve BioStack’s educational and observational boundary. Do not introduce prescribing, dosing advice, diagnosis, injection instructions, or effect-bearing AI behavior.

## Implementation Targets

Start with the highest-value receipt paths:

1. Protocol intelligence / protocol endpoint receipts.
2. Stack review receipts.
3. Knowledge-source intake receipts.
4. Transcript candidate staging/review/promotion receipts.
5. Policy/safety gate receipts.
6. User dose/log or check-in receipts if currently implemented.

Do not try to wire every possible future receipt class in one PR. Make the first PR high-value and clean.

## Tests Required

Add or update tests proving:

1. User-triggered receipt actor is not hardcoded.
2. Receipt evidence refs are populated for protocol intelligence outputs when knowledge/protocol evidence is available.
3. Receipt evidence refs are populated for stack review when stack entries or knowledge entries are used.
4. Admin knowledge promotion or review-state changes produce governance receipts with the relevant candidate/artifact refs.
5. Safety/policy gate receipts include policy or gate references.
6. No receipt is emitted with empty `EvidenceRefs` in paths where evidence was available.
7. System actor is used only for system-initiated operations.

Also run the relevant backend test suites and report the exact commands and results.

## Deliverable

Create or update implementation code and tests.

Then provide a concise handoff report including:

* Files changed
* Receipt classes added
* Receipt paths wired
* Evidence ref formats used
* Actor/tenant resolution approach
* Tests added/updated
* Commands run
* Risks or follow-ups
* Any paths intentionally deferred

## Success Criteria

This lane is complete when BioStack receipts no longer merely say, "something happened."

They must say:

* who initiated it
* what policy applied
* what evidence was used
* what decision was made
* what output was produced
* why the receipt matters

This is the first step toward making BioStack the proof case for Keon Runtime in a messy, evidence-sensitive industry.
