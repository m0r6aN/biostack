---
parcel_id: "[REPLACE: unique parcel id for this standard spec]"
title: "[REPLACE: short, specific title]"
status: review-candidate
owner: "[REPLACE: human owner id]"
created: "[REPLACE: YYYY-MM-DD]"
updated: "[REPLACE: YYYY-MM-DD]"
delivery_classes: [standard]
guidance_classes: []
substance_function_risk: []
surfaces:
  - "[REPLACE: path of first surface this spec creates or modifies]"
---

# [REPLACE: parcel title] — standard parcel spec

## Objective

State the single, concrete problem this parcel solves and why it matters now, in plain, falsifiable terms that a reviewer can check against the acceptance criteria below. [REPLACE: fill in the specific detail for this parcel.]

## Surfaces

List every file, schema, script, and directory this parcel creates or modifies, and nothing else; this list is the bounded scope a reviewer checks the actual diff against. [REPLACE: fill in the specific detail for this parcel.]

## Contracts

Describe every data shape, interface, or document contract this parcel defines or consumes, including upstream and downstream dependencies it must remain compatible with. [REPLACE: fill in the specific detail for this parcel.]

## Acceptance Criteria

Enumerate the specific, checkable conditions that must hold for this parcel to be considered complete, each phrased so a reviewer can mark it pass or fail without guessing. [REPLACE: fill in the specific detail for this parcel.]

## Tests

Describe the deterministic verification this parcel runs, what each check asserts, and how a reviewer reproduces the same pass or fail outcome independently. [REPLACE: fill in the specific detail for this parcel.]

## Rollback

Explain exactly how this parcel's change is reverted if it is found defective after merge, and what state the system returns to once that revert completes. [REPLACE: fill in the specific detail for this parcel.]

## Extension points used

None.
