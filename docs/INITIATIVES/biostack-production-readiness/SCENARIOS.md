# Integration Scenarios

| ID | Surface | Scenario | Environment | Type | Status | Evidence required |
|---|---|---|---|---|---|---|
| SC1 | S5 | Full main CI then Azure deploy and smoke | CI + production | positive/failure | failing | green run, revision/SHA, URLs, smoke |
| SC2 | S1 | Magic-link sign-in returns to intended protected flow | staging | positive/failure | blocked | test identity, delivery, browser trace |
| SC3 | S1/S3 | User cannot read/write another user's profile/protocol | staging | negative | blocked | authenticated isolation test |
| SC4 | S2 | Monthly checkout through renewal, failure, cancel, downgrade and portal | Stripe test/live-authorized | positive/failure | blocked | receipts with secrets/PII redacted |
| SC5 | S2 | Invalid/replayed webhook is denied/idempotent | Stripe test | negative | blocked | signed fixture and persisted-state proof |
| SC6 | S3 | Migration and backup restore recover service within approved objectives | staging | failure/recovery | blocked | backup policy, timed restore, probes |
| SC7 | S4 | Provider lead is durable, minimal, rate-limited and operationally owned | staging | positive/negative | blocked | queue, notification and SLA evidence |
| SC8 | S6 | Analytics/support operate without health or identity leakage | staging | positive/negative | blocked | event inspection and escalation drill |
| SC9 | T1 | Keyboard/mobile/browser/a11y critical funnels pass | staging | positive | blocked | browser matrix and accessibility report |

No scenario is passing merely because a component or focused test passed.
