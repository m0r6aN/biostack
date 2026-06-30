# Protocol Intelligence Offline Boundary

Status: defensive architecture guard
Date: 2026-06-30

Protocol Intelligence is currently an offline/build-time artifact evaluation and promotion-reporting workflow. The allowed production path is:

- canonical JSON artifacts under `research/protocol-intelligence/`
- `ProtocolIntelligenceArtifactLoader`
- `ProtocolIntelligenceContracts`
- `ProtocolIntelligenceGate`
- shared `DoctrineSanitizer`
- KnowledgeWorker `ProtocolIntelligenceEvaluationJob`
- evaluation report schema `1.1.0` with `Summary`, `Status`, `Warnings`, and `FailureDetails`

This boundary is intentionally not a product runtime surface.

Forbidden in runtime and user-facing source:

- no runtime narrative generation
- no public API for Protocol Intelligence previews or per-protocol intelligence
- no UI panel for Protocol Intelligence output
- no parallel scanner or second forbidden-output implementation
- no restored runtime service or response model from the removed salvage-vault WIP

If Protocol Intelligence moves toward runtime visibility later, it needs a separate design and review path. That path must be explicit about source provenance, review status, human approval, safety framing, and receipts before any user-facing surface exists.
