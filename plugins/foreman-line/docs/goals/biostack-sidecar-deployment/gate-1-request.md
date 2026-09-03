# Gate 1 Request — BioStack Research Sidecar Deployment

Status: **AWAITING HUMAN RATIFICATION**

Pinned charter base: `4d8754c670a7d4553ade857be80aa170ede85653`

## Decision summary

The charter proposes a separate internal-only Azure Container App, exact one-replica custody for the in-memory job store, a manual protected OIDC release path, managed-identity ACR pull, secret-referenced service auth, explicit dark configuration, separate Gate 3A dark deployment and Gate 3B scaffold enablement, public-scientific-only synthetic UAT, no external provider execution, EvidenceGate unchanged, and provider/source-locator work deferred.

## Exact ratification form

> Gate 1: I ratify D1-D18 and the P01-P04 parcel plan in `charter.md` as written, and grant contingent Gate 2 authorization for isolated shaping, implementation, deterministic verification, adversarial review, and security review of P01-P04 subject to their final exact Allowed Files, Step 0, dependency order, and stop conditions. Gate 3A and Gate 3B remain ungranted. No push, PR, merge, Azure mutation, secret creation/rotation, provider enablement, external call, protected-data use, public ingress, or sidecar/API production configuration change is authorized by this ratification.
