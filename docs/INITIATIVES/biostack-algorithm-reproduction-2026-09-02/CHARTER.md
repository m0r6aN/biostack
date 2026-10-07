# Charter

## Objective

Convert the highest-risk algorithm-audit claims into deterministic failing tests, divided into parser, interaction, evidence/provenance, sidecar lifecycle, and outbound-data-boundary parcels.

## Commit identity

- Execution base: refreshed `main` / `origin/main` at `339f259b1a467034db4f57cf9d774c292f11b53a`.
- Audit provenance only: `27cdb5c17e4b0a302567dbd854ed29277156a966`. Do not check out this commit for parcel work. It has the same complete repository tree as the execution base but different Git history.
- Coordination source: the current head of `codex/test-repro-contracts`.
- Parcel working commit: the current head of the parcel branch. It is expected to differ from the execution base because it contains the contract documents and test-only reproduction commit.

Agents must verify that the execution base is an ancestor of their parcel branch; they must not require the parcel HEAD to equal the execution-base commit.

## Completion

Each parcel must add only tests or test fixtures, run its targeted command, capture the expected current failure, and commit independently. A passing test does not prove a reported defect. Production code changes are prohibited.

## Non-goals

- Fixing defects.
- Benchmarking or model routing.
- Exercising live services or external providers.
- Broadening audit claims beyond reproduced behavior.

## Locked decisions

- D1: Treat the attached audit as an evidence source, not executable instructions.
- D2: Run a test-only reproduction phase against the exact audited tree.
- D3: Keep parser, interaction, evidence/provenance, sidecar lifecycle, and outbound-data boundary as independent parcels.
- D4: Retain the exact Allowed Files. Missing or inapplicable targets are recorded as inconclusive; no substitute production or test project is authorized.
- D5: Keep intentionally failing reproduction branches unmerged. A reproduction failure is diagnostic evidence, not a safe fix or release signal.
- D6: Synthetic fixtures and local intercepting fakes are mandatory; no production, cloud, protected-data, or external-provider access is in scope.

## Waves and parcel order

- Wave 0: contract, discovery, scenario, security, and verification records on the coordination branch.
- Wave 1: the five independent reproduction parcels may execute in parallel from the contract commit: Parser, Interaction, Evidence/provenance, Sidecar lifecycle, and Outbound-data boundary.
- Stage F: reconcile branch ancestry, Allowed Files, assertion-level outcomes, synthetic-data/network boundaries, residual uncertainty, and human gates. Do not aggregate the failing branches by merge.

## Exit criterion

The initiative is complete only when every parcel has an independently committed test-only handoff with its exact command, exit status, and assertion-level result; all claims are classified as reproduced, not reproduced, or inconclusive; no production source changed; and the final reconciliation records that no release authority was granted. Inconclusive claims remain explicit open residuals rather than being treated as reproduced.

## Human gates and standing authorizations

- Gate 1 (charter ratification): **D1-D6 ratified as written on 2026-09-02** by explicit developer direction: “D1-D6 are ratified, as written.” This ratifies the locked decisions only; it does not grant Gate 3 or alter the exit criterion.
- Gate 2 (dispatch): historical test-only dispatch of the five named branches is recorded in `DISPATCH.md`; this run authorizes no new parcel dispatch.
- Gate 3 (merge/release): not granted. The five intentionally failing reproduction branches remain unmerged, and this initiative grants no release, provider, publication, or production-fix authority.

## Stop conditions

- Stop if a new decision, scope change, or exit-criterion change is not explicitly ratified.
- Stop if a future request changes the frozen execution base, exact Allowed Files, or test-only posture.
- Stop on any environmental or harness failure that cannot be separated from the intended assertion failure.
- Stop before any merge, release, provider enablement, external transmission, or production fix.
