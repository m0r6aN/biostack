# Parcel: KEO-66-STATIC-PROTOCOL-TEMPLATE-QUALIFICATION-027

Status: implementation complete; hosted frontend verification pending.

## Objective

Remove a false-success interaction from the static protocol template. The
template previously changed a button to `Doses Logged` and displayed a success
toast even though it made no persistence or API request.

## Contract

The exported reference template must not imply that dose activity was saved.
Its dose control is now disabled and explicitly states:

> Reference preview — dose activity is not saved

The obsolete `logDose()` simulation and its success message are removed.
Other reference-template content and navigation are unchanged.

## Scope

This parcel changes only:

- `frontend/templates/protocol_template.html`
- `frontend/src/__tests__/templates/ProtocolTemplateQualification.test.ts`
- this parcel and its routing event

It does not touch PR #230, package manifests or lockfiles, live data, billing,
email, deployments, or the primary BioStack checkout.

## Verification

The focused test asserts that:

- the fake `logDose()` handler and success strings are absent;
- the control is disabled and marked `aria-disabled`;
- the visible copy and title disclose that no activity is saved.

Local Vitest execution is environment-blocked because this clean worktree has
no installed frontend dependencies (`vitest/config` is unavailable). Hosted CI
is the test and production-build acceptance gate.

Static verification:

- `git diff --check`: passed.
- routing event schema: passed.

## Residual boundary

This qualification does not implement persistent dose logging. A future
tracking feature would require an authenticated server contract, authorization,
idempotency, retention, and privacy review before the control could claim
success.
