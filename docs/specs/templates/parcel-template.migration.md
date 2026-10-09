---
parcel_id: "[REPLACE: unique parcel id for this migration spec]"
title: "[REPLACE: short, specific title]"
status: review-candidate
owner: "[REPLACE: human owner id]"
created: "[REPLACE: YYYY-MM-DD]"
updated: "[REPLACE: YYYY-MM-DD]"
delivery_classes: [migration]
guidance_classes: []
substance_function_risk: []
surfaces:
  - "[REPLACE: path of first surface this spec creates or modifies]"
---

# [REPLACE: parcel title] — migration parcel spec

## Compatibility Window

State the range of versions, schemas, or environments this migration must remain compatible with, and the date or event that window closes. [REPLACE: fill in the specific detail for this parcel.]

## Forward/Backward Behavior

Describe how the system behaves when old and new versions of data or code coexist during the migration window, in both directions. [REPLACE: fill in the specific detail for this parcel.]

## Rollback

Explain exactly how this parcel's change is reverted if it is found defective after merge, and what state the system returns to once that revert completes. [REPLACE: fill in the specific detail for this parcel.]

## Data-Loss Analysis

Enumerate every scenario in which this migration could lose or corrupt data, and the specific mitigation or safeguard against each scenario. [REPLACE: fill in the specific detail for this parcel.]

## Environment Plan

Describe the order in which this migration is applied across environments, the verification gate between each step, and who approves each promotion. [REPLACE: fill in the specific detail for this parcel.]

## Extension points used

None.
