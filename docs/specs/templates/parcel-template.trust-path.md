---
parcel_id: "[REPLACE: unique parcel id for this trust-path spec]"
title: "[REPLACE: short, specific title]"
status: review-candidate
owner: "[REPLACE: human owner id]"
created: "[REPLACE: YYYY-MM-DD]"
updated: "[REPLACE: YYYY-MM-DD]"
delivery_classes: [trust-path]
guidance_classes: []
substance_function_risk: []
surfaces:
  - "[REPLACE: path of first surface this spec creates or modifies]"
---

# [REPLACE: parcel title] — trust-path parcel spec

## Trust Boundary

Describe exactly where this component's trust boundary sits, what is inside versus outside that boundary, and what crossing it requires. [REPLACE: fill in the specific detail for this parcel.]

## Fail-Open/Closed Behavior

State whether this component fails open or fails closed when its upstream trust dependency is unavailable, and why that choice is correct here. [REPLACE: fill in the specific detail for this parcel.]

## Issuer/Verifier Ownership

Name who owns issuance and who owns verification for this trust artifact, and how those two roles remain independently accountable. [REPLACE: fill in the specific detail for this parcel.]

## Redaction

Describe what sensitive content is redacted before this artifact leaves the trust boundary, and how a reviewer confirms the redaction is complete. [REPLACE: fill in the specific detail for this parcel.]

## External Contract Version

State the exact pinned version of the external contract this component depends on, and the process for bumping that pin safely. [REPLACE: fill in the specific detail for this parcel.]

## Extension points used

None.
