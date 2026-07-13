# Risks

| ID | Risk | Severity | Mitigation/owner | Status |
|---|---|---|---|---|
| R1 | Main deploy is red from test drift and worker exhaustion | critical | focused CI repair, bounded workers, rerun exact SHA / T1,T5 | open |
| R2 | Billing code and live Stripe configuration may diverge | critical | config review + lifecycle test / T3 | open |
| R3 | Unapproved policy/consent creates legal exposure | critical | human approval + versioned acceptance / T6 | open |
| R4 | Missing backup/restore, probes, monitoring and rollback hides or prolongs outage | critical | operational parcels and drills / T5 | open |
| R5 | Auth/ownership failure could expose customer data | critical | live auth and isolation/security review / T1,T2,T6 | open |
| R6 | Provider leads lack notification/SLA/retention ownership | high | operational contract / T4 | open |
| R7 | Focused green tests could be mistaken for release proof | high | evidence contract and gate discipline / T7 | mitigated, ongoing |
