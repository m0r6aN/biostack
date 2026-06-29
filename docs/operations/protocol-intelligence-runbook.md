# Protocol Intelligence Operations Runbook

Protocol Intelligence is educational and observational. Runtime output must be source-first, review-gated, warning-first for high-risk categories, and willing to return Unknown when evidence or review state is insufficient.

## Source refresh cadence

- Daily: check operational feeds used by active modules for availability and fetch failures.
- Weekly: refresh source registry metadata for PubMed/PMC, DailyMed/openFDA, ClinicalTrials.gov, FAERS/openFDA, WADA, OPSS, RxNorm/RxNav, ChEMBL, Reactome, Open Targets, NIH ODS, and DSLD where licensed.
- Monthly: review stale source-quality classifications and artifact versions under `research/protocol-intelligence`.
- Immediate refresh: trigger when a label, WADA list, regulatory status, source license, or high-risk safety boundary changes.

## License review workflow

1. Register every source in the source registry with license, redistribution, commercial-use, credential, and attribution state.
2. Block display, export, and training use when license status is unknown, non-commercial-only, restricted, expired, or pending legal review.
3. Preserve source excerpts only where license permits. Contract endpoints may expose artifact versions and IDs, not restricted source text.
4. Re-review licenses before adding a source to any monetized feature.

## Artifact promotion workflow

1. Load structured artifacts from `research/protocol-intelligence`.
2. Validate contract fields, source references, evidence tier, confidence, review status, and forbidden-output scan result.
3. Require human review for high-risk, regulatory, safety, prescription, hormone-axis, GLP-1, SARM, SERM, peptide, source-quality, adverse-effect, and contradiction claims.
4. Promote only artifacts with approved review status and complete citations.
5. Keep rejected or incomplete artifacts out of user-facing runtime responses.

## Review SLA

- Safety-critical/high-risk artifacts: same business day.
- Regulatory or license-bound artifacts: two business days.
- Standard relationship/source-quality artifacts: five business days.
- Stale WADA, label, or regulatory status findings: same business day when user-facing output is affected.

## Rollback process for bad artifacts

1. Disable runtime visibility for the artifact ID or artifact type.
2. Re-run Protocol Intelligence contract tests and evaluation worker checks.
3. Publish a replacement artifact only after reviewer approval and forbidden-output scan pass.
4. Record the rollback reason, affected artifact IDs, reviewer, timestamp, and replacement status.
5. Verify user-facing endpoints return Unknown or warning-only output while the artifact is disabled.

## Stale WADA, label, and regulatory status handling

- WADA status must cite the current retrieved list year and section. If current-source verification fails, block the status and return a stale-source warning.
- Drug label and regulatory status must come from current retrieved label/regulatory sources, not model memory.
- ClinicalTrials.gov registry status is registry context only. Do not frame registry status as peer-reviewed outcome evidence.
- Retatrutide must remain investigational/not FDA-approved for public use unless current authoritative sources prove otherwise.

## Production safety telemetry

Track only non-sensitive operational events:

- `protocol_intelligence_viewed`
- `protocol_intelligence_unknown_state_viewed`
- `operator_upgrade_from_relationship_gate_clicked`
- `commander_upgrade_from_ambiguity_gate_clicked`
- `high_risk_warning_viewed`
- `source_quality_warning_viewed`

Allowed payload fields: tier, route, panel ID, feature gate ID, artifact type, warning category ID, sourceRefs count, and protocol ID hash. Never log protocol text, compound notes, symptoms, medical details, source excerpts, user-facing explanations, or generated summaries.

## Incident response when forbidden output is detected

1. Treat the event as safety-critical.
2. Remove or disable the affected artifact or response path.
3. Preserve internal evidence: artifact ID, rule ID, timestamp, route, build version, and reviewer state. Do not copy sensitive protocol text into telemetry.
4. Run `ProtocolIntelligenceEvaluationWorker` and focused contract/gate tests.
5. Patch the artifact, scanner rule, or runtime gate before re-enabling output.
6. Document the incident, root cause, affected users if known, reviewer decision, and regression test added.

## Evaluation worker release gate

Run the Protocol Intelligence evaluation worker as a CI/release gate with `Worker:RunMode=ProtocolIntelligenceEvaluation` and `Worker:ProtocolIntelligenceEvaluationOutputPath` set to a durable JSON artifact path.

The gate fails on safety-critical failures for:

- retrieval/citation presence
- forbidden-output absence
- license boundary state
- review gate state
- FAERS caveat
- ClinicalTrials.gov registry-vs-outcome distinction
- WADA stale-source blocking
- Retatrutide investigational handling
