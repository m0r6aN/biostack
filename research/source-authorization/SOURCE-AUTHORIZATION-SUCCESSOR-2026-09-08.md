# Source authorization successor

The current registry had drifted from the exact bytes bound by the issued seven-source decision.
Nineteen unsupported approval declarations are returned to pending/disabled, leaving 30 records:
seven approved and active, 21 pending and disabled, and the two C3 authorization classes retired.
The seven approved source objects are unchanged. This is containment and reconciliation of the
historical acquisition scope, not a new rights determination for the other sources.

`recommended-seven-source-decisions.v2.json` preserves every source object, approval, review date,
stage gate, and product doctrine from v1. Its only differences are the schema version, generation
time, exact registry hash, and A1's future legal-rights-approver assignment to Clint Morgan. The owner
array exactly matches A1's `effectiveOwners` in `owner-source-decisions-2026-09-08.v1.json`.
Generation time records mechanical re-emission; it is not a new review or approval date. B1 covers
seven exact GSRS substance identities and does not authorize these seven source lanes.

The issued v1 decision, its schema, both historical KEO-74 overlays, and the September 8 owner receipt
retain their original bytes. The historical registry snapshot was copied verbatim from commit
`cbaed8e`, where its SHA-256 is the v1-bound `3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28`.
The snapshot is historical evidence, not an alternative current acquisition input.

The unpublished registry's accidental CRLF conversion was normalized to LF before computing the
successor binding. Its final SHA-256 is
`71ed755fbf532f69ccec516e95aa0c821f645263dc081d97f24a6ab3f9c2503d`.
Git attributes prevent automatic newline conversion of byte-bound authorization files and snapshots.
Earlier recovery hashes remain evidence of the intermediate files and must not be presented as final.

## Enforcement

The new v2 schema checks the hash's format. Runtime planning still compares it with the raw registry
bytes and rejects any mismatch. Schema selection uses an explicit version allowlist; missing or
unknown versions cannot fall back to another schema. The old schema still rejects an altered binding
declared as version 1.2.0.

Before acquisition planning, the job also checks that the decision's approved source set equals the
registry's approved, active, acquisition-enabled set. Rebinding a hash after adding or removing an
active source cannot bypass that check. Per-source method, field, approval, and review checks remain.
The worker image packages v2 as its current decision input. CI watches the authorization directory
and runs the version/continuity and original receipt tests before the image can be published.

`source-authorization-continuity-2026-09-08.v1.json` is an audit record with **no authority**. It is
not a runtime authorization input. CI verifies every historical and successor binding and compares
the complete decision documents after restoring only the four permitted mechanical fields. Hashes
alone are insufficient: the test also requires unchanged source scopes and the exact owner-approved
A1 owner array. The original overlay tests continue unchanged.

NCCIH's distinct reviewer assignment and pending reviewer action remain separate. Runtime requires
a substantive review audit, different operator/reviewer identifiers, review timing, and attestations.
It does not authenticate that the supplied reviewer identifier belongs to Sandy Morgan; that existing
limitation is not resolved by the continuity record. No reviewer action or evidence approval is recorded.

## Validation and release status

Independent verification of the saved pre-successor patch was 906 passed / 4 failed / 910 total.
The four failures concerned current registry binding and resulting acquisition preflight. The
successor adds compatibility, historical-byte, tamper, scope-drift, and semantic-continuity coverage.
The full worker suite after the successor change passed **924 / 924**, with zero failures and zero
skips (2m33s). This includes 14 added tests and the original historical-overlay tests. The raw TRX is
retained locally at `research/output/owner-decisions-20260908/successor-worker-tests/successor-worker-tests.trx`.
`git diff --check` and all nine continuity bindings also passed. The pinned Linux worker Docker build
succeeded. A network-disabled, read-only container verified the packaged v2 decision, final registry,
and both schema hashes against local files. The worker itself was not executed. Claude's independent
review found no blockers; its minor stale-default finding was fixed by pointing the read-only source
inventory tool at v2. Historical v1 fixture use is now explicitly documented in the test helper.
Hosted CI has not yet run for this patch.

This change does not complete any of the 30 DrugBank replacement claims, establish production
containment, approve canonical evidence, or authorize merge/deployment. The source-item and release
work remains tracked separately.
