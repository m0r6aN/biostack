# ADR-001: BioStack Scientific Research Sidecar (ToolUniverse + Local Inference + Kompress)

| Field | Value |
|---|---|
| Status | **Accepted for foundation implementation** |
| Date | 2026-08-02 |
| Supersedes | — |
| Related | `docs/architecture/adr-biostack-source-first-knowledge-engine.md`, kickoff.md |
| Decision owners | Human product + engineering approval required before public UX of Class B/C dosing-context |

---

## Context

BioStack needs deeper compound research: identity resolution, published regimens, adverse events, mechanisms, pathways, and structured extraction from literature—without becoming a prescribing engine and without giving the .NET backend unrestricted ToolUniverse access.

A Python sidecar is the practical boundary for ToolUniverse (Python SDK), while BioStack retains ownership of contracts, review, privacy, and user-facing comparison.

---

## Decisions

### D1. Integration seam

**Decision:** Combination approach.

| Layer | Responsibility |
|---|---|
| Application | `IScientificResearchProvider`, job orchestration abstractions, model/compression abstractions |
| Infrastructure | HTTP client to sidecar; Ollama adapter; Kompress adapter |
| Python sidecar | Allowlisted research workflows only; ToolUniverse skill execution; optional GPU-assisted stages |
| KnowledgeWorker / review | Staging, trust, evidence gates, human review, promotion |

**Reject:** Unrestricted `ExecuteAnyTool` from BioStack.  
**Reject:** Second independent promotion lifecycle.  
**Prefer:** Extend candidate → review → promote patterns already proven for transcript/source intake.

### D2. Sidecar language and packaging

**Decision:** Python **3.12** scientific research sidecar.

Base image:

```text
ghcr.io/astral-sh/uv:python3.12-bookworm-slim
```

Location:

```text
backend/src/BioStack.Research
```

Package module: `biostack_research_sidecar`.

**Why Python sidecar (not pure .NET):** ToolUniverse SDK and many scientific extraction libraries are Python-native. Isolating them protects the .NET process from CUDA/model failures and arbitrary tool surface.

### D3. Transport

**Decision:** Internal **HTTP/JSON** with BioStack-owned schemas (OpenAPI + JSON Schema). Async job semantics for long-running research.

| Option | Choice |
|---|---|
| gRPC | Deferred; HTTP first for operability |
| MCP as domain contract | **Rejected** for production domain contract |
| MCP for dev agents | Allowed for operators, not canonical product API |

Service authentication required; not publicly exposed.

### D4. Job lifecycle and data ownership

| Concern | Owner |
|---|---|
| Research job state machine | Sidecar (execution) + BioStack (request/correlation) |
| Immutable raw artifacts | BioStack storage after receipt (content-addressed hashes) |
| Normalized candidate claims | BioStack staging tables / candidate store |
| Canonical knowledge | BioStack only after human-approved promotion |
| Sidecar local cache | Ephemeral; not source of truth |

Sidecar **must not** write BioStack databases.

### D5. Failure, rollback, kill switches

- Global kill switch + per-workflow kill switches (env/config)
- Fail closed on ambiguous identity / unresolved required sources
- Partial results marked partial; never silent empty → “no evidence”
- GPU/Ollama/Kompress failures degrade without taking down retrieval-only paths
- Kill switch leaves existing knowledge base operational

### D6. Privacy boundary

Initial sidecar inputs: public scientific identifiers and research parameters only.  
**No** user PHI/PII/protocol history to ToolUniverse or hosted fallbacks.  
User-vs-evidence comparison remains in BioStack Domain/Application (deterministic).

### D7. Security boundary

- Non-root container user (`biostack`)
- Internal network only
- Service auth
- Outbound allowlist where practical
- Tool allowlist only
- No arbitrary Python/shell execution endpoints
- Secrets outside source control; redacted logs
- Pin ToolUniverse to exact tested release (**pin deferred until smoke-approved**; do not install `tooluniverse[all]` blindly)
- SBOM and license inventory before production enablement

### D8. Review and promotion

Reuse:

- pending / deferred / rejected / approved_for_promotion style states
- EvidenceGate + DoctrineSanitizer banned phrases
- TrustGate Class A/B authority
- Knowledge-ingest fences and explicit admin override receipts

Sidecar output = **candidate evidence packet**, never canonical write.

### D9. GPU acceleration boundary

**Decision:** Logical separation of CPU/Network worker vs GPU worker.

For PoC: **same deployable** may host both, but interfaces, queues, resource ownership, and failure isolation must remain separable.

