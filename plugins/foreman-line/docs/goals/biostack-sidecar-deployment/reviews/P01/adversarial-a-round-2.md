# P01 Adversarial Review A — Round 2 Candidate `6409fc7`

Worktree was clean at completion. The reviewer ran 175 Node tests and the complete local image verifier.

## Findings

1. **HIGH / release-blocking — static census cleanup remains unsafe.** The three static censuses use anonymous `docker run --rm` outside the cidfile lifecycle. A reproduced 500 ms child timeout left the exact candidate container running (`scripts/verify-research-sidecar-container.mjs:733-760`).
2. **HIGH / release-blocking — installer-free runtime is false.** `/usr/local/bin/pip` and `pip3` execute as UID 100, and system Python imports `pip` and `ensurepip`; the verifier checks only virtualenv distributions plus `uv`/`uvx` (`Dockerfile:32`; verifier `:386-424,750-760`).
3. **HIGH — log safety depends on an undocumented downstream setting.** The exact image at `info` level logs generated job IDs; P01 passes by forcing `BIOSTACK_RESEARCH_LOG_LEVEL=warning`, but the candidate does not record the P02/P03 obligation (`verifier:581,935-949,1072`; `PARCELS.md:26-31`).
4. **MEDIUM — late ownership proof after timeout is unverified.** The cidfile is read once, so a daemon-side create completing after the read can escape cleanup (`verifier:627-665,968-975,1104-1105`).
5. **MEDIUM — isolated build closure remains incomplete.** Exact Hatchling versioning does not hash-lock its isolated artifact/transitives; the residual is accurately disclosed.

## Requested moves

Use the owned lifecycle for every census; remove system and virtualenv installer surfaces including `ensurepip`; bind the warning log level and downstream handoff; add a random ownership label plus bounded late-proof reconciliation; retain or improve the explicit PEP 518 residual without overclaiming.

Release-blocking P01 defect remains: **yes**, findings 1 and 2.
