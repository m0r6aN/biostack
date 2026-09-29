# Evidence Index — biostack-local-readiness

Assessment-level (pinned `e5b75e0`, 2026-09-18): `VERIFICATION.md` (git-state, `git diff --check` clean, static-presence reads, prior-evidence triage).

Parcel-built (pending execution — rows land here via coordinator merge, never by direct builder edit to this file except through the parcel's evidence file + PR):

| Evidence | Type | Parcel/Scenario | Path/Link | Status |
|---|---|---|---|---|
| boot proof | run record | BIO-LOCAL-001 / LS1, SG-L5/L6 part | `evidence/BIO-LOCAL-001-boot-proof.md` | pending (spec active) |
| public-read proof | run record | BIO-LOCAL-002 / LS2, LS3 part, SG-L2/L7 part | `evidence/BIO-LOCAL-002-public-read-proof.md` | pending |
| auth-isolation proof | run record | BIO-LOCAL-003 / LS4–LS6, SG-L1/L3/L4 part | `evidence/BIO-LOCAL-003-auth-isolation-proof.md` | pending |
| spine-receipt proof | run record | BIO-LOCAL-004 / LS7–LS8, SG-L3/L5/L6 part | `evidence/BIO-LOCAL-004-spine-receipt-proof.md` | pending |
| guidance proof | run record | BIO-LOCAL-005 / LS9, LS3 joint, SG-L2/L7 | `evidence/BIO-LOCAL-005-guidance-enforcement-proof.md` | pending |
| contract-mirror proof | run record | BIO-LOCAL-006 / LS10, SG-L5 part | `evidence/BIO-LOCAL-006-contract-mirror-proof.md` | pending |
| seed-gap inventory | inventory record | BIO-LOCAL-007 / LS12, SG-L8 part | `evidence/BIO-LOCAL-007-seed-gap-inventory.md` | pending (spec active) |
| seed batch A | corpus diff | BIO-LOCAL-008 / LS13 part, SG-L8 | seed file diff + `evidence/BIO-LOCAL-008-batch-a-record.md` | pending (blocked on 007) |
| seed batch B | corpus diff | BIO-LOCAL-009 / LS13 part, SG-L8 | seed file diff + `evidence/BIO-LOCAL-009-batch-b-record.md` | pending (blocked on 007+008) |
| seed batch C | corpus diff | BIO-LOCAL-010 / LS13 part, SG-L8 | seed file diff + `evidence/BIO-LOCAL-010-batch-c-record.md` | pending (blocked on 007–009) |
| seed-run proof | run record | BIO-LOCAL-011 / LS13, SG-L8 part | `evidence/BIO-LOCAL-011-seed-run-proof.md` + updated test output | pending (blocked on 007–010) |

Redaction rule: no secrets, PII, health payloads, key material, or production credentials in any artifact. Violations fail the parcel regardless of technical results.