**Preferred runtime for local models on this workstation:** host **Ollama** (verified GPU) rather than loading duplicate large models inside a RAM-starved Docker Desktop VM.

GPU worker may later be a separate service when:

- Docker memory is raised
- CUDA passthrough is validated end-to-end
- Concurrency and VRAM budgets are benchmarked

Execution modes: `Auto` | `GpuPreferred` | `GpuRequired` | `CpuOnly` | `HostedFallbackAllowed`.

Default concurrency: **one heavyweight GPU/inference job at a time**.

GPU is never an architectural prerequisite.

### D10. Local inference / Ollama

**Decision:** BioStack-owned provider abstraction with **Ollama infrastructure adapter**.

| Surface | Use |
|---|---|
| Ollama native API | Primary adapter target |
| OpenAI-compatible API | Optional secondary if needed for shared clients |
| Direct Ollama types in Domain/Application | **Forbidden** |

Router is an **execution policy component**, not an autonomous authority.

Initial candidate model: `qwen3.5:9b` digest `6488c96fa5fa` (not approved until BioStack benchmarks pass).  
`gemma4:12b` not installed; do not download until measured gap justifies it.

Cloud Ollama models must not be selected on local-first routes without explicit policy permission.

### D11. Model registry and routing

BioStack owns:

- Model capability profiles (digest-pinned)
- Task classes and evidence-risk classes
- Benchmark results and approval status
- Routing policy versions

High-impact extractions fields (dose/regimen, AE, contraindications) require source locations + human review before promotion.

### D12. Keon Kompress

**Decision:** Consume Kompress through a **BioStack-owned compression provider abstraction**.

| Option | Choice |
|---|---|
| In-process .NET for API admin tools | Exists today |
| Production research path | Service contract with tenant + job + correlation isolation |
| Python sidecar direct demo contract | **Rejected** without production semantics |
| MCP as sole production surface | **Rejected** |

Rules:

- Persist original + BioStack full hash **before** compression
- System/developer/governance messages **never** compressed
- Kompress marker ≠ BioStack canonical hash
- Failure → send original or chunk; never silent truncate required evidence
- Kompress optional; unavailability must not stop source gathering

### D13. ToolUniverse pinning

Do **not** install `tooluniverse[all]` in the foundation.  
Pin exact version only after allowlisted skills are smoke-tested.  
Initial skill candidates (when approved): chemical compound retrieval, literature deep research, drug research, adverse-event detection, pharmacovigilance, systems biology, target research.

### D14. Foreman / Warp

Foreman CLI unavailable in this environment.  
Implementation continues with human approval for sensitive actions and a manual session audit log.  
Foreman-line plugin methodology (parcel-driven) guides decomposition.

### D15. Warp / execution assumptions

Prefer small reviewable changes; ask before new dependencies, infrastructure changes, destructive commands, or public behavior changes. Repository remains buildable after each unit of work.

---

## GPU topology decision (explicit)

Workloads run through:

1. **Primary:** host Ollama as local inference runtime (GPU-backed when available)
2. **Secondary:** in-process CPU scientific transforms in sidecar
3. **Future:** separate GPU worker service if containerized CUDA workloads prove necessary and stable
4. **Not default:** heavyweight model load inside primary API process

---

## Consequences

### Positive

- Clear trust boundary between scientific tool sprawl and BioStack canon
- Local-first privacy preserved
- Existing review machinery reused
- CPU-only deployments remain first-class

### Negative / costs

- Two runtimes to operate ( .NET + Python )
- Contract versioning discipline required
- Ollama/Kompress optional services add failure modes (must be typed and observable)
- Guidance contract must reconcile legal/canon before public Class B/C UX

### Follow-ups

1. Guidance Content Contract v1 (Phase 1) — **blocking for public dosing-context**
2. Sidecar scaffold + health/kill-switch (Phase 2 foundation)
3. JSON Schema research contract (Phase 4)
4. ToolUniverse allowlist pin after approval
5. Benchmark harness for qwen3.5:9b
6. Kompress research-service profile (tenant/job isolation)
7. Deterministic evidence comparison service (Phase 8)

---

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| Pure .NET ToolUniverse bindings | Immature / high friction vs Python SDK |
| Call ToolUniverse from KnowledgeWorker in-process | Couples ingestion worker to large Python/CUDA surface |
| MCP as product API | Unstable domain contract; agent-oriented |
| Always-on hosted LLM extraction | Violates local-first and privacy posture |
| GPU-required architecture | Breaks CPU-only and portable deploys |
