# Directive: Wave 006 Review Hold

Repo: BioStack. Base: `4d8754c670a7d4553ade857be80aa170ede85653`.
Evidence: `brief.md`, `lead-verdict.md`, `claude-out.md`, `gemini-out.md`, and the four sibling `wave006-*-2026-09-05-review.md` source receipts.
Council: two external CLI seats plus lead. Grok/Codex not dispatched; bounded two-seat scope, not a four-provider review. Exact CLI model versions unreported. No model agreement is biomedical or legal evidence.

## Verdict

Hold all six packets pending correction and independent re-review. This is a review-only PR, not promotion wave 006, a legal determination, a deployment, or a production remediation.

## Convergent Findings

- P1, both seats plus lead: explicit statistical/source closure criteria are needed before promotion; scope must preserve human-controlled production and legal boundaries.
- P1, both seats plus lead: Semax's missing authoritative contraindication support remains blocking; no field-authority recalibration is permitted.
- P2, both seats plus lead: a durable handoff must separate evidence correction from operator actions. Production availability and authenticated-flow status are not inferred from a merged PR.

Convergence is agreement about disposition, not proof of the source content. Lead GETs independently verified the ATLAS endpoint mismatch, creatine study-versus-participant denominator, and 2018 USPSTF kidney-stone statement. Full source receipts record other reviewer findings and access gaps.

## Dissent And Corrections

- Adopt Claude's coverage warning: 23 claims were assessed, not 23 fully verified. The batch now states retrieval gaps explicitly. Source receipts already supply per-claim ledgers. Native reviewer separation from authors satisfies the workflow role boundary but does not establish independent model behavior or experiments.
- Do not adopt a blanket claim that same-host agents cannot independently appraise evidence, or that every delegated scientific correction needs a new human gate. Clint retains legal/medical escalation as specified; independent reviewers plus lead remain the delegated evidence-review process.
- Gemini's "PCAC dossier" finding refers to the public article source, not a verified deployed page or every dossier. Its "verified" labels inherit the brief's observations, not fresh CLI research.
- Five production 404s are a wave-005 availability/refresh gate, not evidence that those packets are incorrect and not a reason to hold the six reviewed packets. Raloxifene's one returned reference is observation only, not a clinical quality verdict.
- Validator corrected PCAC inventory from 19 to 18 claims (KPV has five). Decision scopes contain 33 existing IDs with no mismatch; unscoped PCAC claims retain their existing state.
- Independently authored professional appraisal may share trial/IOM ancestry. Require transparent provenance and a materially different qualifying family, not a newly invented RDA or a new trial for every established fact. Mirrors and recaps are not independent experimental replication.

## Execution Order

1. Lead: land schema-valid request-changes batch and source receipts only. Acceptance: six newest holds loaded by Research mode, six excluded from promotion export, Semax hard blocker retained, zero changes to packets/seeds/routes/authority flags.
2. Separate author lane: repair exact claim IDs in the batch using quoted source text, section/table/page, source type/tier, population, numerator/denominator, measure, time window and uncertainty. Do not silently change regulator/medical language, mark an access failure false, or clear old conflicts by narrative.
3. Fresh reviewer lane: verify amended claims, source ancestry and field-matched A1/A2 support. Reviewer must not belong to the amendment-authoring chain. Repeat offline validation; promote only after explicit approval and all hard gates pass.
4. Clint: assess existing public PCAC wording, source-backed correction proposal and foreign-label/legal boundaries. Article links remain false; summaries are not hidden by those flags. Article #2 waits for source-fidelity repair and its own review, rather than recycling these errors.
5. Clint: resolve wave-005 production execution evidence and authenticated smoke test. Verify the actual worker image/configuration before preparing any Refresh invocation. The current startup path can write even with DryRun=true; do not call it read-only.

## Rules Of Engagement

- Review receipts and batch: lead owns integration; source reviewers own separate receipt files. No concurrent packet writes.
- Merges, deployments, production writes, secrets/infra, Stripe production, legal and money remain Clint-only.
- No corpus or route change in this PR. On a later such change, update all four census tripwires including frontend SeoMetadata.test.ts.
- Stop at unresolved medical/legal interpretation or unavailable authoritative evidence. Keep blocked, unverified, skipped and passing statuses distinct.
