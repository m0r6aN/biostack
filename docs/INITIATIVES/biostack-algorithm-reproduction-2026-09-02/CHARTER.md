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
