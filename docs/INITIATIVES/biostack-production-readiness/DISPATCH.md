# Dispatch

| Work item | Type | Track | Status | Dependencies | Blocks |
|---|---|---|---|---|---|
| PR-DOC-001 | documentation parcel | T7 | in-progress | none | durable state |
| PR-CI-001 | test/CI repair parcel | T1/T5 | ready | PR-DOC-001 | SC1 |
| PR-AUTH-001 | integration parcel | T1/T2 | proposed | green CI, live email config | SC2/SC3 |
| PR-BILL-001 | integration parcel | T3 | blocked | human packaging + Stripe config approval | SC4/SC5 |
| PR-DATA-001 | operations parcel | T2/T5 | blocked | RPO/RTO decision | SC6 |
| PR-PROV-001 | operations parcel | T4 | blocked | owner/SLA decision | SC7 |
| PR-SEC-001 | security review | T6 | proposed | stable candidate | all security gates |
| PR-REL-001 | release evidence | T7 | blocked | every prior blocking gate | GO/NO-GO |

Each implementation parcel requires its own branch/worktree, exact allowed files, verification, and handoff.
