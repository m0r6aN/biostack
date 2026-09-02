# Charter

## Objective

Convert the highest-risk algorithm-audit claims into deterministic failing tests, divided into parser, interaction, evidence/provenance, sidecar lifecycle, and outbound-data-boundary parcels.

## Completion

Each parcel must add only tests or test fixtures, run its targeted command, capture the expected current failure, and commit independently. A passing test does not prove a reported defect. Production code changes are prohibited.

## Non-goals

- Fixing defects.
- Benchmarking or model routing.
- Exercising live services or external providers.
- Broadening audit claims beyond reproduced behavior.
