# Release Gates

| Gate | Required evidence | Status | Owner |
|---|---|---|---|
| RG1 CI/deploy | Green main workflow, immutable SHA, live revision/traffic and smoke | failing | platform |
| RG2 product/browser | Critical funnels, calculator, responsive and accessibility evidence | blocked | frontend/product |
| RG3 auth/consent | Live magic-link, return path, ownership, approved consent UI/version | blocked | auth/legal |
| RG4 billing | Approved monthly products/prices plus lifecycle/refund evidence | blocked | billing/product |
| RG5 legal/privacy | Signed approval for policies and customer-facing claims | failing | legal/privacy |
| RG6 data/operations | Migrations, backups, restore drill, RPO/RTO, probes | failing | platform/data |
| RG7 security | All `SECURITY-GATES.md` entries passing or formally waived | blocked | security |
| RG8 observability/support | Monitoring, alerts, redaction, support route/SLA/on-call | failing | operations |
| RG9 rollback | Documented and rehearsed rollback with evidence | failing | platform/release |
| RG10 evidence/handoff | Complete evidence index, risks, decisions, final handoff and owner GO | blocked | coordinator/release owner |

## Verdict

**NO-GO / HOLD.** Any unknown, pending, blocked, or failing blocking gate prevents release.

Candidate `c96bc3b` passes hosted backend/frontend validation, the production dependency audit, production frontend build, offline verification, and current-tree Gitleaks in runs `29283101748`, `29283101730`, and `29283101738`. This closes KEO-64's CI/build scope and the locally remediated KEO-65 code findings. RG1 remains blocked because every production mutation step was intentionally skipped and there is no deployed revision/traffic/live-smoke evidence. Historical credential rotation/full-history proof, proxy verification, operational drills, and named human approvals also remain open.
