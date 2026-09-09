# Lead Independent Verdict — P01 Round 4

Candidate: `a0929aaf37b77b1ccd9a5ecb5ff9f959cec7f742`

Written after the coordinator's independent source review and deterministic reruns, before any round-four external seat output was read.

## Verdict

**PROVISIONAL ACCEPT — no open P01 implementation blocker found; fresh two-seat adversarial and security review remain mandatory.**

1. Direct CLI TERM/INT handling now aborts active work, prevents new work, allows bounded reconciliation/cleanup phases, and exits nonzero. Every container claim is registered before Docker create.
2. One 150-second deadline reserves cleanup time beneath CI's explicit 180-second TERM plus 20-second KILL escalation. Docker and HTTP work receives a remaining-time cap.
3. Proof disagreement stays a verification error, but a unique cidfile/exact-name/random-label ID is retained solely for label-revalidated exact-ID cleanup and is never started.
4. Canonical duplicate package identities now fail closed.
5. The Hatchling closure is six exact, hash-bound binary artifacts in a builder-only environment. The project wheel is built with no index/isolated resolution, installed locally with no dependency resolution, and build tooling is absent from runtime. CI audits the byte-equivalent closure.

## Independent coordinator evidence

- Worktree clean; full parcel diff exactly eight allowed files, zero outside; diff check clean.
- Node verifier suite: 212 passed, 0 failed.
- Python suite: 53 passed, 1 expected skip, 0 failed.
- Container contract: 9/9 passed against and emitted `sha256:82a077111407934c325c6dfa89b4b0b629ada2b1cdd937d3700722be7cac590f`.
- Runtime probe: UID 100; app/venv root-owned and non-writable; `pip`, `ensurepip`, `setuptools`, `wheel`, `uv`, Hatchling and all five Hatchling dependencies absent as importable modules; `pip`, `pip3`, `uv`, `uvx` absent from PATH.
- Post-run ownership-label census: zero.
- Builder evidence: real Docker TERM during `Created` exited nonzero in 1.721 seconds with zero leftovers; runtime and build-closure audits green; pinned bootstrap and gitleaks green; missing-image no-pull probe green.

## Retained residuals

1. SIGKILL, host loss, runtime destruction, or Docker-daemon loss can make cleanup non-atomic. Labels, cidfiles, and full IDs support attribution and safe recovery; no absolute guarantee is claimed.
2. Fixed hosted runner and language maintained labels can drift within patch lines; action code, base images, bootstrap wheels, archive, and build-backend artifacts are separately immutable.
3. The random local service token remains visible to same-host process inspection while passed to Docker. It has no external authority and is barred from output.
4. P01 secret scanning is checkout-only. P03 owns history-wide coverage and the ruleset correction.
5. P01 logging evidence is warning-level local evidence. P02/P03 retain the explicit production configuration and effective-revision custody obligation.

## Fresh-review question

Reviewers should attack signal timing before/during create, reconciliation, start, operation and cleanup; deadline arithmetic and timeout escalation; cleanup-error claim release; proof-disagreement cases; build-manifest equality/hash provenance; no-index build enforcement; runtime build-tool residue; and the accuracy of retained residuals.
